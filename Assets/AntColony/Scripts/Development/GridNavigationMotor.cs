using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.Development
{
    public enum GridNavigationMotorState
    {
        Idle,
        Traversing,
        Arrived,
        Blocked,
        Falling,
        Recovering
    }

    /// <summary>
    /// Experimental ordinary-ant executor. Stable traversal is kinematic and
    /// follows shared grid anchors smoothly; Rigidbody2D gravity is enabled only
    /// while Falling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class GridNavigationMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float baseMovementSpeed = 2f;
        [SerializeField, Min(0.01f)] private float arrivalTolerance = 0.03f;
        [SerializeField, Min(0.1f)] private float fallingGravityScale = 4f;

        private readonly List<GridNavigationAnchor> route = new();
        private Rigidbody2D body;
        private GridNavigationMap map;
        private GridPathfinder pathfinder;
        private AntMovementCapabilityEvaluator capability;
        private int routeIndex;
        private int plannedRevision;
        private Vector2Int requestedDestination;

        public GridNavigationMotorState State { get; private set; } =
            GridNavigationMotorState.Idle;
        public string StatusMessage { get; private set; } = "Grid ant is idle.";
        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public IReadOnlyList<GridNavigationAnchor> Route => route;
        public int RouteIndex => routeIndex;
        public bool HasRoute => route.Count > 0 && routeIndex < route.Count;
        public float BaseMovementSpeed => baseMovementSpeed;

        public void Bind(
            ChunkedWorldData worldData,
            GridNavigationMap sharedMap,
            Vector2Int spawnCell)
        {
            body = GetComponent<Rigidbody2D>();
            map = sharedMap;
            pathfinder = new GridPathfinder(map);
            capability = new AntMovementCapabilityEvaluator(worldData);
            route.Clear();
            routeIndex = 0;

            if (!map.TryGetAnchor(spawnCell, out GridNavigationAnchor spawn))
            {
                State = GridNavigationMotorState.Blocked;
                StatusMessage = $"Grid spawn anchor {spawnCell} is invalid.";
                return;
            }

            EnterStableTraversal();
            body.position = spawn.Position;
            transform.position = spawn.Position;
            State = GridNavigationMotorState.Idle;
            StatusMessage = $"Grid ant is ready at {spawnCell}.";
        }

        public GridPathResult TrySetDestination(Vector2Int destination)
        {
            if (map == null || body == null || State == GridNavigationMotorState.Falling)
            {
                StatusMessage = "Grid destination rejected while navigation is unavailable.";
                return new GridPathResult(GridPathStatus.StartInvalid, null, 0f, 0);
            }

            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(body.position);
            GridPathResult result = pathfinder.FindPath(start, destination);
            if (!result.Succeeded)
            {
                route.Clear();
                State = GridNavigationMotorState.Blocked;
                StatusMessage = $"Grid route rejected: {result.Status}.";
                return result;
            }

            route.Clear();
            route.AddRange(result.Anchors);
            routeIndex = Mathf.Min(1, route.Count);
            requestedDestination = destination;
            plannedRevision = map.Revision;
            EnterStableTraversal();
            State = route.Count <= 1
                ? GridNavigationMotorState.Arrived
                : GridNavigationMotorState.Traversing;
            StatusMessage =
                $"Grid route accepted: {route.Count} anchors, " +
                $"cost {result.TravelTime:0.###}, examined {result.ExaminedNodeCount}.";
            return result;
        }

        public void NotifyTerrainChanged()
        {
            if (map == null || body == null || State == GridNavigationMotorState.Falling)
            {
                return;
            }

            AntMovementCapabilityResult current = capability.Evaluate(
                body.position, 0f, AntCollisionProfile.OrdinaryAnt);
            if (!current.Succeeded)
            {
                route.Clear();
                State = GridNavigationMotorState.Blocked;
                StatusMessage =
                    $"Terrain invalidated the current grid pose: {current.Status}.";
                return;
            }

            if (current.ShouldApplyGravity)
            {
                BeginFalling("Terrain removed the ant's grip or support.");
                return;
            }

            if (HasRoute && plannedRevision != map.Revision)
            {
                TrySetDestination(requestedDestination);
            }
        }

        /// <summary>Development fixture for testing the physics-only fall state.</summary>
        public void BeginFalling(string reason = "Falling fixture started.")
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            route.Clear();
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = fallingGravityScale;
            body.freezeRotation = true;
            State = GridNavigationMotorState.Falling;
            StatusMessage = reason;
        }

        private void FixedUpdate()
        {
            if (body == null || map == null)
            {
                return;
            }

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

            if (State != GridNavigationMotorState.Traversing || !HasRoute)
            {
                return;
            }

            GridNavigationAnchor previous = route[routeIndex - 1];
            GridNavigationAnchor next = route[routeIndex];
            float speedMultiplier =
                (previous.SpeedMultiplier + next.SpeedMultiplier) * 0.5f;
            MoveTowardCurrentAnchor(baseMovementSpeed * speedMultiplier);
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
            if (Vector2.Distance(next, target) > arrivalTolerance)
            {
                return;
            }

            body.position = target;
            transform.position = target;
            routeIndex++;
            if (routeIndex < route.Count)
            {
                return;
            }

            route.Clear();
            routeIndex = 0;
            State = State == GridNavigationMotorState.Recovering
                ? GridNavigationMotorState.Idle
                : GridNavigationMotorState.Arrived;
            StatusMessage = State == GridNavigationMotorState.Arrived
                ? $"Grid ant arrived at {requestedDestination}."
                : "Grid ant recovered onto a valid anchor.";
        }

        private void TryRecoverFromFall()
        {
            AntMovementCapabilityResult result = capability.Evaluate(
                body.position, 0f, AntCollisionProfile.OrdinaryAnt);
            if (!result.Succeeded ||
                (result.SuggestedState != AntMovementEligibilityState.Grounded &&
                 result.SuggestedState != AntMovementEligibilityState.Gripping) ||
                body.linearVelocity.y > 0.05f)
            {
                return;
            }

            Vector2Int cell = WorldTileCoordinates.WorldPositionToCell(body.position);
            if (!map.TryGetAnchor(cell, out GridNavigationAnchor anchor))
            {
                return;
            }

            EnterStableTraversal();
            route.Clear();
            route.Add(anchor);
            routeIndex = 0;
            State = GridNavigationMotorState.Recovering;
            StatusMessage = $"Grid ant is recovering to anchor {cell}.";
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
