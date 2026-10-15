using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public readonly struct ColonyJobId : IEquatable<ColonyJobId>
    {
        public ColonyJobId(string value) => Value = value?.Trim();
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(ColonyJobId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ColonyJobId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public enum ColonyJobStatus
    {
        Available,
        Reserved,
        InProgress,
        Paused,
        Completed,
        Cancelled,
        Blocked
    }

    /// <summary>Authoritative serialisation-ready state for one colony job.</summary>
    public sealed class ColonyJob
    {
        internal ColonyJob(ColonyJobId id, AntWorkCategory category, Vector2Int targetCell)
        {
            Id = id;
            Category = category;
            TargetCell = targetCell;
        }

        public ColonyJobId Id { get; }
        public AntWorkCategory Category { get; }
        public Vector2Int TargetCell { get; }
        public double Progress { get; internal set; }
        public ColonyJobStatus Status { get; internal set; } = ColonyJobStatus.Available;
        public AntId ReservationOwner { get; internal set; }
        public string FailureReason { get; internal set; } = string.Empty;
        public bool CanBeClaimed =>
            Status == ColonyJobStatus.Available || Status == ColonyJobStatus.Paused;
    }

    /// <summary>
    /// Small shared owner for one colony's jobs. It deliberately contains no
    /// GameObjects, navigation code, terrain mutation or inventories.
    /// </summary>
    public sealed class ColonyJobRegistry
    {
        private readonly Dictionary<ColonyJobId, ColonyJob> jobs = new();
        private readonly List<ColonyJob> jobsInCreationOrder = new();
        public IEnumerable<ColonyJob> Jobs => jobsInCreationOrder;
        public int Count => jobs.Count;

        /// <summary>
        /// Changes when removal can shift an existing cursor. New jobs append to
        /// creation order and can safely join a pass without restarting it.
        /// Status changes are rechecked before a candidate is used.
        /// </summary>
        public int CursorRevision { get; private set; }

        internal ColonyJob GetAt(int index) => jobsInCreationOrder[index];

        public bool TryAdd(
            ColonyJobId id,
            AntWorkCategory category,
            Vector2Int targetCell,
            out ColonyJob job,
            out string error)
        {
            job = null;
            if (!id.IsValid)
            {
                error = "A colony job requires a stable identifier.";
                return false;
            }
            if (!Enum.IsDefined(typeof(AntWorkCategory), category))
            {
                error = "The colony job category is invalid.";
                return false;
            }
            if (jobs.TryGetValue(id, out ColonyJob existingById))
            {
                // Designation IDs are deliberately stable per world cell. A
                // cancelled designation may therefore be authored again, but
                // completed work remains terminal and cannot be resurrected.
                if (existingById.Status != ColonyJobStatus.Cancelled)
                {
                    error = $"Job ID {id} already exists.";
                    return false;
                }
                Remove(id);
            }
            foreach (ColonyJob existing in jobs.Values)
            {
                if (category != AntWorkCategory.Hauling &&
                    existing.Category == category && existing.TargetCell == targetCell &&
                    existing.Status != ColonyJobStatus.Completed &&
                    existing.Status != ColonyJobStatus.Cancelled)
                {
                    error = $"An active {category} designation already exists at {targetCell}.";
                    return false;
                }
            }

            job = new ColonyJob(id, category, targetCell);
            jobs.Add(id, job);
            jobsInCreationOrder.Add(job);
            error = string.Empty;
            return true;
        }

        public bool TryGet(ColonyJobId id, out ColonyJob job) => jobs.TryGetValue(id, out job);

        public bool TryReserve(ColonyJobId id, AntId owner, out string error)
        {
            if (!owner.IsValid || !jobs.TryGetValue(id, out ColonyJob job))
            {
                error = "The job or reservation owner is invalid.";
                return false;
            }
            if (!job.CanBeClaimed)
            {
                error = $"Job {id} cannot be claimed while {job.Status}.";
                return false;
            }
            job.ReservationOwner = owner;
            job.Status = ColonyJobStatus.Reserved;
            job.FailureReason = string.Empty;
            error = string.Empty;
            return true;
        }

        public bool TryBegin(ColonyJobId id, AntId owner, out string error)
        {
            if (!TryGetOwned(id, owner, out ColonyJob job, out error)) return false;
            job.Status = ColonyJobStatus.InProgress;
            return true;
        }

        public bool TryAddProgress(
            ColonyJobId id,
            AntId owner,
            double amount,
            out double applied,
            out string error)
        {
            applied = 0d;
            if (!TryGetOwned(id, owner, out ColonyJob job, out error) ||
                job.Status != ColonyJobStatus.InProgress)
            {
                if (string.IsNullOrEmpty(error)) error = "The job is not in progress.";
                return false;
            }
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0d)
            {
                error = "Work progress must be finite and non-negative.";
                return false;
            }

            applied = Math.Min(amount, 1d - job.Progress);
            job.Progress += applied;
            if (job.Progress >= 1d)
            {
                job.Progress = 1d;
                job.Status = ColonyJobStatus.Completed;
                job.ReservationOwner = default;
            }
            error = string.Empty;
            return true;
        }

        public void PauseAndRelease(ColonyJobId id, AntId owner, string reason)
        {
            if (!TryGetOwned(id, owner, out ColonyJob job, out _)) return;
            job.Status = ColonyJobStatus.Paused;
            job.ReservationOwner = default;
            job.FailureReason = reason ?? string.Empty;
        }

        /// <summary>
        /// Records a temporary unmet dependency without assigning the job.
        /// Paused jobs remain reconsiderable when their dependency changes.
        /// </summary>
        public void RecordPendingDependency(ColonyJobId id, string reason)
        {
            if (!jobs.TryGetValue(id, out ColonyJob job) ||
                !job.CanBeClaimed || job.ReservationOwner.IsValid) return;
            job.Status = ColonyJobStatus.Paused;
            job.FailureReason = reason ?? string.Empty;
        }

        public void BlockAndRelease(ColonyJobId id, AntId owner, string reason)
        {
            if (!TryGetOwned(id, owner, out ColonyJob job, out _)) return;
            job.Status = ColonyJobStatus.Blocked;
            job.ReservationOwner = default;
            job.FailureReason = reason ?? "Job destination is blocked.";
        }

        public bool TryCancel(ColonyJobId id, string reason, out string error)
        {
            if (!jobs.TryGetValue(id, out ColonyJob job))
            {
                error = $"Job {id} does not exist.";
                return false;
            }
            if (job.Status == ColonyJobStatus.Completed ||
                job.Status == ColonyJobStatus.Cancelled)
            {
                error = $"Job {id} is already terminal.";
                return false;
            }
            job.Status = ColonyJobStatus.Cancelled;
            job.ReservationOwner = default;
            job.FailureReason = reason ?? "Job cancelled.";
            error = string.Empty;
            return true;
        }

        public void ReleaseOwnedJobs(AntId owner, string reason)
        {
            foreach (ColonyJob job in jobs.Values)
            {
                if (job.ReservationOwner.Equals(owner))
                    PauseAndRelease(job.Id, owner, reason);
            }
        }

        public void Clear()
        {
            if (jobs.Count == 0) return;
            jobs.Clear();
            jobsInCreationOrder.Clear();
            CursorRevision++;
        }

        public void RemoveWhere(Predicate<ColonyJob> shouldRemove)
        {
            if (shouldRemove == null) return;
            var ids = new List<ColonyJobId>();
            foreach (ColonyJob job in jobs.Values)
                if (shouldRemove(job)) ids.Add(job.Id);
            foreach (ColonyJobId id in ids) Remove(id);
        }

        private void Remove(ColonyJobId id)
        {
            if (!jobs.TryGetValue(id, out ColonyJob job)) return;
            jobs.Remove(id);
            jobsInCreationOrder.Remove(job);
            CursorRevision++;
        }

        private bool TryGetOwned(
            ColonyJobId id,
            AntId owner,
            out ColonyJob job,
            out string error)
        {
            if (!jobs.TryGetValue(id, out job))
            {
                error = $"Job {id} does not exist.";
                return false;
            }
            if (!job.ReservationOwner.Equals(owner))
            {
                error = $"Job {id} is not reserved by ant {owner}.";
                return false;
            }
            error = string.Empty;
            return true;
        }
    }

    /// <summary>Read-only evidence from the most recent bounded selection step.</summary>
    public readonly struct ColonyJobSelectionDiagnostics
    {
        public ColonyJobSelectionDiagnostics(
            int examined,
            int total,
            int nextIndex,
            bool passCompleted,
            ColonyJobId selectedJobId,
            int eligibleCandidates,
            int reachableCandidates,
            int unavailableRejected,
            int disabledRejected,
            int eligibilityRejected,
            int unreachableRejected)
        {
            ExaminedThisEvaluation = examined;
            TotalJobs = total;
            NextIndex = nextIndex;
            PassCompleted = passCompleted;
            SelectedJobId = selectedJobId;
            EligibleCandidatesSeen = eligibleCandidates;
            ReachableCandidatesSeen = reachableCandidates;
            UnavailableRejected = unavailableRejected;
            DisabledRejected = disabledRejected;
            EligibilityRejected = eligibilityRejected;
            UnreachableRejected = unreachableRejected;
        }

        public int ExaminedThisEvaluation { get; }
        public int TotalJobs { get; }
        public int NextIndex { get; }
        public bool PassCompleted { get; }
        public ColonyJobId SelectedJobId { get; }
        public int EligibleCandidatesSeen { get; }
        public int ReachableCandidatesSeen { get; }
        public int UnavailableRejected { get; }
        public int DisabledRejected { get; }
        public int EligibilityRejected { get; }
        public int UnreachableRejected { get; }
        public bool HasPendingReachableCandidate =>
            !PassCompleted && ReachableCandidatesSeen > 0;
    }

    public sealed class ColonyJobSelectionService
    {
        public const int MaximumCandidatesPerEvaluation = 32;
        private readonly Dictionary<AntId, SelectionPass> passes = new();

        /// <summary>True when another bounded evaluation is needed to finish the pass.</summary>
        public bool LastEvaluationInProgress { get; private set; }
        public ColonyJobSelectionDiagnostics LastDiagnostics { get; private set; }

        public bool TryChoose(
            AntRecord ant,
            ColonyJobRegistry registry,
            GridNavigationMap map,
            Vector2Int start,
            out ColonyJob selected,
            out Vector2Int approach,
            out string error)
        {
            return TryChoose(ant, registry, map, start, null,
                out selected, out approach, out error);
        }

        public bool TryChoose(
            AntRecord ant,
            ColonyJobRegistry registry,
            GridNavigationMap map,
            Vector2Int start,
            Predicate<ColonyJob> eligibility,
            out ColonyJob selected,
            out Vector2Int approach,
            out string error)
        {
            selected = null;
            approach = default;
            LastEvaluationInProgress = false;
            LastDiagnostics = default;
            if (ant == null || registry == null || map == null)
            {
                error = "Job selection requires an ant, colony registry and navigation map.";
                return false;
            }

            if (registry.Count == 0)
            {
                passes.Remove(ant.Id);
                error = "No reachable enabled colony job is currently available.";
                return false;
            }

            if (!passes.TryGetValue(ant.Id, out SelectionPass pass) ||
                !pass.Matches(registry, map, start, ant.WorkPriorities.Revision))
            {
                pass = new SelectionPass(
                    registry, map, start, ant.WorkPriorities.Revision);
                passes[ant.Id] = pass;
            }

            int remaining = registry.Count - pass.NextIndex;
            int examined = Math.Min(MaximumCandidatesPerEvaluation, remaining);
            int endIndex = pass.NextIndex + examined;
            for (int index = pass.NextIndex; index < endIndex; index++)
            {
                ColonyJob job = registry.GetAt(index);
                if (!job.CanBeClaimed)
                {
                    pass.UnavailableRejected++;
                    continue;
                }
                int priority = (int)ant.WorkPriorities.Get(job.Category);
                if (priority < 1 || priority > 4)
                {
                    pass.DisabledRejected++;
                    continue;
                }
                if (eligibility != null && !eligibility(job))
                {
                    pass.EligibilityRejected++;
                    continue;
                }
                pass.EligibleCandidatesSeen++;
                if (!SurvivalInteractionOrder.TryMeasureApproach(
                        map, start, job.TargetCell,
                        out Vector2Int candidateApproach,
                        out float cost, out _))
                {
                    pass.UnreachableRejected++;
                    continue;
                }
                pass.ReachableCandidatesSeen++;

                int priorityIndex = priority - 1;
                ColonyJob currentBest = pass.BestJobs[priorityIndex];
                if (cost < pass.BestCosts[priorityIndex] ||
                    Mathf.Approximately(cost, pass.BestCosts[priorityIndex]) &&
                    Compare(job, currentBest) < 0)
                {
                    pass.BestJobs[priorityIndex] = job;
                    pass.BestApproaches[priorityIndex] = candidateApproach;
                    pass.BestCosts[priorityIndex] = cost;
                }
            }
            pass.NextIndex = endIndex;

            if (pass.NextIndex < registry.Count)
            {
                LastEvaluationInProgress = true;
                LastDiagnostics = CreateDiagnostics(
                    pass, examined, registry.Count, false, default);
                error = $"Job scan in progress: checked {pass.NextIndex} of " +
                    $"{registry.Count} entries for ant {ant.Id}.";
                return false;
            }

            for (int priorityIndex = 0; priorityIndex < pass.BestJobs.Length;
                 priorityIndex++)
            {
                ColonyJob candidate = pass.BestJobs[priorityIndex];
                if (candidate == null) continue;

                // Another ant or a world transaction may have changed this job
                // during a multi-tick pass. Restarting is conservative: it keeps
                // the priority result correct without retaining an unbounded list.
                if (!registry.TryGet(candidate.Id, out ColonyJob current) ||
                    !ReferenceEquals(candidate, current) || !candidate.CanBeClaimed ||
                    (int)ant.WorkPriorities.Get(candidate.Category) != priorityIndex + 1 ||
                    eligibility != null && !eligibility(candidate) ||
                    !SurvivalInteractionOrder.TryMeasureApproach(
                        map, start, candidate.TargetCell,
                        out Vector2Int currentApproach, out _, out _))
                {
                    passes.Remove(ant.Id);
                    pass.NextIndex = 0;
                    LastEvaluationInProgress = true;
                    LastDiagnostics = CreateDiagnostics(
                        pass, examined, registry.Count, false, default);
                    error = "Job scan restarted because its best candidate changed.";
                    return false;
                }

                selected = candidate;
                approach = currentApproach;
                passes.Remove(ant.Id);
                LastDiagnostics = CreateDiagnostics(
                    pass, examined, registry.Count, true, selected.Id);
                error = string.Empty;
                return true;
            }

            passes.Remove(ant.Id);
            LastDiagnostics = CreateDiagnostics(
                pass, examined, registry.Count, true, default);
            error = "No reachable enabled colony job is currently available.";
            return false;
        }

        private static ColonyJobSelectionDiagnostics CreateDiagnostics(
            SelectionPass pass,
            int examined,
            int total,
            bool passCompleted,
            ColonyJobId selectedJobId) =>
            new(examined, total, pass.NextIndex, passCompleted, selectedJobId,
                pass.EligibleCandidatesSeen, pass.ReachableCandidatesSeen,
                pass.UnavailableRejected, pass.DisabledRejected,
                pass.EligibilityRejected, pass.UnreachableRejected);

        private sealed class SelectionPass
        {
            public SelectionPass(
                ColonyJobRegistry registry,
                GridNavigationMap map,
                Vector2Int start,
                int priorityRevision)
            {
                Registry = registry;
                Map = map;
                Start = start;
                CursorRevision = registry.CursorRevision;
                PriorityRevision = priorityRevision;
                for (int index = 0; index < BestCosts.Length; index++)
                    BestCosts[index] = float.PositiveInfinity;
            }

            public ColonyJobRegistry Registry { get; }
            public GridNavigationMap Map { get; }
            public Vector2Int Start { get; }
            public int CursorRevision { get; }
            public int PriorityRevision { get; }
            public int NextIndex { get; set; }
            public int EligibleCandidatesSeen { get; set; }
            public int ReachableCandidatesSeen { get; set; }
            public int UnavailableRejected { get; set; }
            public int DisabledRejected { get; set; }
            public int EligibilityRejected { get; set; }
            public int UnreachableRejected { get; set; }
            public ColonyJob[] BestJobs { get; } = new ColonyJob[4];
            public Vector2Int[] BestApproaches { get; } = new Vector2Int[4];
            public float[] BestCosts { get; } = new float[4];

            public bool Matches(
                ColonyJobRegistry registry,
                GridNavigationMap map,
                Vector2Int start,
                int priorityRevision) =>
                ReferenceEquals(Registry, registry) && ReferenceEquals(Map, map) &&
                Start == start && CursorRevision == registry.CursorRevision &&
                PriorityRevision == priorityRevision;
        }

        private static int Compare(ColonyJob first, ColonyJob second)
        {
            if (second == null) return -1;
            int category = first.Category.CompareTo(second.Category);
            if (category != 0) return category;
            int y = first.TargetCell.y.CompareTo(second.TargetCell.y);
            if (y != 0) return y;
            int x = first.TargetCell.x.CompareTo(second.TargetCell.x);
            return x != 0 ? x : string.CompareOrdinal(first.Id.Value, second.Id.Value);
        }
    }

    /// <summary>Inspectable per-ant coordinator state; authoritative progress stays on the job.</summary>
    public sealed class AutonomousWorkAgentState
    {
        public ColonyJobId? JobId { get; internal set; }
        public string Status { get; internal set; } = "Autonomous colony work disabled.";
        public string FailureReason { get; internal set; } = string.Empty;
        public double NextEvaluationTime { get; internal set; }
        public int ConsecutiveFailures { get; internal set; }

        internal void Reset(string status = "Waiting for colony work evaluation.")
        {
            JobId = null;
            Status = status;
            FailureReason = string.Empty;
            NextEvaluationTime = 0d;
            ConsecutiveFailures = 0;
        }
    }
}
