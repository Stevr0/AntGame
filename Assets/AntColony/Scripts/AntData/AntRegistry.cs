using System;
using System.Collections.Generic;

namespace AntColony.Ants
{
    /// <summary>
    /// Authoritative in-memory records for one colony. Persistence I/O is not
    /// included; this boundary can later be serialised without consulting visuals.
    /// </summary>
    public sealed class AntRegistry
    {
        private readonly Dictionary<AntId, AntRecord> records = new();

        public AntRegistry(ColonyId colonyId)
        {
            if (!colonyId.IsValid)
            {
                throw new ArgumentException("The registry needs a valid colony ID.", nameof(colonyId));
            }

            ColonyId = colonyId;
        }

        public ColonyId ColonyId { get; }
        public int Count => records.Count;
        public IEnumerable<AntRecord> Records => records.Values;

        /// <summary>Rejects duplicate identities and records owned by another colony.</summary>
        public bool TryRegister(AntRecord record, out string error)
        {
            if (record == null)
            {
                error = "Cannot register a null ant record.";
                return false;
            }

            if (!record.ColonyId.Equals(ColonyId))
            {
                error = $"Ant {record.Id} belongs to colony {record.ColonyId}, not {ColonyId}.";
                return false;
            }

            if (records.ContainsKey(record.Id))
            {
                error = $"Ant ID {record.Id} is already registered.";
                return false;
            }

            records.Add(record.Id, record);
            error = string.Empty;
            return true;
        }

        public bool TryGet(AntId id, out AntRecord record)
        {
            return records.TryGetValue(id, out record);
        }

        public bool TryGet(string id, out AntRecord record)
        {
            record = null;
            return !string.IsNullOrWhiteSpace(id) &&
                records.TryGetValue(new AntId(id), out record);
        }
    }
}
