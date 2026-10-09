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

    /// <summary>
    /// Moves one development ant along a prevalidated horizontal ground corridor.
    /// It deliberately owns no task selection, pathfinding, climbing or falling AI.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class AntAutonomousMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D antBody;
        [SerializeField] private SpriteRenderer antSprite;
        [SerializeField, Min(0.1f)] private float movementSpeed = 2f;
        [SerializeField, Min(0.01f)] private float arrivalTolerance = 0.08f;

        private DirectGroundRouteValidator routeValidator;
        private Vector2 destination;
        private bool hasDestination;

        public AntAutonomousMovementState State { get; private set; } =
            AntAutonomousMovementState.Idle;
        public string StatusMessage { get; private set; } = "Idle.";
        public Vector2 Position => antBody != null
            ? antBody.position
            : (Vector2)transform.position;
        public Vector2 Destination => destination;
        public bool HasDestination => hasDestination;
        public bool IsMoving => State == AntAutonomousMovementState.Moving;
        public float MovementSpeed => movementSpeed;
        public float ArrivalTolerance => arrivalTolerance;

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
            CancelDestination("Idle; world data refreshed.");
        }

        /// <summary>
        /// Accepts only a destination on the currently valid direct ground corridor.
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
            if (!IsMoving || routeValidator == null || antBody == null ||
                fixedDeltaTime <= 0f)
            {
                return;
            }

            Vector2 current = antBody.position;
            float remaining = destination.x - current.x;
            if (Mathf.Abs(remaining) <= arrivalTolerance)
            {
                antBody.linearVelocity = Vector2.zero;
                hasDestination = true;
                State = AntAutonomousMovementState.Arrived;
                StatusMessage = $"Arrived at {destination}.";
                return;
            }

            float nextX = Mathf.MoveTowards(
                current.x,
                destination.x,
                movementSpeed * fixedDeltaTime);
            if (Mathf.Abs(destination.x - nextX) <= arrivalTolerance)
            {
                nextX = destination.x;
            }

            var nextPosition = new Vector2(nextX, destination.y);
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
                antSprite.flipX = remaining < 0f;
            }

            // MovePosition keeps Unity collision resolution authoritative. The
            // motor never writes Transform.position or disables terrain collision.
            antBody.MovePosition(nextPosition);
        }

        /// <summary>Stops an active command without moving the Rigidbody.</summary>
        public void CancelDestination(string reason = "Destination cancelled.")
        {
            if (antBody != null)
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
            if (antBody != null)
            {
                antBody.linearVelocity = Vector2.zero;
            }

            hasDestination = false;
            State = AntAutonomousMovementState.Blocked;
            StatusMessage = reason;
        }

        private static string FormatStatus(DirectGroundRouteStatus status)
        {
            return status switch
            {
                DirectGroundRouteStatus.RouteTooLong => "the direct route is too long",
                DirectGroundRouteStatus.HeightChangeUnsupported =>
                    "height changes are not supported in this milestone",
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
