using System;
using System.Collections.Generic;

namespace AntColony.World
{
    public enum SurfacePathStatus { Success, InvalidStart, InvalidDestination, NoRoute }

    public sealed class SurfacePathResult
    {
        public SurfacePathResult(SurfacePathStatus status,
            IReadOnlyList<SurfaceCell> cells, int visitedCells)
        { Status = status; Cells = cells; VisitedCells = visitedCells; }
        public SurfacePathStatus Status { get; }
        public IReadOnlyList<SurfaceCell> Cells { get; }
        public int VisitedCells { get; }
        public bool Succeeded => Status == SurfacePathStatus.Success;
    }

    /// <summary>Bounded deterministic A* for top-down Surface movement.</summary>
    public sealed class SurfacePathfinder
    {
        private static readonly (int x, int y)[] Directions =
        {
            (0, 1), (1, 0), (0, -1), (-1, 0),
            (1, 1), (1, -1), (-1, -1), (-1, 1)
        };

        public SurfacePathResult FindPath(SurfaceWorldData world,
            SurfaceCell start, SurfaceCell destination)
        {
            if (world == null || !world.IsTraversable(start))
                return Result(SurfacePathStatus.InvalidStart);
            if (!world.IsTraversable(destination))
                return Result(SurfacePathStatus.InvalidDestination);
            if (start.Equals(destination))
                return new SurfacePathResult(SurfacePathStatus.Success,
                    new[] { start }, 1);

            var open = new List<SurfaceCell> { start };
            var closed = new HashSet<SurfaceCell>();
            var cameFrom = new Dictionary<SurfaceCell, SurfaceCell>();
            var costs = new Dictionary<SurfaceCell, int> { [start] = 0 };
            int maximumVisits = world.Width * world.Height;

            while (open.Count > 0 && closed.Count < maximumVisits)
            {
                int bestIndex = FindBest(open, costs, destination);
                SurfaceCell current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (!closed.Add(current)) continue;
                if (current.Equals(destination))
                    return new SurfacePathResult(SurfacePathStatus.Success,
                        Reconstruct(cameFrom, current), closed.Count);

                foreach ((int x, int y) direction in Directions)
                {
                    var next = new SurfaceCell(current.X + direction.x,
                        current.Y + direction.y);
                    if (!world.IsTraversable(next) || closed.Contains(next)) continue;
                    bool diagonal = direction.x != 0 && direction.y != 0;
                    if (diagonal && (!world.IsTraversable(new SurfaceCell(
                            current.X + direction.x, current.Y)) ||
                        !world.IsTraversable(new SurfaceCell(
                            current.X, current.Y + direction.y)))) continue;

                    int tentative = costs[current] + (diagonal ? 14 : 10);
                    if (costs.TryGetValue(next, out int known) && tentative >= known)
                        continue;
                    costs[next] = tentative;
                    cameFrom[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return new SurfacePathResult(SurfacePathStatus.NoRoute,
                Array.Empty<SurfaceCell>(), closed.Count);
        }

        private static int FindBest(List<SurfaceCell> open,
            Dictionary<SurfaceCell, int> costs, SurfaceCell goal)
        {
            int best = 0;
            int bestScore = int.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                SurfaceCell cell = open[i];
                int dx = Math.Abs(goal.X - cell.X);
                int dy = Math.Abs(goal.Y - cell.Y);
                int heuristic = 14 * Math.Min(dx, dy) + 10 * Math.Abs(dx - dy);
                int score = costs[cell] + heuristic;
                if (score < bestScore || score == bestScore &&
                    (cell.Y < open[best].Y || cell.Y == open[best].Y &&
                     cell.X < open[best].X))
                { best = i; bestScore = score; }
            }
            return best;
        }

        private static IReadOnlyList<SurfaceCell> Reconstruct(
            Dictionary<SurfaceCell, SurfaceCell> cameFrom, SurfaceCell current)
        {
            var route = new List<SurfaceCell> { current };
            while (cameFrom.TryGetValue(current, out SurfaceCell previous))
            { current = previous; route.Add(current); }
            route.Reverse();
            return route;
        }

        private static SurfacePathResult Result(SurfacePathStatus status) =>
            new(status, Array.Empty<SurfaceCell>(), 0);
    }
}
