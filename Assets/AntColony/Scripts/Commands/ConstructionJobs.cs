using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    /// <summary>One intentionally small, provisional Background conversion recipe.</summary>
    public sealed class ConstructionRecipe
    {
        public ConstructionRecipe(string stableId, string requiredSourceMaterialId,
            BackgroundTileMaterialDefinition result, ExcavatedMaterialType material,
            int quantity, double baseDurationHours)
        {
            if (string.IsNullOrWhiteSpace(stableId) || result == null ||
                material == ExcavatedMaterialType.None || quantity <= 0 ||
                baseDurationHours <= 0d)
                throw new ArgumentException("Construction recipes require a stable ID, result, material, quantity and duration.");
            StableId = stableId.Trim();
            RequiredSourceMaterialId = requiredSourceMaterialId?.Trim() ?? string.Empty;
            Result = result;
            ResourceTypeId = MaterialResourceCatalog.GetStableId(material);
            RequiredQuantity = quantity;
            BaseDurationHours = baseDurationHours;
        }

        public string StableId { get; }
        public string RequiredSourceMaterialId { get; }
        public BackgroundTileMaterialDefinition Result { get; }
        public string ResourceTypeId { get; }
        public int RequiredQuantity { get; }
        public double BaseDurationHours { get; }
        public bool AcceptsSource(string materialId) =>
            string.IsNullOrEmpty(RequiredSourceMaterialId) ||
            string.Equals(RequiredSourceMaterialId, materialId, StringComparison.Ordinal);
    }

    public sealed class ConstructionRecipeCatalog
    {
        private readonly Dictionary<string, ConstructionRecipe> byTarget =
            new(StringComparer.Ordinal);
        private readonly List<ConstructionRecipe> ordered = new();

        public IReadOnlyList<ConstructionRecipe> Recipes => ordered;

        public ConstructionRecipeCatalog(IEnumerable<ConstructionRecipe> recipes)
        {
            if (recipes == null) return;
            foreach (ConstructionRecipe recipe in recipes)
            {
                if (recipe == null || !byTarget.TryAdd(
                        recipe.Result.StableMaterialId, recipe))
                    throw new ArgumentException("Construction recipe targets must be unique.");
                ordered.Add(recipe);
            }
        }

        public bool TryGet(string targetMaterialId, out ConstructionRecipe recipe) =>
            byTarget.TryGetValue(targetMaterialId ?? string.Empty, out recipe);
    }

    public enum ConstructionDesignationState
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Authoritative physical staging owned by one world cell. Delivered material
    /// is no longer in an ant inventory, but remains recoverable at this site.
    /// </summary>
    public sealed class ConstructionSite
    {
        internal ConstructionSite(Vector2Int cell, string resourceTypeId)
        {
            Cell = cell;
            ResourceTypeId = resourceTypeId;
        }

        public Vector2Int Cell { get; }
        public string ResourceTypeId { get; }
        public int DeliveredQuantity { get; private set; }

        public bool TryStage(AntInventory inventory, int quantity, int maximumStack,
            out string error)
        {
            if (inventory == null || quantity <= 0)
            {
                error = "Construction staging requires an inventory and positive quantity.";
                return false;
            }
            if (!inventory.TryRemoveResource(ResourceTypeId, quantity,
                    maximumStack, out error)) return false;
            DeliveredQuantity += quantity;
            error = string.Empty;
            return true;
        }

        internal bool CanConsume(int quantity) => quantity > 0 &&
            DeliveredQuantity >= quantity;

        internal void CommitConsumption(int quantity)
        {
            DeliveredQuantity -= quantity;
        }
    }

    public sealed class ConstructionDesignation
    {
        internal ConstructionDesignation(Vector2Int cell, string sourceMaterialId,
            ConstructionRecipe recipe, ColonyJob job)
        {
            Cell = cell;
            SourceMaterialId = sourceMaterialId;
            Recipe = recipe;
            Job = job;
            Site = new ConstructionSite(cell, recipe.ResourceTypeId);
        }

        public Vector2Int Cell { get; }
        public string SourceMaterialId { get; }
        public ConstructionRecipe Recipe { get; }
        public ColonyJob Job { get; }
        public ConstructionSite Site { get; }
        public ConstructionDesignationState State => Job.Status switch
        {
            ColonyJobStatus.Reserved or ColonyJobStatus.InProgress =>
                ConstructionDesignationState.Active,
            ColonyJobStatus.Completed => ConstructionDesignationState.Completed,
            ColonyJobStatus.Cancelled => ConstructionDesignationState.Cancelled,
            _ => ConstructionDesignationState.Pending
        };
    }

    /// <summary>
    /// Typed designation view over the shared job registry. It owns no scheduler.
    /// </summary>
    public sealed class ConstructionDesignationRegistry
    {
        public const int MaximumAreaCells = 256;
        private readonly ChunkedWorldData world;
        private readonly GridNavigationMap map;
        private readonly ColonyJobRegistry jobs;
        private readonly ConstructionRecipeCatalog recipes;
        private readonly SurvivalInteractionWorld survival;
        private readonly MaterialStorageWorld materialStorage;
        private readonly WorldMaterialDropRegistry drops;
        private readonly Dictionary<Vector2Int, ConstructionDesignation> byCell = new();
        private readonly Dictionary<ColonyJobId, ConstructionDesignation> byJob = new();

        public ConstructionDesignationRegistry(ChunkedWorldData world,
            GridNavigationMap map, ColonyJobRegistry jobs,
            ConstructionRecipeCatalog recipes, SurvivalInteractionWorld survival,
            MaterialStorageWorld materialStorage,
            WorldMaterialDropRegistry drops = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            this.recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            this.survival = survival;
            this.materialStorage = materialStorage;
            this.drops = drops;
        }

        public IEnumerable<ConstructionDesignation> Designations => byCell.Values;
        public int Count => byCell.Count;

        public bool TryDesignate(Vector2Int cell, string targetMaterialId,
            out ConstructionDesignation designation, out string error)
        {
            designation = null;
            if (!recipes.TryGet(targetMaterialId, out ConstructionRecipe recipe))
            {
                error = "The selected Background conversion has no recipe.";
                return false;
            }
            if (byCell.TryGetValue(cell, out ConstructionDesignation existing) &&
                existing.State is not ConstructionDesignationState.Cancelled)
            {
                error = $"An active Background conversion already exists at {cell}.";
                return false;
            }
            if (world.GetCell(cell, out WorldTileCellData data) !=
                    WorldCellQueryResult.Available)
            {
                error = "Background conversion requires a resident world cell.";
                return false;
            }
            if (data.HasSolidTile)
            {
                error = "Covered foreground Solid cells cannot be converted.";
                return false;
            }
            if (data.BackgroundMaterialId == targetMaterialId)
            {
                error = "The cell already has the selected Background type.";
                return false;
            }
            if (!recipe.AcceptsSource(data.BackgroundMaterialId))
            {
                error = "The current Background does not satisfy this recipe.";
                return false;
            }
            if (!HasAdjacentAnchor(cell))
            {
                error = "Background conversion requires an accessible adjacent work cell.";
                return false;
            }
            if (IsOccupied(cell))
            {
                error = "Occupied functional Background capacity cannot be converted.";
                return false;
            }

            ColonyJobId id = new($"construction.{cell.x}.{cell.y}");
            if (!jobs.TryAdd(id, AntWorkCategory.Construction, cell,
                    out ColonyJob job, out error)) return false;
            designation = new ConstructionDesignation(
                cell, data.BackgroundMaterialId, recipe, job);
            byCell[cell] = designation;
            byJob[id] = designation;
            return true;
        }

        public int DesignateArea(Vector2Int first, Vector2Int second,
            string targetMaterialId, out string summary)
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
                if (TryDesignate(new Vector2Int(x, y), targetMaterialId,
                        out _, out _)) added++;
            }
            summary = $"Created {added} Background conversion designation(s) from {considered} cell(s).";
            return added;
        }

        public bool TryCancel(Vector2Int cell, out string error)
        {
            if (!byCell.TryGetValue(cell, out ConstructionDesignation designation))
            {
                error = $"No Background conversion exists at {cell}.";
                return false;
            }
            if (designation.Site.DeliveredQuantity > 0 && drops != null)
            {
                if (!MaterialResourceCatalog.TryGetMaterial(
                        designation.Site.ResourceTypeId,
                        out ExcavatedMaterialType material))
                {
                    error = "The staged construction material type is invalid.";
                    return false;
                }
                if (!drops.TryAdd($"construction.cancelled.{cell.x}.{cell.y}",
                        material, designation.Site.DeliveredQuantity, cell,
                        out _, out error))
                    return false;
                designation.Site.CommitConsumption(
                    designation.Site.DeliveredQuantity);
            }
            return jobs.TryCancel(designation.Job.Id,
                "Player cancelled the Background conversion.", out error);
        }

        public bool TryGet(ColonyJobId id, out ConstructionDesignation designation) =>
            byJob.TryGetValue(id, out designation);

        public bool IsOccupied(Vector2Int cell) =>
            survival?.IsFunctionOccupied(cell) == true ||
            materialStorage?.IsOccupied(cell) == true;

        private bool HasAdjacentAnchor(Vector2Int cell)
        {
            foreach (Vector2Int direction in CardinalDirections)
                if (map.TryGetAnchor(cell + direction, out _)) return true;
            return false;
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down
        };
    }

    /// <summary>Atomic validation and Background replacement boundary.</summary>
    public sealed class ConstructionWorldTransaction
    {
        private readonly ChunkedWorldData world;
        private readonly SurvivalInteractionWorld survival;
        private readonly MaterialStorageWorld materialStorage;
        private readonly Action<Vector2Int> committed;
        private readonly HashSet<string> committedJobs = new(StringComparer.Ordinal);

        public ConstructionWorldTransaction(ChunkedWorldData world,
            SurvivalInteractionWorld survival, MaterialStorageWorld materialStorage,
            Action<Vector2Int> committed = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.survival = survival;
            this.materialStorage = materialStorage;
            this.committed = committed;
        }

        public bool TryCommit(ConstructionDesignation designation, out string error)
        {
            if (designation == null || committedJobs.Contains(designation.Job.Id.Value))
            {
                error = "This construction completion was already committed.";
                return false;
            }
            ConstructionRecipe recipe = designation.Recipe;
            if (world.GetCell(designation.Cell, out WorldTileCellData data) !=
                    WorldCellQueryResult.Available || data.HasSolidTile ||
                data.BackgroundMaterialId != designation.SourceMaterialId ||
                !recipe.AcceptsSource(data.BackgroundMaterialId) ||
                !world.TryGetBackgroundMaterialDefinition(data.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition sourceDefinition))
            {
                error = "The construction site's source Background is no longer valid.";
                return false;
            }
            if (survival?.IsFunctionOccupied(designation.Cell) == true ||
                materialStorage?.IsOccupied(designation.Cell) == true)
            {
                error = "Occupied functional capacity prevents Background conversion.";
                return false;
            }
            if (!designation.Site.CanConsume(recipe.RequiredQuantity))
            {
                error = "The construction site has not received all required material.";
                return false;
            }

            survival?.RemoveEmptyFunction(designation.Cell);
            materialStorage?.Remove(designation.Cell);
            if (world.SetBackgroundMaterial(designation.Cell,
                    recipe.Result.StableMaterialId) != WorldCellMutationResult.Success)
            {
                error = "The resulting Background could not be written.";
                return false;
            }

            bool registered = RegisterCapacity(recipe.Result.Function,
                designation.Cell, out error);
            if (!registered)
            {
                world.SetBackgroundMaterial(designation.Cell,
                    designation.SourceMaterialId);
                RegisterCapacity(sourceDefinition.Function,
                    designation.Cell, out _);
                return false;
            }

            designation.Site.CommitConsumption(recipe.RequiredQuantity);
            committedJobs.Add(designation.Job.Id.Value);
            committed?.Invoke(designation.Cell);
            error = string.Empty;
            return true;
        }

        private bool RegisterCapacity(BackgroundFunction function, Vector2Int cell,
            out string error)
        {
            if (function == BackgroundFunction.MaterialStorage)
            {
                if (materialStorage == null)
                {
                    error = "Material Storage authority is unavailable.";
                    return false;
                }
                return materialStorage.TryAdd(cell,
                    MaterialStorageFilter.AllBuildingMaterials, string.Empty, 0,
                    MaterialResourceCatalog.ProvisionalMaximumStack, out error);
            }
            if (survival == null)
            {
                error = string.Empty;
                return true;
            }
            return survival.TryRegisterConstructedFunction(cell, function, out error);
        }
    }

    public enum ConstructionOrderPhase
    {
        UnloadingInventory,
        TravellingToSource,
        TravellingToSite,
        Working,
        Completed,
        Cancelled,
        Failed
    }

    public enum InventoryCapacityRecoveryPhase
    {
        TravellingToStorage,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Reusable, task-owned operation which frees inventory space without losing
    /// physical resources. The caller names the resource its current task needs;
    /// that resource is never selected for unloading.
    /// </summary>
    public sealed class InventoryCapacityRecoveryOperation
    {
        public const int MaximumRecoveryAttempts = 8;
        private readonly AntInventory inventory;
        private readonly MaterialStorageWorld storage;
        private readonly WorldMaterialDropRegistry drops;
        private readonly GridNavigationMap map;
        private readonly string ownerId;
        private readonly string operationKey;
        private readonly string protectedResourceTypeId;
        private readonly int protectedMaximumStack;
        private readonly int requiredCapacity;
        private Vector2Int storageCell;
        private string unloadResourceTypeId = string.Empty;
        private int unloadMaximumStack;
        private int unloadQuantity;

        private InventoryCapacityRecoveryOperation(AntInventory inventory,
            MaterialStorageWorld storage, WorldMaterialDropRegistry drops,
            GridNavigationMap map, string ownerId, string operationKey,
            string protectedResourceTypeId,
            int protectedMaximumStack, int requiredCapacity)
        {
            this.inventory = inventory; this.storage = storage; this.drops = drops;
            this.map = map; this.ownerId = ownerId; this.operationKey = operationKey;
            this.protectedResourceTypeId = protectedResourceTypeId;
            this.protectedMaximumStack = protectedMaximumStack;
            this.requiredCapacity = requiredCapacity;
        }

        public InventoryCapacityRecoveryPhase Phase { get; private set; }
        public Vector2Int ApproachCell { get; private set; }
        public int Attempts { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive => Phase == InventoryCapacityRecoveryPhase.TravellingToStorage;

        public static bool TryCreate(AntInventory inventory,
            MaterialStorageWorld storage, WorldMaterialDropRegistry drops,
            GridNavigationMap map, string ownerId, string operationKey,
            string protectedResourceTypeId,
            int protectedMaximumStack, int requiredCapacity, Vector2Int start,
            out InventoryCapacityRecoveryOperation operation, out string error)
        {
            operation = null;
            if (inventory == null || storage == null || drops == null || map == null ||
                string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(operationKey) ||
                string.IsNullOrWhiteSpace(protectedResourceTypeId) ||
                protectedMaximumStack <= 0 || requiredCapacity <= 0)
            {
                error = "Inventory recovery requires a task owner and physical material authorities.";
                return false;
            }
            operation = new InventoryCapacityRecoveryOperation(inventory, storage,
                drops, map, ownerId, operationKey, protectedResourceTypeId,
                protectedMaximumStack, requiredCapacity);
            if (operation.TryPlanNextStep(start, out error)) return true;
            operation = null;
            return false;
        }

        public bool TryArrive(Vector2Int currentCell, out Vector2Int nextApproach,
            out bool capacityReady, out string error)
        {
            nextApproach = default; capacityReady = false;
            if (Phase != InventoryCapacityRecoveryPhase.TravellingToStorage ||
                !SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, currentCell, storageCell))
            {
                error = "The reserved inventory-unload destination is no longer reachable.";
                Fail(error); return false;
            }
            if (!storage.TryCommitReservedDeposit(storageCell,
                    unloadResourceTypeId, unloadMaximumStack, ownerId, unloadQuantity,
                    () => inventory.TryRemoveResource(unloadResourceTypeId,
                        unloadQuantity, unloadMaximumStack, out _), out error))
            {
                Fail(error); return false;
            }
            if (HasRequiredCapacity())
            {
                Phase = InventoryCapacityRecoveryPhase.Completed;
                Status = "Freed inventory capacity; resuming the original task.";
                capacityReady = true; error = string.Empty; return true;
            }
            if (!TryPlanNextStep(currentCell, out error)) return false;
            if (Phase == InventoryCapacityRecoveryPhase.Completed)
            { capacityReady = true; return true; }
            nextApproach = ApproachCell;
            return true;
        }

        public void Cancel(string reason)
        {
            if (Phase == InventoryCapacityRecoveryPhase.TravellingToStorage)
                storage.ReleaseIncoming(storageCell, ownerId);
            Phase = InventoryCapacityRecoveryPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = "Inventory unload interrupted; all uncommitted material remains carried.";
        }

        private bool TryPlanNextStep(Vector2Int start, out string error)
        {
            while (Attempts < MaximumRecoveryAttempts)
            {
                if (HasRequiredCapacity())
                {
                    Phase = InventoryCapacityRecoveryPhase.Completed;
                    Status = "Required inventory capacity is available.";
                    error = string.Empty; return true;
                }
                Attempts++;
                if (!TryChooseUnnecessaryStack(out AntInventorySlot fallbackStack))
                {
                    error = "No unnecessary carried material can be unloaded for this task.";
                    Fail(error); return false;
                }
                if (TryReserveClosestStorage(start, out error)) return true;

                // Storage is preferred. Only when none is reachable and compatible
                // do we use the same validated, conserving transaction as manual drops.
                int slotIndex = FindSlotIndex(fallbackStack.ResourceTypeId,
                    fallbackStack.MaximumStack);
                string dropKey = $"task-unload.{operationKey}.{Attempts:D2}";
                if (slotIndex >= 0 && MaterialDropTransaction.TryDrop(inventory,
                        slotIndex, fallbackStack.Quantity, start, map, drops, dropKey,
                        out _, out error))
                {
                    Status = $"Dropped {fallbackStack.Quantity} unnecessary material unit(s) safely.";
                    continue;
                }
            }
            error = $"Inventory recovery stopped after {MaximumRecoveryAttempts} bounded attempts.";
            Fail(error); return false;
        }

        private bool TryReserveClosestStorage(Vector2Int start, out string error)
        {
            float bestCost = float.PositiveInfinity;
            MaterialStorageSlot best = null;
            AntInventorySlot bestStack = null;
            Vector2Int bestApproach = default;
            foreach (AntInventorySlot stack in inventory.OccupiedSlots)
            {
                if (stack.ResourceTypeId == protectedResourceTypeId &&
                    stack.MaximumStack == protectedMaximumStack) continue;
                foreach (MaterialStorageSlot candidate in storage.Slots)
                {
                    int capacity = storage.GetAvailableCapacity(candidate.Cell,
                        stack.ResourceTypeId, stack.MaximumStack);
                    if (capacity <= 0 || !SurvivalInteractionOrder.TryMeasureApproach(
                            map, start, candidate.Cell, out Vector2Int approach,
                            out float cost, out _) || cost >= bestCost) continue;
                    best = candidate; bestStack = stack;
                    bestCost = cost; bestApproach = approach;
                }
            }
            if (best == null || bestStack == null)
            { error = "No reachable compatible Material Storage has capacity."; return false; }
            if (!storage.TryReserveIncoming(best.Cell, bestStack.ResourceTypeId,
                    bestStack.MaximumStack, ownerId, bestStack.Quantity,
                    out unloadQuantity, out error)) return false;
            storageCell = best.Cell; ApproachCell = bestApproach;
            unloadResourceTypeId = bestStack.ResourceTypeId;
            unloadMaximumStack = bestStack.MaximumStack;
            Phase = InventoryCapacityRecoveryPhase.TravellingToStorage;
            Status = $"Travelling to unload {unloadQuantity} unnecessary material unit(s).";
            return true;
        }

        private bool TryChooseUnnecessaryStack(out AntInventorySlot selected)
        {
            selected = null;
            foreach (AntInventorySlot candidate in inventory.OccupiedSlots)
            {
                if (candidate.ResourceTypeId == protectedResourceTypeId &&
                    candidate.MaximumStack == protectedMaximumStack) continue;
                if (selected == null || candidate.Quantity < selected.Quantity)
                    selected = candidate;
            }
            return selected != null;
        }

        private int FindSlotIndex(string resourceTypeId, int maximumStack)
        {
            for (int index = 0; index < inventory.OccupiedSlots.Count; index++)
            {
                AntInventorySlot slot = inventory.OccupiedSlots[index];
                if (slot.ResourceTypeId == resourceTypeId &&
                    slot.MaximumStack == maximumStack) return index;
            }
            return -1;
        }

        private bool HasRequiredCapacity() => inventory.GetAvailableCapacity(
            protectedResourceTypeId, protectedMaximumStack) >= requiredCapacity;

        private void Fail(string reason)
        {
            if (Phase == InventoryCapacityRecoveryPhase.TravellingToStorage)
                storage.ReleaseIncoming(storageCell, ownerId);
            Phase = InventoryCapacityRecoveryPhase.Failed;
            FailureReason = reason ?? string.Empty;
            Status = "Inventory recovery could not safely free capacity.";
        }
    }

    public sealed class ConstructionJobSettings
    {
        public static ConstructionJobSettings Provisional { get; } = new();
        public ConstructionJobSettings(double skillSpeedPerLevel = 0.005d,
            double baseExperiencePerJob = 20d)
        {
            SkillSpeedPerLevel = skillSpeedPerLevel;
            BaseExperiencePerJob = baseExperiencePerJob;
        }
        public double SkillSpeedPerLevel { get; }
        public double BaseExperiencePerJob { get; }
    }

    /// <summary>One builder-owned gather, stage and construct state machine.</summary>
    public sealed class ConstructionJobOrder
    {
        private readonly ColonyJobRegistry jobs;
        private readonly GridNavigationMap map;
        private readonly WorldMaterialDropRegistry drops;
        private readonly MaterialStorageWorld storage;
        private readonly AntRecord ant;
        private readonly ConstructionDesignation designation;
        private readonly ConstructionWorldTransaction transaction;
        private readonly ConstructionJobSettings settings;
        private ConstructionMaterialSourceKind sourceKind;
        private string sourceKey = string.Empty;
        private Vector2Int sourceCell;
        private Vector2Int sourceApproach;
        private int tripQuantity;
        private int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack;
        private InventoryCapacityRecoveryOperation inventoryRecovery;

        private ConstructionJobOrder(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            AntRecord ant, ConstructionDesignation designation,
            Vector2Int siteApproach, ConstructionWorldTransaction transaction,
            ConstructionJobSettings settings)
        {
            this.jobs = jobs; this.map = map; this.drops = drops;
            this.storage = storage; this.ant = ant; this.designation = designation;
            this.transaction = transaction; this.settings = settings;
            SiteApproachCell = siteApproach;
        }

        public ColonyJob Job => designation.Job;
        public ConstructionDesignation Designation => designation;
        public Vector2Int SiteApproachCell { get; }
        public Vector2Int CurrentSourceCell => sourceCell;
        public int TripQuantity => tripQuantity;
        public InventoryCapacityRecoveryOperation InventoryRecovery => inventoryRecovery;
        public ConstructionOrderPhase Phase { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive => Phase is ConstructionOrderPhase.UnloadingInventory or
            ConstructionOrderPhase.TravellingToSource or
            ConstructionOrderPhase.TravellingToSite or ConstructionOrderPhase.Working;
        public Vector2Int InitialApproachCell => Phase ==
            ConstructionOrderPhase.UnloadingInventory
                ? inventoryRecovery.ApproachCell
                : Phase == ConstructionOrderPhase.TravellingToSource
                    ? sourceApproach : SiteApproachCell;

        public static bool TryCreate(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            AntRecord ant, ConstructionDesignation designation,
            Vector2Int siteApproach, Vector2Int start,
            ConstructionWorldTransaction transaction, out ConstructionJobOrder order,
            out string error, ConstructionJobSettings settings = null)
        {
            order = null;
            if (jobs == null || map == null || drops == null || storage == null ||
                ant == null || designation == null || transaction == null ||
                !ant.CanAcceptMovementOrders || ant.Needs.HasDepletedHealth ||
                !SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, siteApproach, designation.Cell))
            {
                error = "Construction requires an active builder, valid site and physical material authorities.";
                return false;
            }
            if (!jobs.TryReserve(designation.Job.Id, ant.Id, out error)) return false;
            order = new ConstructionJobOrder(jobs, map, drops, storage, ant,
                designation, siteApproach, transaction,
                settings ?? ConstructionJobSettings.Provisional);

            int missing = order.GetMissingQuantity();
            if (missing <= 0)
            {
                order.Phase = ConstructionOrderPhase.TravellingToSite;
                order.Status = "Travelling to resume supplied construction.";
                error = string.Empty;
                return true;
            }
            int carried = GetInventoryQuantity(ant.Inventory,
                designation.Recipe.ResourceTypeId,
                MaterialResourceCatalog.ProvisionalMaximumStack);
            if (carried > 0)
            {
                order.tripQuantity = Math.Min(missing, carried);
                order.Phase = ConstructionOrderPhase.TravellingToSite;
                order.Status = $"Carrying {order.tripQuantity} material to the construction site.";
                error = string.Empty;
                return true;
            }
            if (!order.TryReserveNextSource(start, out error))
            {
                jobs.PauseAndRelease(designation.Job.Id, ant.Id, error);
                order = null;
                return false;
            }
            return true;
        }

        public bool TryArrive(Vector2Int currentCell, out Vector2Int nextApproach)
        {
            nextApproach = default;
            if (Phase == ConstructionOrderPhase.UnloadingInventory)
            {
                if (!inventoryRecovery.TryArrive(currentCell, out nextApproach,
                        out bool capacityReady, out string recoveryError))
                { Fail(recoveryError); return false; }
                if (!capacityReady) return true;
                inventoryRecovery = null;
                if (!TryReserveNextSource(currentCell, out string recoveryReserveError))
                { Fail(recoveryReserveError); return false; }
                nextApproach = sourceApproach;
                return true;
            }
            if (Phase == ConstructionOrderPhase.TravellingToSource)
            {
                if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                        map, currentCell, sourceCell))
                {
                    Fail("The construction material source became unreachable.");
                    return false;
                }
                if (!CollectReservedSource(out string error))
                {
                    Fail(error);
                    return false;
                }
                Phase = ConstructionOrderPhase.TravellingToSite;
                Status = $"Carrying {tripQuantity} material to the construction site.";
                nextApproach = SiteApproachCell;
                return true;
            }

            if (Phase != ConstructionOrderPhase.TravellingToSite ||
                !SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, currentCell, designation.Cell))
            {
                Fail("The construction-site approach became invalid.");
                return false;
            }
            if (tripQuantity > 0 &&
                !designation.Site.TryStage(ant.Inventory, tripQuantity,
                    maximumStack, out string stageError))
            {
                Fail(stageError);
                return false;
            }
            tripQuantity = 0;
            if (GetMissingQuantity() <= 0)
            {
                if (!jobs.TryBegin(Job.Id, ant.Id, out string beginError))
                {
                    Fail(beginError);
                    return false;
                }
                Phase = ConstructionOrderPhase.Working;
                Status = "All materials delivered; constructing Background.";
                return false;
            }
            if (!TryReserveNextSource(currentCell, out string reserveError))
            {
                Fail(reserveError);
                return false;
            }
            nextApproach = sourceApproach;
            return true;
        }

        public void Advance(double elapsedHours, double simulationHours)
        {
            if (Phase != ConstructionOrderPhase.Working || elapsedHours <= 0d) return;
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, SiteApproachCell, designation.Cell))
            {
                Fail("Terrain changed and invalidated the construction position.");
                return;
            }
            int level = ant.Skills.Get(AntSkillId.Construction).Level;
            double requested = elapsedHours / designation.Recipe.BaseDurationHours *
                (1d + level * settings.SkillSpeedPerLevel) *
                ant.Needs.WorkSpeedMultiplier;
            double remaining = 1d - Job.Progress;
            if (requested >= remaining && !transaction.TryCommit(
                    designation, out string commitError))
            {
                Fail(commitError);
                return;
            }
            if (!jobs.TryAddProgress(Job.Id, ant.Id, requested,
                    out double applied, out string progressError))
            {
                Fail(progressError);
                return;
            }
            if (applied > 0d)
                ant.TryAwardSkillExperience(AntSkillId.Construction,
                    settings.BaseExperiencePerJob * applied, 1d,
                    simulationHours, out _, out _);
            if (Job.Status == ColonyJobStatus.Completed)
            {
                Phase = ConstructionOrderPhase.Completed;
                Status = $"Converted Background at {designation.Cell}.";
            }
            else Status = $"Construction progress: {Job.Progress:P0}.";
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            ReleaseSourceReservation();
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = ConstructionOrderPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = "Construction paused; progress and staged materials preserved.";
        }

        private bool TryReserveNextSource(Vector2Int start, out string error)
        {
            int missing = GetMissingQuantity();
            int capacity = ant.Inventory.GetAvailableCapacity(
                designation.Recipe.ResourceTypeId,
                MaterialResourceCatalog.ProvisionalMaximumStack);
            if (missing <= 0)
            {
                error = "The construction site already has its required material.";
                return false;
            }
            if (capacity <= 0)
            {
                if (!InventoryCapacityRecoveryOperation.TryCreate(ant.Inventory,
                        storage, drops, map, ant.Id.Value,
                        designation.Job.Id.Value,
                        designation.Recipe.ResourceTypeId,
                        MaterialResourceCatalog.ProvisionalMaximumStack, 1, start,
                        out inventoryRecovery, out error)) return false;
                if (inventoryRecovery.Phase == InventoryCapacityRecoveryPhase.Completed)
                {
                    inventoryRecovery = null;
                    return TryReserveNextSource(start, out error);
                }
                Phase = ConstructionOrderPhase.UnloadingInventory;
                Status = inventoryRecovery.Status;
                return true;
            }

            float bestCost = float.PositiveInfinity;
            ConstructionMaterialSourceKind bestKind = default;
            string bestKey = string.Empty;
            Vector2Int bestCell = default;
            Vector2Int bestApproach = default;
            int available = 0;
            foreach (MaterialStorageSlot slot in storage.Slots)
            {
                if (slot.ResourceTypeId != designation.Recipe.ResourceTypeId ||
                    slot.AvailableQuantity <= 0 ||
                    !SurvivalInteractionOrder.TryMeasureApproach(map, start, slot.Cell,
                        out Vector2Int approach, out float cost, out _)) continue;
                if (cost < bestCost)
                {
                    bestCost = cost; bestKind = ConstructionMaterialSourceKind.Storage;
                    bestCell = slot.Cell; bestApproach = approach;
                    available = slot.AvailableQuantity; maximumStack = slot.MaximumStack;
                }
            }
            foreach (WorldMaterialDrop drop in drops.Drops)
            {
                if (MaterialResourceCatalog.GetStableId(drop.Material) !=
                        designation.Recipe.ResourceTypeId ||
                    drops.GetReservedQuantity(drop.OwnershipKey) > 0 ||
                    !SurvivalInteractionOrder.TryMeasureApproach(map, start,
                        drop.WorldCell, out Vector2Int approach,
                        out float cost, out _)) continue;
                if (cost < bestCost)
                {
                    bestCost = cost; bestKind = ConstructionMaterialSourceKind.Drop;
                    bestKey = drop.OwnershipKey; bestCell = drop.WorldCell;
                    bestApproach = approach; available = drop.Quantity;
                    maximumStack = drop.MaximumStack;
                }
            }
            if (float.IsPositiveInfinity(bestCost))
            {
                error = "No reachable unreserved construction material is available.";
                return false;
            }

            int requested = Math.Min(missing, Math.Min(capacity, available));
            bool reserved = bestKind == ConstructionMaterialSourceKind.Storage
                ? storage.TryReserveWithdrawal(bestCell,
                    designation.Recipe.ResourceTypeId, ant.Id.Value,
                    requested, out tripQuantity, out error)
                : drops.TryReserve(bestKey, ant.Id.Value, requested,
                    out tripQuantity, out error);
            if (!reserved) return false;
            sourceKind = bestKind; sourceKey = bestKey;
            sourceCell = bestCell; sourceApproach = bestApproach;
            Phase = ConstructionOrderPhase.TravellingToSource;
            Status = $"Travelling to collect {tripQuantity} construction material.";
            return true;
        }

        private bool CollectReservedSource(out string error)
        {
            if (sourceKind == ConstructionMaterialSourceKind.Storage)
            {
                if (!storage.TryGet(sourceCell, out MaterialStorageSlot slot))
                {
                    error = "The reserved Material Storage source disappeared.";
                    return false;
                }
                maximumStack = slot.MaximumStack;
                return storage.TryCommitReservedWithdrawal(sourceCell,
                    designation.Recipe.ResourceTypeId, ant.Id.Value, tripQuantity,
                    () => ant.Inventory.TryAdd(designation.Recipe.ResourceTypeId,
                        tripQuantity, maximumStack, false, out int added, out _) &&
                        added == tripQuantity, out _, out error);
            }
            return drops.TryCommitReservedPickup(sourceKey, ant.Id.Value, tripQuantity,
                () => ant.Inventory.TryAdd(designation.Recipe.ResourceTypeId,
                    tripQuantity, maximumStack, false, out int added, out _) &&
                    added == tripQuantity, out _, out error);
        }

        private int GetMissingQuantity() => Math.Max(0,
            designation.Recipe.RequiredQuantity - designation.Site.DeliveredQuantity);

        private void ReleaseSourceReservation()
        {
            inventoryRecovery?.Cancel("Construction inventory recovery stopped.");
            if (sourceKind == ConstructionMaterialSourceKind.Storage)
                storage.ReleaseWithdrawal(sourceCell, ant.Id.Value);
            else if (sourceKind == ConstructionMaterialSourceKind.Drop)
                drops.ReleaseReservation(sourceKey, ant.Id.Value);
        }

        private void Fail(string reason)
        {
            ReleaseSourceReservation();
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = ConstructionOrderPhase.Failed;
            FailureReason = reason;
            Status = "Construction stopped; physical materials and progress were preserved.";
        }

        private static int GetInventoryQuantity(AntInventory inventory,
            string resourceTypeId, int maximumStack)
        {
            int total = 0;
            foreach (AntInventorySlot slot in inventory.OccupiedSlots)
                if (slot.ResourceTypeId == resourceTypeId &&
                    slot.MaximumStack == maximumStack) total += slot.Quantity;
            return total;
        }

        private enum ConstructionMaterialSourceKind
        {
            Storage,
            Drop
        }
    }
}
