using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.Navigation
{
    public enum SurfaceNavigationState { Idle, Moving, Arrived, Blocked }

    /// <summary>
    /// Kinematic top-down Surface movement. It uses Surface cells only and the
    /// existing shared simulation clock, with no Rigidbody gravity behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceNavigationMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float movementSpeed = 3f;
        [SerializeField, Min(0.001f)] private float arrivalTolerance = 0.02f;
        private readonly List<SurfaceCell> route = new();
        private SurfaceWorldData world;
        private ISimulationClock clock;
        private Vector2 worldOrigin;
        private int routeIndex;

        public SurfaceNavigationState State { get; private set; } =
            SurfaceNavigationState.Idle;
        public string StatusMessage { get; private set; } = "Surface ant is idle.";
        public SurfaceCell CurrentCell { get; private set; }
        public SurfaceCell Destination { get; private set; }
        public IReadOnlyList<SurfaceCell> Route => route;
        public bool HasArrived => State == SurfaceNavigationState.Arrived;
        public bool IsMovementActive => State == SurfaceNavigationState.Moving;
        public float MovementSpeed => movementSpeed;

        public bool Initialize(SurfaceWorldData surfaceWorld,
            ISimulationClock simulationClock, Vector2 origin, SurfaceCell start)
        {
            world = surfaceWorld; clock = simulationClock; worldOrigin = origin;
            route.Clear(); routeIndex = 0;
            if (world == null || !world.IsTraversable(start))
            { State = SurfaceNavigationState.Blocked; StatusMessage =
                $"Surface start {start} is invalid."; return false; }
            WarpTo(start);
            State = SurfaceNavigationState.Idle;
            StatusMessage = $"Surface ant is ready at {start}.";
            return true;
        }

        public bool MoveTo(SurfaceCell destination)
        {
            if (world == null)
            { State = SurfaceNavigationState.Blocked; StatusMessage =
                "Surface navigation is unavailable."; return false; }
            SurfacePathResult result = new SurfacePathfinder().FindPath(
                world, CurrentCell, destination);
            if (!result.Succeeded)
            { State = SurfaceNavigationState.Blocked; StatusMessage =
                $"Surface destination {destination} rejected: {result.Status}.";
              return false; }
            route.Clear(); route.AddRange(result.Cells); routeIndex =
                Mathf.Min(1, route.Count);
            Destination = destination;
            State = routeIndex >= route.Count
                ? SurfaceNavigationState.Arrived : SurfaceNavigationState.Moving;
            StatusMessage = State == SurfaceNavigationState.Arrived
                ? $"Already at {destination}." : $"Moving to {destination}.";
            return true;
        }

        public void CancelMovement(string reason = "Surface movement cancelled.")
        { route.Clear(); routeIndex = 0; State = SurfaceNavigationState.Idle;
          StatusMessage = reason; }

        public void WarpTo(SurfaceCell cell)
        {
            CurrentCell = cell;
            transform.position = CellToWorld(cell);
            route.Clear(); routeIndex = 0;
        }

        public Vector2 CellToWorld(SurfaceCell cell) =>
            worldOrigin + new Vector2(cell.X + 0.5f, cell.Y + 0.5f);

        public bool TryWorldToCell(Vector2 position, out SurfaceCell cell)
        {
            cell = new SurfaceCell(Mathf.FloorToInt(position.x - worldOrigin.x),
                Mathf.FloorToInt(position.y - worldOrigin.y));
            return world != null && world.Contains(cell);
        }

        private void Update()
        { Tick(clock != null ? clock.DeltaSeconds : 0d); }

        /// <summary>Advances one shared-clock step; public for deterministic tests.</summary>
        public void Tick(double simulationDeltaSeconds)
        {
            if (State != SurfaceNavigationState.Moving ||
                simulationDeltaSeconds <= 0d || routeIndex >= route.Count) return;
            Vector2 target = CellToWorld(route[routeIndex]);
            float distance = movementSpeed * (float)simulationDeltaSeconds;
            transform.position = Vector2.MoveTowards(transform.position, target, distance);
            if (Vector2.Distance(transform.position, target) > arrivalTolerance) return;
            transform.position = target;
            CurrentCell = route[routeIndex++];
            if (routeIndex < route.Count) return;
            State = SurfaceNavigationState.Arrived;
            StatusMessage = $"Arrived at Surface destination {Destination}.";
        }
    }
}
