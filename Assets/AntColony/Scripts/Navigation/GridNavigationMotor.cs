using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.Navigation
{
    public enum GridNavigationMotorState
    {
        Idle,
        WaitingForPath,
        FollowingPath,
        Arrived,
        Blocked,
        Falling,
        Recovering
    }

    /// <summary>
    /// Production movement executor for one ordinary ant. It accepts simple
    /// destination commands while the shared world service owns terrain data
    /// and path scheduling. Stable motion is kinematic; Rigidbody2D simulation
    /// is enabled only while Falling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class GridNavigationMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float baseMovementSpeed = 2f;
        [SerializeField, Min(0.01f)] private float arrivalTolerance = 0.03f;
        [SerializeField, Min(0.1f)] private float fallingGravityScale = 4f;
        [SerializeField, Min(0)] private int maximumAutomaticRetries = 2;

        private readonly List<GridNavigationAnchor> route = new();
        private Rigidbody2D body;
        private GridNavigationWorldService navigationWorld;
        private GridNavigationMap map;
        private GridNavigationScheduler scheduler;
        private AntMovementCapabilityEvaluator capability;
        private GridNavigationRequestHandle pendingRequest;
        private GridNavigationRequestPriority requestedPriority;
        private int routeIndex;
        private int plannedRevision;
        private int retryCount;
        private Vector2Int requestedDestination;
        private Vector2 lastValidPosition;
        private bool hasRequestedDestination;
        private bool requestAfterRecovery;

        public GridNavigationMotorState State { get; private set; } =
            GridNavigationMotorState.Idle;
        public string StatusMessage { get; private set; } = "Grid ant is idle.";
        public Vector2 Position => body != null
            ? body.position
            : (Vector2)transform.position;
        public IReadOnlyList<GridNavigationAnchor> Route => route;
        public int RouteIndex => routeIndex;
        public bool HasRoute => route.Count > 0 && routeIndex < route.Count;
        public bool HasPendingRequest => pendingRequest.IsValid;
        public long PendingRequestId => pendingRequest.RequestId;
        public bool HasRequestedDestination => hasRequestedDestination;
        public Vector2Int RequestedDestination => requestedDestination;
        public int RetryCount => retryCount;
        public int MaximumAutomaticRetries => maximumAutomaticRetries;
        public float BaseMovementSpeed => baseMovementSpeed;
        public int NavigationOwnerId => GetInstanceID();
        public GridNavigationWorldService NavigationWorld => navigationWorld;
        public bool HasArrived => State == GridNavigationMotorState.Arrived;
        public bool HasFailed => State == GridNavigationMotorState.Blocked;
        public bool IsMovementActive =>
            State == GridNavigationMotorState.WaitingForPath ||
            State == GridNavigationMotorState.FollowingPath ||
            State == GridNavigationMotorState.Falling ||
            State == GridNavigationMotorState.Recovering;
        public string FailureReason => HasFailed ? StatusMessage : string.Empty;

        /// <summary>
        /// Connects this ant to the active world's shared navigation authority.
        /// Spawners call this once after choosing a valid start anchor.
        /// </summary>
        public bool InitializeNavigation(
            GridNavigationWorldService sharedNavigationWorld,
            Vector2Int spawnCell)
        {
            body = GetComponent<Rigidbody2D>();
            CancelPendingRequest("Grid ant rebound.");
            navigationWorld = sharedNavigationWorld;
            map = navigationWorld != null ? navigationWorld.Map : null;
            scheduler = navigationWorld != null ? navigationWorld.Scheduler : null;
            capability = navigationWorld != null && navigationWorld.WorldData != null
                ? new AntMovementCapabilityEvaluator(navigationWorld.WorldData)
                : null;
            route.Clear();
            routeIndex = 0;
            retryCount = 0;
            hasRequestedDestination = false;
            requestAfterRecovery = false;

            if (map == null || scheduler == null ||
                !map.TryGetAnchor(spawnCell, out GridNavigationAnchor spawn))
            {
                State = GridNavigationMotorState.Blocked;
                StatusMessage = $"Grid spawn anchor {spawnCell} is invalid.";
                return false;
            }

            EnterStableTraversal();
            body.position = spawn.Position;
            transform.position = spawn.Position;
            lastValidPosition = spawn.Position;
            State = GridNavigationMotorState.Idle;
            StatusMessage = $"Grid ant is ready at {spawnCell}.";
            return true;
        }

        /// <summary>
        /// Production command boundary for future player orders and colony AI.
        /// Callers do not need to understand anchors, A* or Rigidbody2D states.
        /// </summary>
        public bool MoveTo(
            Vector2Int destination,
            GridNavigationRequestPriority priority =
                GridNavigationRequestPriority.Normal)
        {
            return RequestDestination(destination, priority);
        }

        /// <summary>Cancels movement and prevents stale path delivery.</summary>
        public void CancelMovement(string reason = "Movement cancelled.")
        {
            CancelNavigation(reason);
        }

        /// <summary>
        /// Replaces any earlier destination and queues bounded shared path work.
        /// A falling ant remembers the destination and submits after recovery.
        /// </summary>
        private bool RequestDestination(
            Vector2Int destination,
            GridNavigationRequestPriority priority =
                GridNavigationRequestPriority.Normal)
        {
            if (map == null || scheduler == null || body == null)
            {
                StatusMessage = "Grid destination rejected: navigation unavailable.";
                return false;
            }

            CancelPendingRequest("Destination replaced.");
            route.Clear();
            routeIndex = 0;
            requestedDestination = destination;
            requestedPriority = priority;
            hasRequestedDestination = true;
            retryCount = 0;

            if (State == GridNavigationMotorState.Falling ||
                State == GridNavigationMotorState.Recovering)
            {
                requestAfterRecovery = true;
                StatusMessage =
                    $"Destination {destination} stored until recovery completes.";
                return true;
            }

            return QueuePathRequest();
        }

        private void CancelNavigation(string reason = "Navigation cancelled.")
        {
            CancelPendingRequest(reason);
            route.Clear();
            routeIndex = 0;
            hasRequestedDestination = false;
            requestAfterRecovery = false;
            if (State != GridNavigationMotorState.Falling)
            {
                State = GridNavigationMotorState.Idle;
            }
            StatusMessage = reason;
        }

        private void OnDisable()
        {
            CancelPendingRequest("Grid ant disabled.");
        }

        public void NotifyTerrainChanged()
        {
            if (map == null || body == null ||
                State == GridNavigationMotorState.Falling)
            {
                return;
            }

            CancelPendingRequest("Terrain changed.");
            AntMovementCapabilityResult current = capability.Evaluate(
                body.position,
                0f,
                AntCollisionProfile.OrdinaryAnt);
            if (!current.Succeeded)
            {
                StopAtLastValidPosition(
                    $"Terrain invalidated the current pose: {current.Status}.");
                return;
            }

            if (current.ShouldApplyGravity)
            {
                BeginFalling("Terrain removed the ant's grip or support.");
                return;
            }

            if (hasRequestedDestination &&
                (HasRoute || State == GridNavigationMotorState.WaitingForPath ||
                 State == GridNavigationMotorState.FollowingPath))
            {
                BeginAnchorRecovery("Terrain changed; recentring before replanning.");
            }
        }

        /// <summary>Development fixture for testing the physics-only fall state.</summary>
        public void BeginFalling(string reason = "Falling fixture started.")
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            CancelPendingRequest("Falling interrupted path request.");
            route.Clear();
            routeIndex = 0;
            requestAfterRecovery = hasRequestedDestination;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = fallingGravityScale;
            body.freezeRotation = true;
            State = GridNavigationMotorState.Falling;
            StatusMessage = reason;
        }

        private void FixedUpdate()
        {
            if (body == null || map == null || scheduler == null)
            {
                return;
            }

            PollScheduledResult();
            if (State == GridNavigationMotorState.Falling)
            {
                TryRecoverFromFall();
                return;
            }

            if (State == GridNavigationMotorState.Recovering)
            {
                MoveTowardCurrentAnchor(baseMovementSpeed);
                return;
            }

            if (State != GridNavigationMotorState.FollowingPath || !HasRoute)
            {
                return;
            }

            if (plannedRevision != map.Revision)
            {
                BeginAnchorRecovery("Route revision became stale.");
                return;
            }

            GridNavigationAnchor previous = route[routeIndex - 1];
            GridNavigationAnchor next = route[routeIndex];
            float speedMultiplier =
                (previous.SpeedMultiplier + next.SpeedMultiplier) * 0.5f;
            MoveTowardCurrentAnchor(baseMovementSpeed * speedMultiplier);
        }

        private bool QueuePathRequest()
        {
            if (retryCount > maximumAutomaticRetries)
            {
                State = GridNavigationMotorState.Blocked;
                StatusMessage =
                    $"Grid route failed after {maximumAutomaticRetries} retries.";
                return false;
            }

            Vector2Int start =
                WorldTileCoordinates.WorldPositionToCell(body.position);
            pendingRequest = scheduler.RequestPath(
                NavigationOwnerId,
                start,
                requestedDestination,
                requestedPriority,
                8192,
                out bool created);
            State = GridNavigationMotorState.WaitingForPath;
            StatusMessage = created
                ? $"Waiting for grid route {pendingRequest.RequestId}."
                : $"Still waiting for grid route {pendingRequest.RequestId}.";
            return true;
        }

        private void PollScheduledResult()
        {
            if (!pendingRequest.IsValid ||
                !scheduler.TryTakeResult(
                    pendingRequest.RequestId,
                    out GridScheduledPathResult scheduled))
            {
                return;
            }

            long expectedRequest = pendingRequest.RequestId;
            pendingRequest = default;
            if (scheduled.RequestId != expectedRequest ||
                scheduled.Destination != requestedDestination)
            {
                return;
            }

            if (scheduled.Succeeded && scheduled.Path != null)
            {
                ApplyRoute(scheduled.Path);
                return;
            }

            if (scheduled.Status == GridNavigationRequestStatus.Cancelled &&
                hasRequestedDestination &&
                retryCount < maximumAutomaticRetries)
            {
                retryCount++;
                QueuePathRequest();
                return;
            }

            route.Clear();
            routeIndex = 0;
            State = GridNavigationMotorState.Blocked;
            StatusMessage = scheduled.Status ==
                GridNavigationRequestStatus.SearchLimitReached
                    ? "Grid route stopped at the bounded search limit."
                    : $"Grid route failed: {scheduled.Status}.";
        }

        private void ApplyRoute(GridPathResult result)
        {
            route.Clear();
            route.AddRange(result.Anchors);
            routeIndex = Mathf.Min(1, route.Count);
            plannedRevision = map.Revision;
            retryCount = 0;
            EnterStableTraversal();
            State = route.Count <= 1
                ? GridNavigationMotorState.Arrived
                : GridNavigationMotorState.FollowingPath;
            StatusMessage =
                $"Following {route.Count} anchors; cost " +
                $"{result.TravelTime:0.###}, examined {result.ExaminedNodeCount}.";
        }

        private void MoveTowardCurrentAnchor(float speed)
        {
            if (!HasRoute)
            {
                return;
            }

            Vector2 target = route[routeIndex].Position;
            Vector2 next = Vector2.MoveTowards(
                body.position,
                target,
                speed * Time.fixedDeltaTime);
            body.position = next;
            transform.position = next;
            lastValidPosition = next;
            if (Vector2.Distance(next, target) > arrivalTolerance)
            {
                return;
            }

            body.position = target;
            transform.position = target;
            lastValidPosition = target;
            routeIndex++;
            if (routeIndex < route.Count)
            {
                return;
            }

            bool recovered = State == GridNavigationMotorState.Recovering;
            route.Clear();
            routeIndex = 0;
            if (recovered && requestAfterRecovery && hasRequestedDestination &&
                WorldTileCoordinates.WorldPositionToCell(body.position) !=
                    requestedDestination)
            {
                requestAfterRecovery = false;
                retryCount++;
                QueuePathRequest();
                return;
            }

            State = recovered
                ? GridNavigationMotorState.Idle
                : GridNavigationMotorState.Arrived;
            StatusMessage = recovered
                ? "Grid ant recovered onto a valid anchor."
                : $"Grid ant arrived at {requestedDestination}.";
        }

        private void TryRecoverFromFall()
        {
            AntMovementCapabilityResult result = capability.Evaluate(
                body.position,
                0f,
                AntCollisionProfile.OrdinaryAnt);
            bool stableGroundContact = result.SuggestedState ==
                    AntMovementEligibilityState.Grounded &&
                Mathf.Abs(body.linearVelocity.y) <= 0.1f;
            bool acquiredGrip = result.SuggestedState ==
                AntMovementEligibilityState.Gripping;
            if (!result.Succeeded || (!stableGroundContact && !acquiredGrip))
            {
                return;
            }

            Vector2Int cell =
                WorldTileCoordinates.WorldPositionToCell(body.position);
            if (!map.TryGetAnchor(cell, out GridNavigationAnchor anchor))
            {
                return;
            }

            EnterStableTraversal();
            route.Clear();
            route.Add(anchor);
            routeIndex = 0;
            State = GridNavigationMotorState.Recovering;
            lastValidPosition = body.position;
            StatusMessage = $"Grid ant is recovering to anchor {cell}.";
        }

        private void BeginAnchorRecovery(string reason)
        {
            CancelPendingRequest(reason);
            route.Clear();
            routeIndex = 0;
            Vector2Int cell =
                WorldTileCoordinates.WorldPositionToCell(body.position);
            if (!map.TryGetAnchor(cell, out GridNavigationAnchor anchor))
            {
                StopAtLastValidPosition(
                    $"No recovery anchor exists at {cell}.");
                return;
            }

            EnterStableTraversal();
            route.Add(anchor);
            requestAfterRecovery = hasRequestedDestination;
            State = GridNavigationMotorState.Recovering;
            StatusMessage = reason;
        }

        private void StopAtLastValidPosition(string reason)
        {
            route.Clear();
            routeIndex = 0;
            EnterStableTraversal();
            body.position = lastValidPosition;
            transform.position = lastValidPosition;
            State = GridNavigationMotorState.Blocked;
            StatusMessage = reason;
        }

        private void CancelPendingRequest(string reason)
        {
            if (pendingRequest.IsValid && scheduler != null)
            {
                scheduler.Cancel(pendingRequest.RequestId, reason);
                scheduler.TryTakeResult(pendingRequest.RequestId, out _);
            }

            pendingRequest = default;
        }

        private void EnterStableTraversal()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.simulated = false;
        }
    }
}
