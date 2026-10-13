using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public sealed class AntInventoryMaterialDestination : IExcavationInventoryDestination
    {
        private readonly AntInventory inventory;
        public AntInventoryMaterialDestination(AntInventory inventory) =>
            this.inventory = inventory;

        public int GetAvailableCapacity(ExcavatedMaterialType material,
            int maximumStack) => inventory?.GetAvailableCapacity(
                MaterialResourceCatalog.GetStableId(material), maximumStack) ?? 0;

        public void Commit(ExcavatedMaterialType material, int quantity, int maximumStack)
        {
            string resourceId = MaterialResourceCatalog.GetStableId(material);
            if (quantity > 0 && (inventory == null || !inventory.TryAdd(
                    resourceId, quantity, maximumStack, false,
                    out int added, out _) || added != quantity))
                throw new System.InvalidOperationException(
                    "A prepared excavation inventory commit unexpectedly failed.");
        }
    }

    public enum MaterialPickupPhase
    {
        Travelling,
        PickingUp,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// One explicit player pickup. Reservation protects the source during travel;
    /// the final callback commits inventory before the registry removes quantity.
    /// </summary>
    public sealed class MaterialPickupOrder
    {
        private readonly WorldMaterialDropRegistry drops;
        private readonly GridNavigationMap map;
        private readonly AntRecord ant;
        private readonly string dropKey;
        private readonly int reservedQuantity;

        private MaterialPickupOrder(WorldMaterialDropRegistry drops,
            GridNavigationMap map, AntRecord ant, WorldMaterialDrop drop,
            Vector2Int approachCell, int reservedQuantity)
        {
            this.drops = drops; this.map = map; this.ant = ant;
            dropKey = drop.OwnershipKey;
            DropCell = drop.WorldCell;
            Material = drop.Material;
            MaximumStack = drop.MaximumStack;
            ApproachCell = approachCell;
            this.reservedQuantity = reservedQuantity;
            Phase = MaterialPickupPhase.Travelling;
            Status = $"Travelling to pick up {reservedQuantity} {Material}.";
        }

        public Vector2Int DropCell { get; }
        public Vector2Int ApproachCell { get; }
        public ExcavatedMaterialType Material { get; }
        public int MaximumStack { get; }
        public int ReservedQuantity => reservedQuantity;
        public int CollectedQuantity { get; private set; }
        public MaterialPickupPhase Phase { get; private set; }
        public string Status { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive => Phase is MaterialPickupPhase.Travelling or
            MaterialPickupPhase.PickingUp;

        public static bool TryCreate(WorldMaterialDropRegistry drops,
            GridNavigationMap map, AntRecord ant, WorldMaterialDrop drop,
            Vector2Int approachCell, out MaterialPickupOrder order, out string error)
        {
            order = null;
            if (drops == null || map == null || ant == null ||
                ant.LifecycleStatus != AntLifecycleStatus.Active ||
                ant.Needs.HasDepletedHealth)
            {
                error = "Pickup requires an active ant and material world.";
                return false;
            }
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, approachCell, drop.WorldCell))
            {
                error = "Pickup requires a reachable adjacent interaction position.";
                return false;
            }
            int capacity = ant.Inventory.GetAvailableCapacity(
                MaterialResourceCatalog.GetStableId(drop.Material), drop.MaximumStack);
            if (capacity <= 0)
            {
                error = "The selected ant has no compatible inventory capacity.";
                return false;
            }
            if (!drops.TryReserve(drop.OwnershipKey, ant.Id.Value,
                    Mathf.Min(capacity, drop.Quantity), out int reserved, out error))
                return false;
            order = new MaterialPickupOrder(
                drops, map, ant, drop, approachCell, reserved);
            return true;
        }

        public bool TryComplete(Vector2Int currentCell)
        {
            if (Phase != MaterialPickupPhase.Travelling) return false;
            if (!SurvivalInteractionOrder.IsValidAdjacentInteractionAnchor(
                    map, currentCell, DropCell))
            {
                Fail("The pickup position is no longer valid.");
                return false;
            }
            Phase = MaterialPickupPhase.PickingUp;
            string resourceId = MaterialResourceCatalog.GetStableId(Material);
            int capacity = ant.Inventory.GetAvailableCapacity(resourceId, MaximumStack);
            int quantity = Mathf.Min(reservedQuantity, capacity);
            if (quantity <= 0)
            {
                Fail("The ant no longer has inventory capacity for this pickup.");
                return false;
            }
            bool committed = drops.TryCommitReservedPickup(
                dropKey, ant.Id.Value, quantity,
                () => ant.Inventory.TryAdd(resourceId, quantity, MaximumStack,
                    false, out int added, out _) && added == quantity,
                out _, out string error);
            if (!committed)
            {
                Fail(error);
                return false;
            }
            CollectedQuantity = quantity;
            Phase = MaterialPickupPhase.Completed;
            Status = $"Picked up {quantity} {Material}.";
            return true;
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            drops.ReleaseReservation(dropKey, ant.Id.Value);
            Phase = MaterialPickupPhase.Cancelled;
            FailureReason = reason ?? string.Empty;
            Status = "Pickup cancelled; world material preserved.";
        }

        private void Fail(string reason)
        {
            drops.ReleaseReservation(dropKey, ant.Id.Value);
            Phase = MaterialPickupPhase.Failed;
            FailureReason = reason;
            Status = "Pickup failed; world material preserved.";
        }
    }

    public static class MaterialDropTransaction
    {
        public static bool TryDrop(AntInventory inventory, int slotIndex,
            int quantity, Vector2Int cell, GridNavigationMap map,
            WorldMaterialDropRegistry drops, string ownershipKey,
            out WorldMaterialDrop drop, out string error)
        {
            drop = default;
            if (inventory == null || drops == null || map == null ||
                !map.TryGetAnchor(cell, out _))
            {
                error = "Material can only be dropped at the ant's accessible position.";
                return false;
            }
            if (slotIndex < 0 || slotIndex >= inventory.OccupiedSlots.Count || quantity <= 0)
            {
                error = "Select an occupied inventory slot and positive quantity.";
                return false;
            }
            AntInventorySlot slot = inventory.OccupiedSlots[slotIndex];
            if (quantity > slot.Quantity ||
                !MaterialResourceCatalog.TryGetMaterial(
                    slot.ResourceTypeId, out ExcavatedMaterialType material))
            {
                error = "The selected inventory quantity is not a supported material.";
                return false;
            }
            return drops.TryAddFromSource(ownershipKey, material, quantity, cell,
                () => inventory.TryRemove(slotIndex, quantity, out string removedId,
                    out int removedMaximum, out _) &&
                    removedId == slot.ResourceTypeId &&
                    removedMaximum == slot.MaximumStack,
                out drop, out error, slot.MaximumStack);
        }
    }
}
