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
    }

    /// <summary>
    /// Atomic excavation output boundary. All fallible loose-drop checks are
    /// completed before foreground removal, then prepared output is committed.
    /// </summary>
    public sealed class ExcavationWorldTransaction
    {
        private readonly ChunkedWorldData world;
        private readonly WorldMaterialDropRegistry drops;
        private readonly ColonyResourceNodeRegistry resourceNodes;
        private readonly Action<Vector2Int> terrainCommitted;

        public ExcavationWorldTransaction(ChunkedWorldData world,
            WorldMaterialDropRegistry drops,
            Action<Vector2Int> terrainCommitted = null,
            ColonyResourceNodeRegistry resourceNodes = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.drops = drops ?? throw new ArgumentNullException(nameof(drops));
            this.terrainCommitted = terrainCommitted;
            this.resourceNodes = resourceNodes;
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
            // Check compatibility first so a variable Node always reports its
            // deliberate WV-B3C.3B deferral instead of a downstream drop error.
            if (resourceNodes != null &&
                !resourceNodes.CanUseLegacyExtraction(cell, out error)) return false;
            if (!drops.CanCommitOutput(ownershipKey, definition.ExcavatedMaterial,
                    definition.ExcavationYield, cell, maximumStack, true, true, false,
                    out error)) return false;
            // Mint the one-use terrain token only after every other fallible
            // preflight has succeeded, so failed attempts leave no authority.
            if (resourceNodes != null &&
                !resourceNodes.TryPrepareLegacyExtractionRemoval(cell, ownershipKey,
                    out authorityToken, out error)) return false;
            if (world.RemoveSolidMaterial(cell, authorityToken) !=
                    WorldCellMutationResult.Success)
            {
                error = string.IsNullOrWhiteSpace(world.LastSolidMutationRejection)
                    ? "The foreground Solid could not be removed."
                    : world.LastSolidMutationRejection;
                return false;
            }
            // Extraction leaves physical material for later explicit hauling.
            drop = drops.CommitOutput(ownershipKey, definition.ExcavatedMaterial,
                definition.ExcavationYield, cell, maximumStack);
            terrainCommitted?.Invoke(cell);
            error = string.Empty;
            return true;
        }
    }
}
