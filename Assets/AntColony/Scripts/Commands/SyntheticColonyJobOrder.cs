using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum SyntheticJobOrderPhase
    {
        Travelling,
        Working,
        Completed,
        Cancelled,
        Failed
    }

    public sealed class SyntheticJobSettings
    {
        public static SyntheticJobSettings Provisional { get; } = new();

        public SyntheticJobSettings(
            double baseDurationHours = 4d,
            double skillSpeedPerLevel = 0.005d,
            double baseExperiencePerCompletedJob = 20d)
        {
            BaseDurationHours = baseDurationHours;
            SkillSpeedPerLevel = skillSpeedPerLevel;
            BaseExperiencePerCompletedJob = baseExperiencePerCompletedJob;
        }

        public double BaseDurationHours { get; }
        public double SkillSpeedPerLevel { get; }
        public double BaseExperiencePerCompletedJob { get; }
    }

    /// <summary>
    /// Development-only execution of one registry-owned job. It moves through the
    /// production motor but never mutates terrain, resources or inventories.
    /// </summary>
    public sealed class SyntheticColonyJobOrder
    {
        private readonly ColonyJobRegistry registry;
        private readonly GridNavigationMap map;
        private readonly AntRecord ant;
        private readonly SyntheticJobSettings settings;

        private SyntheticColonyJobOrder(
            ColonyJobRegistry registry,
            GridNavigationMap map,
            AntRecord ant,
            ColonyJob job,
            Vector2Int approachCell,
            SyntheticJobSettings settings)
        {
            this.registry = registry;
            this.map = map;
            this.ant = ant;
            this.settings = settings;
            Job = job;
            ApproachCell = approachCell;
            Phase = SyntheticJobOrderPhase.Travelling;
            Status = $"Travelling to {job.Category} job {job.Id}.";
        }

        public ColonyJob Job { get; }
        public Vector2Int ApproachCell { get; }
        public SyntheticJobOrderPhase Phase { get; private set; }
        public string Status { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive =>
            Phase == SyntheticJobOrderPhase.Travelling ||
            Phase == SyntheticJobOrderPhase.Working;

        public static bool TryCreate(
            ColonyJobRegistry registry,
            GridNavigationMap map,
            AntRecord ant,
            ColonyJob job,
            Vector2Int approachCell,
            out SyntheticColonyJobOrder order,
            out string error,
            SyntheticJobSettings settings = null)
        {
            order = null;
            if (registry == null || map == null || ant == null || job == null ||
                !ant.CanAcceptMovementOrders)
            {
                error = "Synthetic work requires an active ant, job and navigation world.";
                return false;
            }
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, approachCell, job.TargetCell))
            {
                error = "The synthetic job has no valid adjacent work position.";
                return false;
            }
            if (!registry.TryReserve(job.Id, ant.Id, out error)) return false;

            order = new SyntheticColonyJobOrder(
                registry, map, ant, job, approachCell,
                settings ?? SyntheticJobSettings.Provisional);
            return true;
        }

        public bool TryBegin(Vector2Int currentCell)
        {
            if (Phase != SyntheticJobOrderPhase.Travelling) return false;
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, currentCell, Job.TargetCell))
            {
                FailBlocked("The synthetic job no longer has a valid adjacent work position.");
                return false;
            }
            if (!registry.TryBegin(Job.Id, ant.Id, out string error))
            {
                FailPaused(error);
                return false;
            }
            Phase = SyntheticJobOrderPhase.Working;
            Status = $"Performing synthetic {Job.Category} work.";
            return true;
        }

        public void Advance(double elapsedHours, double simulationHours)
        {
            if (Phase != SyntheticJobOrderPhase.Working || elapsedHours <= 0d) return;
            if (!ant.CanAcceptMovementOrders || ant.Needs.HasDepletedHealth)
            {
                FailPaused("Synthetic work stopped because the ant is inactive.");
                return;
            }
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, ApproachCell, Job.TargetCell))
            {
                FailBlocked("Terrain changed and invalidated the synthetic work position.");
                return;
            }

            AntWorkCategoryDefinition definition =
                AntWorkCategoryCatalog.Get(Job.Category);
            int skillLevel = definition.Skill.HasValue
                ? ant.Skills.Get(definition.Skill.Value).Level
                : 0;
            double skillSpeed = 1d + skillLevel * settings.SkillSpeedPerLevel;
            double progress = elapsedHours / settings.BaseDurationHours *
                skillSpeed * ant.Needs.WorkSpeedMultiplier;
            if (!registry.TryAddProgress(
                    Job.Id, ant.Id, progress, out double applied, out string error))
            {
                FailPaused(error);
                return;
            }

            // XP is tied to authoritative completed progress, never reservation or travel.
            if (applied > 0d && definition.Skill.HasValue)
            {
                ant.TryAwardSkillExperience(
                    definition.Skill.Value,
                    settings.BaseExperiencePerCompletedJob * applied,
                    1d,
                    simulationHours,
                    out _, out _);
            }

            if (Job.Status == ColonyJobStatus.Completed)
            {
                Phase = SyntheticJobOrderPhase.Completed;
                Status = $"Synthetic {Job.Category} job completed.";
            }
            else
            {
                Status = $"Synthetic {Job.Category} progress: {Job.Progress:P0}.";
            }
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            registry.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = SyntheticJobOrderPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = "Synthetic job paused; progress preserved.";
        }

        private void FailPaused(string reason)
        {
            registry.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = SyntheticJobOrderPhase.Failed;
            FailureReason = reason;
            Status = "Synthetic job stopped; progress preserved.";
        }

        private void FailBlocked(string reason)
        {
            registry.BlockAndRelease(Job.Id, ant.Id, reason);
            Phase = SyntheticJobOrderPhase.Failed;
            FailureReason = reason;
            Status = "Synthetic job blocked; progress preserved.";
        }
    }
}
