using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public readonly struct WorldMaterialDrop
    {
        public WorldMaterialDrop(string ownershipKey, ExcavatedMaterialType material,
            int quantity, Vector2Int worldCell)
        {
            OwnershipKey = ownershipKey;
            Material = material;
            Quantity = quantity;
            WorldCell = worldCell;
        }

        public string OwnershipKey { get; }
        public ExcavatedMaterialType Material { get; }
        public int Quantity { get; }
        public Vector2Int WorldCell { get; }
    }

    /// <summary>
    /// Authoritative loose-material state. Drops are data, not per-item
    /// GameObjects, and the completion key makes retries idempotent.
    /// </summary>
    public sealed class WorldMaterialDropRegistry
    {
        private readonly Dictionary<string, WorldMaterialDrop> drops =
            new(StringComparer.Ordinal);

        public IEnumerable<WorldMaterialDrop> Drops => drops.Values;
        public int Count => drops.Count;
        public bool RejectNextCreationForTesting { get; set; }

        internal bool CanCreate(string key, ExcavatedMaterialType material,
            int quantity, out string error)
        {
            if (RejectNextCreationForTesting)
            {
                RejectNextCreationForTesting = false;
                error = "Development failure injection rejected material output.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(key) || material == ExcavatedMaterialType.None ||
                quantity <= 0)
            {
                error = "A world drop requires a stable key, material and positive quantity.";
                return false;
            }
            if (drops.ContainsKey(key))
            {
                error = "This excavation output was already committed.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        internal void Commit(WorldMaterialDrop drop) =>
            drops.Add(drop.OwnershipKey, drop);

        public bool TryGet(string ownershipKey, out WorldMaterialDrop drop) =>
            drops.TryGetValue(ownershipKey, out drop);

        public int GetQuantity(ExcavatedMaterialType material)
        {
            int total = 0;
            foreach (WorldMaterialDrop drop in drops.Values)
                if (drop.Material == material) total += drop.Quantity;
            return total;
        }

        public void Clear() => drops.Clear();
    }

    /// <summary>
    /// The only completion boundary for excavation. All fallible checks happen
    /// before terrain changes; after removal, adding the prepared drop cannot
    /// fail. Unity's single-threaded gameplay loop therefore cannot observe a
    /// removed resource block without its corresponding recoverable output.
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
            drop = default;
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
            if (!drops.CanCreate(ownershipKey, definition.ExcavatedMaterial,
                    definition.ExcavationYield, out error)) return false;

            drop = new WorldMaterialDrop(ownershipKey, definition.ExcavatedMaterial,
                definition.ExcavationYield, cell);
            if (world.RemoveSolidMaterial(cell) != WorldCellMutationResult.Success)
            {
                error = "The foreground Solid could not be removed.";
                drop = default;
                return false;
            }

            // Commit is deliberately non-fallible after the authoritative removal.
            drops.Commit(drop);
            terrainCommitted?.Invoke(cell);
            error = string.Empty;
            return true;
        }
    }
}
