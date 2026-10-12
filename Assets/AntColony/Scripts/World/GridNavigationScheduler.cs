using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace AntColony.World
{
    public enum GridNavigationRequestPriority
    {
        High,
        Normal,
        Low
    }

    public enum GridNavigationRequestStatus
    {
        Pending,
        Processing,
        Success,
        Unreachable,
        Cancelled,
        SearchLimitReached
    }

    public readonly struct GridNavigationRequestHandle
    {
        internal GridNavigationRequestHandle(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
        public bool IsValid => RequestId > 0;
    }

    public readonly struct GridScheduledPathResult
    {
        internal GridScheduledPathResult(
            long requestId,
            int ownerId,
            Vector2Int destination,
            GridNavigationRequestStatus status,
            GridPathResult path,
            long submittedUpdate,
            long completedUpdate,
            string message)
        {
            RequestId = requestId;
            OwnerId = ownerId;
            Destination = destination;
            Status = status;
            Path = path;
            SubmittedUpdate = submittedUpdate;
            CompletedUpdate = completedUpdate;
            Message = message;
        }

        public long RequestId { get; }
        public int OwnerId { get; }
        public Vector2Int Destination { get; }
        public GridNavigationRequestStatus Status { get; }
        public GridPathResult Path { get; }
        public long SubmittedUpdate { get; }
        public long CompletedUpdate { get; }
        public long LatencyUpdates => CompletedUpdate - SubmittedUpdate;
        public string Message { get; }
        public bool Succeeded => Status == GridNavigationRequestStatus.Success;
    }

    public readonly struct GridNavigationSchedulerDiagnostics
    {
        internal GridNavigationSchedulerDiagnostics(
            int pending,
            int processing,
            int completedWaitingDelivery,
            int peakQueueLength,
            int nodesExpandedLastUpdate,
            long totalNodesExpanded,
            long totalSubmitted,
            long totalCompleted,
            long totalCancelled,
            long cacheInvalidations,
            double lastUpdateMilliseconds,
            double maximumUpdateMilliseconds,
            long updateIndex)
        {
            Pending = pending;
            Processing = processing;
            CompletedWaitingDelivery = completedWaitingDelivery;
            PeakQueueLength = peakQueueLength;
            NodesExpandedLastUpdate = nodesExpandedLastUpdate;
            TotalNodesExpanded = totalNodesExpanded;
            TotalSubmitted = totalSubmitted;
            TotalCompleted = totalCompleted;
            TotalCancelled = totalCancelled;
            CacheInvalidations = cacheInvalidations;
            LastUpdateMilliseconds = lastUpdateMilliseconds;
            MaximumUpdateMilliseconds = maximumUpdateMilliseconds;
            UpdateIndex = updateIndex;
        }

        public int Pending { get; }
        public int Processing { get; }
        public int CompletedWaitingDelivery { get; }
        public int PeakQueueLength { get; }
        public int NodesExpandedLastUpdate { get; }
        public long TotalNodesExpanded { get; }
        public long TotalSubmitted { get; }
        public long TotalCompleted { get; }
        public long TotalCancelled { get; }
        public long CacheInvalidations { get; }
        public double LastUpdateMilliseconds { get; }
        public double MaximumUpdateMilliseconds { get; }
        public long UpdateIndex { get; }
    }

    /// <summary>
    /// Shared main-thread path service. Each update has a total expansion budget,
    /// and each request receives only a small slice before returning to its queue.
    /// The weighted service order favours urgent work but always includes a Low
    /// slot, preventing indefinite starvation without adding colony priorities.
    /// </summary>
    public sealed class GridNavigationScheduler
    {
        private static readonly GridNavigationRequestPriority[] ServicePattern =
        {
            GridNavigationRequestPriority.High,
            GridNavigationRequestPriority.High,
            GridNavigationRequestPriority.Normal,
            GridNavigationRequestPriority.High,
            GridNavigationRequestPriority.Normal,
            GridNavigationRequestPriority.Low
        };

        private readonly GridNavigationMap map;
        private readonly GridPathfinder pathfinder;
        private readonly Queue<RequestRecord> high = new();
        private readonly Queue<RequestRecord> normal = new();
        private readonly Queue<RequestRecord> low = new();
        private readonly Dictionary<long, RequestRecord> requests = new();
        private readonly Dictionary<int, RequestRecord> activeByOwner = new();
        private long nextRequestId = 1;
        private int servicePatternIndex;
        private int processingCount;
        private int peakQueueLength;
        private long totalNodesExpanded;
        private long totalSubmitted;
        private long totalCompleted;
        private long totalCancelled;
        private long cacheInvalidations;
        private double lastUpdateMilliseconds;
        private double maximumUpdateMilliseconds;
        private long updateIndex;

        public GridNavigationScheduler(GridNavigationMap map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            pathfinder = new GridPathfinder(map);
        }

        public GridNavigationSchedulerDiagnostics Diagnostics => new(
            CountQueuedActive(),
            processingCount,
            CountCompletedWaitingDelivery(),
            peakQueueLength,
            NodesExpandedLastUpdate,
            totalNodesExpanded,
            totalSubmitted,
            totalCompleted,
            totalCancelled,
            cacheInvalidations,
            lastUpdateMilliseconds,
            maximumUpdateMilliseconds,
            updateIndex);

        public int NodesExpandedLastUpdate { get; private set; }

        /// <summary>
        /// Queues one owner request. An identical outstanding request is reused;
        /// a changed destination cancels the old request before creating the new.
        /// </summary>
        public GridNavigationRequestHandle RequestPath(
            int ownerId,
            Vector2Int start,
            Vector2Int destination,
            GridNavigationRequestPriority priority,
            int maximumExaminedNodes,
            out bool created)
        {
            if (activeByOwner.TryGetValue(ownerId, out RequestRecord existing) &&
                !existing.IsTerminal)
            {
                if (existing.Start == start &&
                    existing.Destination == destination &&
                    existing.Priority == priority)
                {
                    created = false;
                    return new GridNavigationRequestHandle(existing.RequestId);
                }

                Cancel(existing.RequestId, "Destination replaced.");
                requests.Remove(existing.RequestId);
            }

            long requestId = nextRequestId++;
            var record = new RequestRecord(
                requestId,
                ownerId,
                start,
                destination,
                priority,
                map.Revision,
                updateIndex,
                maximumExaminedNodes);
            requests.Add(requestId, record);
            activeByOwner[ownerId] = record;
            Enqueue(record);
            totalSubmitted++;
            peakQueueLength = Mathf.Max(peakQueueLength, CountQueuedActive());
            created = true;
            return new GridNavigationRequestHandle(requestId);
        }

        public bool Cancel(long requestId, string reason = "Request cancelled.")
        {
            if (!requests.TryGetValue(requestId, out RequestRecord record) ||
                record.IsTerminal)
            {
                return false;
            }

            Complete(
                record,
                GridNavigationRequestStatus.Cancelled,
                null,
                reason);
            return true;
        }

        public bool CancelOwner(int ownerId, string reason = "Owner cancelled request.")
        {
            return activeByOwner.TryGetValue(ownerId, out RequestRecord record) &&
                Cancel(record.RequestId, reason);
        }

        /// <summary>
        /// Invalidating any map cell changes the shared revision, so every search
        /// created against the old revision is cancelled before it can deliver a
        /// stale route. Motors may immediately submit a fresh bounded request.
        /// </summary>
        public void NotifyTerrainInvalidated()
        {
            cacheInvalidations++;
            var stale = new List<RequestRecord>();
            foreach (RequestRecord record in requests.Values)
            {
                if (!record.IsTerminal && record.MapRevision != map.Revision)
                {
                    stale.Add(record);
                }
            }

            foreach (RequestRecord record in stale)
            {
                Complete(
                    record,
                    GridNavigationRequestStatus.Cancelled,
                    null,
                    "Terrain revision changed.");
            }
        }

        /// <summary>Processes no more than totalNodeBudget expansions.</summary>
        public void ProcessUpdate(int totalNodeBudget, int nodesPerRequestSlice)
        {
            long started = Stopwatch.GetTimestamp();
            updateIndex++;
            NodesExpandedLastUpdate = 0;
            int remaining = Mathf.Max(0, totalNodeBudget);
            int slice = Mathf.Max(1, nodesPerRequestSlice);
            int maximumRequestsThisUpdate = Mathf.Max(1, remaining / slice);
            int requestsServiced = 0;

            while (remaining > 0 &&
                requestsServiced < maximumRequestsThisUpdate &&
                TryDequeueFair(out RequestRecord record))
            {
                if (record.IsTerminal)
                {
                    continue;
                }

                if (record.MapRevision != map.Revision)
                {
                    Complete(
                        record,
                        GridNavigationRequestStatus.Cancelled,
                        null,
                        "Terrain revision changed.");
                    continue;
                }

                record.Status = GridNavigationRequestStatus.Processing;
                processingCount++;
                requestsServiced++;
                record.Search ??= pathfinder.BeginSearch(
                    record.Start,
                    record.Destination,
                    record.MaximumExaminedNodes);
                int expanded = record.Search.Advance(Mathf.Min(slice, remaining));
                processingCount--;
                remaining -= expanded;
                NodesExpandedLastUpdate += expanded;
                totalNodesExpanded += expanded;

                if (record.Search.IsComplete)
                {
                    FinishSearch(record);
                }
                else
                {
                    record.Status = GridNavigationRequestStatus.Pending;
                    Enqueue(record);
                }

                // A malformed search must not spin forever without consuming
                // work. Current searches either finish or expand at least one.
                if (expanded == 0 && !record.IsTerminal)
                {
                    Complete(
                        record,
                        GridNavigationRequestStatus.SearchLimitReached,
                        null,
                        "Search made no bounded progress.");
                }
            }

            lastUpdateMilliseconds = ElapsedMilliseconds(started);
            maximumUpdateMilliseconds = Math.Max(
                maximumUpdateMilliseconds,
                lastUpdateMilliseconds);
        }

        public GridNavigationRequestStatus GetStatus(long requestId)
        {
            return requests.TryGetValue(requestId, out RequestRecord record)
                ? record.Status
                : GridNavigationRequestStatus.Cancelled;
        }

        public bool TryTakeResult(
            long requestId,
            out GridScheduledPathResult result)
        {
            if (!requests.TryGetValue(requestId, out RequestRecord record) ||
                !record.IsTerminal)
            {
                result = default;
                return false;
            }

            result = record.Result;
            requests.Remove(requestId);
            if (activeByOwner.TryGetValue(record.OwnerId, out RequestRecord active) &&
                active.RequestId == requestId)
            {
                activeByOwner.Remove(record.OwnerId);
            }

            return true;
        }

        private void FinishSearch(RequestRecord record)
        {
            GridPathResult path = record.Search.Result;
            switch (path.Status)
            {
                case GridPathStatus.Success:
                    Complete(record, GridNavigationRequestStatus.Success, path, "Path found.");
                    break;
                case GridPathStatus.SearchLimitReached:
                    Complete(record, GridNavigationRequestStatus.SearchLimitReached, path,
                        "Search node limit reached.");
                    break;
                case GridPathStatus.TerrainChanged:
                    Complete(record, GridNavigationRequestStatus.Cancelled, path,
                        "Terrain revision changed.");
                    break;
                default:
                    Complete(record, GridNavigationRequestStatus.Unreachable, path,
                        $"Path failed: {path.Status}.");
                    break;
            }
        }

        private void Complete(
            RequestRecord record,
            GridNavigationRequestStatus status,
            GridPathResult path,
            string message)
        {
            record.Status = status;
            record.Result = new GridScheduledPathResult(
                record.RequestId,
                record.OwnerId,
                record.Destination,
                status,
                path,
                record.SubmittedUpdate,
                updateIndex,
                message);
            totalCompleted++;
            if (status == GridNavigationRequestStatus.Cancelled)
            {
                totalCancelled++;
            }
        }

        private void Enqueue(RequestRecord record)
        {
            switch (record.Priority)
            {
                case GridNavigationRequestPriority.High:
                    high.Enqueue(record);
                    break;
                case GridNavigationRequestPriority.Low:
                    low.Enqueue(record);
                    break;
                default:
                    normal.Enqueue(record);
                    break;
            }
        }

        private bool TryDequeueFair(out RequestRecord record)
        {
            for (int attempt = 0; attempt < ServicePattern.Length; attempt++)
            {
                GridNavigationRequestPriority priority =
                    ServicePattern[servicePatternIndex];
                servicePatternIndex =
                    (servicePatternIndex + 1) % ServicePattern.Length;
                Queue<RequestRecord> queue = QueueFor(priority);
                while (queue.Count > 0)
                {
                    record = queue.Dequeue();
                    if (!record.IsTerminal)
                    {
                        return true;
                    }
                }
            }

            if (TryDequeueActive(high, out record) ||
                TryDequeueActive(normal, out record) ||
                TryDequeueActive(low, out record))
            {
                return true;
            }

            record = null;
            return false;
        }

        private static bool TryDequeueActive(
            Queue<RequestRecord> queue,
            out RequestRecord record)
        {
            while (queue.Count > 0)
            {
                record = queue.Dequeue();
                if (!record.IsTerminal)
                {
                    return true;
                }
            }

            record = null;
            return false;
        }

        private Queue<RequestRecord> QueueFor(GridNavigationRequestPriority priority)
        {
            return priority switch
            {
                GridNavigationRequestPriority.High => high,
                GridNavigationRequestPriority.Low => low,
                _ => normal
            };
        }

        private int CountQueuedActive()
        {
            int count = 0;
            foreach (RequestRecord record in requests.Values)
            {
                if (!record.IsTerminal)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountCompletedWaitingDelivery()
        {
            int count = 0;
            foreach (RequestRecord record in requests.Values)
            {
                if (record.IsTerminal)
                {
                    count++;
                }
            }

            return count;
        }

        private static double ElapsedMilliseconds(long started)
        {
            return (Stopwatch.GetTimestamp() - started) * 1000d /
                Stopwatch.Frequency;
        }

        private sealed class RequestRecord
        {
            public RequestRecord(
                long requestId,
                int ownerId,
                Vector2Int start,
                Vector2Int destination,
                GridNavigationRequestPriority priority,
                int mapRevision,
                long submittedUpdate,
                int maximumExaminedNodes)
            {
                RequestId = requestId;
                OwnerId = ownerId;
                Start = start;
                Destination = destination;
                Priority = priority;
                MapRevision = mapRevision;
                SubmittedUpdate = submittedUpdate;
                MaximumExaminedNodes = Mathf.Max(1, maximumExaminedNodes);
                Status = GridNavigationRequestStatus.Pending;
            }

            public long RequestId { get; }
            public int OwnerId { get; }
            public Vector2Int Start { get; }
            public Vector2Int Destination { get; }
            public GridNavigationRequestPriority Priority { get; }
            public int MapRevision { get; }
            public long SubmittedUpdate { get; }
            public int MaximumExaminedNodes { get; }
            public GridPathSearch Search { get; set; }
            public GridNavigationRequestStatus Status { get; set; }
            public GridScheduledPathResult Result { get; set; }
            public bool IsTerminal =>
                Status == GridNavigationRequestStatus.Success ||
                Status == GridNavigationRequestStatus.Unreachable ||
                Status == GridNavigationRequestStatus.Cancelled ||
                Status == GridNavigationRequestStatus.SearchLimitReached;
        }
    }
}
