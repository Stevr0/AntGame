using System;
using System.Collections.Generic;

namespace AntColony.World
{
    public enum RuntimeCellReadResult
    {
        Success,
        InvalidRegion,
        InvalidCoordinateKind,
        RegionUnpublished,
        CellUnavailable,
        MissingBackground,
        InconsistentProjection
    }

    public enum RuntimeOccupantProjectionState
    {
        Absent,
        Authoritative,
        CompatibilityProjected,
        DetailsUnavailable
    }

    public enum RuntimeNodeInteractionState
    {
        Unsupported,
        GenerationOnly,
        CompatibilityOnly,
        Interactive
    }

    [Flags]
    public enum RuntimeCellChangeCategory
    {
        None = 0,
        Background = 1 << 0,
        Node = 1 << 1,
        Placement = 1 << 2,
        LooseResource = 1 << 3,
        RegionPublication = 1 << 4
    }

    /// <summary>Read-only Node information; null quantities mean not authoritative.</summary>
    public sealed class RuntimeNodeSnapshot
    {
        public RuntimeNodeSnapshot(RuntimeOccupantProjectionState projectionState,
            string stableId, string definitionId, bool? obstructsMovement,
            int? initialQuantity, int? remainingQuantity,
            RuntimeNodeInteractionState interactionState,
            string resourceDefinitionId = "", long? nodeRevision = null,
            ColonyResourceNodeLifecycle? lifecycle = null,
            ColonyResourceNodeExtractionStatus? extractionStatus = null)
        {
            ProjectionState = projectionState;
            StableId = stableId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            ObstructsMovement = obstructsMovement;
            InitialQuantity = initialQuantity;
            RemainingQuantity = remainingQuantity;
            InteractionState = interactionState;
            ResourceDefinitionId = resourceDefinitionId ?? string.Empty;
            NodeRevision = nodeRevision;
            Lifecycle = lifecycle;
            ExtractionStatus = extractionStatus;
        }

        public RuntimeOccupantProjectionState ProjectionState { get; }
        public string StableId { get; }
        public string DefinitionId { get; }
        public bool? ObstructsMovement { get; }
        public int? InitialQuantity { get; }
        public int? RemainingQuantity { get; }
        public RuntimeNodeInteractionState InteractionState { get; }
        public string ResourceDefinitionId { get; }
        public long? NodeRevision { get; }
        public ColonyResourceNodeLifecycle? Lifecycle { get; }
        public ColonyResourceNodeExtractionStatus? ExtractionStatus { get; }
        public bool HasCompleteDetails => !string.IsNullOrEmpty(StableId) &&
            !string.IsNullOrEmpty(DefinitionId) && ObstructsMovement.HasValue &&
            InitialQuantity.HasValue && RemainingQuantity.HasValue;
    }

    public enum RuntimePlacementFunctionAuthority
    {
        None,
        LegacyBackground,
        DelegatedMaterialStorage
    }

    /// <summary>Compatibility view of one built or authored function.</summary>
    public sealed class RuntimePlacementSnapshot
    {
        public RuntimePlacementSnapshot(RuntimeOccupantProjectionState projectionState,
            string stableId, string definitionId, string functionId,
            bool contentsArePlacementOwned,
            string scenarioContextId = "",
            RegionCellId? anchor = null,
            IEnumerable<RegionCellId> footprint = null,
            PlacementLifecycle lifecycle = PlacementLifecycle.Completed,
            PlacementAuthorityState authorityState =
                PlacementAuthorityState.LegacyBackgroundProjection,
            long placementRevision = 1L,
            int? currentHitPoints = null,
            int? maximumHitPoints = null,
            int? functionalCapacity = null,
            RuntimePlacementFunctionAuthority functionAuthority =
                RuntimePlacementFunctionAuthority.None,
            long? functionRevision = null,
            string resourceFilterId = "",
            string storedResourceId = "",
            int? storedQuantity = null,
            int? availableCapacity = null,
            int? reservedIncomingQuantity = null,
            int? reservedWithdrawalQuantity = null,
            string visualDefinitionId = "",
            PlacementVisualState visualState = PlacementVisualState.Completed)
        {
            ProjectionState = projectionState;
            StableId = stableId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            FunctionId = functionId ?? string.Empty;
            ContentsArePlacementOwned = contentsArePlacementOwned;
            ScenarioContextId = scenarioContextId ?? string.Empty;
            Anchor = anchor;
            Footprint = footprint == null
                ? Array.Empty<RegionCellId>()
                : new List<RegionCellId>(footprint).AsReadOnly();
            Lifecycle = lifecycle;
            AuthorityState = authorityState;
            PlacementRevision = Math.Max(1L, placementRevision);
            CurrentHitPoints = currentHitPoints;
            MaximumHitPoints = maximumHitPoints;
            FunctionalCapacity = functionalCapacity;
            FunctionAuthority = functionAuthority;
            FunctionRevision = functionRevision;
            ResourceFilterId = resourceFilterId ?? string.Empty;
            StoredResourceId = storedResourceId ?? string.Empty;
            StoredQuantity = storedQuantity;
            AvailableCapacity = availableCapacity;
            ReservedIncomingQuantity = reservedIncomingQuantity;
            ReservedWithdrawalQuantity = reservedWithdrawalQuantity;
            VisualDefinitionId = visualDefinitionId ?? string.Empty;
            VisualState = visualState;
        }

        public RuntimeOccupantProjectionState ProjectionState { get; }
        public string StableId { get; }
        public string DefinitionId { get; }
        public string FunctionId { get; }
        public bool ContentsArePlacementOwned { get; }
        public string ScenarioContextId { get; }
        public RegionCellId? Anchor { get; }
        public IReadOnlyList<RegionCellId> Footprint { get; }
        public PlacementLifecycle Lifecycle { get; }
        public PlacementAuthorityState AuthorityState { get; }
        public long PlacementRevision { get; }
        public int? CurrentHitPoints { get; }
        public int? MaximumHitPoints { get; }
        public int? FunctionalCapacity { get; }
        public RuntimePlacementFunctionAuthority FunctionAuthority { get; }
        public long? FunctionRevision { get; }
        public string ResourceFilterId { get; }
        public string StoredResourceId { get; }
        public int? StoredQuantity { get; }
        public int? AvailableCapacity { get; }
        public int? ReservedIncomingQuantity { get; }
        public int? ReservedWithdrawalQuantity { get; }
        public string VisualDefinitionId { get; }
        public PlacementVisualState VisualState { get; }
        public float? ConditionPercentage => CurrentHitPoints.HasValue &&
            MaximumHitPoints.HasValue
                ? 100f * CurrentHitPoints.Value / MaximumHitPoints.Value
                : null;
    }

    /// <summary>
    /// Read-only loose occupancy. A missing stable ID means several legacy
    /// records were conservatively projected as one occupied cell.
    /// </summary>
    public sealed class RuntimeLooseStackSnapshot
    {
        public RuntimeLooseStackSnapshot(RuntimeOccupantProjectionState projectionState,
            string stableId, string resourceTypeId, int? quantity, int? capacity,
            bool representsMultipleLegacyRecords, int? reservedQuantity = null,
            int? availableQuantity = null, long? stackRevision = null,
            RegionCellId? cellId = null)
        {
            ProjectionState = projectionState;
            StableId = stableId ?? string.Empty;
            ResourceTypeId = resourceTypeId ?? string.Empty;
            Quantity = quantity;
            Capacity = capacity;
            RepresentsMultipleLegacyRecords = representsMultipleLegacyRecords;
            ReservedQuantity = reservedQuantity;
            AvailableQuantity = availableQuantity;
            StackRevision = stackRevision;
            CellId = cellId;
        }

        public RuntimeOccupantProjectionState ProjectionState { get; }
        public string StableId { get; }
        public string ResourceTypeId { get; }
        public int? Quantity { get; }
        public int? Capacity { get; }
        public bool RepresentsMultipleLegacyRecords { get; }
        public int? ReservedQuantity { get; }
        public int? AvailableQuantity { get; }
        public long? StackRevision { get; }
        public RegionCellId? CellId { get; }
    }

    /// <summary>
    /// Immutable point-in-time read. Consumers compare CellRevision or query the
    /// region change feed before reusing it.
    /// </summary>
    public sealed class RuntimeCellSnapshot
    {
        public RuntimeCellSnapshot(RegionCellId cellId, string backgroundDefinitionId,
            long cellRevision, long regionPublication,
            RuntimeNodeSnapshot node = null,
            RuntimePlacementSnapshot placement = null,
            RuntimeLooseStackSnapshot looseStack = null,
            string naturalBackgroundDefinitionId = null,
            RuntimeOccupantProjectionState naturalBackgroundProjectionState =
                RuntimeOccupantProjectionState.Authoritative,
            string legacyFunctionalBackgroundDefinitionId = "")
        {
            CellId = cellId;
            BackgroundDefinitionId = backgroundDefinitionId ?? string.Empty;
            CellRevision = cellRevision;
            RegionPublication = regionPublication;
            Node = node;
            Placement = placement;
            LooseStack = looseStack;
            NaturalBackgroundDefinitionId = naturalBackgroundDefinitionId ??
                BackgroundDefinitionId;
            NaturalBackgroundProjectionState = naturalBackgroundProjectionState;
            LegacyFunctionalBackgroundDefinitionId =
                legacyFunctionalBackgroundDefinitionId ?? string.Empty;
        }

        public RegionCellId CellId { get; }
        public string BackgroundDefinitionId { get; }
        public long CellRevision { get; }
        public long RegionPublication { get; }
        public RuntimeNodeSnapshot Node { get; }
        public RuntimePlacementSnapshot Placement { get; }
        public RuntimeLooseStackSnapshot LooseStack { get; }
        /// <summary>
        /// Natural environmental provenance. Legacy functional Backgrounds did
        /// not retain this value, so an empty value with DetailsUnavailable is
        /// more honest than inventing Earth or Stone.
        /// </summary>
        public string NaturalBackgroundDefinitionId { get; }
        public RuntimeOccupantProjectionState NaturalBackgroundProjectionState
        {
            get;
        }
        public string LegacyFunctionalBackgroundDefinitionId { get; }
    }

    public readonly struct RuntimeCellChange
    {
        public RuntimeCellChange(RegionCellId cellId, long oldRevision,
            long newRevision, RuntimeCellChangeCategory category)
        {
            CellId = cellId;
            OldRevision = oldRevision;
            NewRevision = newRevision;
            Category = category;
        }

        public RegionCellId CellId { get; }
        public long OldRevision { get; }
        public long NewRevision { get; }
        public RuntimeCellChangeCategory Category { get; }
    }

    /// <summary>One immutable region-scoped batch emitted after a committed change.</summary>
    public sealed class RuntimeCellChangeSet
    {
        private readonly RuntimeCellChange[] changes;

        public RuntimeCellChangeSet(RegionId regionId, long sequence,
            IEnumerable<RuntimeCellChange> changes)
        {
            RegionId = regionId;
            Sequence = sequence;
            this.changes = changes == null
                ? Array.Empty<RuntimeCellChange>()
                : new List<RuntimeCellChange>(changes).ToArray();
        }

        public RegionId RegionId { get; }
        public long Sequence { get; }
        public IReadOnlyList<RuntimeCellChange> Changes => changes;
    }

    public enum RuntimeCellChangeReadResult
    {
        Success,
        NoChanges,
        SnapshotRequired
    }

    /// <summary>
    /// Bounded journal with caller-owned cursors. Multiple consumers can read the
    /// same committed changes without consuming them for one another.
    /// </summary>
    public sealed class RuntimeCellChangeFeed
    {
        private readonly int capacity;
        private readonly List<RuntimeCellChangeSet> entries = new();

        public RuntimeCellChangeFeed(RegionId regionId, int capacity = 256)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            RegionId = regionId;
            this.capacity = capacity;
        }

        public RegionId RegionId { get; }
        public long CurrentSequence { get; private set; }
        public long OldestAvailableSequence => entries.Count == 0
            ? CurrentSequence + 1 : entries[0].Sequence;

        public RuntimeCellChangeSet Publish(IEnumerable<RuntimeCellChange> changes)
        {
            var entry = new RuntimeCellChangeSet(RegionId, ++CurrentSequence, changes);
            entries.Add(entry);
            if (entries.Count > capacity) entries.RemoveAt(0);
            return entry;
        }

        public RuntimeCellChangeReadResult ReadAfter(long sequence,
            out IReadOnlyList<RuntimeCellChangeSet> changeSets)
        {
            if (sequence < 0 || sequence < OldestAvailableSequence - 1)
            {
                changeSets = Array.Empty<RuntimeCellChangeSet>();
                return RuntimeCellChangeReadResult.SnapshotRequired;
            }
            if (sequence >= CurrentSequence)
            {
                changeSets = Array.Empty<RuntimeCellChangeSet>();
                return RuntimeCellChangeReadResult.NoChanges;
            }
            var result = new List<RuntimeCellChangeSet>();
            foreach (RuntimeCellChangeSet entry in entries)
                if (entry.Sequence > sequence) result.Add(entry);
            changeSets = result.ToArray();
            return RuntimeCellChangeReadResult.Success;
        }
    }

    public interface IReadOnlyRuntimeCellRegion
    {
        RegionId RegionId { get; }
        RegionLocationKind CoordinateKind { get; }
        long PublicationRevision { get; }
        RuntimeCellChangeFeed Changes { get; }
        RuntimeCellReadResult TryGetCell(RegionCellId cellId,
            out RuntimeCellSnapshot snapshot);
        IEnumerable<RegionCellId> EnumerateResidentCells();
    }

    public enum RuntimeOccupancyCompatibility
    {
        Valid,
        Invalid,
        Unknown
    }

    public enum RuntimeOccupancyOperation
    {
        Observe,
        StandardConstruction,
        AddLooseResource
    }

    public readonly struct RuntimeOccupancyValidation
    {
        public RuntimeOccupancyValidation(RuntimeOccupancyCompatibility result,
            string reason)
        {
            Result = result;
            Reason = reason ?? string.Empty;
        }

        public RuntimeOccupancyCompatibility Result { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// No exception is enabled by default. A future definition-specific policy
    /// must explicitly approve a combination instead of adding a global loophole.
    /// </summary>
    public interface IRuntimeOccupancyExceptionPolicy
    {
        bool IsExplicitlyAllowed(RuntimeCellSnapshot snapshot,
            RuntimeOccupancyOperation operation);
    }

    public static class RuntimeCellCompatibilityValidator
    {
        public static RuntimeOccupancyValidation ValidateObserved(
            RuntimeCellSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(
                    snapshot.BackgroundDefinitionId))
                return Unknown("A resident Background could not be established.");
            if (snapshot.LooseStack != null && string.IsNullOrWhiteSpace(
                    snapshot.LooseStack.ResourceTypeId))
                return Unknown("Legacy loose occupancy has no reliable resource type.");
            if (snapshot.LooseStack?.RepresentsMultipleLegacyRecords == true)
                return Unknown("Several legacy drop records share this cell.");
            if (snapshot.Placement?.AuthorityState ==
                    PlacementAuthorityState.LegacyBackgroundProjection &&
                snapshot.NaturalBackgroundProjectionState ==
                    RuntimeOccupantProjectionState.DetailsUnavailable)
                return Unknown(
                    "Legacy functional Background data obscures natural provenance.");
            if (snapshot.Node?.ProjectionState ==
                    RuntimeOccupantProjectionState.DetailsUnavailable &&
                snapshot.Placement != null)
                return Unknown("Node obstruction is unknown beside a Placement.");
            if (snapshot.Node?.ObstructsMovement == true && snapshot.Placement != null)
                return Invalid("An obstructing Node and Placement occupy one cell.");
            return Valid();
        }

        public static RuntimeOccupancyValidation ValidateStandardConstruction(
            RuntimeCellSnapshot snapshot,
            IRuntimeOccupancyExceptionPolicy exceptionPolicy = null)
        {
            RuntimeOccupancyValidation observed = ValidateObserved(snapshot);
            if (observed.Result != RuntimeOccupancyCompatibility.Valid) return observed;
            if (snapshot.Node != null)
            {
                if (!snapshot.Node.ObstructsMovement.HasValue)
                    return Unknown("Legacy Node obstruction is unavailable.");
                if (snapshot.Node.ObstructsMovement.Value &&
                    (exceptionPolicy == null || !exceptionPolicy.IsExplicitlyAllowed(
                        snapshot, RuntimeOccupancyOperation.StandardConstruction)))
                    return Invalid("Standard Construction cannot occupy an obstructing Node.");
            }
            if (snapshot.LooseStack != null &&
                (exceptionPolicy == null || !exceptionPolicy.IsExplicitlyAllowed(
                    snapshot, RuntimeOccupancyOperation.StandardConstruction)))
                return Invalid("Standard Construction requires incompatible loose output to be cleared.");
            if (snapshot.Placement != null)
                return Invalid("The cell already projects a Placement.");
            return Valid();
        }

        public static RuntimeOccupancyValidation ValidateLooseMerge(
            RuntimeCellSnapshot snapshot, string resourceTypeId, int quantity,
            int capacity, IRuntimeOccupancyExceptionPolicy exceptionPolicy = null)
        {
            RuntimeOccupancyValidation observed = ValidateObserved(snapshot);
            if (observed.Result != RuntimeOccupancyCompatibility.Valid) return observed;
            if (string.IsNullOrWhiteSpace(resourceTypeId) || quantity <= 0 || capacity <= 0)
                return Invalid("A ground stack requires a resource, positive quantity and capacity.");
            if (snapshot.Placement != null &&
                (exceptionPolicy == null || !exceptionPolicy.IsExplicitlyAllowed(
                    snapshot, RuntimeOccupancyOperation.AddLooseResource)))
                return Invalid("The Placement does not explicitly accept ground occupancy.");
            if (snapshot.Node?.ObstructsMovement == true)
                return Invalid("An obstructing Node cannot receive a ground stack.");
            if (snapshot.LooseStack == null)
                return quantity <= capacity ? Valid() :
                    Invalid("The new stack would exceed capacity.");
            if (snapshot.LooseStack.ProjectionState ==
                    RuntimeOccupantProjectionState.DetailsUnavailable ||
                !snapshot.LooseStack.Quantity.HasValue)
                return Unknown("Legacy ground-stack quantity is unavailable.");
            if (!string.Equals(snapshot.LooseStack.ResourceTypeId,
                    resourceTypeId, StringComparison.Ordinal))
                return Invalid("Different ground resource types cannot share one cell.");
            return snapshot.LooseStack.Quantity.Value + quantity <= capacity
                ? Valid() : Invalid("The compatible stack would exceed capacity.");
        }

        private static RuntimeOccupancyValidation Valid() =>
            new(RuntimeOccupancyCompatibility.Valid, string.Empty);
        private static RuntimeOccupancyValidation Invalid(string reason) =>
            new(RuntimeOccupancyCompatibility.Invalid, reason);
        private static RuntimeOccupancyValidation Unknown(string reason) =>
            new(RuntimeOccupancyCompatibility.Unknown, reason);
    }
}
