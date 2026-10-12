using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public enum GridPathStatus
    {
        Success,
        StartInvalid,
        DestinationInvalid,
        Unreachable,
        SearchLimitReached,
        TerrainChanged
    }

    public sealed class GridPathResult
    {
        public GridPathResult(
            GridPathStatus status,
            List<GridNavigationAnchor> anchors,
            float travelTime,
            int examinedNodeCount)
        {
            Status = status;
            Anchors = anchors ?? new List<GridNavigationAnchor>();
            TravelTime = travelTime;
            ExaminedNodeCount = examinedNodeCount;
        }

        public GridPathStatus Status { get; }
        public IReadOnlyList<GridNavigationAnchor> Anchors { get; }
        public float TravelTime { get; }
        public int ExaminedNodeCount { get; }
        public bool Succeeded => Status == GridPathStatus.Success;
    }

    /// <summary>
    /// One resumable deterministic A* search. Keeping the frontier here lets the
    /// scheduler bound node expansions without restarting work every update.
    /// </summary>
    public sealed class GridPathSearch
    {
        private readonly GridNavigationMap map;
        private readonly Vector2Int start;
        private readonly Vector2Int destination;
        private readonly int mapRevision;
        private readonly int maximumExaminedNodes;
        private readonly List<OpenNode> open = new();
        private readonly Dictionary<Vector2Int, float> costs = new();
        private readonly Dictionary<Vector2Int, Vector2Int> previous = new();
        private readonly HashSet<Vector2Int> closed = new();
        private readonly List<GridNavigationConnection> neighbours = new(8);

        internal GridPathSearch(
            GridNavigationMap map,
            Vector2Int start,
            Vector2Int destination,
            int maximumExaminedNodes)
        {
            this.map = map;
            this.start = start;
            this.destination = destination;
            this.maximumExaminedNodes = Mathf.Max(1, maximumExaminedNodes);
            mapRevision = map.Revision;

            if (!map.TryGetAnchor(start, out _))
            {
                Complete(GridPathStatus.StartInvalid);
                return;
            }

            if (!map.TryGetAnchor(destination, out _))
            {
                Complete(GridPathStatus.DestinationInvalid);
                return;
            }

            costs[start] = 0f;
            Push(open, new OpenNode(start, Heuristic(start), 0f));
        }

        public bool IsComplete => Result != null;
        public GridPathResult Result { get; private set; }
        public int ExaminedNodeCount { get; private set; }
        public int MapRevision => mapRevision;

        /// <summary>Expands no more than the requested number of A* nodes.</summary>
        public int Advance(int maximumNodeExpansions)
        {
            if (IsComplete || maximumNodeExpansions <= 0)
            {
                return 0;
            }

            if (map.Revision != mapRevision)
            {
                Complete(GridPathStatus.TerrainChanged);
                return 0;
            }

            int expandedThisCall = 0;
            while (!IsComplete && open.Count > 0 &&
                expandedThisCall < maximumNodeExpansions)
            {
                OpenNode current = Pop(open);
                if (!closed.Add(current.Cell))
                {
                    continue;
                }

                ExaminedNodeCount++;
                expandedThisCall++;
                if (current.Cell == destination)
                {
                    BuildSuccessResult();
                    break;
                }

                if (ExaminedNodeCount >= maximumExaminedNodes)
                {
                    Complete(GridPathStatus.SearchLimitReached);
                    break;
                }

                map.GetConnections(current.Cell, neighbours);
                foreach (GridNavigationConnection edge in neighbours)
                {
                    Vector2Int next = edge.Destination.Cell;
                    if (closed.Contains(next))
                    {
                        continue;
                    }

                    float candidate = costs[current.Cell] + edge.TravelTime;
                    if (costs.TryGetValue(next, out float existing) &&
                        candidate >= existing)
                    {
                        continue;
                    }

                    costs[next] = candidate;
                    previous[next] = current.Cell;
                    Push(open, new OpenNode(
                        next,
                        candidate + Heuristic(next),
                        candidate));
                }
            }

            if (!IsComplete && open.Count == 0)
            {
                Complete(GridPathStatus.Unreachable);
            }

            return expandedThisCall;
        }

        private void BuildSuccessResult()
        {
            var cells = new List<Vector2Int> { destination };
            while (cells[cells.Count - 1] != start)
            {
                cells.Add(previous[cells[cells.Count - 1]]);
            }

            cells.Reverse();
            var anchors = new List<GridNavigationAnchor>(cells.Count);
            foreach (Vector2Int cell in cells)
            {
                if (!map.TryGetAnchor(cell, out GridNavigationAnchor anchor))
                {
                    Complete(GridPathStatus.TerrainChanged);
                    return;
                }

                anchors.Add(anchor);
            }

            Result = new GridPathResult(
                GridPathStatus.Success,
                anchors,
                costs[destination],
                ExaminedNodeCount);
        }

        private void Complete(GridPathStatus status)
        {
            Result = new GridPathResult(
                status,
                null,
                0f,
                ExaminedNodeCount);
        }

        private float Heuristic(Vector2Int cell)
        {
            return Vector2.Distance(cell, destination) /
                Mathf.Max(0.01f, map.MaximumSpeedMultiplier);
        }

        private static void Push(List<OpenNode> heap, OpenNode item)
        {
            heap.Add(item);
            int index = heap.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (Compare(heap[parent], item) <= 0)
                {
                    break;
                }

                heap[index] = heap[parent];
                index = parent;
            }

            heap[index] = item;
        }

        private static OpenNode Pop(List<OpenNode> heap)
        {
            OpenNode result = heap[0];
            OpenNode replacement = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0)
            {
                return result;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= heap.Count)
                {
                    break;
                }

                int right = left + 1;
                int child = right < heap.Count &&
                    Compare(heap[right], heap[left]) < 0
                        ? right
                        : left;
                if (Compare(replacement, heap[child]) <= 0)
                {
                    break;
                }

                heap[index] = heap[child];
                index = child;
            }

            heap[index] = replacement;
            return result;
        }

        private static int Compare(OpenNode first, OpenNode second)
        {
            int estimate = first.EstimatedTotal.CompareTo(second.EstimatedTotal);
            if (estimate != 0)
            {
                return estimate;
            }

            int cost = first.Cost.CompareTo(second.Cost);
            if (cost != 0)
            {
                return cost;
            }

            int y = first.Cell.y.CompareTo(second.Cell.y);
            return y != 0 ? y : first.Cell.x.CompareTo(second.Cell.x);
        }

        private readonly struct OpenNode
        {
            public OpenNode(Vector2Int cell, float estimatedTotal, float cost)
            {
                Cell = cell;
                EstimatedTotal = estimatedTotal;
                Cost = cost;
            }

            public Vector2Int Cell { get; }
            public float EstimatedTotal { get; }
            public float Cost { get; }
        }
    }

    /// <summary>Creates synchronous or resumable searches over one shared map.</summary>
    public sealed class GridPathfinder
    {
        private readonly GridNavigationMap map;

        public GridPathfinder(GridNavigationMap map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public GridPathSearch BeginSearch(
            Vector2Int start,
            Vector2Int destination,
            int maximumExaminedNodes = 8192)
        {
            return new GridPathSearch(
                map,
                start,
                destination,
                maximumExaminedNodes);
        }

        /// <summary>
        /// Compatibility helper for deterministic tests and editor tools. Runtime
        /// ants use the bounded scheduler instead.
        /// </summary>
        public GridPathResult FindPath(
            Vector2Int start,
            Vector2Int destination,
            int maximumExaminedNodes = 8192)
        {
            GridPathSearch search = BeginSearch(
                start,
                destination,
                maximumExaminedNodes);
            while (!search.IsComplete)
            {
                search.Advance(maximumExaminedNodes);
            }

            return search.Result;
        }
    }
}
