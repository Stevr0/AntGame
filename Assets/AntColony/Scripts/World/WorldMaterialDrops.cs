using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public static class MaterialResourceCatalog
    {
        public const int ProvisionalMaximumStack = 10;
        public static string GetStableId(ExcavatedMaterialType material) => material switch
        {
            ExcavatedMaterialType.Soil => "material.soil",
            ExcavatedMaterialType.Stone => "material.stone",
            _ => string.Empty
        };
        public static bool TryGetMaterial(string stableId, out ExcavatedMaterialType material)
        {
            material = stableId switch
            {
                "material.soil" => ExcavatedMaterialType.Soil,
                "material.stone" => ExcavatedMaterialType.Stone,
                _ => ExcavatedMaterialType.None
            };
            return material != ExcavatedMaterialType.None;
        }
    }

    public readonly struct WorldMaterialDrop
    {
        public WorldMaterialDrop(string ownershipKey, ExcavatedMaterialType material,
            int quantity, Vector2Int worldCell,
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack)
        {
            OwnershipKey = ownershipKey; Material = material; Quantity = quantity;
            WorldCell = worldCell; MaximumStack = maximumStack;
        }
        public string OwnershipKey { get; }
        public ExcavatedMaterialType Material { get; }
        public int Quantity { get; }
        public Vector2Int WorldCell { get; }
        public int MaximumStack { get; }
    }

    public interface IExcavationInventoryDestination
    {
        int GetAvailableCapacity(ExcavatedMaterialType material, int maximumStack);
        void Commit(ExcavatedMaterialType material, int quantity, int maximumStack);
    }

    public readonly struct ExcavationOutputResult
    {
        public ExcavationOutputResult(int inventoryQuantity, int droppedQuantity,
            WorldMaterialDrop drop)
        {
            InventoryQuantity = inventoryQuantity;
            DroppedQuantity = droppedQuantity;
            Drop = drop;
        }
        public int InventoryQuantity { get; }
        public int DroppedQuantity { get; }
        public WorldMaterialDrop Drop { get; }
    }

    /// <summary>Authoritative loose-material data with bounded stack merging.</summary>
    public sealed class WorldMaterialDropRegistry
    {
        private readonly Dictionary<string, WorldMaterialDrop> drops =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> committedOutputKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DropReservation> reservations =
            new(StringComparer.Ordinal);

        public IEnumerable<WorldMaterialDrop> Drops => drops.Values;
        public int Count => drops.Count;
        public bool RejectNextCreationForTesting { get; set; }

        internal bool CanCommitOutput(string key, ExcavatedMaterialType material,
            int quantity, Vector2Int cell, int maximumStack, bool needsDrop,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(key) || material == ExcavatedMaterialType.None ||
                quantity < 0 || maximumStack <= 0)
            {
                error = "Material output requires a stable key, material and valid quantity.";
                return false;
            }
            if (committedOutputKeys.Contains(key) || drops.ContainsKey(key))
            {
                error = "This material output was already committed.";
                return false;
            }
            if (needsDrop && RejectNextCreationForTesting)
            {
                RejectNextCreationForTesting = false;
                error = "Development failure injection rejected material output.";
                return false;
            }
            int mergeCapacity = 0;
            foreach (WorldMaterialDrop existing in drops.Values)
                if (existing.WorldCell == cell && existing.Material == material &&
                    existing.MaximumStack == maximumStack)
                    mergeCapacity += existing.MaximumStack - existing.Quantity;
            if (needsDrop && quantity - mergeCapacity > maximumStack)
            {
                error = "The requested output cannot be represented by bounded stacks.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        internal WorldMaterialDrop CommitOutput(string key,
            ExcavatedMaterialType material, int quantity, Vector2Int cell,
            int maximumStack)
        {
            // Inventory-only excavation still consumes its transaction key, but it
            // must not touch an unrelated compatible stack already at this cell.
            if (quantity == 0)
            {
                committedOutputKeys.Add(key);
                return default;
            }

            WorldMaterialDrop last = default;
            int remaining = quantity;
            var keys = new List<string>(drops.Keys);
            foreach (string existingKey in keys)
            {
                WorldMaterialDrop existing = drops[existingKey];
                if (existing.WorldCell != cell || existing.Material != material ||
                    existing.MaximumStack != maximumStack ||
                    existing.Quantity >= maximumStack) continue;
                int moved = Math.Min(remaining, maximumStack - existing.Quantity);
                last = new WorldMaterialDrop(existing.OwnershipKey, material,
                    existing.Quantity + moved, cell, maximumStack);
                drops[existingKey] = last;
                remaining -= moved;
                if (remaining == 0) break;
            }
            if (remaining > 0)
            {
                last = new WorldMaterialDrop(key, material, remaining, cell, maximumStack);
                drops.Add(key, last);
            }
            committedOutputKeys.Add(key);
            return last;
        }

        public bool TryAdd(string key, ExcavatedMaterialType material, int quantity,
            Vector2Int cell, out WorldMaterialDrop result, out string error,
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack)
        {
            result = default;
            error = string.Empty;
            if (quantity <= 0 || !CanCommitOutput(key, material, quantity, cell,
                    maximumStack, true, out error)) return false;
            result = CommitOutput(key, material, quantity, cell, maximumStack);
            error = string.Empty;
            return true;
        }

        public bool TryAddFromSource(string key, ExcavatedMaterialType material,
            int quantity, Vector2Int cell, Func<bool> commitSource,
            out WorldMaterialDrop result, out string error,
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack)
        {
            result = default;
            error = string.Empty;
            if (quantity <= 0 || !CanCommitOutput(key, material, quantity, cell,
                    maximumStack, true, out error)) return false;
            if (commitSource == null || !commitSource())
            {
                error = "The source inventory rejected the prepared transfer.";
                return false;
            }
            result = CommitOutput(key, material, quantity, cell, maximumStack);
            error = string.Empty;
            return true;
        }

        public bool TryGet(string ownershipKey, out WorldMaterialDrop drop) =>
            drops.TryGetValue(ownershipKey, out drop);

        public bool TryFindAtCell(Vector2Int cell, out WorldMaterialDrop drop)
        {
            foreach (WorldMaterialDrop candidate in drops.Values)
                if (candidate.WorldCell == cell && candidate.Quantity > 0)
                { drop = candidate; return true; }
            drop = default;
            return false;
        }

        public bool TryReserve(string key, string ownerId, int requestedQuantity,
            out int reservedQuantity, out string error)
        {
            reservedQuantity = 0;
            if (string.IsNullOrWhiteSpace(ownerId) || requestedQuantity <= 0 ||
                !drops.TryGetValue(key, out WorldMaterialDrop drop))
            {
                error = "Pickup reservation requires an existing drop, owner and quantity.";
                return false;
            }
            if (reservations.TryGetValue(key, out DropReservation reservation))
            {
                error = reservation.OwnerId == ownerId
                    ? "This ant already reserved the drop."
                    : "The drop is reserved by another ant.";
                return false;
            }
            reservedQuantity = Math.Min(requestedQuantity, drop.Quantity);
            reservations.Add(key, new DropReservation(ownerId, reservedQuantity));
            error = string.Empty;
            return true;
        }

        public void ReleaseReservation(string key, string ownerId)
        {
            if (reservations.TryGetValue(key, out DropReservation reservation) &&
                reservation.OwnerId == ownerId) reservations.Remove(key);
        }

        public bool TryCommitReservedPickup(string key, string ownerId, int quantity,
            Func<bool> commitDestination, out WorldMaterialDrop remaining,
            out string error)
        {
            remaining = default;
            if (!drops.TryGetValue(key, out WorldMaterialDrop drop) ||
                !reservations.TryGetValue(key, out DropReservation reservation) ||
                reservation.OwnerId != ownerId || quantity <= 0 ||
                quantity > reservation.Quantity || quantity > drop.Quantity)
            {
                error = "The reserved pickup is no longer valid.";
                return false;
            }
            if (commitDestination == null || !commitDestination())
            {
                error = "The destination inventory rejected the prepared pickup.";
                return false;
            }
            int left = drop.Quantity - quantity;
            reservations.Remove(key);
            if (left == 0) drops.Remove(key);
            else
            {
                remaining = new WorldMaterialDrop(key, drop.Material, left,
                    drop.WorldCell, drop.MaximumStack);
                drops[key] = remaining;
            }
            error = string.Empty;
            return true;
        }

        public int GetReservedQuantity(string key) =>
            reservations.TryGetValue(key, out DropReservation reservation)
                ? reservation.Quantity : 0;

        public int GetQuantity(ExcavatedMaterialType material)
        {
            int total = 0;
            foreach (WorldMaterialDrop drop in drops.Values)
                if (drop.Material == material) total += drop.Quantity;
            return total;
        }

        public void Clear()
        {
            drops.Clear(); reservations.Clear(); committedOutputKeys.Clear();
        }

        private readonly struct DropReservation
        {
            public DropReservation(string ownerId, int quantity)
            { OwnerId = ownerId; Quantity = quantity; }
            public string OwnerId { get; }
            public int Quantity { get; }
        }
    }

    /// <summary>
    /// Atomic excavation output boundary. Every fallible inventory/drop check is
    /// completed before foreground removal, then prepared ownership is committed.
    /// </summary>
    public sealed class ExcavationWorldTransaction
    {
        private readonly ChunkedWorldData world;
        private readonly WorldMaterialDropRegistry drops;
        private readonly Action<Vector2Int> terrainCommitted;

        public ExcavationWorldTransaction(ChunkedWorldData world,
            WorldMaterialDropRegistry drops, Action<Vector2Int> terrainCommitted = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.drops = drops ?? throw new ArgumentNullException(nameof(drops));
            this.terrainCommitted = terrainCommitted;
        }

        public bool TryCommit(string ownershipKey, Vector2Int cell,
            string expectedSolidMaterialId, out WorldMaterialDrop drop, out string error)
        {
            bool succeeded = TryCommit(ownershipKey, cell, expectedSolidMaterialId,
                null, out ExcavationOutputResult output, out error);
            drop = output.Drop;
            return succeeded;
        }

        public bool TryCommit(string ownershipKey, Vector2Int cell,
            string expectedSolidMaterialId, IExcavationInventoryDestination inventory,
            out ExcavationOutputResult output, out string error)
        {
            output = default;
            if (world.GetCell(cell, out WorldTileCellData cellData) !=
                    WorldCellQueryResult.Available || !cellData.HasSolidTile ||
                !string.Equals(cellData.SolidMaterialId, expectedSolidMaterialId,
                    StringComparison.Ordinal) ||
                !world.TryGetSolidMaterialDefinition(cellData.SolidMaterialId,
                    out SolidTileMaterialDefinition definition) ||
                !definition.IsExcavatable)
            {
                error = "The designated foreground Solid is no longer excavatable.";
                return false;
            }
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack;
            int inventoryQuantity = inventory == null ? 0 : Math.Min(
                definition.ExcavationYield,
                inventory.GetAvailableCapacity(definition.ExcavatedMaterial, maximumStack));
            int droppedQuantity = definition.ExcavationYield - inventoryQuantity;
            if (!drops.CanCommitOutput(ownershipKey, definition.ExcavatedMaterial,
                    droppedQuantity, cell, maximumStack, droppedQuantity > 0,
                    out error)) return false;
            if (world.RemoveSolidMaterial(cell) != WorldCellMutationResult.Success)
            {
                error = "The foreground Solid could not be removed.";
                return false;
            }
            if (inventoryQuantity > 0)
                inventory.Commit(definition.ExcavatedMaterial, inventoryQuantity,
                    maximumStack);
            WorldMaterialDrop drop = drops.CommitOutput(ownershipKey,
                definition.ExcavatedMaterial, droppedQuantity, cell, maximumStack);
            output = new ExcavationOutputResult(inventoryQuantity, droppedQuantity, drop);
            terrainCommitted?.Invoke(cell);
            error = string.Empty;
            return true;
        }
    }
}
