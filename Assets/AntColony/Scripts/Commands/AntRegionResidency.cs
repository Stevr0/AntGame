using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;

namespace AntColony.Commands
{
    public readonly struct AntResidency
    {
        public AntResidency(AntId antId, RegionDestination location, int revision)
        { AntId = antId; Location = location; Revision = revision; }
        public AntId AntId { get; }
        public RegionDestination Location { get; }
        public RegionId RegionId => Location.RegionId;
        public int Revision { get; }
    }

    /// <summary>The single authoritative region owner for each loaded ant.</summary>
    public sealed class AntRegionResidencyRegistry
    {
        private readonly Dictionary<AntId, AntResidency> entries = new();
        public int Count => entries.Count;

        public bool Register(AntId antId, RegionDestination location)
        {
            if (!antId.IsValid || entries.ContainsKey(antId)) return false;
            entries.Add(antId, new AntResidency(antId, location, 0));
            return true;
        }

        public bool TryGet(AntId antId, out AntResidency residency) =>
            entries.TryGetValue(antId, out residency);

        public bool IsInRegion(AntId antId, RegionId regionId) =>
            entries.TryGetValue(antId, out AntResidency residency) &&
            residency.RegionId == regionId;

        public bool TryMove(AntId antId, int expectedRevision,
            RegionDestination destination, out AntResidency updated)
        {
            updated = default;
            if (!entries.TryGetValue(antId, out AntResidency current) ||
                current.Revision != expectedRevision) return false;
            updated = new AntResidency(antId, destination, current.Revision + 1);
            entries[antId] = updated;
            return true;
        }

        /// <summary>
        /// Records completed local movement without allowing this API to change
        /// region ownership. Entrance transactions remain the only region-change
        /// authority.
        /// </summary>
        public bool TryRelocate(AntId antId, int expectedRevision,
            RegionDestination location, out AntResidency updated)
        {
            updated = default;
            if (!entries.TryGetValue(antId, out AntResidency current) ||
                current.Revision != expectedRevision ||
                current.RegionId != location.RegionId ||
                current.Location.Kind != location.Kind)
                return false;
            if (current.Location.Equals(location))
            {
                updated = current;
                return true;
            }
            updated = new AntResidency(antId, location, current.Revision + 1);
            entries[antId] = updated;
            return true;
        }

        public bool TryRestore(AntResidency residency, int expectedRevision)
        {
            if (!entries.TryGetValue(residency.AntId, out AntResidency current) ||
                current.Revision != expectedRevision) return false;
            entries[residency.AntId] = new AntResidency(residency.AntId,
                residency.Location, current.Revision + 1);
            return true;
        }

        public void Clear() => entries.Clear();
    }
}
