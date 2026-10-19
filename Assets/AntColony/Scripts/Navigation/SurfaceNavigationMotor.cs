using System;
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
        private Vector2 logicalPosition;
        private Func<Vector2, Vector2> projectLogicalPosition;
        private Func<Vector2, SurfaceCell?> resolvePresentationPosition;
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
        public Vector2 LogicalPosition => logicalPosition;

        public bool Initialize(SurfaceWorldData surfaceWorld,
            ISimulationClock simulationClock, Vector2 origin, SurfaceCell start)
        {
            world = surfaceWorld; clock = simulationClock; worldOrigin = origin;
            RestoreLegacyPresentation();
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
            logicalPosition = new Vector2(cell.X, cell.Y);
            RefreshTransform();
            route.Clear(); routeIndex = 0;
        }

        public Vector2 CellToWorld(SurfaceCell cell) =>
            projectLogicalPosition != null
                ? projectLogicalPosition(new Vector2(cell.X, cell.Y))
                : LegacyCellToWorld(new Vector2(cell.X, cell.Y));

        /// <summary>
        /// Changes presentation conversion only. Route cells, progress, path
        /// status and arrival authority remain owned by this motor.
        /// </summary>
        public void ConfigurePresentation(
            Func<Vector2, Vector2> logicalToWorld,
            Func<Vector2, SurfaceCell?> worldToLogicalCell)
        {
            projectLogicalPosition = logicalToWorld ?? LegacyCellToWorld;
            resolvePresentationPosition = worldToLogicalCell ?? ResolveLegacyCell;
            // Colony residents keep this component disabled on the same
            // authoritative ant root. Updating its Surface presentation must
            // not move that root out from under the active Colony motor.
            if (isActiveAndEnabled) RefreshTransform();
        }

        public void RestoreLegacyPresentation()
        {
            projectLogicalPosition = LegacyCellToWorld;
            resolvePresentationPosition = ResolveLegacyCell;
            if (isActiveAndEnabled) RefreshTransform();
        }

        public bool TryWorldToCell(Vector2 position, out SurfaceCell cell)
        {
            SurfaceCell? resolved = resolvePresentationPosition?.Invoke(position);
            cell = resolved ?? default;
            return resolved.HasValue && world != null && world.Contains(cell);
        }

        private void Update()
        { Tick(clock != null ? clock.DeltaSeconds : 0d); }

        /// <summary>Advances one shared-clock step; public for deterministic tests.</summary>
        public void Tick(double simulationDeltaSeconds)
        {
            if (State != SurfaceNavigationState.Moving ||
                simulationDeltaSeconds <= 0d || routeIndex >= route.Count) return;
            SurfaceCell targetCell = route[routeIndex];
            Vector2 target = new(targetCell.X, targetCell.Y);
            float distance = movementSpeed * (float)simulationDeltaSeconds;
            logicalPosition = Vector2.MoveTowards(logicalPosition, target, distance);
            RefreshTransform();
            if (Vector2.Distance(logicalPosition, target) > arrivalTolerance) return;
            logicalPosition = target;
            RefreshTransform();
            CurrentCell = route[routeIndex++];
            if (routeIndex < route.Count) return;
            State = SurfaceNavigationState.Arrived;
            StatusMessage = $"Arrived at Surface destination {Destination}.";
        }

        private Vector2 LegacyCellToWorld(Vector2 logical) =>
            worldOrigin + logical + new Vector2(0.5f, 0.5f);

        private SurfaceCell? ResolveLegacyCell(Vector2 position)
        {
            var cell = new SurfaceCell(
                Mathf.FloorToInt(position.x - worldOrigin.x),
                Mathf.FloorToInt(position.y - worldOrigin.y));
            return world != null && world.Contains(cell) ? cell : null;
        }

        private void RefreshTransform()
        {
            if (projectLogicalPosition == null) return;
            transform.position = projectLogicalPosition(logicalPosition);
        }
    }
}
