using AntColony.World;
using UnityEngine;

namespace AntColony.Development
{
    /// <summary>Readable states for the first bounded autonomous-ant prototype.</summary>
    public enum AntAutonomousMovementState
    {
        Idle,
        Moving,
        Arrived,
        Blocked
    }

    /// <summary>Physical locomotion is independent from command progress.</summary>
    public enum AntAutonomousLocomotionState
    {
        Grounded,
        Gripping,
        Falling
    }

    /// <summary>
    /// Moves one development ant along a prevalidated direct controlled segment.
    /// Rigidbody gravity alone moves the ant while Falling; controlled movement
    /// never runs in that same physics step.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class AntAutonomousMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D antBody;
        [SerializeField] private SpriteRenderer antSprite;
        [SerializeField, Min(0.1f)] private float movementSpeed = 2f;
        [SerializeField, Min(0.01f)] private float arrivalTolerance = 0.08f;

        private DirectGroundRouteValidator routeValidator;
        private AntMovementCapabilityEvaluator capabilityEvaluator;
        private Vector2 destination;
        private bool hasDestination;
        private float fallingGravityScale = 4f;
        private AntMovementCapabilityResult latestCapability;

        public AntAutonomousMovementState State { get; private set; } =
            AntAutonomousMovementState.Idle;
        public string StatusMessage { get; private set; } = "Idle.";
        public AntAutonomousLocomotionState LocomotionState { get; private set; } =
            AntAutonomousLocomotionState.Falling;
        public Vector2 Position => antBody != null
            ? antBody.position
            : (Vector2)transform.position;
        public Vector2 Destination => destination;
        public bool HasDestination => hasDestination;
        public bool IsMoving => State == AntAutonomousMovementState.Moving;
        public float MovementSpeed => movementSpeed;
        public float ArrivalTolerance => arrivalTolerance;
        public string BackgroundMaterialId =>
            latestCapability.BackgroundMaterialId ?? string.Empty;
        public bool HasBackgroundGrip => latestCapability.HasBackgroundGrip;
        public bool ShouldApplyGravity => latestCapability.ShouldApplyGravity;
        public AntMovementCapabilityResult LatestCapability => latestCapability;

        private void Awake()
        {
            if (antBody == null)
            {
                antBody = GetComponent<Rigidbody2D>();
            }

            if (antSprite == null)
            {
                antSprite = GetComponent<SpriteRenderer>();
            }
            if (antBody != null && antBody.gravityScale > 0f)
            {
                fallingGravityScale = antBody.gravityScale;
            }
        }

        private void FixedUpdate()
        {
            StepMovement(Time.fixedDeltaTime);
        }

        /// <summary>Supplies the current authoritative world after generation.</summary>
        public void BindWorldData(ChunkedWorldData worldData)
        {
            routeValidator = worldData != null
                ? new DirectGroundRouteValidator(
                    worldData,
                    AntCollisionProfile.OrdinaryAnt)
                : null;
            capabilityEvaluator = worldData != null
                ? new AntMovementCapabilityEvaluator(worldData)
                : null;
            CancelDestination("Idle; world data refreshed.");
            RefreshLocomotionState();
        }

        /// <summary>
        /// Accepts only a destination on the currently valid direct controlled segment.
        /// </summary>
        public DirectGroundRouteResult TrySetDestination(Vector2 requestedDestination)
        {
            if (routeValidator == null)
            {
                var unavailable = new DirectGroundRouteResult(
                    DirectGroundRouteStatus.UnavailableWorldData,
                    requestedDestination,
                    WorldTileCoordinates.WorldPositionToCell(requestedDestination),
                    0);
                StopBlocked("Destination rejected: world data is unavailable.");
                return unavailable;
            }

            DirectGroundRouteResult result = routeValidator.Validate(
                antBody.position,
                requestedDestination);
            if (!result.Succeeded)
            {
                StopBlocked($"Destination rejected: {FormatStatus(result.Status)}.");
                return result;
            }

            destination = requestedDestination;
            hasDestination = true;
            State = AntAutonomousMovementState.Moving;
            StatusMessage = $"Moving to {destination}.";
            return result;
        }

        /// <summary>
        /// Runs one physics-sized movement decision. FixedUpdate is the normal
        /// caller; focused tests may call it before Physics2D simulation.
        /// </summary>
        public void StepMovement(float fixedDeltaTime)
        {
            if (antBody == null || fixedDeltaTime <= 0f)
            {
                return;
            }
            RefreshLocomotionState();
            if (LocomotionState == AntAutonomousLocomotionState.Falling)
            {
                if (IsMoving)
                {
                    StopBlocked(
                        "Movement interrupted: support and grip were lost; falling.");
                }
                return;
            }

            if (!IsMoving || routeValidator == null)
            {
                return;
            }

            Vector2 current = antBody.position;
            Vector2 remaining = destination - current;
            if (remaining.magnitude <= arrivalTolerance)
            {
                antBody.linearVelocity = Vector2.zero;
                hasDestination = true;
                State = AntAutonomousMovementState.Arrived;
                StatusMessage = $"Arrived at {destination}.";
                return;
            }

            Vector2 nextPosition = Vector2.MoveTowards(
                current,
                destination,
                movementSpeed * fixedDeltaTime);
            if (Vector2.Distance(destination, nextPosition) <= arrivalTolerance)
            {
                nextPosition = destination;
            }
            DirectGroundRouteResult immediateRoute = routeValidator.Validate(
                current,
                nextPosition);
            if (!immediateRoute.Succeeded)
            {
                StopBlocked(
                    $"Movement stopped: {FormatStatus(immediateRoute.Status)}.");
                return;
            }

            if (antSprite != null)
            {
                if (Mathf.Abs(remaining.x) > AntClearanceValidator.GeometryTolerance)
                {
                    antSprite.flipX = remaining.x < 0f;
                }
            }

            // MovePosition keeps Unity collision resolution authoritative. The
            // motor never writes Transform.position or disables terrain collision.
            antBody.MovePosition(nextPosition);
        }

        /// <summary>
        /// Requeries local authoritative data after a developer terrain mutation.
        /// The next FixedUpdate still owns any actual movement.
        /// </summary>
        public void NotifyWorldChanged()
        {
            RefreshLocomotionState();
            if (IsMoving && routeValidator != null)
            {
                DirectGroundRouteResult route = routeValidator.Validate(
                    Position,
                    destination);
                if (!route.Succeeded)
                {
                    StopBlocked($"Movement interrupted: {FormatStatus(route.Status)}.");
                }
            }
        }

        /// <summary>Stops an active command without moving the Rigidbody.</summary>
        public void CancelDestination(string reason = "Destination cancelled.")
        {
            if (antBody != null &&
                LocomotionState != AntAutonomousLocomotionState.Falling)
            {
                antBody.linearVelocity = Vector2.zero;
            }

            hasDestination = false;
            State = AntAutonomousMovementState.Idle;
            StatusMessage = reason;
        }

        /// <summary>Used by the authored setup utility and focused tests.</summary>
        public void Configure(
            Rigidbody2D body,
            SpriteRenderer sprite,
            float speed,
            float tolerance)
        {
            antBody = body;
            antSprite = sprite;
            movementSpeed = Mathf.Max(0.1f, speed);
            arrivalTolerance = Mathf.Max(0.01f, tolerance);
        }

        private void StopBlocked(string reason)
        {
            if (antBody != null &&
                LocomotionState != AntAutonomousLocomotionState.Falling)
            {
                antBody.linearVelocity = Vector2.zero;
            }

            hasDestination = false;
            State = AntAutonomousMovementState.Blocked;
            StatusMessage = reason;
        }

        private void RefreshLocomotionState()
        {
            if (capabilityEvaluator == null || antBody == null)
            {
                return;
            }

            latestCapability = capabilityEvaluator.Evaluate(
                antBody.position,
                0f,
                AntCollisionProfile.OrdinaryAnt);
            if (!latestCapability.Succeeded)
            {
                if (IsMoving)
                {
                    StopBlocked(
                        $"Movement interrupted: capability is {latestCapability.Status}.");
                }
                return;
            }

            AntAutonomousLocomotionState nextState =
                latestCapability.SuggestedState switch
                {
                    AntMovementEligibilityState.Grounded =>
                        AntAutonomousLocomotionState.Grounded,
                    AntMovementEligibilityState.Gripping =>
                        AntAutonomousLocomotionState.Gripping,
                    _ => AntAutonomousLocomotionState.Falling
                };

            bool stateChanged = nextState != LocomotionState;
            LocomotionState = nextState;
            if (nextState == AntAutonomousLocomotionState.Falling)
            {
                antBody.gravityScale = latestCapability.ShouldApplyGravity
                    ? fallingGravityScale
                    : 0f;
                if (stateChanged)
                {
                    // A stale horizontal command must not carry the ant over a gap.
                    antBody.linearVelocity = new Vector2(0f, antBody.linearVelocity.y);
                }
                return;
            }

            antBody.gravityScale = 0f;
            if (stateChanged || Mathf.Abs(antBody.linearVelocity.y) > 0.001f)
            {
                // Entering Grounded or Gripping ends gravity-driven motion before
                // a controlled MovePosition can run in this physics step.
                antBody.linearVelocity = Vector2.zero;
            }
        }

        private static string FormatStatus(DirectGroundRouteStatus status)
        {
            return status switch
            {
                DirectGroundRouteStatus.RouteTooLong => "the direct route is too long",
                DirectGroundRouteStatus.DirectionNotPermitted =>
                    "the Background does not permit that direction",
                DirectGroundRouteStatus.DiagonalCornerBlocked =>
                    "the diagonal would cut a blocked corner",
                DirectGroundRouteStatus.StartBlocked => "the ant is blocked",
                DirectGroundRouteStatus.StartUnsupported => "the ant is unsupported",
                DirectGroundRouteStatus.DestinationBlocked =>
                    "the destination is blocked",
                DirectGroundRouteStatus.DestinationUnsupported =>
                    "the destination is unsupported",
                DirectGroundRouteStatus.RouteBlocked => "terrain blocks the direct route",
                DirectGroundRouteStatus.RouteUnsupported =>
                    "the direct route crosses unsupported space",
                DirectGroundRouteStatus.UnavailableWorldData =>
                    "the route leaves resident world data",
                _ => "the request is invalid"
            };
        }
    }
}
