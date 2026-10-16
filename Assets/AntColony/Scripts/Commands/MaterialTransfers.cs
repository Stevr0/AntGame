using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    /// <summary>Scenario-local identity for one exact physical material transfer.</summary>
    public readonly struct MaterialTransferId : IEquatable<MaterialTransferId>
    {
        public MaterialTransferId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A material transfer ID cannot be empty.",
                    nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(MaterialTransferId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is MaterialTransferId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Scenario-local identity for one source pickup within a transfer.</summary>
    public readonly struct MaterialPickupOperationId : IEquatable<MaterialPickupOperationId>
    {
        public MaterialPickupOperationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A material pickup operation ID cannot be empty.",
                    nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(MaterialPickupOperationId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is MaterialPickupOperationId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Immutable evidence for one reserved source pickup.</summary>
    public sealed class MaterialPickupSnapshot
    {
        internal MaterialPickupSnapshot(MaterialPickupRecord record)
        {
            Id = record.Id;
            SourceKind = record.SourceKind;
            SourceStackId = record.SourceStackId;
            SourceCell = record.SourceCell;
            Quantity = record.Quantity;
            SourceRevision = record.SourceRevision;
            Committed = record.Committed;
        }

        public MaterialPickupOperationId Id { get; }
        public MaterialHaulSourceKind SourceKind { get; }
        public GroundStackId SourceStackId { get; }
        public Vector2Int SourceCell { get; }
        public int Quantity { get; }
        public long SourceRevision { get; }
        public bool Committed { get; }
    }

    public enum MaterialTransferPhase
    {
        Prepared,
        Reserved,
        PickedUp,
        Delivered,
        Completed,
        CancelledBeforePickup,
        RecoveryRequired,
        Rejected
    }

    /// <summary>Immutable inspection snapshot; it never owns resource quantity.</summary>
    public sealed class MaterialTransferSnapshot
    {
        internal MaterialTransferSnapshot(MaterialTransferRecord record)
        {
            Id = record.Id;
            ScenarioContextId = record.ScenarioContextId;
            AntId = record.AntId;
            SourceKind = record.SourceKind;
            SourceStackId = record.SourceStackId;
            ResourceTypeId = record.ResourceTypeId;
            Quantity = record.Quantity;
            DestinationKind = record.DestinationKind;
            DestinationCell = record.DestinationCell;
            JobId = record.JobId;
            SourceRevision = record.SourceRevision;
            DestinationRevision = record.DestinationRevision;
            Phase = record.Phase;
            Revision = record.Revision;
            PickupCommitted = record.PickupCommitted;
            DeliveryCommitted = record.DeliveryCommitted;
            RecoveryReason = record.RecoveryReason;
            var pickups = new MaterialPickupSnapshot[record.Pickups.Count];
            int collected = 0;
            for (int index = 0; index < pickups.Length; index++)
            {
                pickups[index] = new MaterialPickupSnapshot(record.Pickups[index]);
                if (record.Pickups[index].Committed)
                    collected += record.Pickups[index].Quantity;
            }
            Pickups = pickups;
            CollectedQuantity = collected;
        }

        public MaterialTransferId Id { get; }
        public string ScenarioContextId { get; }
        public AntId AntId { get; }
        public MaterialHaulSourceKind SourceKind { get; }
        public GroundStackId SourceStackId { get; }
        public string ResourceTypeId { get; }
        public int Quantity { get; }
        public MaterialHaulDestinationKind DestinationKind { get; }
        public Vector2Int DestinationCell { get; }
        public ColonyJobId JobId { get; }
        public long SourceRevision { get; }
        public long DestinationRevision { get; }
        public MaterialTransferPhase Phase { get; }
        public long Revision { get; }
        public bool PickupCommitted { get; }
        public bool DeliveryCommitted { get; }
        public string RecoveryReason { get; }
        public IReadOnlyList<MaterialPickupSnapshot> Pickups { get; }
        public int CollectedQuantity { get; }
    }

    /// <summary>
    /// Coordinates replay-safe transfer phases. Physical quantities stay in the
    /// ground-stack, ant-inventory, Storage, or Construction-site authorities.
    /// </summary>
    public sealed class MaterialTransferRegistry
    {
        private const int TerminalHistoryLimit = 256;
        private readonly Dictionary<MaterialTransferId, MaterialTransferRecord>
            records = new();
        private readonly Queue<MaterialTransferId> terminalHistory = new();
        private long nextSequence;
        private bool isActive = true;

        public MaterialTransferRegistry(string scenarioContextId)
        {
            if (string.IsNullOrWhiteSpace(scenarioContextId))
                throw new ArgumentException("Transfers require a scenario context.",
                    nameof(scenarioContextId));
            ScenarioContextId = scenarioContextId.Trim();
        }

        public string ScenarioContextId { get; }
        public int Count => records.Count;
        public bool IsActive => isActive;

        /// <summary>
        /// Invalidates every handle when its scenario is replaced. Old orders
        /// may still hold this object briefly, but can no longer commit a leg.
        /// </summary>
        public void Invalidate()
        {
            isActive = false;
            records.Clear();
            terminalHistory.Clear();
        }

        public bool TryPrepare(AntId antId, MaterialHaulSourceKind sourceKind,
            GroundStackId sourceStackId, Vector2Int sourceCell,
            string resourceTypeId, int quantity,
            MaterialHaulDestinationKind destinationKind, Vector2Int destinationCell,
            ColonyJobId jobId, long sourceRevision, long destinationRevision,
            out MaterialTransferSnapshot transfer, out string error)
        {
            transfer = null;
            if (!isActive || !antId.IsValid || !jobId.IsValid ||
                string.IsNullOrWhiteSpace(resourceTypeId) || quantity <= 0 ||
                sourceKind == MaterialHaulSourceKind.WorldDrop &&
                string.IsNullOrWhiteSpace(sourceStackId.Value))
            {
                error = "A transfer requires valid source, carrier, resource, quantity, destination, and job identities.";
                return false;
            }

            MaterialTransferId id = new($"material-transfer.{ScenarioContextId}." +
                $"{++nextSequence:D8}");
            var record = new MaterialTransferRecord(id, ScenarioContextId, antId,
                sourceKind, sourceStackId, resourceTypeId, quantity, destinationKind,
                destinationCell, jobId, sourceRevision, destinationRevision);
            record.AddPickup(sourceKind, sourceStackId, sourceCell, quantity,
                sourceRevision);
            records.Add(id, record);
            transfer = new MaterialTransferSnapshot(record);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Adjusts the compatibility-created first pickup before reservation.
        /// The parent quantity remains the full single-destination load.
        /// </summary>
        public bool TrySetFirstPickupQuantity(MaterialTransferId id, int quantity,
            out string error)
        {
            if (!TryRequirePhase(id, MaterialTransferPhase.Prepared,
                    out MaterialTransferRecord record, out error) || quantity <= 0 ||
                record.Pickups.Count == 0)
            {
                if (string.IsNullOrEmpty(error))
                    error = "A prepared transfer and positive pickup quantity are required.";
                return false;
            }
            record.Pickups[0].Quantity = quantity;
            error = string.Empty;
            return true;
        }

        /// <summary>Adds another independently replay-safe source pickup.</summary>
        public bool TryAddPickup(MaterialTransferId id,
            MaterialHaulSourceKind sourceKind, GroundStackId sourceStackId,
            Vector2Int sourceCell, int quantity, long sourceRevision,
            out MaterialPickupSnapshot pickup, out string error)
        {
            pickup = null;
            if (!TryRequirePhase(id, MaterialTransferPhase.Prepared,
                    out MaterialTransferRecord record, out error) || quantity <= 0 ||
                sourceKind == MaterialHaulSourceKind.WorldDrop &&
                    string.IsNullOrWhiteSpace(sourceStackId.Value))
            {
                if (string.IsNullOrEmpty(error))
                    error = "A pickup requires a valid source identity and quantity.";
                return false;
            }
            MaterialPickupRecord added = record.AddPickup(sourceKind,
                sourceStackId, sourceCell, quantity, sourceRevision);
            pickup = new MaterialPickupSnapshot(added);
            return true;
        }

        public bool TryMarkReserved(MaterialTransferId id,
            out MaterialTransferSnapshot transfer, out string error)
        {
            if (!TryRequirePhase(id, MaterialTransferPhase.Prepared,
                    out MaterialTransferRecord record, out error))
            {
                transfer = null;
                return false;
            }
            SetPhase(record, MaterialTransferPhase.Reserved);
            transfer = new MaterialTransferSnapshot(record);
            return true;
        }

        public bool TryCommitPickup(MaterialTransferId id, Func<bool> commitPhysical,
            out MaterialTransferSnapshot transfer, out string error)
        {
            if (!isActive || !records.TryGetValue(id, out MaterialTransferRecord record) ||
                record.Pickups.Count == 0)
            {
                transfer = null;
                error = "The material transfer is stale or has no pickup operation.";
                return false;
            }
            return TryCommitPickup(id, record.Pickups[0].Id, commitPhysical,
                out transfer, out error);
        }

        public bool TryCommitPickup(MaterialTransferId id,
            MaterialPickupOperationId pickupId, Func<bool> commitPhysical,
            out MaterialTransferSnapshot transfer, out string error)
        {
            transfer = null;
            if (!isActive)
            {
                error = "The material-transfer scenario is no longer active.";
                return false;
            }
            if (!records.TryGetValue(id, out MaterialTransferRecord record))
            {
                error = "The material transfer is stale or belongs to another scenario.";
                return false;
            }
            MaterialPickupRecord pickup = record.Pickups.Find(candidate =>
                candidate.Id.Equals(pickupId));
            if (pickup == null)
            {
                error = "The pickup operation does not belong to this transfer.";
                return false;
            }
            if (pickup.Committed)
            {
                transfer = new MaterialTransferSnapshot(record);
                error = string.Empty;
                return true;
            }
            if (record.Phase is not MaterialTransferPhase.Reserved and
                    not MaterialTransferPhase.PickedUp ||
                commitPhysical == null || !commitPhysical())
            {
                error = record.Phase != MaterialTransferPhase.Reserved
                    ? "The material transfer is not reserved for pickup."
                    : "The physical pickup authority rejected the prepared transfer.";
                return false;
            }
            pickup.Committed = true;
            record.PickupCommitted = true;
            SetPhase(record, MaterialTransferPhase.PickedUp);
            transfer = new MaterialTransferSnapshot(record);
            error = string.Empty;
            return true;
        }

        public bool TryCommitDelivery(MaterialTransferId id, Func<bool> commitPhysical,
            out MaterialTransferSnapshot transfer, out string error)
        {
            transfer = null;
            if (!isActive)
            {
                error = "The material-transfer scenario is no longer active.";
                return false;
            }
            if (!records.TryGetValue(id, out MaterialTransferRecord record))
            {
                error = "The material transfer is stale or belongs to another scenario.";
                return false;
            }
            if (record.DeliveryCommitted)
            {
                transfer = new MaterialTransferSnapshot(record);
                error = string.Empty;
                return true;
            }
            if (record.Phase != MaterialTransferPhase.PickedUp ||
                record.Pickups.Exists(pickup => !pickup.Committed) ||
                commitPhysical == null || !commitPhysical())
            {
                error = record.Phase != MaterialTransferPhase.PickedUp
                    ? "The material transfer has not committed every pickup."
                    : "The physical destination rejected the prepared transfer.";
                return false;
            }
            record.DeliveryCommitted = true;
            SetPhase(record, MaterialTransferPhase.Delivered);
            transfer = new MaterialTransferSnapshot(record);
            error = string.Empty;
            return true;
        }

        public bool TryComplete(MaterialTransferId id,
            out MaterialTransferSnapshot transfer, out string error)
        {
            transfer = null;
            if (!isActive)
            {
                error = "The material-transfer scenario is no longer active.";
                return false;
            }
            if (!records.TryGetValue(id, out MaterialTransferRecord record))
            {
                error = "The material transfer is stale or belongs to another scenario.";
                return false;
            }
            if (record.Phase == MaterialTransferPhase.Completed)
            {
                transfer = new MaterialTransferSnapshot(record);
                error = string.Empty;
                return true;
            }
            if (record.Phase != MaterialTransferPhase.Delivered)
            {
                error = "Only a delivered transfer can complete.";
                return false;
            }
            SetPhase(record, MaterialTransferPhase.Completed);
            RememberTerminal(record.Id);
            transfer = new MaterialTransferSnapshot(record);
            error = string.Empty;
            return true;
        }

        public void CancelBeforePickup(MaterialTransferId id, string reason)
        {
            if (!isActive ||
                !records.TryGetValue(id, out MaterialTransferRecord record) ||
                record.PickupCommitted) return;
            record.RecoveryReason = reason ?? string.Empty;
            SetPhase(record, MaterialTransferPhase.CancelledBeforePickup);
            RememberTerminal(id);
        }

        public void RequireRecovery(MaterialTransferId id, string reason)
        {
            if (!isActive ||
                !records.TryGetValue(id, out MaterialTransferRecord record) ||
                !record.PickupCommitted || record.DeliveryCommitted) return;
            record.RecoveryReason = reason ?? string.Empty;
            SetPhase(record, MaterialTransferPhase.RecoveryRequired);
            RememberTerminal(id);
        }

        public void Reject(MaterialTransferId id, string reason)
        {
            if (!isActive ||
                !records.TryGetValue(id, out MaterialTransferRecord record) ||
                record.PickupCommitted) return;
            record.RecoveryReason = reason ?? string.Empty;
            SetPhase(record, MaterialTransferPhase.Rejected);
            RememberTerminal(id);
        }

        public bool TryGet(MaterialTransferId id, out MaterialTransferSnapshot transfer)
        {
            if (isActive && records.TryGetValue(id, out MaterialTransferRecord record))
            {
                transfer = new MaterialTransferSnapshot(record);
                return true;
            }
            transfer = null;
            return false;
        }

        private bool TryRequirePhase(MaterialTransferId id,
            MaterialTransferPhase phase, out MaterialTransferRecord record,
            out string error)
        {
            record = null;
            if (!isActive || !records.TryGetValue(id, out record))
            {
                error = "The material transfer is stale or belongs to another scenario.";
                return false;
            }
            if (record.Phase != phase)
            {
                error = $"The material transfer is {record.Phase}, not {phase}.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private static void SetPhase(MaterialTransferRecord record,
            MaterialTransferPhase phase)
        {
            record.Phase = phase;
            record.Revision++;
        }

        private void RememberTerminal(MaterialTransferId id)
        {
            terminalHistory.Enqueue(id);
            while (terminalHistory.Count > TerminalHistoryLimit)
                records.Remove(terminalHistory.Dequeue());
        }
    }

    internal sealed class MaterialTransferRecord
    {
        public MaterialTransferRecord(MaterialTransferId id, string scenarioContextId,
            AntId antId, MaterialHaulSourceKind sourceKind,
            GroundStackId sourceStackId, string resourceTypeId, int quantity,
            MaterialHaulDestinationKind destinationKind, Vector2Int destinationCell,
            ColonyJobId jobId, long sourceRevision, long destinationRevision)
        {
            Id = id;
            ScenarioContextId = scenarioContextId;
            AntId = antId;
            SourceKind = sourceKind;
            SourceStackId = sourceStackId;
            ResourceTypeId = resourceTypeId;
            Quantity = quantity;
            DestinationKind = destinationKind;
            DestinationCell = destinationCell;
            JobId = jobId;
            SourceRevision = sourceRevision;
            DestinationRevision = destinationRevision;
            Phase = MaterialTransferPhase.Prepared;
            Revision = 1;
            RecoveryReason = string.Empty;
        }

        public MaterialTransferId Id { get; }
        public string ScenarioContextId { get; }
        public AntId AntId { get; }
        public MaterialHaulSourceKind SourceKind { get; }
        public GroundStackId SourceStackId { get; }
        public string ResourceTypeId { get; }
        public int Quantity { get; set; }
        public MaterialHaulDestinationKind DestinationKind { get; }
        public Vector2Int DestinationCell { get; }
        public ColonyJobId JobId { get; }
        public long SourceRevision { get; }
        public long DestinationRevision { get; }
        public MaterialTransferPhase Phase { get; set; }
        public long Revision { get; set; }
        public bool PickupCommitted { get; set; }
        public bool DeliveryCommitted { get; set; }
        public string RecoveryReason { get; set; }
        public List<MaterialPickupRecord> Pickups { get; } = new();

        public MaterialPickupRecord AddPickup(MaterialHaulSourceKind sourceKind,
            GroundStackId sourceStackId, Vector2Int sourceCell, int quantity,
            long sourceRevision)
        {
            var id = new MaterialPickupOperationId($"{Id.Value}.pickup.{Pickups.Count + 1:D3}");
            var pickup = new MaterialPickupRecord(id, sourceKind, sourceStackId,
                sourceCell, quantity, sourceRevision);
            Pickups.Add(pickup);
            return pickup;
        }
    }

    internal sealed class MaterialPickupRecord
    {
        public MaterialPickupRecord(MaterialPickupOperationId id,
            MaterialHaulSourceKind sourceKind, GroundStackId sourceStackId,
            Vector2Int sourceCell, int quantity, long sourceRevision)
        {
            Id = id;
            SourceKind = sourceKind;
            SourceStackId = sourceStackId;
            SourceCell = sourceCell;
            Quantity = quantity;
            SourceRevision = sourceRevision;
        }

        public MaterialPickupOperationId Id { get; }
        public MaterialHaulSourceKind SourceKind { get; }
        public GroundStackId SourceStackId { get; }
        public Vector2Int SourceCell { get; }
        public int Quantity { get; set; }
        public long SourceRevision { get; }
        public bool Committed { get; set; }
    }
}
