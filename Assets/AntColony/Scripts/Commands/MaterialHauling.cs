using System;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum MaterialHaulSourceKind { WorldDrop, PersonalInventory }
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
            string resourceTypeId, string dropKey = null, AntId inventoryOwner = default)
        {
            Job = job; SourceKind = sourceKind; ResourceTypeId = resourceTypeId;
            DropKey = dropKey ?? string.Empty; InventoryOwner = inventoryOwner;
        }
        public ColonyJob Job { get; }
        public MaterialHaulSourceKind SourceKind { get; }
        public string ResourceTypeId { get; }
        public string DropKey { get; }
        public AntId InventoryOwner { get; }
    }

    /// <summary>
    /// Executes one physical source-to-storage delivery. Source and destination
    /// reservations are acquired before travel; cancellation before pickup leaves
    /// the drop untouched, while cancellation after pickup keeps cargo on the ant.
    /// </summary>
    public sealed class MaterialHaulingOrder
    {
        public const double ExperiencePerDeliveredUnit = 5d;
        private readonly ColonyJobRegistry jobs;
        private readonly GridNavigationMap map;
        private readonly WorldMaterialDropRegistry drops;
        private readonly MaterialTransferRegistry transfers;
        private readonly MaterialStorageWorld storage;
        private readonly ConstructionDesignationRegistry constructionDesignations;
        private readonly ConstructionSite destinationSite;
        private readonly AntRecord ant;
        private readonly MaterialHaulingPlan plan;
        private readonly int maximumStack;
        private bool sourceCollected;

        private MaterialHaulingOrder(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            MaterialTransferRegistry transfers,
            ConstructionDesignationRegistry constructionDesignations,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int sourceApproach,
            Vector2Int destinationCell, Vector2Int destinationApproach,
            MaterialHaulDestinationKind destinationKind,
            ConstructionSite destinationSite, int quantity, int maximumStack,
            MaterialTransferId transferId)
        {
            this.jobs = jobs; this.map = map; this.drops = drops;
            this.storage = storage; this.transfers = transfers;
            this.constructionDesignations = constructionDesignations;
            this.ant = ant; this.plan = plan;
            SourceApproachCell = sourceApproach; DestinationCell = destinationCell;
            DestinationApproachCell = destinationApproach; Quantity = quantity;
            DestinationKind = destinationKind; this.destinationSite = destinationSite;
            this.maximumStack = maximumStack;
            TransferId = transferId;
            sourceCollected = plan.SourceKind == MaterialHaulSourceKind.PersonalInventory;
            Phase = plan.SourceKind == MaterialHaulSourceKind.WorldDrop
                ? MaterialHaulingPhase.TravellingToSource
                : MaterialHaulingPhase.TravellingToDestination;
            Status = Phase == MaterialHaulingPhase.TravellingToSource
                ? $"Travelling to collect {quantity} material."
                : $"Carrying {quantity} material to {DestinationLabel}.";
        }

        public ColonyJob Job => plan.Job;
        public MaterialHaulSourceKind SourceKind => plan.SourceKind;
        public string DropKey => plan.DropKey;
        public string ResourceTypeId => plan.ResourceTypeId;
        public Vector2Int SourceCell => plan.Job.TargetCell;
        public Vector2Int SourceApproachCell { get; }
        public Vector2Int DestinationCell { get; }
        public Vector2Int DestinationApproachCell { get; }
        public MaterialHaulDestinationKind DestinationKind { get; }
        public int Quantity { get; }
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
            MaterialStorageWorld storage,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int sourceApproach,
            Vector2Int startCell, out MaterialHaulingOrder order, out string error)
        {
            return TryCreate(jobs, map, drops, transfers, storage, null, ant, plan,
                sourceApproach, startCell, out order, out error);
        }

        public static bool TryCreate(ColonyJobRegistry jobs, GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialTransferRegistry transfers,
            MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int sourceApproach,
            Vector2Int startCell, out MaterialHaulingOrder order, out string error)
        {
            order = null;
            if (jobs == null || map == null || drops == null || transfers == null ||
                storage == null ||
                ant == null || plan?.Job == null || !ant.CanAcceptMovementOrders ||
                ant.Needs.HasDepletedHealth)
            { error = "Hauling requires an active ant and physical material authorities."; return false; }
            if (transfers.ScenarioContextId != drops.ScenarioContextId)
            { error = "Hauling transfer and ground-stack scenarios do not match."; return false; }
            if (plan.InventoryOwner.IsValid && !plan.InventoryOwner.Equals(ant.Id))
            { error = "Only the carrying ant may claim its self-deposit task."; return false; }

            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack;
            int available;
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop)
            {
                if (!drops.TryGet(plan.DropKey, out WorldMaterialDrop drop) ||
                    MaterialResourceCatalog.GetStableId(drop.Material) != plan.ResourceTypeId ||
                    !SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                        map, sourceApproach, drop.WorldCell))
                { error = "The hauling source is no longer available or reachable."; return false; }
                maximumStack = drop.MaximumStack;
                available = Math.Min(drop.Quantity,
                    ant.Inventory.GetAvailableCapacity(plan.ResourceTypeId, maximumStack));
            }
            else available = GetInventoryQuantity(ant.Inventory,
                plan.ResourceTypeId, maximumStack);
            if (available <= 0)
            { error = "The hauling source has no transferable material."; return false; }

            Vector2Int destinationStart = plan.SourceKind == MaterialHaulSourceKind.WorldDrop
                ? sourceApproach : startCell;
            if (!TryChooseDestination(storage, constructionDesignations, map, destinationStart,
                    plan.ResourceTypeId, maximumStack, available,
                    out Vector2Int destination, out Vector2Int destinationApproach,
                    out MaterialHaulDestinationKind destinationKind,
                    out ConstructionSite destinationSite,
                    out int quantity, out error)) return false;

            GroundStackId sourceStackId = default;
            long sourceRevision = 0;
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop &&
                drops.TryGet(plan.DropKey, out WorldMaterialDrop preparedSource))
            {
                sourceStackId = preparedSource.StackId;
                sourceRevision = preparedSource.Revision;
            }
            if (!transfers.TryPrepare(ant.Id, plan.SourceKind, sourceStackId,
                    plan.ResourceTypeId, quantity, destinationKind, destination,
                    plan.Job.Id, sourceRevision, 0,
                    out MaterialTransferSnapshot preparedTransfer, out error))
                return false;
            MaterialTransferId transferId = preparedTransfer.Id;
            string owner = transferId.Value;
            bool destinationReserved = destinationKind ==
                    MaterialHaulDestinationKind.ConstructionSite
                ? destinationSite.TryReserveIncoming(plan.ResourceTypeId, owner,
                    quantity, out int reserved, out error)
                : storage.TryReserveIncoming(destination, plan.ResourceTypeId,
                    maximumStack, owner, quantity, out reserved, out error);
            if (!destinationReserved)
            {
                transfers.Reject(transferId, error);
                return false;
            }
            if (reserved != quantity)
            {
                ReleaseDestinationReservation(storage, destinationSite,
                    destinationKind, destination, plan.ResourceTypeId, owner);
                error = "The destination could not reserve the prepared exact quantity.";
                transfers.Reject(transferId, error);
                return false;
            }
            int sourceReserved = quantity;
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop &&
                !drops.TryReserve(plan.DropKey, owner, quantity, out sourceReserved,
                    out error))
            {
                ReleaseDestinationReservation(storage, destinationSite,
                    destinationKind, destination, plan.ResourceTypeId, owner);
                transfers.Reject(transferId, error);
                return false;
            }
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop)
            {
                if (sourceReserved != quantity)
                {
                    drops.ReleaseReservation(plan.DropKey, owner);
                    ReleaseDestinationReservation(storage, destinationSite,
                        destinationKind, destination, plan.ResourceTypeId, owner);
                    error = "The source could not reserve the prepared exact quantity.";
                    transfers.Reject(transferId, error);
                    return false;
                }
            }
            else if (!ant.Inventory.TryAllocateExisting(owner,
                    plan.ResourceTypeId, maximumStack, quantity, out error))
            {
                ReleaseDestinationReservation(storage, destinationSite,
                    destinationKind, destination, plan.ResourceTypeId, owner);
                transfers.Reject(transferId, error);
                return false;
            }
            if (!jobs.TryReserve(plan.Job.Id, ant.Id, out error))
            {
                drops.ReleaseReservation(plan.DropKey, owner);
                ant.Inventory.ReleaseAllocation(owner);
                ReleaseDestinationReservation(storage, destinationSite,
                    destinationKind, destination, plan.ResourceTypeId, owner);
                transfers.Reject(transferId, error);
                return false;
            }
            if (!transfers.TryMarkReserved(transferId, out _, out error))
            {
                drops.ReleaseReservation(plan.DropKey, owner);
                ant.Inventory.ReleaseAllocation(owner);
                ReleaseDestinationReservation(storage, destinationSite,
                    destinationKind, destination, plan.ResourceTypeId, owner);
                jobs.PauseAndRelease(plan.Job.Id, ant.Id, error);
                transfers.Reject(transferId, error);
                return false;
            }
            if (plan.SourceKind == MaterialHaulSourceKind.PersonalInventory &&
                !transfers.TryCommitPickup(transferId, () => true, out _, out error))
            {
                ant.Inventory.ReleaseAllocation(owner);
                ReleaseDestinationReservation(storage, destinationSite,
                    destinationKind, destination, plan.ResourceTypeId, owner);
                jobs.PauseAndRelease(plan.Job.Id, ant.Id, error);
                transfers.Reject(transferId, error);
                return false;
            }
            order = new MaterialHaulingOrder(jobs, map, drops, storage,
                transfers, constructionDesignations, ant, plan, sourceApproach, destination,
                destinationApproach, destinationKind, destinationSite, quantity,
                maximumStack, transferId);
            return true;
        }

        /// <summary>
        /// Checks whether this ant can currently complete both ends of a haul
        /// without reserving the source, destination, job, or ant. Autonomous
        /// selection uses this to leave destination-less hauling pending while
        /// construction and excavation remain eligible.
        /// </summary>
        public static bool CanStart(GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int startCell,
            out string reason)
        {
            return CanStart(map, drops, storage, null, ant, plan, startCell,
                out reason);
        }

        public static bool CanStart(GridNavigationMap map,
            WorldMaterialDropRegistry drops, MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            AntRecord ant, MaterialHaulingPlan plan, Vector2Int startCell,
            out string reason)
        {
            if (map == null || drops == null || storage == null || ant == null ||
                plan?.Job == null || !ant.CanAcceptMovementOrders ||
                ant.Needs.HasDepletedHealth)
            {
                reason = "Hauling requires an active ant and physical material authorities.";
                return false;
            }
            if (plan.InventoryOwner.IsValid && !plan.InventoryOwner.Equals(ant.Id))
            {
                reason = "Only the carrying ant may claim its self-deposit task.";
                return false;
            }

            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack;
            int available;
            Vector2Int destinationStart = startCell;
            if (plan.SourceKind == MaterialHaulSourceKind.WorldDrop)
            {
                if (!drops.TryGet(plan.DropKey, out WorldMaterialDrop drop) ||
                    MaterialResourceCatalog.GetStableId(drop.Material) !=
                        plan.ResourceTypeId)
                {
                    reason = "The hauling source is no longer available.";
                    return false;
                }
                if (!SurvivalInteractionOrder.TryMeasureApproach(
                        map, startCell, drop.WorldCell,
                        out Vector2Int sourceApproach, out _, out string pathError))
                {
                    reason = $"The hauling source is unreachable: {pathError}";
                    return false;
                }
                maximumStack = drop.MaximumStack;
                int unreserved = Math.Max(0, drop.Quantity -
                    drops.GetReservedQuantity(drop.OwnershipKey));
                available = Math.Min(unreserved,
                    ant.Inventory.GetAvailableCapacity(
                        plan.ResourceTypeId, maximumStack));
                destinationStart = sourceApproach;
            }
            else
            {
                available = GetInventoryQuantity(
                    ant.Inventory, plan.ResourceTypeId, maximumStack);
            }
            if (available <= 0)
            {
                reason = "The hauling source has no unreserved transferable material.";
                return false;
            }

            return TryChooseDestination(storage, constructionDesignations, map,
                destinationStart,
                plan.ResourceTypeId, maximumStack, available,
                out _, out _, out _, out _, out _, out reason);
        }

        public bool TryArrive(Vector2Int currentCell, double simulationHours,
            out Vector2Int nextApproach)
        {
            nextApproach = default;
            // A duplicated arrival notification after completion is a replay,
            // not a new delivery or XP event.
            if (Phase == MaterialHaulingPhase.Completed) return true;
            if (!IsActive) return false;
            if (Phase == MaterialHaulingPhase.TravellingToSource)
            {
                if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                        map, currentCell, SourceCell))
                { Fail("The source approach became invalid."); return false; }
                if (!jobs.TryBegin(Job.Id, ant.Id, out string beginError))
                { Fail(beginError); return false; }
                string owner = TransferId.Value;
                string pickupError = string.Empty;
                bool pickedUp = transfers.TryCommitPickup(TransferId,
                    () => drops.TryCommitReservedPickup(plan.DropKey, owner, Quantity,
                        () => ant.Inventory.TryAddAllocated(owner,
                            plan.ResourceTypeId, Quantity, maximumStack, out _),
                        out _, out pickupError), out _, out string transferError);
                if (string.IsNullOrEmpty(pickupError)) pickupError = transferError;
                if (!pickedUp) { Fail(pickupError); return false; }
                sourceCollected = true;
                Phase = MaterialHaulingPhase.TravellingToDestination;
                Status = $"Carrying {Quantity} material to {DestinationLabel}.";
                nextApproach = DestinationApproachCell;
                return true;
            }

            if (Phase != MaterialHaulingPhase.TravellingToDestination ||
                !SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, currentCell, DestinationCell))
            { Fail("The hauling destination approach became invalid."); return false; }
            if (plan.SourceKind == MaterialHaulSourceKind.PersonalInventory &&
                !jobs.TryBegin(Job.Id, ant.Id, out string error))
            { Fail(error); return false; }
            string transferOwner = TransferId.Value;
            string depositError = string.Empty;
            bool deposited = transfers.TryCommitDelivery(TransferId, () =>
                DestinationKind == MaterialHaulDestinationKind.ConstructionSite
                    ? destinationSite.TryCommitReservedDelivery(plan.ResourceTypeId,
                        transferOwner, Quantity,
                        () => ant.Inventory.TryRemoveAllocated(transferOwner,
                            plan.ResourceTypeId, Quantity, maximumStack, out _),
                        out depositError)
                    : storage.TryCommitReservedDeposit(DestinationCell,
                        plan.ResourceTypeId, maximumStack, transferOwner, Quantity,
                        () => ant.Inventory.TryRemoveAllocated(transferOwner,
                            plan.ResourceTypeId, Quantity, maximumStack, out _),
                        out depositError), out _, out string deliveryError);
            if (string.IsNullOrEmpty(depositError)) depositError = deliveryError;
            if (!deposited) { Fail(depositError); return false; }
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
            string owner = TransferId.Value;
            drops.ReleaseReservation(plan.DropKey, owner);
            ReleaseDestinationReservation();
            ant.Inventory.ReleaseAllocation(owner);
            if (sourceCollected) transfers.RequireRecovery(TransferId, reason);
            else transfers.CancelBeforePickup(TransferId, reason);
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = MaterialHaulingPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = sourceCollected
                ? "Hauling interrupted; collected material remains in inventory."
                : "Hauling interrupted; source material remains in the world.";
        }

        private void Fail(string reason)
        {
            string owner = TransferId.Value;
            drops.ReleaseReservation(plan.DropKey, owner);
            ReleaseDestinationReservation();
            ant.Inventory.ReleaseAllocation(owner);
            if (sourceCollected) transfers.RequireRecovery(TransferId, reason);
            else transfers.CancelBeforePickup(TransferId, reason);
            jobs.PauseAndRelease(Job.Id, ant.Id, reason);
            Phase = MaterialHaulingPhase.Failed;
            FailureReason = reason;
            Status = sourceCollected
                ? "Hauling failed; carried material remains in inventory."
                : "Hauling failed; source material remains in the world.";
        }

        private string DestinationLabel => DestinationKind ==
            MaterialHaulDestinationKind.ConstructionSite
                ? "construction staging" : "Material Storage";

        private void ReleaseDestinationReservation() =>
            ReleaseDestinationReservation(storage, destinationSite, DestinationKind,
                DestinationCell, plan.ResourceTypeId, TransferId.Value);

        private static void ReleaseDestinationReservation(MaterialStorageWorld storage,
            ConstructionSite site, MaterialHaulDestinationKind kind,
            Vector2Int cell, string resourceTypeId, string ownerId)
        {
            if (kind == MaterialHaulDestinationKind.ConstructionSite)
                site?.ReleaseIncoming(resourceTypeId, ownerId);
            else
                storage?.ReleaseIncoming(cell, ownerId);
        }

        /// <summary>
        /// Chooses an explicit destination. Pending construction staging is
        /// intentionally considered before optional storage, then each group is
        /// ordered by reachable path cost and deterministic cell order.
        /// </summary>
        public static bool TryChooseDestination(MaterialStorageWorld storage,
            ConstructionDesignationRegistry constructionDesignations,
            GridNavigationMap map, Vector2Int start, string resourceTypeId,
            int maximumStack, int requested, out Vector2Int cell,
            out Vector2Int approach, out MaterialHaulDestinationKind kind,
            out ConstructionSite site, out int quantity, out string error)
        {
            cell = default;
            approach = default;
            kind = default;
            site = null;
            quantity = 0;

            float bestCost = float.PositiveInfinity;
            if (constructionDesignations != null)
            {
                foreach (ConstructionDesignation candidate in
                    constructionDesignations.Designations)
                {
                    if (candidate.State is ConstructionDesignationState.Completed or
                        ConstructionDesignationState.Cancelled) continue;
                    int capacity = candidate.Site.GetAvailableCapacity(resourceTypeId);
                    if (capacity <= 0 || !SurvivalInteractionOrder.TryMeasureApproach(
                            map, start, candidate.Cell,
                            out Vector2Int candidateApproach, out float cost, out _))
                        continue;
                    if (cost < bestCost || Mathf.Approximately(cost, bestCost) &&
                        CompareCell(candidate.Cell, cell) < 0)
                    {
                        cell = candidate.Cell;
                        approach = candidateApproach;
                        site = candidate.Site;
                        quantity = Math.Min(requested, capacity);
                        bestCost = cost;
                    }
                }
            }
            if (quantity > 0)
            {
                kind = MaterialHaulDestinationKind.ConstructionSite;
                error = string.Empty;
                return true;
            }

            bestCost = float.PositiveInfinity;
            if (storage != null)
            {
                foreach (MaterialStorageSlot candidate in storage.Slots)
                {
                    int capacity = storage.GetAvailableCapacity(candidate.Cell,
                        resourceTypeId, maximumStack);
                    if (capacity <= 0 || !SurvivalInteractionOrder.TryMeasureApproach(
                            map, start, candidate.Cell,
                            out Vector2Int candidateApproach, out float cost, out _))
                        continue;
                    if (cost < bestCost || Mathf.Approximately(cost, bestCost) &&
                        CompareCell(candidate.Cell, cell) < 0)
                    {
                        cell = candidate.Cell;
                        approach = candidateApproach;
                        quantity = Math.Min(requested, capacity);
                        bestCost = cost;
                    }
                }
            }
            if (quantity <= 0)
            {
                error = "No reachable compatible Construction Site or Material Storage has capacity.";
                return false;
            }
            kind = MaterialHaulDestinationKind.MaterialStorage;
            error = string.Empty;
            return true;
        }

        // Compatibility overload retained for storage-only callers and tests.
        public static bool TryChooseDestination(MaterialStorageWorld storage,
            GridNavigationMap map, Vector2Int start, string resourceTypeId,
            int maximumStack, int requested, out Vector2Int cell,
            out Vector2Int approach, out int quantity, out string error)
        {
            return TryChooseDestination(storage, null, map, start, resourceTypeId,
                maximumStack, requested, out cell, out approach, out _, out _,
                out quantity, out error);
        }

        private static int GetInventoryQuantity(AntInventory inventory,
            string resourceTypeId, int maximumStack) =>
            inventory.GetUnallocatedQuantity(resourceTypeId, maximumStack);

        private static int CompareCell(Vector2Int first, Vector2Int second)
        {
            int y = first.y.CompareTo(second.y);
            return y != 0 ? y : first.x.CompareTo(second.x);
        }
    }
}
