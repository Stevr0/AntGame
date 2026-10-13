using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    [Flags]
    public enum MaterialStorageFilter
    {
        None = 0,
        Soil = 1,
        Stone = 2,
        AllBuildingMaterials = Soil | Stone
    }

    /// <summary>Authoritative one-stack slot owned by one Background cell.</summary>
    public sealed class MaterialStorageSlot
    {
        internal MaterialStorageSlot(Vector2Int cell, MaterialStorageFilter filter,
            string resourceTypeId, int quantity, int maximumStack)
        {
            Cell = cell; Filter = filter; ResourceTypeId = resourceTypeId;
            Quantity = quantity; MaximumStack = maximumStack;
        }
        public Vector2Int Cell { get; }
        public MaterialStorageFilter Filter { get; internal set; }
        public string ResourceTypeId { get; internal set; }
        public int Quantity { get; internal set; }
        public int MaximumStack { get; internal set; }
        public string ReservationOwnerId { get; internal set; } = string.Empty;
        public string ReservedResourceTypeId { get; internal set; } = string.Empty;
        public int ReservedIncomingQuantity { get; internal set; }
        public int FreeCapacity => Math.Max(0, MaximumStack - Quantity - ReservedIncomingQuantity);
    }

    /// <summary>
    /// Plain-C# physical Material Storage authority. Tilemaps only display its
    /// Background cells; quantities and reservations live here.
    /// </summary>
    public sealed class MaterialStorageWorld
    {
        private readonly ChunkedWorldData world;
        private readonly Dictionary<Vector2Int, MaterialStorageSlot> slots = new();
        public MaterialStorageWorld(ChunkedWorldData world) =>
            this.world = world ?? throw new ArgumentNullException(nameof(world));
        public IEnumerable<MaterialStorageSlot> Slots => slots.Values;
        public int Count => slots.Count;

        public bool TryAdd(Vector2Int cell, MaterialStorageFilter filter,
            string resourceTypeId, int quantity, int maximumStack, out string error)
        {
            resourceTypeId ??= string.Empty;
            if (!HasStorageFunction(cell) || filter == MaterialStorageFilter.None ||
                quantity < 0 || maximumStack <= 0 || quantity > maximumStack ||
                quantity > 0 && (!Accepts(filter, resourceTypeId) ||
                    !MaterialResourceCatalog.TryGetMaterial(resourceTypeId, out _)))
            {
                error = "Material Storage requires its functional Background, a valid filter and bounded compatible contents.";
                return false;
            }
            if (slots.ContainsKey(cell))
            {
                error = $"Material Storage {cell} already owns its one slot.";
                return false;
            }
            slots.Add(cell, new MaterialStorageSlot(cell, filter,
                quantity == 0 ? string.Empty : resourceTypeId, quantity, maximumStack));
            error = string.Empty;
            return true;
        }

        public bool TryGet(Vector2Int cell, out MaterialStorageSlot slot) =>
            slots.TryGetValue(cell, out slot);

        public bool TrySetFilter(Vector2Int cell, MaterialStorageFilter filter,
            out string error)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                filter == MaterialStorageFilter.None)
            { error = "Material Storage and a non-empty filter are required."; return false; }
            slot.Filter = filter;
            if (!string.IsNullOrEmpty(slot.ReservationOwnerId) &&
                !Accepts(filter, slot.ReservedResourceTypeId))
                ReleaseIncoming(cell, slot.ReservationOwnerId);
            error = string.Empty;
            return true;
        }

        public int GetAvailableCapacity(Vector2Int cell, string resourceTypeId,
            int maximumStack)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                !HasStorageFunction(cell) || !Accepts(slot.Filter, resourceTypeId) ||
                slot.MaximumStack != maximumStack ||
                slot.Quantity > 0 && slot.ResourceTypeId != resourceTypeId) return 0;
            return slot.FreeCapacity;
        }

        public bool TryReserveIncoming(Vector2Int cell, string resourceTypeId,
            int maximumStack, string ownerId, int requested, out int reserved,
            out string error)
        {
            reserved = 0;
            int available = GetAvailableCapacity(cell, resourceTypeId, maximumStack);
            if (string.IsNullOrWhiteSpace(ownerId) || requested <= 0 || available <= 0 ||
                !slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                !string.IsNullOrEmpty(slot.ReservationOwnerId))
            { error = "Material Storage has no compatible unreserved capacity."; return false; }
            reserved = Math.Min(requested, available);
            slot.ReservationOwnerId = ownerId;
            slot.ReservedResourceTypeId = resourceTypeId;
            slot.ReservedIncomingQuantity = reserved;
            error = string.Empty;
            return true;
        }

        public bool TryCommitReservedDeposit(Vector2Int cell, string resourceTypeId,
            int maximumStack, string ownerId, int quantity, Func<bool> commitSource,
            out string error)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                slot.ReservationOwnerId != ownerId || quantity <= 0 ||
                quantity > slot.ReservedIncomingQuantity ||
                !Accepts(slot.Filter, resourceTypeId) || !HasStorageFunction(cell) ||
                slot.Quantity > 0 && slot.ResourceTypeId != resourceTypeId ||
                slot.Quantity + quantity > slot.MaximumStack)
            { error = "The reserved Material Storage deposit is no longer valid."; return false; }
            if (commitSource == null || !commitSource())
            { error = "The carrier inventory rejected the prepared deposit."; return false; }
            if (slot.Quantity == 0) slot.ResourceTypeId = resourceTypeId;
            slot.Quantity += quantity;
            ReleaseIncoming(cell, ownerId);
            error = string.Empty;
            return true;
        }

        public bool TryWithdraw(Vector2Int cell, int quantity,
            out string resourceTypeId, out int maximumStack, out string error)
        {
            resourceTypeId = string.Empty; maximumStack = 0;
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) || quantity <= 0 ||
                quantity > slot.Quantity)
            { error = "Withdrawal exceeds the physical Material Storage quantity."; return false; }
            resourceTypeId = slot.ResourceTypeId; maximumStack = slot.MaximumStack;
            slot.Quantity -= quantity;
            if (slot.Quantity == 0) slot.ResourceTypeId = string.Empty;
            error = string.Empty;
            return true;
        }

        public void ReleaseIncoming(Vector2Int cell, string ownerId)
        {
            if (slots.TryGetValue(cell, out MaterialStorageSlot slot) &&
                slot.ReservationOwnerId == ownerId)
            {
                slot.ReservationOwnerId = string.Empty;
                slot.ReservedResourceTypeId = string.Empty;
                slot.ReservedIncomingQuantity = 0;
            }
        }

        public bool Remove(Vector2Int cell) => slots.Remove(cell);
        public void Clear() => slots.Clear();

        private bool HasStorageFunction(Vector2Int cell) =>
            world.GetCell(cell, out WorldTileCellData data) == WorldCellQueryResult.Available &&
            !data.HasSolidTile && world.TryGetBackgroundMaterialDefinition(
                data.BackgroundMaterialId, out BackgroundTileMaterialDefinition definition) &&
            definition.Function == BackgroundFunction.MaterialStorage;

        public static bool Accepts(MaterialStorageFilter filter, string resourceTypeId) =>
            resourceTypeId == MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil)
                ? (filter & MaterialStorageFilter.Soil) != 0
                : resourceTypeId == MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone) &&
                    (filter & MaterialStorageFilter.Stone) != 0;
    }
}
