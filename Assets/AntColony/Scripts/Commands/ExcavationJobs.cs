using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum ExcavationDesignationState
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }

    public sealed class ExcavationDesignation
    {
        internal ExcavationDesignation(Vector2Int cell, string solidMaterialId,
            ExcavatedMaterialType outputMaterial, int outputQuantity, ColonyJob job)
        {
            Cell = cell;
            SolidMaterialId = solidMaterialId;
            OutputMaterial = outputMaterial;
            OutputQuantity = outputQuantity;
            Job = job;
        }

        public Vector2Int Cell { get; }
        public string SolidMaterialId { get; }
        public ExcavatedMaterialType OutputMaterial { get; }
        public int OutputQuantity { get; }
        public ColonyJob Job { get; }
        public ExcavationDesignationState State => Job.Status switch
        {
            ColonyJobStatus.Reserved or ColonyJobStatus.InProgress =>
                ExcavationDesignationState.Active,
            ColonyJobStatus.Completed => ExcavationDesignationState.Completed,
            ColonyJobStatus.Cancelled => ExcavationDesignationState.Cancelled,
            _ => ExcavationDesignationState.Pending
        };
    }

    /// <summary>
    /// Player designations are a typed view over the shared ColonyJobRegistry;
    /// they never form a second work queue.
    /// </summary>
    public sealed class ExcavationDesignationRegistry
    {
        public const int MaximumAreaCells = 256;
        private readonly ChunkedWorldData world;
        private readonly ColonyJobRegistry jobs;
        private readonly ColonyResourceNodeRegistry resourceNodes;
        private readonly Dictionary<Vector2Int, ExcavationDesignation> byCell = new();
        private readonly Dictionary<ColonyJobId, ExcavationDesignation> byJob = new();

        public ExcavationDesignationRegistry(ChunkedWorldData world,
            ColonyJobRegistry jobs, ColonyResourceNodeRegistry resourceNodes = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            this.resourceNodes = resourceNodes;
        }

        public IEnumerable<ExcavationDesignation> Designations => byCell.Values;
        public int Count => byCell.Count;

        /// <summary>
        /// Uses the same rules as designation commit without creating a job.
        /// Presentation calls this so preview colours cannot drift from gameplay.
        /// </summary>
        public DesignationPreviewState EvaluateCell(Vector2Int cell,
            out string reason)
        {
            if (byCell.TryGetValue(cell, out ExcavationDesignation existing) &&
                existing.State != ExcavationDesignationState.Cancelled)
            {
                reason = $"An excavation designation already exists at {cell}.";
                return DesignationPreviewState.ExistingDesignation;
            }
            if (world.GetCell(cell, out WorldTileCellData cellData) !=
                    WorldCellQueryResult.Available)
            {
                reason = $"Cell {cell} is outside the resident Colony world.";
                return DesignationPreviewState.Invalid;
            }
            if (!cellData.HasSolidTile)
            {
                reason = $"Cell {cell} has natural Background but no excavatable foreground Resource Node.";
                return DesignationPreviewState.Invalid;
            }
            if (!world.TryGetSolidMaterialDefinition(cellData.SolidMaterialId,
                    out SolidTileMaterialDefinition definition))
            {
                reason = $"Foreground Solid '{cellData.SolidMaterialId}' is not registered.";
                return DesignationPreviewState.Invalid;
            }
            if (!definition.IsExcavatable ||
                definition.ExcavatedMaterial == ExcavatedMaterialType.None ||
                definition.ExcavationYield <= 0)
            {
                reason = $"Foreground Solid '{cellData.SolidMaterialId}' is not a resource-bearing excavation material.";
                return DesignationPreviewState.Invalid;
            }

            reason = "Foreground Solid is eligible for excavation.";
            return DesignationPreviewState.Valid;
        }

        public bool TryDesignate(Vector2Int cell, out ExcavationDesignation designation,
            out string error)
        {
            designation = null;
            if (EvaluateCell(cell, out error) != DesignationPreviewState.Valid)
                return false;

            world.GetCell(cell, out WorldTileCellData cellData);
            world.TryGetSolidMaterialDefinition(cellData.SolidMaterialId,
                out SolidTileMaterialDefinition definition);

            ColonyJobId id = new($"excavation.{cell.x}.{cell.y}");
            if (!jobs.TryAdd(id, AntWorkCategory.Extraction, cell,
                    out ColonyJob job, out error)) return false;
            int outputQuantity = definition.ExcavationYield;
            if (resourceNodes != null && TryGetResourceNode(cell, out var node))
                outputQuantity = node.RemainingQuantity;
            designation = new ExcavationDesignation(cell, cellData.SolidMaterialId,
                definition.ExcavatedMaterial, outputQuantity, job);
            byCell[cell] = designation;
            byJob[id] = designation;
            return true;
        }

        public int DesignateArea(Vector2Int first, Vector2Int second,
            out string summary)
        {
            int minX = Mathf.Min(first.x, second.x);
            int maxX = Mathf.Max(first.x, second.x);
            int minY = Mathf.Min(first.y, second.y);
            int maxY = Mathf.Max(first.y, second.y);
            int considered = 0;
            int added = 0;
            for (int y = minY; y <= maxY && considered < MaximumAreaCells; y++)
            for (int x = minX; x <= maxX && considered < MaximumAreaCells; x++)
            {
                considered++;
                if (TryDesignate(new Vector2Int(x, y), out _, out _)) added++;
            }
            summary = $"Created {added} excavation designation(s) from {considered} cell(s).";
            return added;
        }

        public bool TryCancel(Vector2Int cell, out string error)
        {
            if (!byCell.TryGetValue(cell, out ExcavationDesignation designation))
            {
                error = $"No excavation designation exists at {cell}.";
                return false;
            }
            return jobs.TryCancel(designation.Job.Id,
                "Player cancelled the excavation designation.", out error);
        }

        public bool TryGet(ColonyJobId jobId, out ExcavationDesignation designation) =>
            byJob.TryGetValue(jobId, out designation);

        /// <summary>
        /// Designation remains legal for inspection, but generated multi-unit
        /// Nodes stay unowned until the whole-node transaction exists.
        /// </summary>
        public bool CanExecuteLegacyExtraction(ColonyJobId jobId, out string reason)
        {
            if (!byJob.TryGetValue(jobId, out ExcavationDesignation designation))
            { reason = "The Extraction designation is unavailable."; return false; }
            if (resourceNodes == null)
            {
                reason = string.Empty;
                return true;
            }
            return resourceNodes.CanExtract(designation.Cell, out reason);
        }

        public bool TryGetResourceNode(Vector2Int cell,
            out ColonyResourceNodeSnapshot node)
        {
            node = null;
            if (resourceNodes == null) return false;
            RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(cell.x, cell.y));
            return resourceNodes.TryGet(cellId, out node) ==
                ColonyResourceNodeReadResult.Success;
        }

        public int DeferredVariableExtractionCount
        {
            get
            {
                int count = 0;
                foreach (ExcavationDesignation designation in byCell.Values)
                    if (designation.State is ExcavationDesignationState.Pending &&
                        TryGetResourceNode(designation.Cell, out var node) &&
                        node.ExtractionStatus == ColonyResourceNodeExtractionStatus
                            .DeferredUntilWholeNodeExtraction)
                        count++;
                return count;
            }
        }
    }

    public enum ExcavationOrderPhase
    {
        Travelling,
        Working,
        Completed,
        Cancelled,
        Failed
    }

    public sealed class ExcavationJobSettings
    {
        public static ExcavationJobSettings Provisional { get; } = new();
        public ExcavationJobSettings(double baseDurationHours = 4d,
            double skillSpeedPerLevel = 0.005d,
            double baseExperiencePerBlock = 20d)
        {
            BaseDurationHours = baseDurationHours;
            SkillSpeedPerLevel = skillSpeedPerLevel;
            BaseExperiencePerBlock = baseExperiencePerBlock;
        }
        public double BaseDurationHours { get; }
        public double SkillSpeedPerLevel { get; }
        public double BaseExperiencePerBlock { get; }
    }

    /// <summary>Executes a real excavation job through shared navigation and jobs.</summary>
    public sealed class ExcavationJobOrder
    {
        private readonly ColonyJobRegistry jobs;
        private readonly GridNavigationMap map;
        private readonly AntRecord ant;
        private readonly ExcavationDesignation designation;
        private readonly ExcavationWorldTransaction transaction;
        private readonly ExcavationJobSettings settings;

        private ExcavationJobOrder(ColonyJobRegistry jobs, GridNavigationMap map,
            AntRecord ant, ExcavationDesignation designation, Vector2Int approach,
            ExcavationWorldTransaction transaction, ExcavationJobSettings settings)
        {
            this.jobs = jobs; this.map = map; this.ant = ant;
            this.designation = designation; this.transaction = transaction;
            this.settings = settings;
            ApproachCell = approach;
            Phase = ExcavationOrderPhase.Travelling;
            Status = $"Travelling to excavation at {designation.Cell}.";
        }

        public ColonyJob Job => designation.Job;
        public Vector2Int ApproachCell { get; }
        public ExcavationOrderPhase Phase { get; private set; }
        public string Status { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive => Phase is ExcavationOrderPhase.Travelling or
            ExcavationOrderPhase.Working;

        public static bool TryCreate(ColonyJobRegistry jobs, GridNavigationMap map,
            AntRecord ant, ExcavationDesignation designation, Vector2Int approach,
            ExcavationWorldTransaction transaction, out ExcavationJobOrder order,
            out string error, ExcavationJobSettings settings = null)
        {
            order = null;
            if (jobs == null || map == null || ant == null || designation == null ||
                transaction == null || !ant.CanAcceptMovementOrders)
            {
                error = "Extraction requires an active ant, designation and world.";
                return false;
            }
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, approach, designation.Cell))
            {
                error = "Extraction requires a reachable adjacent work position.";
                return false;
            }
            if (!jobs.TryReserve(designation.Job.Id, ant.Id, out error)) return false;
            order = new ExcavationJobOrder(jobs, map, ant, designation, approach,
                transaction, settings ?? ExcavationJobSettings.Provisional);
            return true;
        }

        public bool TryBegin(Vector2Int currentCell)
        {
            if (Phase != ExcavationOrderPhase.Travelling) return false;
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, currentCell, designation.Cell))
            {
                Block("The excavation work position is no longer valid.");
                return false;
            }
            if (!jobs.TryBegin(Job.Id, ant.Id, out string error))
            {
                Fail(error); return false;
            }
            Phase = ExcavationOrderPhase.Working;
            Status = $"Extracting {designation.SolidMaterialId}.";
            return true;
        }

        public void Advance(double elapsedHours, double simulationHours)
        {
            if (Phase != ExcavationOrderPhase.Working || elapsedHours <= 0d) return;
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, ApproachCell, designation.Cell))
            {
                Block("Terrain changed and invalidated the excavation position.");
                return;
            }
            int level = ant.Skills.Get(AntSkillId.Extraction).Level;
            double requested = elapsedHours / settings.BaseDurationHours *
                (1d + level * settings.SkillSpeedPerLevel) *
                ant.Needs.WorkSpeedMultiplier;
            double remaining = 1d - Job.Progress;

            // The final work increment is not recorded until the physical output
            // transaction succeeds. A failure therefore preserves both terrain
            // and the resumable pre-completion progress.
            if (requested >= remaining &&
                !transaction.TryCommit(Job.Id.Value, designation.Cell,
                    designation.SolidMaterialId, ApproachCell, out _,
                    out string commitError))
            {
                Fail(commitError);
                return;
            }
            if (!jobs.TryAddProgress(Job.Id, ant.Id, requested,
                    out double applied, out string error))
            {
                Fail(error); return;
            }
            if (applied > 0d)
                ant.TryAwardSkillExperience(AntSkillId.Extraction,
                    settings.BaseExperiencePerBlock * applied, 1d,
                    simulationHours, out _, out _);
            if (Job.Status == ColonyJobStatus.Completed)
            {
                Phase = ExcavationOrderPhase.Completed;
                Status = $"Extraction completed at {designation.Cell}.";
            }
            else Status = $"Extraction progress: {Job.Progress:P0}.";
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = ExcavationOrderPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = "Extraction paused; progress preserved.";
        }

        private void Fail(string reason)
        {
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = ExcavationOrderPhase.Failed;
            FailureReason = reason;
            Status = "Extraction stopped; progress preserved.";
        }

        private void Block(string reason)
        {
            jobs.BlockAndRelease(Job.Id, ant.Id, reason);
            Phase = ExcavationOrderPhase.Failed;
            FailureReason = reason;
            Status = "Extraction blocked; progress preserved.";
        }
    }
}
