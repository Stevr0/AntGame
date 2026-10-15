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
        public IEnumerable<ColonyJob> Jobs => jobs.Values;
        public int Count => jobs.Count;

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
                jobs.Remove(id);
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

        public void Clear() => jobs.Clear();

        public void RemoveWhere(Predicate<ColonyJob> shouldRemove)
        {
            if (shouldRemove == null) return;
            var ids = new List<ColonyJobId>();
            foreach (ColonyJob job in jobs.Values)
                if (shouldRemove(job)) ids.Add(job.Id);
            foreach (ColonyJobId id in ids) jobs.Remove(id);
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

    public sealed class ColonyJobSelectionService
    {
        public const int MaximumCandidatesPerEvaluation = 32;
        private readonly List<ColonyJob> candidates = new(MaximumCandidatesPerEvaluation);

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
            if (ant == null || registry == null || map == null)
            {
                error = "Job selection requires an ant, colony registry and navigation map.";
                return false;
            }

            for (int priority = 1; priority <= 4; priority++)
            {
                candidates.Clear();
                foreach (ColonyJob job in registry.Jobs)
                {
                    if (candidates.Count >= MaximumCandidatesPerEvaluation) break;
                    if (job.CanBeClaimed && (eligibility == null || eligibility(job)) &&
                        (int)ant.WorkPriorities.Get(job.Category) == priority)
                        candidates.Add(job);
                }

                float bestCost = float.PositiveInfinity;
                foreach (ColonyJob candidate in candidates)
                {
                    if (!SurvivalInteractionOrder.TryMeasureApproach(
                            map, start, candidate.TargetCell,
                            out Vector2Int candidateApproach,
                            out float cost, out _)) continue;
                    if (cost < bestCost || Mathf.Approximately(cost, bestCost) &&
                        Compare(candidate, selected) < 0)
                    {
                        selected = candidate;
                        approach = candidateApproach;
                        bestCost = cost;
                    }
                }
                if (selected != null)
                {
                    error = string.Empty;
                    return true;
                }
            }

            error = "No reachable enabled colony job is currently available.";
            return false;
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
