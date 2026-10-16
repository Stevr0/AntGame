using System;
using System.Collections.Generic;

namespace AntColony.Ants
{
    /// <summary>One approved Hauling-level carrying-capacity band.</summary>
    public readonly struct HaulingCapacityBand
    {
        public HaulingCapacityBand(int minimumLevel, int maximumLevel,
            int slotCount, int baseUnitsPerSlot)
        {
            MinimumLevel = minimumLevel;
            MaximumLevel = maximumLevel;
            SlotCount = slotCount;
            BaseUnitsPerSlot = baseUnitsPerSlot;
        }

        public int MinimumLevel { get; }
        public int MaximumLevel { get; }
        public int SlotCount { get; }
        public int BaseUnitsPerSlot { get; }
    }

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
    /// Plain-C# personal inventory. Normal slot count and units per slot follow
    /// the owning ant's current Hauling level. Skill loss never deletes cargo:
    /// over-capacity slots remain recoverable, but accept no new material.
    /// </summary>
    public sealed class AntInventory
    {
        private readonly Func<int> haulingLevel;
        private readonly List<AntInventorySlot> slots = new();
        private readonly Dictionary<string, InventoryAllocation> allocations = new();
        private static readonly HaulingCapacityBand[] haulingCapacityBands =
        {
            new(0, 19, 2, 2),
            new(20, 39, 3, 2),
            new(40, 59, 3, 3),
            new(60, 74, 4, 4),
            new(75, 89, 5, 5),
            new(90, 100, 6, 6)
        };

        internal AntInventory(Func<int> haulingLevel)
        {
            this.haulingLevel = haulingLevel ??
                throw new ArgumentNullException(nameof(haulingLevel));
        }

        public IReadOnlyList<AntInventorySlot> OccupiedSlots => slots;
        public static IReadOnlyList<HaulingCapacityBand> HaulingCapacityBands =>
            haulingCapacityBands;
        public HaulingCapacityBand CapacityBand => GetCapacityBand(haulingLevel());
        public int NaturalSlotCapacity => CapacityBand.SlotCount;
        public int BaseUnitsPerSlot => CapacityBand.BaseUnitsPerSlot;
        public int NormalTotalCapacity => NaturalSlotCapacity * BaseUnitsPerSlot;
        public int OccupiedSlotCount => slots.Count;
        public int AllocatedQuantity
        {
            get
            {
                int total = 0;
                foreach (InventoryAllocation allocation in allocations.Values)
                    total += allocation.Quantity;
                return total;
            }
        }
        public bool IsOverCapacity
        {
            get
            {
                if (slots.Count > NaturalSlotCapacity) return true;
                foreach (AntInventorySlot slot in slots)
                    if (slot.Quantity > GetNormalSlotLimit(slot.MaximumStack))
                        return true;
                return false;
            }
        }
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
            return GetCapacityBand(haulingSkillLevel).SlotCount;
        }

        public static int GetBaseUnitsPerSlot(int haulingSkillLevel)
        {
            return GetCapacityBand(haulingSkillLevel).BaseUnitsPerSlot;
        }

        public static HaulingCapacityBand GetCapacityBand(int haulingSkillLevel)
        {
            if (haulingSkillLevel < 0) haulingSkillLevel = 0;
            if (haulingSkillLevel > 100) haulingSkillLevel = 100;
            foreach (HaulingCapacityBand band in haulingCapacityBands)
                if (haulingSkillLevel >= band.MinimumLevel &&
                    haulingSkillLevel <= band.MaximumLevel)
                    return band;
            return haulingCapacityBands[haulingCapacityBands.Length - 1];
        }

        public int GetAvailableCapacity(string resourceTypeId, int maximumStack)
        {
            if (!IsValidResource(resourceTypeId, maximumStack) || IsOverCapacity)
                return 0;
            int normalSlotLimit = GetNormalSlotLimit(maximumStack);
            int available = 0;
            foreach (AntInventorySlot slot in slots)
                if (string.Equals(slot.ResourceTypeId, resourceTypeId,
                        StringComparison.Ordinal) && slot.MaximumStack == maximumStack)
                    available += Math.Max(0, normalSlotLimit - slot.Quantity);
            available += Math.Max(0, NaturalSlotCapacity - slots.Count) *
                normalSlotLimit;
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
            AddUnchecked(resourceTypeId, added, maximumStack);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Adds physical cargo and earmarks the exact quantity for one transfer.
        /// The allocation is metadata only; this inventory remains the owner.
        /// </summary>
        public bool TryAddAllocated(string allocationId, string resourceTypeId,
            int quantity, int maximumStack, out string error)
        {
            if (string.IsNullOrWhiteSpace(allocationId) ||
                allocations.ContainsKey(allocationId))
            {
                error = "Inventory transfer allocation requires a new stable identity.";
                return false;
            }
            int available = GetAvailableCapacity(resourceTypeId, maximumStack);
            if (!IsValidResource(resourceTypeId, maximumStack) || quantity <= 0 ||
                available < quantity)
            {
                error = "The ant inventory does not have sufficient compatible capacity.";
                return false;
            }
            AddUnchecked(resourceTypeId, quantity, maximumStack);
            allocations.Add(allocationId,
                new InventoryAllocation(resourceTypeId, maximumStack, quantity));
            error = string.Empty;
            return true;
        }

        /// <summary>Earmarks already-carried unallocated material for delivery.</summary>
        public bool TryAllocateExisting(string allocationId, string resourceTypeId,
            int maximumStack, int quantity, out string error)
        {
            if (string.IsNullOrWhiteSpace(allocationId) ||
                allocations.ContainsKey(allocationId) || quantity <= 0 ||
                GetUnallocatedQuantity(resourceTypeId, maximumStack) < quantity)
            {
                error = "The inventory cannot reserve the requested carried quantity.";
                return false;
            }
            allocations.Add(allocationId,
                new InventoryAllocation(resourceTypeId, maximumStack, quantity));
            error = string.Empty;
            return true;
        }

        /// <summary>Consumes only the cargo earmarked for this transfer.</summary>
        public bool TryRemoveAllocated(string allocationId, string resourceTypeId,
            int quantity, int maximumStack, out string error)
        {
            if (!allocations.TryGetValue(allocationId ?? string.Empty,
                    out InventoryAllocation allocation) ||
                allocation.ResourceTypeId != resourceTypeId ||
                allocation.MaximumStack != maximumStack ||
                allocation.Quantity != quantity)
            {
                error = "The transfer no longer owns the requested carried quantity.";
                return false;
            }
            if (!TryRemoveResourceUnchecked(resourceTypeId, quantity, maximumStack,
                    out error)) return false;
            allocations.Remove(allocationId);
            return true;
        }

        public void ReleaseAllocation(string allocationId)
        {
            if (!string.IsNullOrEmpty(allocationId)) allocations.Remove(allocationId);
        }

        public bool HasAllocation(string allocationId) =>
            !string.IsNullOrEmpty(allocationId) && allocations.ContainsKey(allocationId);

        public int GetUnallocatedQuantity(string resourceTypeId, int maximumStack)
        {
            int total = GetResourceQuantity(resourceTypeId, maximumStack);
            foreach (InventoryAllocation allocation in allocations.Values)
                if (allocation.ResourceTypeId == resourceTypeId &&
                    allocation.MaximumStack == maximumStack)
                    total -= allocation.Quantity;
            return Math.Max(0, total);
        }

        private void AddUnchecked(string resourceTypeId, int added, int maximumStack)
        {
            int normalSlotLimit = GetNormalSlotLimit(maximumStack);
            int remaining = added;
            foreach (AntInventorySlot slot in slots)
            {
                if (!string.Equals(slot.ResourceTypeId, resourceTypeId,
                        StringComparison.Ordinal) || slot.MaximumStack != maximumStack)
                    continue;
                int moved = Math.Min(remaining,
                    Math.Max(0, normalSlotLimit - slot.Quantity));
                slot.Quantity += moved;
                remaining -= moved;
                if (remaining == 0) break;
            }
            while (remaining > 0)
            {
                int moved = Math.Min(remaining, normalSlotLimit);
                slots.Add(new AntInventorySlot(resourceTypeId, moved, maximumStack));
                remaining -= moved;
            }
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
            if (quantity > slot.Quantity ||
                quantity > GetUnallocatedQuantity(slot.ResourceTypeId,
                    slot.MaximumStack))
            {
                error = "Inventory removal exceeds unallocated selected material.";
                return false;
            }
            resourceTypeId = slot.ResourceTypeId;
            maximumStack = slot.MaximumStack;
            slot.Quantity -= quantity;
            if (slot.Quantity == 0) slots.RemoveAt(slotIndex);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Removes one resource across compatible stacks only after proving the
        /// complete quantity exists. This lets a destination commit atomically.
        /// </summary>
        public bool TryRemoveResource(string resourceTypeId, int quantity,
            int maximumStack, out string error)
        {
            if (!IsValidResource(resourceTypeId, maximumStack) || quantity <= 0)
            {
                error = "Inventory removal requires a stable resource and positive quantity.";
                return false;
            }
            int available = GetUnallocatedQuantity(resourceTypeId, maximumStack);
            if (available < quantity)
            {
                error = "The inventory no longer owns the complete transfer quantity.";
                return false;
            }
            return TryRemoveResourceUnchecked(resourceTypeId, quantity, maximumStack,
                out error);
        }

        private bool TryRemoveResourceUnchecked(string resourceTypeId, int quantity,
            int maximumStack, out string error)
        {
            if (GetResourceQuantity(resourceTypeId, maximumStack) < quantity)
            {
                error = "The inventory no longer owns the complete transfer quantity.";
                return false;
            }
            int remaining = quantity;
            int normalSlotLimit = GetNormalSlotLimit(maximumStack);
            // Unload excess within overfilled slots first. This reduces the
            // over-capacity condition without compacting or moving cargo.
            for (int index = slots.Count - 1; index >= 0 && remaining > 0; index--)
            {
                AntInventorySlot slot = slots[index];
                if (slot.ResourceTypeId != resourceTypeId ||
                    slot.MaximumStack != maximumStack ||
                    slot.Quantity <= normalSlotLimit) continue;
                int removed = Math.Min(remaining, slot.Quantity - normalSlotLimit);
                slot.Quantity -= removed;
                remaining -= removed;
            }
            for (int index = slots.Count - 1; index >= 0 && remaining > 0; index--)
            {
                AntInventorySlot slot = slots[index];
                if (slot.ResourceTypeId != resourceTypeId ||
                    slot.MaximumStack != maximumStack) continue;
                int removed = Math.Min(remaining, slot.Quantity);
                slot.Quantity -= removed;
                remaining -= removed;
                if (slot.Quantity == 0) slots.RemoveAt(index);
            }
            error = string.Empty;
            return true;
        }

        public void Clear()
        {
            slots.Clear();
            allocations.Clear();
        }

        private int GetNormalSlotLimit(int resourceMaximumStack) =>
            Math.Min(resourceMaximumStack, BaseUnitsPerSlot);

        private int GetResourceQuantity(string resourceTypeId, int maximumStack)
        {
            int total = 0;
            foreach (AntInventorySlot slot in slots)
                if (slot.ResourceTypeId == resourceTypeId &&
                    slot.MaximumStack == maximumStack) total += slot.Quantity;
            return total;
        }

        private static bool IsValidResource(string resourceTypeId, int maximumStack) =>
            !string.IsNullOrWhiteSpace(resourceTypeId) &&
            string.Equals(resourceTypeId, resourceTypeId.Trim(),
                StringComparison.Ordinal) && maximumStack > 0;

        private sealed class InventoryAllocation
        {
            public InventoryAllocation(string resourceTypeId, int maximumStack,
                int quantity)
            {
                ResourceTypeId = resourceTypeId;
                MaximumStack = maximumStack;
                Quantity = quantity;
            }

            public string ResourceTypeId { get; }
            public int MaximumStack { get; }
            public int Quantity { get; }
        }
    }
}
