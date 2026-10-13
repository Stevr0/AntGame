using System;
using System.Collections.Generic;

namespace AntColony.Ants
{
    /// <summary>Read-only view of one occupied personal inventory slot.</summary>
    public sealed class AntInventorySlot
    {
        internal AntInventorySlot(string resourceTypeId, int quantity, int maximumStack)
        {
            ResourceTypeId = resourceTypeId;
            Quantity = quantity;
            MaximumStack = maximumStack;
        }

        public string ResourceTypeId { get; }
        public int Quantity { get; internal set; }
        public int MaximumStack { get; }
        public int FreeCapacity => MaximumStack - Quantity;
    }

    /// <summary>
    /// Plain-C# personal inventory. Capacity is slot-only and follows the owning
    /// ant's current Hauling level. If decay lowers capacity, occupied overflow
    /// slots remain intact but no new material is accepted until usage is legal.
    /// </summary>
    public sealed class AntInventory
    {
        private readonly Func<int> haulingLevel;
        private readonly List<AntInventorySlot> slots = new();

        internal AntInventory(Func<int> haulingLevel)
        {
            this.haulingLevel = haulingLevel ??
                throw new ArgumentNullException(nameof(haulingLevel));
        }

        public IReadOnlyList<AntInventorySlot> OccupiedSlots => slots;
        public int NaturalSlotCapacity => GetNaturalSlotCapacity(haulingLevel());
        public int OccupiedSlotCount => slots.Count;
        public int TotalQuantity
        {
            get
            {
                int total = 0;
                foreach (AntInventorySlot slot in slots) total += slot.Quantity;
                return total;
            }
        }

        public static int GetNaturalSlotCapacity(int haulingSkillLevel)
        {
            if (haulingSkillLevel < 0) haulingSkillLevel = 0;
            if (haulingSkillLevel > 100) haulingSkillLevel = 100;
            return 1 + Math.Min(4, haulingSkillLevel / 20);
        }

        public int GetAvailableCapacity(string resourceTypeId, int maximumStack)
        {
            if (!IsValidResource(resourceTypeId, maximumStack) ||
                slots.Count > NaturalSlotCapacity) return 0;
            int available = 0;
            foreach (AntInventorySlot slot in slots)
                if (string.Equals(slot.ResourceTypeId, resourceTypeId,
                        StringComparison.Ordinal) && slot.MaximumStack == maximumStack)
                    available += slot.FreeCapacity;
            available += Math.Max(0, NaturalSlotCapacity - slots.Count) * maximumStack;
            return available;
        }

        public bool TryAdd(string resourceTypeId, int requestedQuantity,
            int maximumStack, bool allowPartial, out int added, out string error)
        {
            added = 0;
            if (!IsValidResource(resourceTypeId, maximumStack) || requestedQuantity <= 0)
            {
                error = "Inventory additions require a stable resource, positive quantity and stack limit.";
                return false;
            }
            int available = GetAvailableCapacity(resourceTypeId, maximumStack);
            if (available <= 0 || !allowPartial && available < requestedQuantity)
            {
                error = "The ant inventory does not have sufficient compatible capacity.";
                return false;
            }
            added = Math.Min(requestedQuantity, available);
            int remaining = added;
            foreach (AntInventorySlot slot in slots)
            {
                if (!string.Equals(slot.ResourceTypeId, resourceTypeId,
                        StringComparison.Ordinal) || slot.MaximumStack != maximumStack)
                    continue;
                int moved = Math.Min(remaining, slot.FreeCapacity);
                slot.Quantity += moved;
                remaining -= moved;
                if (remaining == 0) break;
            }
            while (remaining > 0)
            {
                int moved = Math.Min(remaining, maximumStack);
                slots.Add(new AntInventorySlot(resourceTypeId, moved, maximumStack));
                remaining -= moved;
            }
            error = string.Empty;
            return true;
        }

        public bool TryRemove(int slotIndex, int quantity,
            out string resourceTypeId, out int maximumStack, out string error)
        {
            resourceTypeId = string.Empty;
            maximumStack = 0;
            if (slotIndex < 0 || slotIndex >= slots.Count || quantity <= 0)
            {
                error = "Inventory removal requires an occupied slot and positive quantity.";
                return false;
            }
            AntInventorySlot slot = slots[slotIndex];
            if (quantity > slot.Quantity)
            {
                error = "Inventory removal exceeds the selected stack quantity.";
                return false;
            }
            resourceTypeId = slot.ResourceTypeId;
            maximumStack = slot.MaximumStack;
            slot.Quantity -= quantity;
            if (slot.Quantity == 0) slots.RemoveAt(slotIndex);
            error = string.Empty;
            return true;
        }

        public void Clear() => slots.Clear();

        private static bool IsValidResource(string resourceTypeId, int maximumStack) =>
            !string.IsNullOrWhiteSpace(resourceTypeId) &&
            string.Equals(resourceTypeId, resourceTypeId.Trim(),
                StringComparison.Ordinal) && maximumStack > 0;
    }
}
