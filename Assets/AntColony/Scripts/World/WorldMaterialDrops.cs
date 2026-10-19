using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>Ground-stack rules belonging to one supported resource.</summary>
    public readonly struct MaterialResourceDefinition
    {
        public MaterialResourceDefinition(string stableId,
            ExcavatedMaterialType material, int groundStackCapacity,
            bool capacityIsProvisional)
        {
            StableId = stableId;
            Material = material;
            GroundStackCapacity = groundStackCapacity;
            CapacityIsProvisional = capacityIsProvisional;
        }

        public string StableId { get; }
        public ExcavatedMaterialType Material { get; }
        public int GroundStackCapacity { get; }
        public bool CapacityIsProvisional { get; }
    }

    public static class MaterialResourceCatalog
    {
        public const int ProvisionalMaximumStack = 10;
        private static readonly MaterialResourceDefinition Soil = new(
            "material.soil", ExcavatedMaterialType.Soil, ProvisionalMaximumStack, true);
        private static readonly MaterialResourceDefinition Stone = new(
            "material.stone", ExcavatedMaterialType.Stone, ProvisionalMaximumStack, true);

        public static string GetStableId(ExcavatedMaterialType material) =>
            TryGetDefinition(material, out MaterialResourceDefinition definition)
                ? definition.StableId : string.Empty;

        public static bool TryGetMaterial(string stableId,
            out ExcavatedMaterialType material)
        {
            if (TryGetDefinition(stableId, out MaterialResourceDefinition definition))
            {
                material = definition.Material;
                return true;
            }
            material = ExcavatedMaterialType.None;
            return false;
        }

        public static bool TryGetDefinition(ExcavatedMaterialType material,
            out MaterialResourceDefinition definition)
        {
            definition = material switch
            {
                ExcavatedMaterialType.Soil => Soil,
                ExcavatedMaterialType.Stone => Stone,
                _ => default
            };
            return !string.IsNullOrEmpty(definition.StableId);
        }

        public static bool TryGetDefinition(string stableId,
            out MaterialResourceDefinition definition)
        {
            definition = stableId switch
            {
                "material.soil" => Soil,
                "material.stone" => Stone,
                _ => default
            };
            return !string.IsNullOrEmpty(definition.StableId);
        }
    }

    /// <summary>Stable identity for one physical stack in one scenario.</summary>
    public readonly struct GroundStackId : IEquatable<GroundStackId>
    {
        public GroundStackId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A ground stack ID cannot be empty.",
                    nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool Equals(GroundStackId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is GroundStackId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(GroundStackId left, GroundStackId right) =>
            left.Equals(right);
        public static bool operator !=(GroundStackId left, GroundStackId right) =>
            !left.Equals(right);
    }

    public enum GroundStackLifecycle { Active, Removed }
    public enum GroundStackPlacementCompatibility { Valid, Invalid, Unknown }

    /// <summary>One reserved destination in a prepared Extraction output plan.</summary>
    public readonly struct ExtractionOutputDestination
    {
        public ExtractionOutputDestination(Vector2Int cell, int quantity,
            GroundStackId existingStackId, long expectedStackRevision)
        {
            Cell = cell;
            Quantity = quantity;
            ExistingStackId = existingStackId;
            ExpectedStackRevision = expectedStackRevision;
        }

        public Vector2Int Cell { get; }
        public int Quantity { get; }
        public GroundStackId ExistingStackId { get; }
        public long ExpectedStackRevision { get; }
        public bool CreatesNewStack => string.IsNullOrEmpty(ExistingStackId.Value);
    }

    public readonly struct GroundStackPlacementValidation
    {
        public GroundStackPlacementValidation(
            GroundStackPlacementCompatibility compatibility, string reason)
        {
            Compatibility = compatibility;
            Reason = reason ?? string.Empty;
        }

        public GroundStackPlacementCompatibility Compatibility { get; }
        public string Reason { get; }
        public static GroundStackPlacementValidation Valid() =>
            new(GroundStackPlacementCompatibility.Valid, string.Empty);
        public static GroundStackPlacementValidation Invalid(string reason) =>
            new(GroundStackPlacementCompatibility.Invalid, reason);
        public static GroundStackPlacementValidation Unknown(string reason) =>
            new(GroundStackPlacementCompatibility.Unknown, reason);
    }

    /// <summary>Immutable point-in-time view of one authoritative ground stack.</summary>
    public readonly struct WorldMaterialDrop
    {
        internal WorldMaterialDrop(GroundStackId stackId,
            ExcavatedMaterialType material, int quantity, int reservedQuantity,
            Vector2Int worldCell, int maximumStack, long revision,
            string provenance, GroundStackLifecycle lifecycle)
        {
            StackId = stackId;
            Material = material;
            Quantity = quantity;
            ReservedQuantity = reservedQuantity;
            WorldCell = worldCell;
            CellId = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(worldCell.x, worldCell.y));
            MaximumStack = maximumStack;
            Revision = revision;
            Provenance = provenance ?? string.Empty;
            Lifecycle = lifecycle;
        }

        public GroundStackId StackId { get; }
        // Compatibility name retained for existing hauling and job APIs.
        public string OwnershipKey => StackId.Value ?? string.Empty;
        public ExcavatedMaterialType Material { get; }
        public string ResourceTypeId => MaterialResourceCatalog.GetStableId(Material);
        public int Quantity { get; }
        public int ReservedQuantity { get; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
        public Vector2Int WorldCell { get; }
        public RegionCellId CellId { get; }
        public int MaximumStack { get; }
        public long Revision { get; }
        public string Provenance { get; }
        public GroundStackLifecycle Lifecycle { get; }
    }

    public readonly struct GroundStackRegistryDiagnostics
    {
        public GroundStackRegistryDiagnostics(string scenarioContextId, int stackCount,
            int occupiedCellCount, int soilQuantity, int stoneQuantity,
            int incompatiblePlacementAttempts, int capacityExceededAttempts,
            int invalidCellAttempts, int staleIdentityAttempts,
            int unsupportedResourceAttempts)
        {
            ScenarioContextId = scenarioContextId;
            StackCount = stackCount;
            OccupiedCellCount = occupiedCellCount;
            SoilQuantity = soilQuantity;
            StoneQuantity = stoneQuantity;
            IncompatiblePlacementAttempts = incompatiblePlacementAttempts;
            CapacityExceededAttempts = capacityExceededAttempts;
            InvalidCellAttempts = invalidCellAttempts;
            StaleIdentityAttempts = staleIdentityAttempts;
            UnsupportedResourceAttempts = unsupportedResourceAttempts;
        }

        public string ScenarioContextId { get; }
        public int StackCount { get; }
        public int OccupiedCellCount { get; }
        public int SoilQuantity { get; }
        public int StoneQuantity { get; }
        public int IncompatiblePlacementAttempts { get; }
        public int CapacityExceededAttempts { get; }
        public int InvalidCellAttempts { get; }
        public int StaleIdentityAttempts { get; }
        public int UnsupportedResourceAttempts { get; }
    }

    /// <summary>
    /// Sole mutable authority for supported physical ground resources. One
    /// canonical record is indexed by both stable identity and occupied cell.
    /// </summary>
    public sealed class WorldMaterialDropRegistry
    {
        private static long nextScenarioContext;
        private readonly Dictionary<GroundStackId, GroundStackRecord> stacks = new();
        private readonly Dictionary<Vector2Int, GroundStackId> stackByCell = new();
        private readonly HashSet<string> committedOutputKeys =
            new(StringComparer.Ordinal);
        private readonly Dictionary<GroundStackId, DropReservation> reservations = new();
        private readonly Dictionary<Vector2Int, OutputCapacityReservation>
            outputReservations = new();
        private readonly ChunkedWorldData world;
        private readonly Func<Vector2Int, GroundStackPlacementValidation>
            customPlacementValidator;
        private long nextStackSequence;
        private int incompatiblePlacementAttempts;
        private int capacityExceededAttempts;
        private int invalidCellAttempts;
        private int staleIdentityAttempts;
        private int unsupportedResourceAttempts;

        /// <summary>
        /// Scene-independent compatibility constructor used by focused registry
        /// tests. Runtime scenarios pass ChunkedWorldData for terrain validation.
        /// </summary>
        public WorldMaterialDropRegistry() { InitializeScenarioContext(); }

        public WorldMaterialDropRegistry(ChunkedWorldData world)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            InitializeScenarioContext();
        }

        public WorldMaterialDropRegistry(
            Func<Vector2Int, GroundStackPlacementValidation> placementValidator)
        {
            customPlacementValidator = placementValidator ??
                throw new ArgumentNullException(nameof(placementValidator));
            InitializeScenarioContext();
        }

        public IEnumerable<WorldMaterialDrop> Drops
        {
            get
            {
                foreach (GroundStackRecord record in stacks.Values)
                    yield return CreateSnapshot(record);
            }
        }

        public int Count => stacks.Count;
        public string ScenarioContextId { get; private set; }
        public bool HasAuthoritativePlacementValidation => world != null ||
            customPlacementValidator != null;
        public bool RejectNextCreationForTesting { get; set; }
        public event Action<Vector2Int> GroundCellChanged;

        /// <summary>
        /// Reserves one cell's exact merge or creation capacity without exposing
        /// planned output as a physical stack.
        /// </summary>
        public bool TryReserveOutputCapacity(string operationId,
            ExcavatedMaterialType material, int quantity, Vector2Int cell,
            bool allowExcavationSourceSolid,
            out ExtractionOutputDestination destination, out string error)
        {
            destination = default;
            if (string.IsNullOrWhiteSpace(operationId) || quantity <= 0 ||
                !MaterialResourceCatalog.TryGetDefinition(material, out var definition))
            { error = "Output capacity requires an operation, resource and positive quantity."; return false; }
            if (outputReservations.TryGetValue(cell, out var claimed))
            {
                error = claimed.OperationId == operationId
                    ? "This operation already reserved the output cell."
                    : "The output cell is reserved by another Extraction operation.";
                return false;
            }
            GroundStackPlacementValidation placement = ValidatePlacement(cell,
                allowExcavationSourceSolid, false);
            if (placement.Compatibility != GroundStackPlacementCompatibility.Valid)
            { error = placement.Reason; return false; }

            GroundStackId stackId = default;
            long stackRevision = 0L;
            int available = definition.GroundStackCapacity;
            if (stackByCell.TryGetValue(cell, out GroundStackId existingId))
            {
                GroundStackRecord existing = stacks[existingId];
                if (existing.Material != material)
                { error = "A loose-ground cell cannot contain different resource types."; return false; }
                if (reservations.ContainsKey(existingId))
                { error = "The compatible stack is reserved for Hauling."; return false; }
                stackId = existingId;
                stackRevision = existing.Revision;
                available = existing.Capacity - existing.Quantity;
            }
            else if (RejectNextCreationForTesting)
            {
                RejectNextCreationForTesting = false;
                error = "Development failure injection rejected material output.";
                return false;
            }
            if (quantity > available)
            { error = "The output exceeds the destination's available stack capacity."; return false; }

            destination = new ExtractionOutputDestination(cell, quantity,
                stackId, stackRevision);
            outputReservations.Add(cell, new OutputCapacityReservation(
                operationId.Trim(), material, destination));
            error = string.Empty;
            return true;
        }

        public void ReleaseOutputReservations(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId)) return;
            var cells = new List<Vector2Int>();
            foreach (var pair in outputReservations)
                if (pair.Value.OperationId == operationId) cells.Add(pair.Key);
            foreach (Vector2Int cell in cells) outputReservations.Remove(cell);
        }

        /// <summary>
        /// Revalidates every destination before applying any stack mutation.
        /// After successful validation the mutations are non-fallible.
        /// </summary>
        public bool TryCommitReservedOutputs(string operationId,
            IReadOnlyList<ExtractionOutputDestination> destinations,
            out IReadOnlyList<WorldMaterialDrop> committed, out string error)
        {
            committed = Array.Empty<WorldMaterialDrop>();
            if (!CanCommitReservedOutputs(operationId, destinations, out error))
                return false;

            var results = new List<WorldMaterialDrop>(destinations.Count);
            for (int i = 0; i < destinations.Count; i++)
            {
                ExtractionOutputDestination destination = destinations[i];
                string key = $"{operationId}.output.{i}";
                results.Add(CommitOutput(key,
                    outputReservations[destination.Cell].Material,
                    destination.Quantity, destination.Cell,
                    MaterialResourceCatalog.ProvisionalMaximumStack));
                outputReservations.Remove(destination.Cell);
            }
            committed = results;
            error = string.Empty;
            return true;
        }

        internal bool CanCommitReservedOutputs(string operationId,
            IReadOnlyList<ExtractionOutputDestination> destinations,
            out string error)
        {
            if (destinations == null || destinations.Count == 0)
            { error = "Extraction has no reserved physical output destinations."; return false; }
            foreach (ExtractionOutputDestination destination in destinations)
            {
                if (!outputReservations.TryGetValue(destination.Cell, out var reservation) ||
                    reservation.OperationId != operationId ||
                    !reservation.Destination.Equals(destination))
                { error = "An Extraction output reservation is missing or stale."; return false; }
                if (destination.CreatesNewStack)
                {
                    if (stackByCell.ContainsKey(destination.Cell))
                    { error = "A reserved empty output cell became occupied."; return false; }
                }
                else if (!stacks.TryGetValue(destination.ExistingStackId,
                        out GroundStackRecord existing) ||
                    existing.Revision != destination.ExpectedStackRevision ||
                    existing.Material != reservation.Material ||
                    existing.Quantity + destination.Quantity > existing.Capacity)
                { error = "A reserved output stack changed before commit."; return false; }
            }
            error = string.Empty;
            return true;
        }

        internal bool CanCommitOutput(string key, ExcavatedMaterialType material,
            int quantity, Vector2Int cell, int maximumStack, bool needsDrop,
            out string error) => CanCommitOutput(key, material, quantity, cell,
                maximumStack, needsDrop, false, false, out error);

        internal bool CanCommitOutput(string key, ExcavatedMaterialType material,
            int quantity, Vector2Int cell, int maximumStack, bool needsDrop,
            bool allowExcavationSourceSolid,
            bool allowLegacyFunctionalBackground, out string error)
        {
            if (string.IsNullOrWhiteSpace(key) || quantity < 0)
            {
                error = "Material output requires a stable operation key and valid quantity.";
                return false;
            }
            if (!MaterialResourceCatalog.TryGetDefinition(material,
                    out MaterialResourceDefinition definition))
            {
                unsupportedResourceAttempts++;
                error = "The resource definition is unsupported.";
                return false;
            }
            if (maximumStack != definition.GroundStackCapacity)
            {
                unsupportedResourceAttempts++;
                error = "Ground capacity must match the resource definition.";
                return false;
            }
            if (committedOutputKeys.Contains(key))
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
            GroundStackPlacementValidation placement = ValidatePlacement(
                cell, allowExcavationSourceSolid,
                allowLegacyFunctionalBackground);
            if (placement.Compatibility != GroundStackPlacementCompatibility.Valid)
            {
                invalidCellAttempts++;
                error = placement.Compatibility == GroundStackPlacementCompatibility.Unknown
                    ? $"Ground-stack compatibility is unknown: {placement.Reason}"
                    : placement.Reason;
                return false;
            }
            if (stackByCell.TryGetValue(cell, out GroundStackId existingId))
            {
                GroundStackRecord existing = stacks[existingId];
                if (existing.Material != material)
                {
                    incompatiblePlacementAttempts++;
                    error = "A loose-ground cell cannot contain different resource types.";
                    return false;
                }
                if (existing.Quantity + quantity > existing.Capacity)
                {
                    capacityExceededAttempts++;
                    error = "The compatible ground stack would exceed its resource capacity.";
                    return false;
                }
            }
            else if (needsDrop && quantity > definition.GroundStackCapacity)
            {
                capacityExceededAttempts++;
                error = "The requested output exceeds its resource ground-stack capacity.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        internal WorldMaterialDrop CommitOutput(string key,
            ExcavatedMaterialType material, int quantity, Vector2Int cell,
            int maximumStack)
        {
            GroundStackRecord record;
            if (stackByCell.TryGetValue(cell, out GroundStackId existingId))
            {
                record = stacks[existingId];
                record.Quantity += quantity;
                record.Revision++;
            }
            else
            {
                GroundStackId id = new($"ground-stack.{ScenarioContextId}." +
                    $"{++nextStackSequence:D8}");
                record = new GroundStackRecord(id, material, quantity, cell,
                    maximumStack, key);
                stacks.Add(id, record);
                stackByCell.Add(cell, id);
            }
            committedOutputKeys.Add(key);
            GroundCellChanged?.Invoke(cell);
            return CreateSnapshot(record);
        }

        public bool TryAdd(string key, ExcavatedMaterialType material, int quantity,
            Vector2Int cell, out WorldMaterialDrop result, out string error,
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack)
        {
            result = default;
            error = string.Empty;
            if (quantity <= 0 || !CanCommitOutput(key, material, quantity, cell,
                    maximumStack, true, out error))
            {
                if (string.IsNullOrEmpty(error))
                    error = "Material output requires a positive quantity.";
                return false;
            }
            result = CommitOutput(key, material, quantity, cell, maximumStack);
            error = string.Empty;
            return true;
        }

        public bool CanAdd(string key, ExcavatedMaterialType material, int quantity,
            Vector2Int cell, out string error,
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack)
        {
            if (quantity <= 0)
            {
                error = "Material output requires a positive quantity.";
                return false;
            }
            return CanCommitOutput(key, material, quantity, cell,
                maximumStack, true, out error);
        }

        public bool TryAddFromSource(string key, ExcavatedMaterialType material,
            int quantity, Vector2Int cell, Func<bool> commitSource,
            out WorldMaterialDrop result, out string error,
            int maximumStack = MaterialResourceCatalog.ProvisionalMaximumStack)
        {
            result = default;
            error = string.Empty;
            if (quantity <= 0 || !CanCommitOutput(key, material, quantity, cell,
                    maximumStack, true, false, true, out error))
            {
                if (string.IsNullOrEmpty(error))
                    error = "Material output requires a positive quantity.";
                return false;
            }
            if (commitSource == null || !commitSource())
            {
                error = "The source inventory rejected the prepared transfer.";
                return false;
            }
            result = CommitOutput(key, material, quantity, cell, maximumStack);
            error = string.Empty;
            return true;
        }

        public GroundStackPlacementValidation ValidatePlacement(Vector2Int cell) =>
            ValidatePlacement(cell, false, false);

        public bool TryGet(string ownershipKey, out WorldMaterialDrop drop)
        {
            if (TryGetRecord(ownershipKey, out GroundStackRecord record))
            {
                drop = CreateSnapshot(record);
                return true;
            }
            drop = default;
            return false;
        }

        public bool TryFindAtCell(Vector2Int cell, out WorldMaterialDrop drop)
        {
            if (stackByCell.TryGetValue(cell, out GroundStackId id) &&
                stacks.TryGetValue(id, out GroundStackRecord record))
            {
                drop = CreateSnapshot(record);
                return true;
            }
            drop = default;
            return false;
        }

        public bool TryReserve(string key, string ownerId, int requestedQuantity,
            out int reservedQuantity, out string error)
        {
            reservedQuantity = 0;
            if (string.IsNullOrWhiteSpace(ownerId) || requestedQuantity <= 0 ||
                !TryGetRecord(key, out GroundStackRecord record))
            {
                staleIdentityAttempts++;
                error = "Pickup reservation requires an active stack, owner and quantity.";
                return false;
            }
            if (reservations.TryGetValue(record.Id, out DropReservation reservation))
            {
                error = reservation.OwnerId == ownerId
                    ? "This ant already reserved the stack."
                    : "The stack is reserved by another ant.";
                return false;
            }
            if (outputReservations.ContainsKey(record.Cell))
            {
                error = "The stack is reserved for prepared Extraction output.";
                return false;
            }
            reservedQuantity = Math.Min(requestedQuantity, record.Quantity);
            reservations.Add(record.Id, new DropReservation(ownerId, reservedQuantity));
            record.Revision++;
            GroundCellChanged?.Invoke(record.Cell);
            error = string.Empty;
            return true;
        }

        public void ReleaseReservation(string key, string ownerId)
        {
            if (!TryGetRecord(key, out GroundStackRecord record)) return;
            if (reservations.TryGetValue(record.Id, out DropReservation reservation) &&
                reservation.OwnerId == ownerId)
            {
                reservations.Remove(record.Id);
                record.Revision++;
                GroundCellChanged?.Invoke(record.Cell);
            }
        }

        public bool TryCommitReservedPickup(string key, string ownerId, int quantity,
            Func<bool> commitDestination, out WorldMaterialDrop remaining,
            out string error)
        {
            remaining = default;
            if (!TryGetRecord(key, out GroundStackRecord record) ||
                !reservations.TryGetValue(record.Id, out DropReservation reservation) ||
                reservation.OwnerId != ownerId || quantity <= 0 ||
                quantity > reservation.Quantity || quantity > record.Quantity)
            {
                staleIdentityAttempts++;
                error = "The reserved pickup is no longer valid.";
                return false;
            }
            if (commitDestination == null || !commitDestination())
            {
                error = "The destination inventory rejected the prepared pickup.";
                return false;
            }
            reservations.Remove(record.Id);
            record.Quantity -= quantity;
            record.Revision++;
            if (record.Quantity == 0)
            {
                record.Lifecycle = GroundStackLifecycle.Removed;
                stacks.Remove(record.Id);
                stackByCell.Remove(record.Cell);
            }
            else remaining = CreateSnapshot(record);
            GroundCellChanged?.Invoke(record.Cell);
            error = string.Empty;
            return true;
        }

        public int GetReservedQuantity(string key) =>
            TryGetRecord(key, out GroundStackRecord record) &&
            reservations.TryGetValue(record.Id, out DropReservation reservation)
                ? reservation.Quantity : 0;

        public int GetQuantity(ExcavatedMaterialType material)
        {
            int total = 0;
            foreach (GroundStackRecord record in stacks.Values)
                if (record.Material == material) total += record.Quantity;
            return total;
        }

        public GroundStackRegistryDiagnostics GetDiagnostics() => new(
            ScenarioContextId, Count, stackByCell.Count,
            GetQuantity(ExcavatedMaterialType.Soil),
            GetQuantity(ExcavatedMaterialType.Stone),
            incompatiblePlacementAttempts, capacityExceededAttempts,
            invalidCellAttempts, staleIdentityAttempts,
            unsupportedResourceAttempts);

        public void Clear()
        {
            var affectedCells = new List<Vector2Int>(stackByCell.Keys);
            stacks.Clear();
            stackByCell.Clear();
            reservations.Clear();
            outputReservations.Clear();
            committedOutputKeys.Clear();
            foreach (Vector2Int cell in affectedCells)
                GroundCellChanged?.Invoke(cell);
        }

        private void InitializeScenarioContext()
        {
            long sequence = Interlocked.Increment(ref nextScenarioContext);
            ScenarioContextId = $"scenario-{sequence:D8}";
        }

        private GroundStackPlacementValidation ValidatePlacement(Vector2Int cell,
            bool allowExcavationSourceSolid,
            bool allowLegacyFunctionalBackground)
        {
            if (customPlacementValidator != null)
                return customPlacementValidator(cell);
            if (world == null)
                return GroundStackPlacementValidation.Valid();
            if (world.GetCell(cell, out WorldTileCellData cellData) !=
                    WorldCellQueryResult.Available)
                return GroundStackPlacementValidation.Invalid(
                    "The ground-stack cell is not resident.");
            if (string.IsNullOrWhiteSpace(cellData.BackgroundMaterialId))
                return GroundStackPlacementValidation.Unknown(
                    "The resident cell has no reliable Background.");
            if (!world.TryGetBackgroundMaterialDefinition(cellData.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition background))
                return GroundStackPlacementValidation.Unknown(
                    "The Background definition is unavailable.");
            if (background.Function != BackgroundFunction.None &&
                !allowLegacyFunctionalBackground)
                return GroundStackPlacementValidation.Invalid(
                    "A completed functional Placement owns this cell.");
            if (cellData.HasSolidTile && !allowExcavationSourceSolid)
                return GroundStackPlacementValidation.Invalid(
                    "An obstructing Resource Node occupies this cell.");
            return GroundStackPlacementValidation.Valid();
        }

        private bool TryGetRecord(string key, out GroundStackRecord record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(key)) return false;
            return stacks.TryGetValue(new GroundStackId(key), out record);
        }

        private WorldMaterialDrop CreateSnapshot(GroundStackRecord record)
        {
            int reserved = reservations.TryGetValue(record.Id,
                out DropReservation reservation) ? reservation.Quantity : 0;
            return new WorldMaterialDrop(record.Id, record.Material,
                record.Quantity, reserved, record.Cell, record.Capacity,
                record.Revision, record.Provenance, record.Lifecycle);
        }

        private sealed class GroundStackRecord
        {
            public GroundStackRecord(GroundStackId id,
                ExcavatedMaterialType material, int quantity, Vector2Int cell,
                int capacity, string provenance)
            {
                Id = id;
                Material = material;
                Quantity = quantity;
                Cell = cell;
                Capacity = capacity;
                Provenance = provenance;
                Revision = 1L;
                Lifecycle = GroundStackLifecycle.Active;
            }

            public GroundStackId Id { get; }
            public ExcavatedMaterialType Material { get; }
            public int Quantity { get; set; }
            public Vector2Int Cell { get; }
            public int Capacity { get; }
            public string Provenance { get; }
            public long Revision { get; set; }
            public GroundStackLifecycle Lifecycle { get; set; }
        }

        private readonly struct DropReservation
        {
            public DropReservation(string ownerId, int quantity)
            { OwnerId = ownerId; Quantity = quantity; }
            public string OwnerId { get; }
            public int Quantity { get; }
        }

        private readonly struct OutputCapacityReservation
        {
            public OutputCapacityReservation(string operationId,
                ExcavatedMaterialType material,
                ExtractionOutputDestination destination)
            { OperationId = operationId; Material = material; Destination = destination; }
            public string OperationId { get; }
            public ExcavatedMaterialType Material { get; }
            public ExtractionOutputDestination Destination { get; }
        }
    }

    /// <summary>
    /// Atomic excavation output boundary. All fallible loose-drop checks are
    /// completed before foreground removal, then prepared output is committed.
    /// </summary>
    public sealed class ExcavationWorldTransaction
    {
        private const int MaximumOutputSearchRadius = 8;
        private readonly ChunkedWorldData world;
        private readonly WorldMaterialDropRegistry drops;
        private readonly ColonyResourceNodeRegistry resourceNodes;
        private readonly Action<Vector2Int> terrainCommitted;
        private readonly GridNavigationMap navigationMap;
        private readonly Func<Vector2Int, bool> outputCellBlocked;
        private readonly HashSet<string> committedOperations = new(StringComparer.Ordinal);
        private readonly List<ExtractionOutputDestination> lastPlannedDestinations = new();

        public string LastOperationId { get; private set; } = string.Empty;
        public string LastOperationPhase { get; private set; } = "None";
        public int LastOutputQuantity { get; private set; }
        public int LastReservedCapacity { get; private set; }
        public int LastPendingOutputQuantity { get; private set; }
        public string LastBlockedReason { get; private set; } = string.Empty;
        public bool LastOperationWasReplay { get; private set; }
        public IReadOnlyList<ExtractionOutputDestination> LastPlannedDestinations =>
            lastPlannedDestinations;

        public ExcavationWorldTransaction(ChunkedWorldData world,
            WorldMaterialDropRegistry drops,
            Action<Vector2Int> terrainCommitted = null,
            ColonyResourceNodeRegistry resourceNodes = null,
            GridNavigationMap navigationMap = null,
            Func<Vector2Int, bool> outputCellBlocked = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.drops = drops ?? throw new ArgumentNullException(nameof(drops));
            this.terrainCommitted = terrainCommitted;
            this.resourceNodes = resourceNodes;
            this.navigationMap = navigationMap;
            this.outputCellBlocked = outputCellBlocked;
        }

        public bool TryCommit(string ownershipKey, Vector2Int cell,
            string expectedSolidMaterialId, out WorldMaterialDrop drop, out string error)
            => TryCommit(ownershipKey, cell, expectedSolidMaterialId, cell,
                out drop, out error);

        /// <summary>
        /// Performs the same bounded output-capacity preparation used at commit,
        /// then releases it immediately. The scheduler uses this read-like probe
        /// so a blocked job never needs to monopolise an ant.
        /// </summary>
        public bool CanPrepare(string operationId, Vector2Int cell,
            string expectedSolidMaterialId, Vector2Int approachCell,
            out string error)
        {
            if (world.GetCell(cell, out WorldTileCellData cellData) !=
                    WorldCellQueryResult.Available || !cellData.HasSolidTile ||
                !string.Equals(cellData.SolidMaterialId, expectedSolidMaterialId,
                    StringComparison.Ordinal) ||
                !world.TryGetSolidMaterialDefinition(expectedSolidMaterialId,
                    out SolidTileMaterialDefinition definition) ||
                !MaterialResourceCatalog.TryGetDefinition(
                    definition.ExcavatedMaterial, out MaterialResourceDefinition resource))
            { error = "The designated foreground Solid is no longer extractable."; return false; }
            int quantity = definition.ExcavationYield;
            if (resourceNodes != null)
            {
                RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                    new ColonyCell(cell.x, cell.y));
                if (resourceNodes.TryGet(cellId, out var node) !=
                        ColonyResourceNodeReadResult.Success)
                { error = "The designated Resource Node is not registered."; return false; }
                if (!resourceNodes.CanExtract(cell, out error)) return false;
                quantity = node.RemainingQuantity;
            }
            string probeId = $"{operationId}.output-probe";
            bool prepared = TryPrepareOutputPlan(probeId,
                definition.ExcavatedMaterial, quantity,
                resource.GroundStackCapacity, cell, approachCell,
                out _, out error);
            drops.ReleaseOutputReservations(probeId);
            return prepared;
        }

        public bool TryCommit(string ownershipKey, Vector2Int cell,
            string expectedSolidMaterialId, Vector2Int approachCell,
            out WorldMaterialDrop drop, out string error)
        {
            drop = default;
            LastOperationId = ownershipKey?.Trim() ?? string.Empty;
            LastOperationPhase = "Validating";
            LastOutputQuantity = 0;
            LastReservedCapacity = 0;
            LastPendingOutputQuantity = 0;
            LastBlockedReason = string.Empty;
            LastOperationWasReplay = false;
            lastPlannedDestinations.Clear();
            if (string.IsNullOrWhiteSpace(ownershipKey))
            { error = "Extraction requires a stable operation identity."; return false; }
            ownershipKey = ownershipKey.Trim();
            if (committedOperations.Contains(ownershipKey))
            {
                LastOperationPhase = "ReplayRejected";
                LastOperationWasReplay = true;
                error = "This Extraction operation was already committed.";
                LastBlockedReason = error;
                return false;
            }
            if (world.GetCell(cell, out WorldTileCellData cellData) !=
                    WorldCellQueryResult.Available || !cellData.HasSolidTile ||
                !string.Equals(cellData.SolidMaterialId, expectedSolidMaterialId,
                    StringComparison.Ordinal) ||
                !world.TryGetSolidMaterialDefinition(cellData.SolidMaterialId,
                    out SolidTileMaterialDefinition definition) ||
                !definition.IsExcavatable ||
                definition.ExcavatedMaterial == ExcavatedMaterialType.None ||
                definition.ExcavationYield <= 0)
            {
                error = "The designated foreground Solid is no longer excavatable.";
                return false;
            }
            if (!MaterialResourceCatalog.TryGetDefinition(definition.ExcavatedMaterial,
                    out MaterialResourceDefinition resource))
            {
                error = "The excavated resource definition is unsupported.";
                return false;
            }
            int maximumStack = resource.GroundStackCapacity;
            string authorityToken = string.Empty;
            ColonyResourceNodeSnapshot node = null;
            int outputQuantity = definition.ExcavationYield;
            if (resourceNodes != null)
            {
                RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                    new ColonyCell(cell.x, cell.y));
                if (resourceNodes.TryGet(cellId, out node) !=
                        ColonyResourceNodeReadResult.Success)
                { error = "The designated Resource Node is not registered."; return false; }
                if (!resourceNodes.CanExtract(cell, out error)) return false;
                if (!string.Equals(node.DefinitionId, expectedSolidMaterialId,
                        StringComparison.Ordinal) ||
                    node.OutputMaterial != definition.ExcavatedMaterial)
                { error = "The Resource Node and Solid definition disagree."; return false; }
                outputQuantity = node.RemainingQuantity;
            }

            if (!TryPrepareOutputPlan(ownershipKey, definition.ExcavatedMaterial,
                    outputQuantity, maximumStack, cell, approachCell,
                    out List<ExtractionOutputDestination> plan, out error))
            {
                LastOperationPhase = "Blocked";
                LastBlockedReason = error;
                LastPendingOutputQuantity = outputQuantity;
                return false;
            }
            LastOutputQuantity = outputQuantity;
            LastReservedCapacity = outputQuantity;
            lastPlannedDestinations.AddRange(plan);
            LastOperationPhase = "OutputReserved";

            if (resourceNodes != null)
            {
                if (!resourceNodes.TryReserve(node.NodeId, ownershipKey,
                        node.Revision, out error) ||
                    !resourceNodes.TryBeginPendingExtraction(node.NodeId,
                        ownershipKey, node.Revision + 1L, out error) ||
                    !resourceNodes.TryPrepareExtractionRemoval(node.NodeId,
                        ownershipKey, ownershipKey, out authorityToken, out error))
                {
                    drops.ReleaseOutputReservations(ownershipKey);
                    resourceNodes.TryReleaseReservation(node.NodeId, ownershipKey, out _);
                    return false;
                }
            }
            if (!drops.CanCommitReservedOutputs(ownershipKey, plan, out error))
            {
                drops.ReleaseOutputReservations(ownershipKey);
                if (resourceNodes != null)
                    resourceNodes.TryReleaseReservation(node.NodeId, ownershipKey, out _);
                return false;
            }
            if (world.RemoveSolidMaterial(cell, authorityToken) !=
                    WorldCellMutationResult.Success)
            {
                error = string.IsNullOrWhiteSpace(world.LastSolidMutationRejection)
                    ? "The foreground Solid could not be removed."
                    : world.LastSolidMutationRejection;
                drops.ReleaseOutputReservations(ownershipKey);
                if (resourceNodes != null)
                    resourceNodes.TryReleaseReservation(node.NodeId, ownershipKey, out _);
                return false;
            }
            if (resourceNodes != null &&
                !resourceNodes.TryCommitQuantityReduction(node.NodeId, ownershipKey,
                    ownershipKey, outputQuantity, node.Revision + 2L,
                    out _, out error))
                return false;
            if (!drops.TryCommitReservedOutputs(ownershipKey, plan,
                    out IReadOnlyList<WorldMaterialDrop> outputs, out error))
                return false;
            drop = outputs[0];
            committedOperations.Add(ownershipKey);
            LastOperationPhase = "Committed";
            terrainCommitted?.Invoke(cell);
            error = string.Empty;
            return true;
        }

        private bool TryPrepareOutputPlan(string operationId,
            ExcavatedMaterialType material, int quantity, int maximumStack,
            Vector2Int sourceCell, Vector2Int approachCell,
            out List<ExtractionOutputDestination> plan, out string error)
        {
            plan = new List<ExtractionOutputDestination>();
            int remaining = quantity;
            var candidates = new List<(Vector2Int cell, int order, int distance)>();

            // Compatible stacks are considered first so existing physical
            // capacity is used before new stack identities are created.
            foreach (WorldMaterialDrop existing in drops.Drops)
                if (existing.Material == material &&
                    existing.Quantity < existing.MaximumStack &&
                    IsCandidateReachable(approachCell, existing.WorldCell))
                    candidates.Add((existing.WorldCell, 0,
                        Manhattan(sourceCell, existing.WorldCell)));
            candidates.Add((sourceCell, 1, 0));
            for (int radius = 1; radius <= MaximumOutputSearchRadius; radius++)
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                if (Math.Abs(x) + Math.Abs(y) != radius) continue;
                Vector2Int candidate = sourceCell + new Vector2Int(x, y);
                if ((outputCellBlocked?.Invoke(candidate) ?? false) ||
                    !IsCandidateReachable(approachCell, candidate) ||
                    drops.TryFindAtCell(candidate, out _)) continue;
                candidates.Add((candidate, 2, radius));
            }
            candidates.Sort((a, b) =>
            {
                int compared = a.order.CompareTo(b.order);
                if (compared != 0) return compared;
                compared = a.distance.CompareTo(b.distance);
                if (compared != 0) return compared;
                compared = a.cell.y.CompareTo(b.cell.y);
                return compared != 0 ? compared : a.cell.x.CompareTo(b.cell.x);
            });

            var seen = new HashSet<Vector2Int>();
            foreach (var candidate in candidates)
            {
                if (remaining == 0) break;
                if (!seen.Add(candidate.cell)) continue;
                int available = maximumStack;
                if (drops.TryFindAtCell(candidate.cell, out WorldMaterialDrop existing))
                    available = existing.MaximumStack - existing.Quantity;
                int assigned = Math.Min(remaining, available);
                if (assigned <= 0) continue;
                if (!drops.TryReserveOutputCapacity(operationId,
                        material, assigned, candidate.cell,
                        candidate.cell == sourceCell,
                        out ExtractionOutputDestination destination,
                        out string reserveError))
                {
                    if (reserveError.StartsWith("Development failure injection",
                            StringComparison.Ordinal))
                    {
                        drops.ReleaseOutputReservations(operationId);
                        plan.Clear();
                        error = reserveError;
                        return false;
                    }
                    continue;
                }
                plan.Add(destination);
                remaining -= assigned;
            }
            if (remaining == 0)
            { error = string.Empty; return true; }
            drops.ReleaseOutputReservations(operationId);
            plan.Clear();
            error = $"Extraction output is blocked: no deterministic physical stack plan can place {remaining} remaining unit(s).";
            return false;
        }

        private bool IsCandidateReachable(Vector2Int approachCell,
            Vector2Int candidate)
        {
            if (navigationMap == null) return candidate == approachCell;
            if (!navigationMap.TryGetAnchor(candidate, out _)) return false;
            return new GridPathfinder(navigationMap).FindPath(
                approachCell, candidate, 2048).Succeeded;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }
}
