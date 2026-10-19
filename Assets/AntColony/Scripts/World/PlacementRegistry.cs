using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AntColony.World
{
    public readonly struct PlacementId : IEquatable<PlacementId>,
        IComparable<PlacementId>
    {
        public PlacementId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A Placement ID cannot be empty.",
                    nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool Equals(PlacementId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is PlacementId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(PlacementId other) =>
            StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PlacementId left, PlacementId right) =>
            left.Equals(right);
        public static bool operator !=(PlacementId left, PlacementId right) =>
            !left.Equals(right);
    }

    [Flags]
    public enum PlacementRegionSupport
    {
        None = 0,
        Colony = 1 << 0,
        Surface = 1 << 1
    }

    public enum PlacementCategory
    {
        MovementImprovement,
        Rest,
        Storage,
        Nursery,
        Infrastructure,
        Production
    }

    public enum PlacementInteractionRule
    {
        None,
        OccupiedCell,
        AdjacentCardinal
    }

    public enum PlacementOrientation
    {
        North,
        East,
        South,
        West
    }

    public enum PlacementLifecycle
    {
        Completed,
        Retired
    }

    public enum PlacementAuthorityState
    {
        AuthoritativePlacement,
        LegacyBackgroundProjection
    }

    public enum PlacementRegistryLifecycle
    {
        Active,
        ContextInvalidated
    }

    public enum PlacementReadResult
    {
        Success,
        NotRegistered,
        StaleScenario,
        ContextInvalidated
    }

    public enum PlacementMutationResult
    {
        Success,
        NoChange,
        InvalidDefinition,
        InvalidCell,
        Occupied,
        DurabilityUnknown,
        LegacyProjectionReadOnly,
        InvalidAuthorityState,
        RevisionMismatch,
        StaleScenario,
        ContextInvalidated
    }

    public readonly struct PlacementConstructionRequirement
    {
        public PlacementConstructionRequirement(string resourceDefinitionId,
            int quantity)
        {
            if (string.IsNullOrWhiteSpace(resourceDefinitionId) || quantity <= 0)
                throw new ArgumentException(
                    "A Placement construction requirement needs a resource and positive quantity.");
            ResourceDefinitionId = resourceDefinitionId.Trim();
            Quantity = quantity;
        }

        public string ResourceDefinitionId { get; }
        public int Quantity { get; }
    }

    /// <summary>
    /// Immutable footprint metadata. WV-B3D.2 registers one-cell definitions,
    /// while preserving one identity and one anchor for future rotated footprints.
    /// </summary>
    public sealed class PlacementFootprintDefinition
    {
        private readonly Vector2Int[] relativeCells;

        public PlacementFootprintDefinition(IEnumerable<Vector2Int> relativeCells)
        {
            if (relativeCells == null)
                throw new ArgumentNullException(nameof(relativeCells));
            var cells = new List<Vector2Int>();
            var unique = new HashSet<Vector2Int>();
            bool containsAnchor = false;
            foreach (Vector2Int cell in relativeCells)
            {
                if (!unique.Add(cell))
                    throw new ArgumentException(
                        "Placement footprint cells must be unique.",
                        nameof(relativeCells));
                cells.Add(cell);
                containsAnchor |= cell == Vector2Int.zero;
            }
            if (cells.Count == 0 || !containsAnchor)
                throw new ArgumentException(
                    "A Placement footprint must contain its zero-offset anchor.",
                    nameof(relativeCells));
            this.relativeCells = cells.ToArray();
        }

        public static PlacementFootprintDefinition OneCell { get; } =
            new(new[] { Vector2Int.zero });
        public IReadOnlyList<Vector2Int> RelativeCells =>
            Array.AsReadOnly((Vector2Int[])relativeCells.Clone());
        public int CellCount => relativeCells.Length;

        public IReadOnlyList<RegionCellId> Resolve(RegionCellId anchor,
            PlacementOrientation orientation)
        {
            var cells = new RegionCellId[relativeCells.Length];
            for (int index = 0; index < relativeCells.Length; index++)
            {
                Vector2Int offset = Rotate(relativeCells[index], orientation);
                cells[index] = anchor.Kind == RegionLocationKind.Colony
                    ? RegionCellId.Colony(anchor.RegionId, new ColonyCell(
                        anchor.ColonyCell.X + offset.x,
                        anchor.ColonyCell.Y + offset.y))
                    : RegionCellId.Surface(anchor.RegionId, new SurfaceCell(
                        anchor.SurfaceCell.X + offset.x,
                        anchor.SurfaceCell.Y + offset.y));
            }
            return Array.AsReadOnly(cells);
        }

        private static Vector2Int Rotate(Vector2Int cell,
            PlacementOrientation orientation) => orientation switch
        {
            PlacementOrientation.East => new Vector2Int(cell.y, -cell.x),
            PlacementOrientation.South => new Vector2Int(-cell.x, -cell.y),
            PlacementOrientation.West => new Vector2Int(-cell.y, cell.x),
            _ => cell
        };
    }

    /// <summary>
    /// Immutable data shared by every instance of one Placement type. Mutable
    /// contents, reservations, HP and work progress never belong here.
    /// </summary>
    public sealed class PlacementDefinition
    {
        private readonly string[] compatibleBackgroundDefinitionIds;
        private readonly PlacementConstructionRequirement[] constructionRequirements;
        private readonly string[] roomQualityModifierIds;

        public PlacementDefinition(string stableId, string displayName,
            PlacementCategory category, PlacementRegionSupport supportedRegions,
            PlacementFootprintDefinition footprint,
            IEnumerable<string> compatibleBackgroundDefinitionIds,
            bool allowsObstructingNode, bool allowsLooseGroundStack,
            string constructionRecipeId,
            IEnumerable<PlacementConstructionRequirement> constructionRequirements,
            double constructionWorkHours, int minimumConstructionSkill,
            bool traversable, bool obstructsMovement,
            PlacementInteractionRule interactionRule, int functionalCapacity,
            IEnumerable<string> roomQualityModifierIds, string presentationId,
            bool allowsDismantling, int? maximumHitPoints,
            bool destructible, bool repairable,
            string damageResistanceMetadata = "")
        {
            if (string.IsNullOrWhiteSpace(stableId) ||
                string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException(
                    "A Placement definition needs stable and display names.");
            if (supportedRegions == PlacementRegionSupport.None)
                throw new ArgumentException(
                    "A Placement definition must support at least one region.");
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            if (constructionWorkHours < 0d || minimumConstructionSkill < 0 ||
                functionalCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(constructionWorkHours));
            if (maximumHitPoints.HasValue && maximumHitPoints.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumHitPoints),
                    "Known maximum HP must be positive.");
            if ((destructible || repairable) && !maximumHitPoints.HasValue)
                throw new ArgumentException(
                    "Destructible or repairable definitions require known maximum HP.");

            StableId = stableId.Trim();
            DisplayName = displayName.Trim();
            Category = category;
            SupportedRegions = supportedRegions;
            Footprint = footprint;
            this.compatibleBackgroundDefinitionIds = CopyDistinctStrings(
                compatibleBackgroundDefinitionIds);
            AllowsObstructingNode = allowsObstructingNode;
            AllowsLooseGroundStack = allowsLooseGroundStack;
            ConstructionRecipeId = constructionRecipeId?.Trim() ?? string.Empty;
            this.constructionRequirements = CopyRequirements(
                constructionRequirements);
            ConstructionWorkHours = constructionWorkHours;
            MinimumConstructionSkill = minimumConstructionSkill;
            Traversable = traversable;
            ObstructsMovement = obstructsMovement;
            InteractionRule = interactionRule;
            FunctionalCapacity = functionalCapacity;
            this.roomQualityModifierIds = CopyDistinctStrings(roomQualityModifierIds);
            PresentationId = presentationId?.Trim() ?? string.Empty;
            AllowsDismantling = allowsDismantling;
            MaximumHitPoints = maximumHitPoints;
            Destructible = destructible;
            Repairable = repairable;
            DamageResistanceMetadata = damageResistanceMetadata?.Trim() ?? string.Empty;
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public PlacementCategory Category { get; }
        public PlacementRegionSupport SupportedRegions { get; }
        public PlacementFootprintDefinition Footprint { get; }
        public IReadOnlyList<string> CompatibleBackgroundDefinitionIds =>
            Array.AsReadOnly((string[])compatibleBackgroundDefinitionIds.Clone());
        public bool AllowsObstructingNode { get; }
        public bool AllowsLooseGroundStack { get; }
        public string ConstructionRecipeId { get; }
        public IReadOnlyList<PlacementConstructionRequirement>
            ConstructionRequirements => Array.AsReadOnly(
                (PlacementConstructionRequirement[])constructionRequirements.Clone());
        public double ConstructionWorkHours { get; }
        public int MinimumConstructionSkill { get; }
        public bool Traversable { get; }
        public bool ObstructsMovement { get; }
        public PlacementInteractionRule InteractionRule { get; }
        public int FunctionalCapacity { get; }
        public IReadOnlyList<string> RoomQualityModifierIds =>
            Array.AsReadOnly((string[])roomQualityModifierIds.Clone());
        public string PresentationId { get; }
        /// <summary>
        /// Stable lookup key for authorable presentation data. Presentation
        /// assets never own mutable Placement state.
        /// </summary>
        public string VisualDefinitionId => PresentationId;
        public bool AllowsDismantling { get; }
        public int? MaximumHitPoints { get; }
        public bool Destructible { get; }
        public bool Repairable { get; }
        public string DamageResistanceMetadata { get; }
        public bool HasKnownDurability => MaximumHitPoints.HasValue;

        public bool Supports(RegionLocationKind kind) => kind switch
        {
            RegionLocationKind.Colony =>
                (SupportedRegions & PlacementRegionSupport.Colony) != 0,
            RegionLocationKind.Surface =>
                (SupportedRegions & PlacementRegionSupport.Surface) != 0,
            _ => false
        };

        public bool AcceptsNaturalBackground(string definitionId)
        {
            if (compatibleBackgroundDefinitionIds.Length == 0) return true;
            foreach (string candidate in compatibleBackgroundDefinitionIds)
                if (string.Equals(candidate, definitionId,
                        StringComparison.Ordinal)) return true;
            return false;
        }

        private static string[] CopyDistinctStrings(IEnumerable<string> values)
        {
            if (values == null) return Array.Empty<string>();
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                string normalized = value.Trim();
                if (seen.Add(normalized)) result.Add(normalized);
            }
            return result.ToArray();
        }

        private static PlacementConstructionRequirement[] CopyRequirements(
            IEnumerable<PlacementConstructionRequirement> values)
        {
            if (values == null) return Array.Empty<PlacementConstructionRequirement>();
            var result = new List<PlacementConstructionRequirement>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlacementConstructionRequirement requirement in values)
            {
                if (!seen.Add(requirement.ResourceDefinitionId))
                    throw new ArgumentException(
                        "Placement construction resources must be unique.",
                        nameof(values));
                result.Add(requirement);
            }
            return result.ToArray();
        }
    }

    public readonly struct PlacementHandle
    {
        public PlacementHandle(string scenarioContextId, PlacementId placementId)
        {
            ScenarioContextId = scenarioContextId ?? string.Empty;
            PlacementId = placementId;
        }

        public string ScenarioContextId { get; }
        public PlacementId PlacementId { get; }
    }

    /// <summary>Immutable point-in-time Placement state.</summary>
    public sealed class PlacementSnapshot
    {
        private readonly RegionCellId[] footprint;

        internal PlacementSnapshot(PlacementRecord record, string scenarioContextId)
        {
            ScenarioContextId = scenarioContextId;
            PlacementId = record.PlacementId;
            DefinitionId = record.DefinitionId;
            Anchor = record.Anchor;
            footprint = (RegionCellId[])record.Footprint.Clone();
            Orientation = record.Orientation;
            Lifecycle = record.Lifecycle;
            AuthorityState = record.AuthorityState;
            Revision = record.Revision;
            CurrentHitPoints = record.CurrentHitPoints;
            MaximumHitPoints = record.MaximumHitPoints;
        }

        public string ScenarioContextId { get; }
        public PlacementId PlacementId { get; }
        public string DefinitionId { get; }
        public RegionCellId Anchor { get; }
        public IReadOnlyList<RegionCellId> Footprint =>
            Array.AsReadOnly((RegionCellId[])footprint.Clone());
        public PlacementOrientation Orientation { get; }
        public PlacementLifecycle Lifecycle { get; }
        public PlacementAuthorityState AuthorityState { get; }
        public long Revision { get; }
        public int? CurrentHitPoints { get; }
        public int? MaximumHitPoints { get; }
        public float? ConditionPercentage => CurrentHitPoints.HasValue &&
            MaximumHitPoints.HasValue
                ? 100f * CurrentHitPoints.Value / MaximumHitPoints.Value
                : null;
        public PlacementHandle Handle => new(ScenarioContextId, PlacementId);
    }

    public readonly struct PlacementRegistryDiagnostics
    {
        public PlacementRegistryDiagnostics(string scenarioStableId,
            string scenarioContextId, PlacementRegistryLifecycle lifecycle,
            long revision, int definitionCount, int completedCount,
            int legacyProjectionCount)
        {
            ScenarioStableId = scenarioStableId;
            ScenarioContextId = scenarioContextId;
            Lifecycle = lifecycle;
            Revision = revision;
            DefinitionCount = definitionCount;
            CompletedCount = completedCount;
            LegacyProjectionCount = legacyProjectionCount;
        }

        public string ScenarioStableId { get; }
        public string ScenarioContextId { get; }
        public PlacementRegistryLifecycle Lifecycle { get; }
        public long Revision { get; }
        public int DefinitionCount { get; }
        public int CompletedCount { get; }
        public int LegacyProjectionCount { get; }
    }

    internal sealed class PlacementRecord
    {
        public PlacementRecord(PlacementId placementId, string definitionId,
            RegionCellId anchor, RegionCellId[] footprint,
            PlacementOrientation orientation, PlacementLifecycle lifecycle,
            PlacementAuthorityState authorityState, long revision,
            int? currentHitPoints, int? maximumHitPoints)
        {
            PlacementId = placementId;
            DefinitionId = definitionId;
            Anchor = anchor;
            Footprint = footprint;
            Orientation = orientation;
            Lifecycle = lifecycle;
            AuthorityState = authorityState;
            Revision = revision;
            CurrentHitPoints = currentHitPoints;
            MaximumHitPoints = maximumHitPoints;
        }

        public PlacementId PlacementId;
        public string DefinitionId;
        public RegionCellId Anchor;
        public RegionCellId[] Footprint;
        public PlacementOrientation Orientation;
        public PlacementLifecycle Lifecycle;
        public PlacementAuthorityState AuthorityState;
        public long Revision;
        public int? CurrentHitPoints;
        public int? MaximumHitPoints;
    }

    /// <summary>
    /// Scenario-scoped identity and occupancy owner. Legacy projections are
    /// observational: their functions, capacities, contents and reservations
    /// continue to belong to the existing gameplay services.
    /// </summary>
    public sealed class PlacementRegistry
    {
        private static long nextContextSequence;
        private readonly Dictionary<string, PlacementDefinition> definitions =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> legacyDefinitionByBackground =
            new(StringComparer.Ordinal);
        private readonly Dictionary<PlacementId, PlacementRecord> records = new();
        private readonly Dictionary<RegionCellId, PlacementId> occupiedCells = new();
        private long nextPlacementSequence;
        private long nextLegacyReplacementSequence;
        private PlacementRegistryLifecycle lifecycle = PlacementRegistryLifecycle.Active;

        public PlacementRegistry(string scenarioStableId,
            IEnumerable<PlacementDefinition> definitions,
            IReadOnlyDictionary<string, string> legacyDefinitionByBackground = null)
        {
            if (string.IsNullOrWhiteSpace(scenarioStableId))
                throw new ArgumentException(
                    "A Placement registry requires stable scenario identity.",
                    nameof(scenarioStableId));
            ScenarioStableId = scenarioStableId.Trim();
            long context = Interlocked.Increment(ref nextContextSequence);
            ScenarioContextId = $"placement-context-{context:D8}";

            if (definitions != null)
            {
                foreach (PlacementDefinition definition in definitions)
                {
                    if (definition == null ||
                        !this.definitions.TryAdd(definition.StableId, definition))
                        throw new ArgumentException(
                            "Placement Definition IDs must be unique.",
                            nameof(definitions));
                }
            }

            if (legacyDefinitionByBackground != null)
            {
                foreach (KeyValuePair<string, string> mapping in
                    legacyDefinitionByBackground)
                {
                    if (string.IsNullOrWhiteSpace(mapping.Key) ||
                        !this.definitions.ContainsKey(mapping.Value) ||
                        !this.legacyDefinitionByBackground.TryAdd(
                            mapping.Key.Trim(), mapping.Value))
                        throw new ArgumentException(
                            "Legacy Background mappings require unique Backgrounds and registered Placement Definitions.",
                            nameof(legacyDefinitionByBackground));
                }
            }
        }

        public string ScenarioStableId { get; }
        public string ScenarioContextId { get; }
        public PlacementRegistryLifecycle Lifecycle => lifecycle;
        public long Revision { get; private set; } = 1L;
        public int DefinitionCount => definitions.Count;
        /// <summary>
        /// Number of completed identities, not occupied cells. A future
        /// multi-cell Placement still counts as one structure.
        /// </summary>
        public int Count => Diagnostics.CompletedCount;
        public event Action<IReadOnlyList<RegionCellId>> PlacementChanged;

        public IReadOnlyList<PlacementDefinition> Definitions
        {
            get
            {
                var result = new List<PlacementDefinition>(definitions.Values);
                result.Sort((left, right) => StringComparer.Ordinal.Compare(
                    left.StableId, right.StableId));
                return result.AsReadOnly();
            }
        }

        public PlacementRegistryDiagnostics Diagnostics
        {
            get
            {
                int completed = 0;
                int legacy = 0;
                foreach (PlacementRecord record in records.Values)
                {
                    if (record.Lifecycle != PlacementLifecycle.Completed) continue;
                    completed++;
                    if (record.AuthorityState ==
                        PlacementAuthorityState.LegacyBackgroundProjection)
                        legacy++;
                }
                return new PlacementRegistryDiagnostics(ScenarioStableId,
                    ScenarioContextId, lifecycle, Revision, definitions.Count,
                    completed, legacy);
            }
        }

        public bool TryBuildLegacyProjections(ChunkedWorldData world,
            out string error)
        {
            if (world == null)
            {
                error = "Colony terrain is unavailable.";
                return false;
            }
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
            {
                error = "The Placement scenario has already been retired.";
                return false;
            }

            foreach (Vector2Int chunk in world.GetResidentChunkCoordinates())
            for (int y = 0; y < WorldTileCoordinates.ChunkSize; y++)
            for (int x = 0; x < WorldTileCoordinates.ChunkSize; x++)
            {
                Vector2Int cell = WorldTileCoordinates.GetWorldCellCoordinate(
                    chunk, new Vector2Int(x, y));
                if (world.GetCell(cell, out WorldTileCellData data) !=
                        WorldCellQueryResult.Available ||
                    string.IsNullOrWhiteSpace(data.BackgroundMaterialId))
                {
                    error = $"Placement projection could not read resident cell {cell}.";
                    return false;
                }
                RefreshLegacyProjection(RegionCellId.Colony(AntRegionIds.Colony,
                    new ColonyCell(cell.x, cell.y)), data.BackgroundMaterialId);
            }

            error = string.Empty;
            return true;
        }

        public bool TryGetDefinition(string definitionId,
            out PlacementDefinition definition) => definitions.TryGetValue(
                definitionId ?? string.Empty, out definition);

        public PlacementReadResult TryGet(PlacementId placementId,
            out PlacementSnapshot snapshot)
        {
            snapshot = null;
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
                return PlacementReadResult.ContextInvalidated;
            if (!records.TryGetValue(placementId, out PlacementRecord record))
                return PlacementReadResult.NotRegistered;
            snapshot = new PlacementSnapshot(record, ScenarioContextId);
            return PlacementReadResult.Success;
        }

        public PlacementReadResult TryGet(PlacementHandle handle,
            out PlacementSnapshot snapshot)
        {
            snapshot = null;
            if (!string.Equals(handle.ScenarioContextId, ScenarioContextId,
                    StringComparison.Ordinal)) return PlacementReadResult.StaleScenario;
            return TryGet(handle.PlacementId, out snapshot);
        }

        public PlacementReadResult TryGetByCell(RegionCellId cellId,
            out PlacementSnapshot snapshot)
        {
            snapshot = null;
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
                return PlacementReadResult.ContextInvalidated;
            if (!occupiedCells.TryGetValue(cellId, out PlacementId placementId))
                return PlacementReadResult.NotRegistered;
            return TryGet(placementId, out snapshot);
        }

        /// <summary>
        /// Registers current legacy state without granting function, capacity or
        /// durability authority to the Placement registry.
        /// </summary>
        public bool RefreshLegacyProjection(RegionCellId cellId,
            string backgroundDefinitionId)
        {
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
                return false;
            legacyDefinitionByBackground.TryGetValue(
                backgroundDefinitionId ?? string.Empty, out string definitionId);

            PlacementRecord current = null;
            if (occupiedCells.TryGetValue(cellId, out PlacementId currentId) &&
                records.TryGetValue(currentId, out PlacementRecord found))
            {
                // Legacy observation must never displace a future authoritative
                // Placement if a caller changes its underlying Background.
                if (found.AuthorityState !=
                        PlacementAuthorityState.LegacyBackgroundProjection)
                    return false;
                current = found;
            }

            if (current != null && string.Equals(current.DefinitionId,
                    definitionId, StringComparison.Ordinal)) return false;

            bool changed = false;
            if (current != null)
            {
                occupiedCells.Remove(cellId);
                current.Lifecycle = PlacementLifecycle.Retired;
                current.Revision++;
                changed = true;
            }

            if (!string.IsNullOrEmpty(definitionId))
            {
                PlacementId id = CreateLegacyId(cellId, definitionId);
                if (records.ContainsKey(id))
                    id = new PlacementId($"{id.Value}.replacement." +
                        $"{++nextLegacyReplacementSequence:D8}");
                var footprint = new[] { cellId };
                var record = new PlacementRecord(id, definitionId, cellId,
                    footprint, PlacementOrientation.North,
                    PlacementLifecycle.Completed,
                    PlacementAuthorityState.LegacyBackgroundProjection,
                    1L, null, null);
                records[id] = record;
                occupiedCells[cellId] = id;
                changed = true;
            }

            if (changed) Revision++;
            return changed;
        }

        /// <summary>
        /// Foundation API for a later completion cutover. Calling it creates
        /// identity/occupancy only; no gameplay function or capacity is registered.
        /// </summary>
        public PlacementMutationResult TryRegisterCompleted(string definitionId,
            RegionCellId anchor, PlacementOrientation orientation,
            out PlacementSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
                return Fail(PlacementMutationResult.ContextInvalidated,
                    "The Placement scenario has been retired.", out error);
            if (!definitions.TryGetValue(definitionId ?? string.Empty,
                    out PlacementDefinition definition))
                return Fail(PlacementMutationResult.InvalidDefinition,
                    "The Placement Definition is not registered.", out error);
            if (!definition.Supports(anchor.Kind))
                return Fail(PlacementMutationResult.InvalidCell,
                    "The Placement Definition does not support this region.", out error);
            if (!definition.MaximumHitPoints.HasValue)
                return Fail(PlacementMutationResult.DurabilityUnknown,
                    "Authoritative completion requires approved maximum HP.", out error);

            IReadOnlyList<RegionCellId> resolved = definition.Footprint.Resolve(
                anchor, orientation);
            var footprint = new RegionCellId[resolved.Count];
            for (int index = 0; index < resolved.Count; index++)
            {
                RegionCellId cell = resolved[index];
                if (cell.RegionId != anchor.RegionId || cell.Kind != anchor.Kind)
                    return Fail(PlacementMutationResult.InvalidCell,
                        "The Placement footprint crossed a typed region boundary.",
                        out error);
                if (occupiedCells.ContainsKey(cell))
                    return Fail(PlacementMutationResult.Occupied,
                        "A Placement already occupies part of the footprint.",
                        out error);
                footprint[index] = cell;
            }

            PlacementId id = new($"placement.{ScenarioStableId}." +
                $"{++nextPlacementSequence:D8}");
            var record = new PlacementRecord(id, definition.StableId, anchor,
                footprint, orientation, PlacementLifecycle.Completed,
                PlacementAuthorityState.AuthoritativePlacement, 1L,
                definition.MaximumHitPoints, definition.MaximumHitPoints);
            records.Add(id, record);
            foreach (RegionCellId cell in footprint) occupiedCells.Add(cell, id);
            Revision++;
            snapshot = new PlacementSnapshot(record, ScenarioContextId);
            PlacementChanged?.Invoke(snapshot.Footprint);
            error = string.Empty;
            return PlacementMutationResult.Success;
        }

        /// <summary>
        /// Promotes an already-observed legacy projection during a quiescent
        /// scenario-initialisation cutover. The identity and nullable durability
        /// state are retained; this method does not create function contents.
        /// Every target is validated before any record changes.
        /// </summary>
        public PlacementMutationResult TryPromoteLegacyProjections(
            IReadOnlyList<RegionCellId> cells, string expectedDefinitionId,
            out string error)
        {
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
                return Fail(PlacementMutationResult.ContextInvalidated,
                    "The Placement scenario has been retired.", out error);
            if (cells == null || cells.Count == 0)
            {
                error = string.Empty;
                return PlacementMutationResult.NoChange;
            }
            if (!definitions.ContainsKey(expectedDefinitionId ?? string.Empty))
                return Fail(PlacementMutationResult.InvalidDefinition,
                    "The Placement Definition is not registered.", out error);

            var recordsToPromote = new List<PlacementRecord>(cells.Count);
            var changedCells = new List<RegionCellId>(cells.Count);
            var seen = new HashSet<PlacementId>();
            foreach (RegionCellId cell in cells)
            {
                if (!occupiedCells.TryGetValue(cell, out PlacementId id) ||
                    !records.TryGetValue(id, out PlacementRecord record) ||
                    record.Lifecycle != PlacementLifecycle.Completed ||
                    record.AuthorityState !=
                        PlacementAuthorityState.LegacyBackgroundProjection ||
                    !string.Equals(record.DefinitionId, expectedDefinitionId,
                        StringComparison.Ordinal) ||
                    record.Footprint.Length != 1 || record.Anchor != cell ||
                    !seen.Add(id))
                    return Fail(PlacementMutationResult.InvalidAuthorityState,
                        "Every cutover cell must identify one matching completed legacy projection.",
                        out error);
                recordsToPromote.Add(record);
                changedCells.Add(cell);
            }

            foreach (PlacementRecord record in recordsToPromote)
            {
                record.AuthorityState =
                    PlacementAuthorityState.AuthoritativePlacement;
                record.Revision++;
            }
            Revision++;
            PlacementChanged?.Invoke(changedCells.AsReadOnly());
            error = string.Empty;
            return PlacementMutationResult.Success;
        }

        /// <summary>
        /// Commits bounded structural state only. Damage, Repair, destruction,
        /// material staging and gameplay targeting remain future systems.
        /// </summary>
        public PlacementMutationResult TryCommitHitPoints(PlacementHandle handle,
            int requestedHitPoints, long expectedRevision,
            out PlacementSnapshot snapshot)
        {
            snapshot = null;
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated)
                return PlacementMutationResult.ContextInvalidated;
            if (!string.Equals(handle.ScenarioContextId, ScenarioContextId,
                    StringComparison.Ordinal)) return PlacementMutationResult.StaleScenario;
            if (!records.TryGetValue(handle.PlacementId,
                    out PlacementRecord record))
                return PlacementMutationResult.StaleScenario;
            if (record.AuthorityState !=
                    PlacementAuthorityState.AuthoritativePlacement)
                return PlacementMutationResult.LegacyProjectionReadOnly;
            if (!record.MaximumHitPoints.HasValue ||
                !record.CurrentHitPoints.HasValue)
                return PlacementMutationResult.DurabilityUnknown;
            if (record.Revision != expectedRevision)
                return PlacementMutationResult.RevisionMismatch;

            int bounded = Math.Max(0, Math.Min(record.MaximumHitPoints.Value,
                requestedHitPoints));
            if (bounded == record.CurrentHitPoints.Value)
            {
                snapshot = new PlacementSnapshot(record, ScenarioContextId);
                return PlacementMutationResult.NoChange;
            }

            record.CurrentHitPoints = bounded;
            record.Revision++;
            Revision++;
            snapshot = new PlacementSnapshot(record, ScenarioContextId);
            PlacementChanged?.Invoke(snapshot.Footprint);
            return PlacementMutationResult.Success;
        }

        public void InvalidateContext()
        {
            if (lifecycle == PlacementRegistryLifecycle.ContextInvalidated) return;
            lifecycle = PlacementRegistryLifecycle.ContextInvalidated;
            Revision++;
            foreach (PlacementRecord record in records.Values)
            {
                if (record.Lifecycle == PlacementLifecycle.Retired) continue;
                record.Lifecycle = PlacementLifecycle.Retired;
                record.Revision++;
            }
            occupiedCells.Clear();
        }

        public int GetCompletedCount(PlacementCategory category)
        {
            int count = 0;
            foreach (PlacementRecord record in records.Values)
                if (record.Lifecycle == PlacementLifecycle.Completed &&
                    definitions.TryGetValue(record.DefinitionId,
                        out PlacementDefinition definition) &&
                    definition.Category == category) count++;
            return count;
        }

        private PlacementId CreateLegacyId(RegionCellId cellId,
            string definitionId)
        {
            string coordinate = cellId.Kind == RegionLocationKind.Colony
                ? $"colony.{cellId.ColonyCell.X}.{cellId.ColonyCell.Y}"
                : $"surface.{cellId.SurfaceCell.X}.{cellId.SurfaceCell.Y}";
            return new PlacementId(
                $"placement.legacy.{ScenarioStableId}.{coordinate}.{definitionId}");
        }

        private static PlacementMutationResult Fail(PlacementMutationResult result,
            string message, out string error)
        {
            error = message;
            return result;
        }
    }

    /// <summary>
    /// Pure, conservative compatibility checks. Unknown natural provenance is
    /// never promoted to Valid for a future mutation.
    /// </summary>
    public static class PlacementCompatibilityValidator
    {
        public static RuntimeOccupancyValidation Validate(
            PlacementDefinition definition, RuntimeCellSnapshot snapshot,
            bool hasConflictingConstructionSite)
        {
            if (definition == null || snapshot == null)
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Unknown,
                    "Placement or cell details are unavailable.");
            if (!definition.Supports(snapshot.CellId.Kind))
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Invalid,
                    "The Placement Definition does not support this region.");
            if (hasConflictingConstructionSite)
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Invalid,
                    "A Construction Site already occupies this cell.");
            if (snapshot.Placement != null)
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Invalid,
                    "A completed Placement already occupies this cell.");
            if (snapshot.Node != null)
            {
                if (!snapshot.Node.ObstructsMovement.HasValue)
                    return new RuntimeOccupancyValidation(
                        RuntimeOccupancyCompatibility.Unknown,
                        "Resource Node obstruction is unavailable.");
                if (snapshot.Node.ObstructsMovement.Value &&
                    !definition.AllowsObstructingNode)
                    return new RuntimeOccupancyValidation(
                        RuntimeOccupancyCompatibility.Invalid,
                        "An obstructing Resource Node occupies this cell.");
            }
            if (snapshot.LooseStack != null &&
                !definition.AllowsLooseGroundStack)
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Invalid,
                    "An incompatible loose resource stack occupies this cell.");
            if (snapshot.NaturalBackgroundProjectionState ==
                    RuntimeOccupantProjectionState.DetailsUnavailable)
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Unknown,
                    "The natural Background behind legacy function data is unknown.");
            if (!definition.AcceptsNaturalBackground(
                    snapshot.NaturalBackgroundDefinitionId))
                return new RuntimeOccupancyValidation(
                    RuntimeOccupancyCompatibility.Invalid,
                    "The natural Background is incompatible with this Placement.");
            return new RuntimeOccupancyValidation(
                RuntimeOccupancyCompatibility.Valid, string.Empty);
        }
    }
}
