using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.World;

namespace AntColony.PlayerUI
{
    /// <summary>One physical-resource ownership breakdown.</summary>
    public readonly struct ColonyResourceAmount
    {
        public ColonyResourceAmount(int stored, int carried, int loose, int staged)
        {
            Stored = stored;
            Carried = carried;
            Loose = loose;
            Staged = staged;
        }

        public int Stored { get; }
        public int Carried { get; }
        public int Loose { get; }
        public int Staged { get; }
        public int Total => Stored + Carried + Loose + Staged;
    }

    /// <summary>
    /// Read-only aggregation boundary. Reservations are deliberately not added:
    /// reserved quantities remain owned by storage or a loose stack until commit.
    /// </summary>
    public sealed class ColonyResourceSummary
    {
        private readonly Dictionary<string, ColonyResourceAmount> amounts =
            new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, ColonyResourceAmount> Amounts => amounts;

        public void Rebuild(AntRegistry ants, MaterialStorageWorld storage,
            WorldMaterialDropRegistry drops,
            ConstructionDesignationRegistry construction)
        {
            var mutable = new Dictionary<string, int[]>(StringComparer.Ordinal);
            void Add(string resource, int category, int quantity)
            {
                if (string.IsNullOrWhiteSpace(resource) || quantity <= 0) return;
                if (!mutable.TryGetValue(resource, out int[] values))
                {
                    values = new int[4];
                    mutable.Add(resource, values);
                }
                values[category] += quantity;
            }

            if (storage != null)
                foreach (MaterialStorageSlot slot in storage.Slots)
                    Add(slot.ResourceTypeId, 0, slot.Quantity);
            if (ants != null)
                foreach (AntRecord ant in ants.Records)
                    foreach (AntInventorySlot slot in ant.Inventory.OccupiedSlots)
                        Add(slot.ResourceTypeId, 1, slot.Quantity);
            if (drops != null)
                foreach (WorldMaterialDrop drop in drops.Drops)
                    Add(MaterialResourceCatalog.GetStableId(drop.Material), 2,
                        drop.Quantity);
            if (construction != null)
                foreach (ConstructionDesignation designation in construction.Designations)
                    Add(designation.Site.ResourceTypeId, 3,
                        designation.Site.DeliveredQuantity);

            amounts.Clear();
            foreach (KeyValuePair<string, int[]> entry in mutable)
                amounts.Add(entry.Key, new ColonyResourceAmount(entry.Value[0],
                    entry.Value[1], entry.Value[2], entry.Value[3]));
        }

        public ColonyResourceAmount Get(string resourceTypeId) =>
            amounts.TryGetValue(resourceTypeId, out ColonyResourceAmount amount)
                ? amount : default;
    }
}
