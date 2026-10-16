using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum MaterialHaulSourceKind { WorldDrop, PersonalInventory, MaterialStorage }
    public enum MaterialHaulDestinationKind { ConstructionSite, MaterialStorage }
    public enum MaterialHaulingPhase
    {
        TravellingToSource,
        TravellingToDestination,
        [Obsolete("Use TravellingToDestination.")]
        TravellingToStorage = TravellingToDestination,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>Typed meaning attached to a registry-owned Hauling job.</summary>
    public sealed class MaterialHaulingPlan
    {
        public MaterialHaulingPlan(ColonyJob job, MaterialHaulSourceKind sourceKind,
            string resourceTypeId, string dropKey = null,
            AntId inventoryOwner = default, Vector2Int sourceCell = default)
        {
            Job = job; SourceKind = sourceKind; ResourceTypeId = resourceTypeId;
            DropKey = dropKey ?? string.Empty; InventoryOwner = inventoryOwner;
            SourceCell = sourceCell;
        }
        public ColonyJob Job { get; }
        public MaterialHaulSourceKind SourceKind { get; }
        public string ResourceTypeId { get; }
        public string DropKey { get; }
        public AntId InventoryOwner { get; }
        public Vector2Int SourceCell { get; }
    }

    /// <summary>
    /// Executes one bounded multi-source run followed by one destination delivery.
    /// Each pickup has its own replay-safe operation while the parent transfer owns
    /// the combined inventory allocation and destination reservation.
    /// </summary>
    public sealed class MaterialHaulingOrder
    {
        public const double ExperiencePerDeliveredUnit = 5d;
        public const int MaximumPickupsPerRun = 8;
        public const int MaximumEvaluatedSources = 24;

        private readonly ColonyJobRegistry jobs;
        private readonly GridNavigationMap map;
        private readonly WorldMaterialDropRegistry drops;
        private readonly MaterialTransferRegistry transfers;
        private readonly MaterialStorageWorld storage;
        private readonly ConstructionSite destinationSite;
        private readonly AntRecord ant;
        private readonly MaterialHaulingPlan plan;
        private readonly int maximumStack;
        private readonly List<HaulingPickup> pickups;
        private int pickupIndex;
        private bool sourceCollected;
        private bool jobBegan;

        private MaterialHaulingOrder(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            MaterialTransferRegistry transfers, AntRecord ant,
            MaterialHaulingPlan plan, List<HaulingPickup> pickups,
            Vector2Int destinationCell, Vector2Int destinationApproach,
            MaterialHaulDestinationKind destinationKind,
            ConstructionSite destinationSite, int quantity, int maximumStack,
            MaterialTransferId transferId, int sourceAvailable,
            int inventoryFreeCapacity, int destinationFreeCapacity)
        {
            this.jobs = jobs; this.map = map; this.drops = drops;
            this.storage = storage; this.transfers = transfers; this.ant = ant;
            this.plan = plan; this.pickups = pickups;
            this.destinationSite = destinationSite; this.maximumStack = maximumStack;
            DestinationCell = destinationCell;
            DestinationApproachCell = destinationApproach;
            DestinationKind = destinationKind; Quantity = quantity;
            TransferId = transferId; SourceAvailableAtPlanning = sourceAvailable;
            InventoryFreeCapacityAtPlanning = inventoryFreeCapacity;
            DestinationFreeCapacityAtPlanning = destinationFreeCapacity;
            sourceCollected = plan.SourceKind == MaterialHaulSourceKind.PersonalInventory;
            Phase = sourceCollected ? MaterialHaulingPhase.TravellingToDestination
                : MaterialHaulingPhase.TravellingToSource;
            Status = sourceCollected
                ? $"Carrying {quantity} material to {DestinationLabel}."
                : $"Travelling to pickup 1/{pickups.Count}.";
        }

        public ColonyJob Job => plan.Job;
        public MaterialHaulSourceKind SourceKind => pickups.Count > 0
            ? pickups[Math.Min(pickupIndex, pickups.Count - 1)].Kind : plan.SourceKind;
        public string DropKey => pickups.Count > 0
            ? pickups[Math.Min(pickupIndex, pickups.Count - 1)].DropKey : plan.DropKey;
        public string ResourceTypeId => plan.ResourceTypeId;
        public Vector2Int SourceCell => pickups.Count > 0
            ? pickups[Math.Min(pickupIndex, pickups.Count - 1)].Cell : plan.Job.TargetCell;
        public Vector2Int SourceApproachCell => pickups.Count > 0
            ? pickups[Math.Min(pickupIndex, pickups.Count - 1)].Approach : default;
        public Vector2Int DestinationCell { get; }
        public Vector2Int DestinationApproachCell { get; }
        public MaterialHaulDestinationKind DestinationKind { get; }
        public int Quantity { get; }
        public int CollectedQuantity { get; private set; }
        public int SourceCount => pickups.Count;
        public int SourceAvailableAtPlanning { get; }
        public int InventoryFreeCapacityAtPlanning { get; }
        public int DestinationFreeCapacityAtPlanning { get; }
        public string CurrentSourceIdentity => pickups.Count == 0 ? "Inventory" :
            pickups[Math.Min(pickupIndex, pickups.Count - 1)].Identity;
        public MaterialTransferId TransferId { get; }
        public MaterialTransferSnapshot Transfer =>
            transfers.TryGet(TransferId, out MaterialTransferSnapshot transfer)
                ? transfer : null;
        public MaterialHaulingPhase Phase { get; private set; }
        public string Status { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive => Phase is MaterialHaulingPhase.TravellingToSource or
            MaterialHaulingPhase.TravellingToDestination;
        public Vector2Int InitialApproachCell =>
            Phase == MaterialHaulingPhase.TravellingToSource
                ? SourceApproachCell : DestinationApproachCell;

        public static bool TryCreate(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialTransferRegistry transfers,
            MaterialStorageWorld storage, AntRecord ant, MaterialHaulingPlan plan,
            Vector2Int sourceApproach, Vector2Int startCell,
            out MaterialHaulingOrder order, out string error) =>
            TryCreate(jobs, map, drops, transfers, storage, null, ant, plan,
                sourceApproach, startCell, out order, out error);

        public static bool TryCreate(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialTransferRegistry transfers,
            MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int sourceApproach,
            Vector2Int startCell, out MaterialHaulingOrder order, out string error)
        {
            order = null;
            if (jobs == null || map == null || drops == null || transfers == null ||
                storage == null || ant == null || plan?.Job == null ||
                !ant.CanAcceptMovementOrders || ant.Needs.HasDepletedHealth)
            { error = "Hauling requires an active ant and physical material authorities."; return false; }
            if (transfers.ScenarioContextId != drops.ScenarioContextId)
            { error = "Hauling transfer and ground-stack scenarios do not match."; return false; }
            if (plan.InventoryOwner.IsValid && !plan.InventoryOwner.Equals(ant.Id))
            { error = "Only the carrying ant may claim its self-deposit task."; return false; }

            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack;
            int inventoryFree = plan.SourceKind == MaterialHaulSourceKind.PersonalInventory
                ? GetInventoryQuantity(ant.Inventory, plan.ResourceTypeId, maximumStack)
                : ant.Inventory.GetAvailableCapacity(plan.ResourceTypeId, maximumStack);
            if (inventoryFree <= 0)
            { error = "The hauling source has no transferable material or inventory capacity."; return false; }

            bool allowStorageDestination = plan.SourceKind != MaterialHaulSourceKind.MaterialStorage;
            Vector2Int destinationStart = plan.SourceKind == MaterialHaulSourceKind.PersonalInventory
                ? startCell : sourceApproach;
            if (!TryChooseDestination(storage, constructionDesignations, map,
                    destinationStart, plan.ResourceTypeId, maximumStack, inventoryFree,
                    allowStorageDestination, out Vector2Int destination,
                    out Vector2Int destinationApproach,
                    out MaterialHaulDestinationKind destinationKind,
                    out ConstructionSite destinationSite, out int destinationCapacity,
                    out error)) return false;
            int destinationFree = destinationKind ==
                    MaterialHaulDestinationKind.ConstructionSite
                ? destinationSite.GetAvailableCapacity(plan.ResourceTypeId)
                : storage.GetAvailableCapacity(destination, plan.ResourceTypeId,
                    maximumStack);

            var plannedPickups = new List<HaulingPickup>();
            int sourceAvailable = inventoryFree;
            int quantity = destinationCapacity;
            if (plan.SourceKind != MaterialHaulSourceKind.PersonalInventory &&
                !TryBuildPickupPlan(map, drops, storage, plan, sourceApproach,
                    destination, destinationKind, destinationCapacity,
                    plannedPickups, out sourceAvailable, out quantity, out error))
                return false;

            GroundStackId firstStackId = plannedPickups.Count > 0
                ? plannedPickups[0].StackId : default;
            Vector2Int firstCell = plannedPickups.Count > 0
                ? plannedPickups[0].Cell : default;
            long firstRevision = plannedPickups.Count > 0
                ? plannedPickups[0].SourceRevision : 0;
            int firstQuantity = plannedPickups.Count > 0
                ? plannedPickups[0].Quantity : quantity;
            if (!transfers.TryPrepare(ant.Id, plan.SourceKind, firstStackId,
                    firstCell, plan.ResourceTypeId, quantity, destinationKind,
                    destination, plan.Job.Id, firstRevision, 0,
                    out MaterialTransferSnapshot prepared, out error)) return false;
            MaterialTransferId transferId = prepared.Id;
            string owner = transferId.Value;
            if (plannedPickups.Count > 0)
            {
                plannedPickups[0].OperationId = prepared.Pickups[0].Id;
                if (firstQuantity != quantity &&
                    !transfers.TrySetFirstPickupQuantity(transferId, firstQuantity, out error))
                { transfers.Reject(transferId, error); return false; }
                for (int index = 1; index < plannedPickups.Count; index++)
                {
                    HaulingPickup pickup = plannedPickups[index];
                    if (!transfers.TryAddPickup(transferId, pickup.Kind,
                            pickup.StackId, pickup.Cell, pickup.Quantity,
                            pickup.SourceRevision, out MaterialPickupSnapshot added,
                            out error))
                    { transfers.Reject(transferId, error); return false; }
                    pickup.OperationId = added.Id;
                }
            }

            bool destinationReserved = destinationKind == MaterialHaulDestinationKind.ConstructionSite
                ? destinationSite.TryReserveIncoming(plan.ResourceTypeId, owner,
                    quantity, out int reserved, out error)
                : storage.TryReserveIncoming(destination, plan.ResourceTypeId,
                    maximumStack, owner, quantity, out reserved, out error);
            if (!destinationReserved || reserved != quantity)
            {
                ReleaseDestinationReservation(storage, destinationSite, destinationKind,
                    destination, plan.ResourceTypeId, owner);
                if (destinationReserved)
                    error = "The destination could not reserve the prepared exact quantity.";
                transfers.Reject(transferId, error); return false;
            }
            if (!TryReserveSources(drops, storage, plannedPickups, owner, out error))
            {
                ReleaseSourceReservations(drops, storage, plannedPickups, owner);
                ReleaseDestinationReservation(storage, destinationSite, destinationKind,
                    destination, plan.ResourceTypeId, owner);
                transfers.Reject(transferId, error); return false;
            }
            if (plan.SourceKind == MaterialHaulSourceKind.PersonalInventory &&
                !ant.Inventory.TryAllocateExisting(owner, plan.ResourceTypeId,
                    maximumStack, quantity, out error))
            {
                ReleaseDestinationReservation(storage, destinationSite, destinationKind,
                    destination, plan.ResourceTypeId, owner);
                transfers.Reject(transferId, error); return false;
            }
            if (!jobs.TryReserve(plan.Job.Id, ant.Id, out error) ||
                !transfers.TryMarkReserved(transferId, out _, out error))
            {
                ReleaseSourceReservations(drops, storage, plannedPickups, owner);
                ant.Inventory.ReleaseAllocation(owner);
                ReleaseDestinationReservation(storage, destinationSite, destinationKind,
                    destination, plan.ResourceTypeId, owner);
                jobs.PauseAndRelease(plan.Job.Id, ant.Id, error);
                transfers.Reject(transferId, error); return false;
            }
            if (plan.SourceKind == MaterialHaulSourceKind.PersonalInventory &&
                !transfers.TryCommitPickup(transferId, () => true, out _, out error))
            {
                ant.Inventory.ReleaseAllocation(owner);
                ReleaseDestinationReservation(storage, destinationSite, destinationKind,
                    destination, plan.ResourceTypeId, owner);
                jobs.PauseAndRelease(plan.Job.Id, ant.Id, error);
                transfers.Reject(transferId, error); return false;
            }

            order = new MaterialHaulingOrder(jobs, map, drops, storage, transfers,
                ant, plan, plannedPickups, destination, destinationApproach,
                destinationKind, destinationSite, quantity, maximumStack,
                transferId, sourceAvailable, inventoryFree, destinationFree);
            return true;
        }

        public static bool CanStart(GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int startCell,
            out string reason) => CanStart(map, drops, storage, null, ant, plan,
                startCell, out reason);

        public static bool CanStart(GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int startCell,
            out string reason)
        {
            if (map == null || drops == null || storage == null || ant == null ||
                plan?.Job == null || !ant.CanAcceptMovementOrders || ant.Needs.HasDepletedHealth)
            { reason = "Hauling requires an active ant and physical material authorities."; return false; }
            if (plan.InventoryOwner.IsValid && !plan.InventoryOwner.Equals(ant.Id))
            { reason = "Only the carrying ant may claim its self-deposit task."; return false; }

            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack;
            int available;
            Vector2Int destinationStart = startCell;
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop)
            {
                if (!drops.TryGet(plan.DropKey, out WorldMaterialDrop drop) ||
                    MaterialResourceCatalog.GetStableId(drop.Material) != plan.ResourceTypeId)
                { reason = "The hauling source is no longer available."; return false; }
                if (!SurvivalInteractionOrder.TryMeasureApproach(map, startCell,
                        drop.WorldCell, out Vector2Int approach, out _, out reason)) return false;
                available = Math.Min(Math.Max(0, drop.Quantity -
                    drops.GetReservedQuantity(drop.OwnershipKey)),
                    ant.Inventory.GetAvailableCapacity(plan.ResourceTypeId, maximumStack));
                destinationStart = approach;
            }
            else if (plan.SourceKind == MaterialHaulSourceKind.MaterialStorage)
            {
                if (!storage.TryGet(plan.SourceCell, out MaterialStorageSlot slot) ||
                    slot.ResourceTypeId != plan.ResourceTypeId ||
                    slot.AvailableQuantity <= 0 ||
                    !string.IsNullOrEmpty(slot.WithdrawalOwnerId))
                { reason = "The Material Storage source has no unreserved compatible contents."; return false; }
                if (!SurvivalInteractionOrder.TryMeasureApproach(map, startCell,
                        plan.SourceCell, out Vector2Int approach, out _, out reason)) return false;
                available = Math.Min(slot.AvailableQuantity,
                    ant.Inventory.GetAvailableCapacity(plan.ResourceTypeId, maximumStack));
                destinationStart = approach;
            }
            else available = GetInventoryQuantity(ant.Inventory,
                plan.ResourceTypeId, maximumStack);
            if (available <= 0)
            { reason = "The hauling source has no unreserved transferable material."; return false; }
            return TryChooseDestination(storage, constructionDesignations, map,
                destinationStart, plan.ResourceTypeId, maximumStack, available,
                plan.SourceKind != MaterialHaulSourceKind.MaterialStorage,
                out _, out _, out _, out _, out _, out reason);
        }

        public bool TryArrive(Vector2Int currentCell, double simulationHours,
            out Vector2Int nextApproach)
        {
            nextApproach = default;
            if (Phase == MaterialHaulingPhase.Completed) return true;
            if (!IsActive) return false;
            if (Phase == MaterialHaulingPhase.TravellingToSource)
            {
                HaulingPickup pickup = pickups[pickupIndex];
                if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(map,
                        currentCell, pickup.Cell))
                { Fail("The source approach became invalid."); return false; }
                if (!jobBegan && !jobs.TryBegin(Job.Id, ant.Id, out string beginError))
                { Fail(beginError); return false; }
                jobBegan = true;
                string owner = TransferId.Value;
                string physicalError = string.Empty;
                bool committed = transfers.TryCommitPickup(TransferId,
                    pickup.OperationId, () => CommitPickup(pickup, owner,
                        out physicalError), out _, out string transferError);
                if (!committed)
                { Fail(string.IsNullOrEmpty(physicalError) ? transferError : physicalError); return false; }
                pickup.Committed = true; sourceCollected = true;
                CollectedQuantity += pickup.Quantity; pickupIndex++;
                if (pickupIndex < pickups.Count)
                {
                    Status = $"Travelling to pickup {pickupIndex + 1}/{pickups.Count}; " +
                        $"carrying {CollectedQuantity}/{Quantity}.";
                    nextApproach = pickups[pickupIndex].Approach;
                    return true;
                }
                Phase = MaterialHaulingPhase.TravellingToDestination;
                Status = $"Carrying {CollectedQuantity} material to {DestinationLabel}.";
                nextApproach = DestinationApproachCell;
                return true;
            }

            if (Phase != MaterialHaulingPhase.TravellingToDestination ||
                !SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(map,
                    currentCell, DestinationCell))
            { Fail("The hauling destination approach became invalid."); return false; }
            if (!jobBegan && !jobs.TryBegin(Job.Id, ant.Id, out string error))
            { Fail(error); return false; }
            jobBegan = true;
            string ownerId = TransferId.Value;
            string depositError = string.Empty;
            bool deposited = transfers.TryCommitDelivery(TransferId, () =>
                DestinationKind == MaterialHaulDestinationKind.ConstructionSite
                    ? destinationSite.TryCommitReservedDelivery(plan.ResourceTypeId,
                        ownerId, Quantity,
                        () => ant.Inventory.TryRemoveAllocated(ownerId,
                            plan.ResourceTypeId, Quantity, maximumStack, out _),
                        out depositError)
                    : storage.TryCommitReservedDeposit(DestinationCell,
                        plan.ResourceTypeId, maximumStack, ownerId, Quantity,
                        () => ant.Inventory.TryRemoveAllocated(ownerId,
                            plan.ResourceTypeId, Quantity, maximumStack, out _),
                        out depositError), out _, out string deliveryError);
            if (!deposited)
            { Fail(string.IsNullOrEmpty(depositError) ? deliveryError : depositError); return false; }
            if (!jobs.TryAddProgress(Job.Id, ant.Id, 1d, out _, out string progressError))
            { Fail(progressError); return false; }
            ant.TryAwardSkillExperience(AntSkillId.Hauling,
                Quantity * ExperiencePerDeliveredUnit, 1d, simulationHours,
                out _, out _);
            if (!transfers.TryComplete(TransferId, out _, out string completeError))
            { Fail(completeError); return false; }
            Phase = MaterialHaulingPhase.Completed;
            Status = $"Delivered {Quantity} material to {DestinationLabel}.";
            return true;
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            CleanupReservations();
            if (sourceCollected) transfers.RequireRecovery(TransferId, reason);
            else transfers.CancelBeforePickup(TransferId, reason);
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = MaterialHaulingPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = sourceCollected
                ? "Hauling interrupted; collected material remains in inventory."
                : "Hauling interrupted; source material remains physical.";
        }

        private bool CommitPickup(HaulingPickup pickup, string owner, out string error)
        {
            bool AddToInventory()
            {
                if (!ant.Inventory.HasAllocation(owner))
                    return ant.Inventory.TryAddAllocated(owner, plan.ResourceTypeId,
                        pickup.Quantity, maximumStack, out _);
                return ant.Inventory.TryAddToAllocation(owner, plan.ResourceTypeId,
                    pickup.Quantity, maximumStack, out _);
            }
            if (pickup.Kind == MaterialHaulSourceKind.WorldDrop)
                return drops.TryCommitReservedPickup(pickup.DropKey, owner,
                    pickup.Quantity, AddToInventory, out _, out error);
            if (pickup.Kind == MaterialHaulSourceKind.MaterialStorage)
                return storage.TryCommitReservedWithdrawal(pickup.Cell,
                    plan.ResourceTypeId, owner, pickup.Quantity, AddToInventory,
                    out _, out error);
            error = "The pickup source kind is not physical world material.";
            return false;
        }

        private void Fail(string reason)
        {
            CleanupReservations();
            if (sourceCollected) transfers.RequireRecovery(TransferId, reason);
            else transfers.CancelBeforePickup(TransferId, reason);
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = MaterialHaulingPhase.Failed;
            FailureReason = reason ?? string.Empty;
            Status = sourceCollected
                ? "Hauling failed; carried material remains in inventory."
                : "Hauling failed; source material remains physical.";
        }

        private void CleanupReservations()
        {
            string owner = TransferId.Value;
            ReleaseSourceReservations(drops, storage, pickups, owner);
            ReleaseDestinationReservation(storage, destinationSite, DestinationKind,
                DestinationCell, plan.ResourceTypeId, owner);
            ant.Inventory.ReleaseAllocation(owner);
        }

        private string DestinationLabel => DestinationKind ==
            MaterialHaulDestinationKind.ConstructionSite
                ? "construction staging" : "Material Storage";

        private static bool TryBuildPickupPlan(GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            MaterialHaulingPlan plan, Vector2Int initialApproach,
            Vector2Int destinationCell, MaterialHaulDestinationKind destinationKind,
            int requested, List<HaulingPickup> result, out int sourceAvailable,
            out int plannedQuantity, out string error)
        {
            sourceAvailable = 0; plannedQuantity = 0;
            if (!TryCreateInitialPickup(drops, storage, plan, initialApproach,
                    requested, out HaulingPickup first, out error)) return false;
            result.Add(first); sourceAvailable += first.AvailableAtPlanning;
            plannedQuantity += first.Quantity;
            Vector2Int currentApproach = first.Approach;
            var candidates = new List<HaulingPickup>();
            foreach (WorldMaterialDrop drop in drops.Drops)
            {
                if (drop.OwnershipKey == first.DropKey ||
                    MaterialResourceCatalog.GetStableId(drop.Material) != plan.ResourceTypeId) continue;
                int available = Math.Max(0, drop.Quantity -
                    drops.GetReservedQuantity(drop.OwnershipKey));
                if (available > 0) candidates.Add(HaulingPickup.ForDrop(drop, default, available));
            }
            if (destinationKind == MaterialHaulDestinationKind.ConstructionSite)
            {
                foreach (MaterialStorageSlot slot in storage.Slots)
                {
                    if ((first.Kind == MaterialHaulSourceKind.MaterialStorage && slot.Cell == first.Cell) ||
                        slot.Cell == destinationCell || slot.ResourceTypeId != plan.ResourceTypeId ||
                        slot.AvailableQuantity <= 0 ||
                        !string.IsNullOrEmpty(slot.WithdrawalOwnerId)) continue;
                    candidates.Add(HaulingPickup.ForStorage(slot, default, slot.AvailableQuantity));
                }
            }
            // Enumerating records is cheap; actual A* measurement is bounded to
            // the deterministic nearest 24 candidates selected by grid distance.
            candidates.Sort((left, right) =>
            {
                int leftDistance = ManhattanDistance(currentApproach, left.Cell);
                int rightDistance = ManhattanDistance(currentApproach, right.Cell);
                int distance = leftDistance.CompareTo(rightDistance);
                return distance != 0 ? distance : CompareSource(left, right);
            });
            if (candidates.Count > MaximumEvaluatedSources)
                candidates.RemoveRange(MaximumEvaluatedSources,
                    candidates.Count - MaximumEvaluatedSources);
            while (plannedQuantity < requested && result.Count < MaximumPickupsPerRun)
            {
                int bestIndex = -1; float bestCost = float.PositiveInfinity;
                Vector2Int bestApproach = default;
                for (int index = 0; index < candidates.Count; index++)
                {
                    HaulingPickup candidate = candidates[index];
                    if (!SurvivalInteractionOrder.TryMeasureApproach(map, currentApproach,
                            candidate.Cell, out Vector2Int approach,
                            out float pickupCost, out _) ||
                        !SurvivalInteractionOrder.TryMeasureApproach(map, approach,
                            destinationCell, out _, out float deliveryCost, out _)) continue;
                    float totalCost = pickupCost + deliveryCost;
                    if (totalCost < bestCost || Mathf.Approximately(totalCost, bestCost) &&
                        (bestIndex < 0 || CompareSource(candidate, candidates[bestIndex]) < 0))
                    { bestIndex = index; bestCost = totalCost; bestApproach = approach; }
                }
                if (bestIndex < 0) break;
                HaulingPickup selected = candidates[bestIndex]; candidates.RemoveAt(bestIndex);
                if (SurvivalInteractionOrder.TryMeasureApproach(map, currentApproach,
                        destinationCell, out _, out float directCost, out _) &&
                    bestCost > directCost + Math.Max(8f, directCost * 1.5f)) continue;
                selected.Approach = bestApproach;
                selected.Quantity = Math.Min(selected.AvailableAtPlanning,
                    requested - plannedQuantity);
                result.Add(selected); sourceAvailable += selected.AvailableAtPlanning;
                plannedQuantity += selected.Quantity; currentApproach = selected.Approach;
            }
            if (plannedQuantity <= 0)
            { error = "No useful reachable source could supply this destination."; return false; }
            error = string.Empty; return true;
        }

        private static bool TryCreateInitialPickup(WorldMaterialDropRegistry drops,
            MaterialStorageWorld storage, MaterialHaulingPlan plan,
            Vector2Int approach, int requested, out HaulingPickup pickup,
            out string error)
        {
            pickup = null;
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop &&
                drops.TryGet(plan.DropKey, out WorldMaterialDrop drop) &&
                MaterialResourceCatalog.GetStableId(drop.Material) == plan.ResourceTypeId)
            {
                int available = Math.Max(0, drop.Quantity -
                    drops.GetReservedQuantity(drop.OwnershipKey));
                if (available > 0)
                {
                    pickup = HaulingPickup.ForDrop(drop, approach, available);
                    pickup.Quantity = Math.Min(available, requested);
                    error = string.Empty; return true;
                }
            }
            else if (plan.SourceKind == MaterialHaulSourceKind.MaterialStorage &&
                storage.TryGet(plan.SourceCell, out MaterialStorageSlot slot) &&
                slot.ResourceTypeId == plan.ResourceTypeId && slot.AvailableQuantity > 0 &&
                string.IsNullOrEmpty(slot.WithdrawalOwnerId))
            {
                pickup = HaulingPickup.ForStorage(slot, approach, slot.AvailableQuantity);
                pickup.Quantity = Math.Min(slot.AvailableQuantity, requested);
                error = string.Empty; return true;
            }
            error = "The planned hauling source is unavailable or already reserved.";
            return false;
        }

        private static bool TryReserveSources(WorldMaterialDropRegistry drops,
            MaterialStorageWorld storage, List<HaulingPickup> pickups,
            string owner, out string error)
        {
            foreach (HaulingPickup pickup in pickups)
            {
                bool reserved;
                int amount;
                if (pickup.Kind == MaterialHaulSourceKind.WorldDrop)
                    reserved = drops.TryReserve(pickup.DropKey, owner, pickup.Quantity,
                        out amount, out error);
                else
                    reserved = storage.TryReserveWithdrawal(pickup.Cell,
                        pickup.ResourceTypeId, owner, pickup.Quantity, out amount, out error);
                if (!reserved || amount != pickup.Quantity)
                {
                    if (reserved) error = "A source could not reserve its prepared exact quantity.";
                    return false;
                }
            }
            error = string.Empty; return true;
        }

        private static void ReleaseSourceReservations(WorldMaterialDropRegistry drops,
            MaterialStorageWorld storage, List<HaulingPickup> pickups, string owner)
        {
            foreach (HaulingPickup pickup in pickups)
            {
                if (pickup.Committed) continue;
                if (pickup.Kind == MaterialHaulSourceKind.WorldDrop)
                    drops.ReleaseReservation(pickup.DropKey, owner);
                else if (pickup.Kind == MaterialHaulSourceKind.MaterialStorage)
                    storage.ReleaseWithdrawal(pickup.Cell, owner);
            }
        }

        private static void ReleaseDestinationReservation(MaterialStorageWorld storage,
            ConstructionSite site, MaterialHaulDestinationKind kind,
            Vector2Int cell, string resourceTypeId, string ownerId)
        {
            if (kind == MaterialHaulDestinationKind.ConstructionSite)
                site?.ReleaseIncoming(resourceTypeId, ownerId);
            else storage?.ReleaseIncoming(cell, ownerId);
        }

        public static bool TryChooseDestination(MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            GridNavigationMap map, Vector2Int start, string resourceTypeId,
            int maximumStack, int requested, out Vector2Int cell,
            out Vector2Int approach, out MaterialHaulDestinationKind kind,
            out ConstructionSite site, out int quantity, out string error) =>
            TryChooseDestination(storage, constructionDesignations, map, start,
                resourceTypeId, maximumStack, requested, true, out cell,
                out approach, out kind, out site, out quantity, out error);

        private static bool TryChooseDestination(MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            GridNavigationMap map, Vector2Int start, string resourceTypeId,
            int maximumStack, int requested, bool allowStorageDestination,
            out Vector2Int cell, out Vector2Int approach,
            out MaterialHaulDestinationKind kind, out ConstructionSite site,
            out int quantity, out string error)
        {
            cell = default; approach = default; kind = default; site = null; quantity = 0;
            float bestCost = float.PositiveInfinity;
            if (constructionDesignations != null)
            {
                foreach (ConstructionDesignation candidate in constructionDesignations.Designations)
                {
                    if (candidate.State is ConstructionDesignationState.Completed or
                        ConstructionDesignationState.Cancelled) continue;
                    int capacity = candidate.Site.GetAvailableCapacity(resourceTypeId);
                    if (capacity <= 0 || !SurvivalInteractionOrder.TryMeasureApproach(map,
                            start, candidate.Cell, out Vector2Int candidateApproach,
                            out float cost, out _)) continue;
                    if (cost < bestCost || Mathf.Approximately(cost, bestCost) &&
                        CompareCell(candidate.Cell, cell) < 0)
                    {
                        cell = candidate.Cell; approach = candidateApproach;
                        site = candidate.Site; quantity = Math.Min(requested, capacity);
                        bestCost = cost;
                    }
                }
            }
            if (quantity > 0)
            { kind = MaterialHaulDestinationKind.ConstructionSite; error = string.Empty; return true; }
            if (!allowStorageDestination)
            { error = "Stored material is only withdrawn for reachable Construction staging."; return false; }
            bestCost = float.PositiveInfinity;
            foreach (MaterialStorageSlot candidate in storage.Slots)
            {
                int capacity = storage.GetAvailableCapacity(candidate.Cell,
                    resourceTypeId, maximumStack);
                if (capacity <= 0 || !SurvivalInteractionOrder.TryMeasureApproach(map,
                        start, candidate.Cell, out Vector2Int candidateApproach,
                        out float cost, out _)) continue;
                if (cost < bestCost || Mathf.Approximately(cost, bestCost) &&
                    CompareCell(candidate.Cell, cell) < 0)
                {
                    cell = candidate.Cell; approach = candidateApproach;
                    quantity = Math.Min(requested, capacity); bestCost = cost;
                }
            }
            if (quantity <= 0)
            { error = "No reachable compatible Construction Site or Material Storage has capacity."; return false; }
            kind = MaterialHaulDestinationKind.MaterialStorage;
            error = string.Empty; return true;
        }

        public static bool TryChooseDestination(MaterialStorageWorld storage,
            GridNavigationMap map, Vector2Int start, string resourceTypeId,
            int maximumStack, int requested, out Vector2Int cell,
            out Vector2Int approach, out int quantity, out string error) =>
            TryChooseDestination(storage, null, map, start, resourceTypeId,
                maximumStack, requested, true, out cell, out approach, out _,
                out _, out quantity, out error);

        private static int GetInventoryQuantity(AntInventory inventory,
            string resourceTypeId, int maximumStack) =>
            inventory.GetUnallocatedQuantity(resourceTypeId, maximumStack);

        private static int CompareSource(HaulingPickup first, HaulingPickup second)
        {
            if (second == null) return -1;
            int cell = CompareCell(first.Cell, second.Cell);
            return cell != 0 ? cell : string.CompareOrdinal(first.Identity, second.Identity);
        }

        private static int CompareCell(Vector2Int first, Vector2Int second)
        {
            int y = first.y.CompareTo(second.y);
            return y != 0 ? y : first.x.CompareTo(second.x);
        }

        private static int ManhattanDistance(Vector2Int first, Vector2Int second) =>
            Math.Abs(first.x - second.x) + Math.Abs(first.y - second.y);

        private sealed class HaulingPickup
        {
            public MaterialHaulSourceKind Kind;
            public string DropKey = string.Empty;
            public GroundStackId StackId;
            public Vector2Int Cell;
            public Vector2Int Approach;
            public string ResourceTypeId = string.Empty;
            public int Quantity;
            public int AvailableAtPlanning;
            public long SourceRevision;
            public MaterialPickupOperationId OperationId;
            public bool Committed;
            public string Identity => Kind == MaterialHaulSourceKind.WorldDrop
                ? StackId.ToString() : $"storage:{Cell.x},{Cell.y}";

            public static HaulingPickup ForDrop(WorldMaterialDrop drop,
                Vector2Int approach, int available) => new()
            {
                Kind = MaterialHaulSourceKind.WorldDrop, DropKey = drop.OwnershipKey,
                StackId = drop.StackId, Cell = drop.WorldCell, Approach = approach,
                ResourceTypeId = MaterialResourceCatalog.GetStableId(drop.Material),
                AvailableAtPlanning = available, SourceRevision = drop.Revision
            };

            public static HaulingPickup ForStorage(MaterialStorageSlot slot,
                Vector2Int approach, int available) => new()
            {
                Kind = MaterialHaulSourceKind.MaterialStorage, Cell = slot.Cell,
                Approach = approach, ResourceTypeId = slot.ResourceTypeId,
                AvailableAtPlanning = available
            };
        }
    }
}
