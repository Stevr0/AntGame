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
        SearchLimitReached
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

    /// <summary>Deterministic A* over the shared terrain-only navigation map.</summary>
    public sealed class GridPathfinder
    {
        private readonly GridNavigationMap map;
        private readonly List<GridNavigationConnection> neighbours = new(8);

        public GridPathfinder(GridNavigationMap map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public GridPathResult FindPath(
            Vector2Int start,
            Vector2Int destination,
            int maximumExaminedNodes = 8192)
        {
            if (!map.TryGetAnchor(start, out _))
            {
                return Failure(GridPathStatus.StartInvalid, 0);
            }

            if (!map.TryGetAnchor(destination, out _))
            {
                return Failure(GridPathStatus.DestinationInvalid, 0);
            }

            var open = new List<OpenNode>();
            var costs = new Dictionary<Vector2Int, float>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();
            costs[start] = 0f;
            Push(open, new OpenNode(start, Heuristic(start, destination), 0f));
            int examined = 0;

            while (open.Count > 0)
            {
                OpenNode current = Pop(open);
                if (!closed.Add(current.Cell))
                {
                    continue;
                }

                examined++;
                if (current.Cell == destination)
                {
                    return BuildResult(start, destination, previous, costs[destination], examined);
                }

                if (examined >= maximumExaminedNodes)
                {
                    return Failure(GridPathStatus.SearchLimitReached, examined);
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
                    if (costs.TryGetValue(next, out float existing) && candidate >= existing)
                    {
                        continue;
                    }

                    costs[next] = candidate;
                    previous[next] = current.Cell;
                    Push(open, new OpenNode(
                        next,
                        candidate + Heuristic(next, destination),
                        candidate));
                }
            }

            return Failure(GridPathStatus.Unreachable, examined);
        }

        private GridPathResult BuildResult(
            Vector2Int start,
            Vector2Int destination,
            Dictionary<Vector2Int, Vector2Int> previous,
            float cost,
            int examined)
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
                map.TryGetAnchor(cell, out GridNavigationAnchor anchor);
                anchors.Add(anchor);
            }

            return new GridPathResult(GridPathStatus.Success, anchors, cost, examined);
        }

        private float Heuristic(Vector2Int cell, Vector2Int destination)
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
            return cost != 0 ? cost : Compare(first.Cell, second.Cell);
        }

        private static int Compare(Vector2Int first, Vector2Int second)
        {
            int y = first.y.CompareTo(second.y);
            return y != 0 ? y : first.x.CompareTo(second.x);
        }

        private static GridPathResult Failure(GridPathStatus status, int examined) =>
            new(status, null, 0f, examined);

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
}
