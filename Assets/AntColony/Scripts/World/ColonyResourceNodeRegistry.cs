using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public readonly struct ColonyResourceNodeScenarioIdentity :
        IEquatable<ColonyResourceNodeScenarioIdentity>
    {
        public ColonyResourceNodeScenarioIdentity(string namespaceId, int version,
            long seed)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                throw new ArgumentException("A Node scenario namespace cannot be empty.",
                    nameof(namespaceId));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            NamespaceId = namespaceId.Trim(); Version = version; Seed = seed;
        }
        public string NamespaceId { get; }
        public int Version { get; }
        public long Seed { get; }
        public string StableKey => $"{NamespaceId}.v{Version}.{unchecked((ulong)Seed):X16}";
        public bool Equals(ColonyResourceNodeScenarioIdentity other) =>
            StringComparer.Ordinal.Equals(NamespaceId, other.NamespaceId) &&
            Version == other.Version && Seed == other.Seed;
        public override bool Equals(object obj) =>
            obj is ColonyResourceNodeScenarioIdentity other && Equals(other);
        public override int GetHashCode() =>
            (NamespaceId == null ? 0 : StringComparer.Ordinal.GetHashCode(NamespaceId)) ^
            (Version * 397) ^ Seed.GetHashCode();
        public override string ToString() => StableKey;
    }

    public enum ColonyResourceNodeLifecycle
    {
        Active,
        Reserved,
        PendingExtractionCommit,
        Depleted,
        Invalidated,
        ScenarioRetired
    }

    public enum ColonyResourceNodeRegistryLifecycle
    {
        Active,
        ContainsInvalidatedRecords,
        ContextInvalidated
    }

    public enum ColonyResourceNodeReadResult
    {
        Success,
        InvalidRegion,
        InvalidCoordinateKind,
        NotRegistered,
        ContextInvalidated
    }

    public enum ColonyResourceNodeExtractionStatus
    {
        LegacyQuantityOneCompatible,
        DeferredUntilWholeNodeExtraction,
        Reserved,
        PendingCommit,
        Depleted,
        Invalid,
        ScenarioRetired
    }

    /// <summary>Immutable point-in-time view of one authoritative Colony Node.</summary>
    public sealed class ColonyResourceNodeSnapshot
    {
        internal ColonyResourceNodeSnapshot(NodeRecord record)
        {
            NodeId = record.NodeId; CellId = record.CellId;
            DefinitionId = record.DefinitionId; InitialQuantity = record.InitialQuantity;
            RemainingQuantity = record.RemainingQuantity; Revision = record.Revision;
            Provenance = record.Provenance; BlocksMovement = record.BlocksMovement;
            SupportsNavigation = record.SupportsNavigation;
            ActivityId = record.ActivityId; SkillId = record.SkillId;
            OutputMaterial = record.OutputMaterial;
            OutputResourceId = record.OutputResourceId;
            ExtractionWorkQuantity = record.ExtractionWorkQuantity;
            Lifecycle = record.Lifecycle; ReservationOwner = record.ReservationOwner;
            LastCommittedOperationId = record.LastCommittedOperationId;
            IsLegacyQuantityOneFixture = record.IsLegacyQuantityOneFixture;
        }
        public string NodeId { get; }
        public RegionCellId CellId { get; }
        public string DefinitionId { get; }
        public int InitialQuantity { get; }
        public int RemainingQuantity { get; }
        public long Revision { get; }
        public string Provenance { get; }
        public bool BlocksMovement { get; }
        public bool SupportsNavigation { get; }
        public string ActivityId { get; }
        public string SkillId { get; }
        public ExcavatedMaterialType OutputMaterial { get; }
        public string OutputResourceId { get; }
        public int ExtractionWorkQuantity { get; }
        public ColonyResourceNodeLifecycle Lifecycle { get; }
        public string ReservationOwner { get; }
        public string LastCommittedOperationId { get; }
        public bool IsLegacyQuantityOneFixture { get; }
        public bool IsVariableQuantity => InitialQuantity > 1;
        public ColonyResourceNodeExtractionStatus ExtractionStatus => Lifecycle switch
        {
            ColonyResourceNodeLifecycle.Reserved => ColonyResourceNodeExtractionStatus.Reserved,
            ColonyResourceNodeLifecycle.PendingExtractionCommit => ColonyResourceNodeExtractionStatus.PendingCommit,
            ColonyResourceNodeLifecycle.Depleted => ColonyResourceNodeExtractionStatus.Depleted,
            ColonyResourceNodeLifecycle.Invalidated => ColonyResourceNodeExtractionStatus.Invalid,
            ColonyResourceNodeLifecycle.ScenarioRetired => ColonyResourceNodeExtractionStatus.ScenarioRetired,
            _ when IsLegacyQuantityOneFixture => ColonyResourceNodeExtractionStatus.LegacyQuantityOneCompatible,
            _ => ColonyResourceNodeExtractionStatus.DeferredUntilWholeNodeExtraction
        };
        public string ExtractionCompatibilityReason => ExtractionStatus ==
            ColonyResourceNodeExtractionStatus.DeferredUntilWholeNodeExtraction
                ? ColonyResourceNodeRegistry.VariableExtractionDeferredReason
                : string.Empty;
    }

    public sealed class ColonyResourceNodeRegistryBuildReport
    {
        private readonly string[] errors;
        internal ColonyResourceNodeRegistryBuildReport(IEnumerable<string> errors,
            int candidateCount, int unsupportedSolidCount, int duplicateNodeCount = 0)
        {
            this.errors = errors == null ? Array.Empty<string>() : new List<string>(errors).ToArray();
            CandidateCount = candidateCount; UnsupportedSolidCount = unsupportedSolidCount;
            DuplicateNodeCount = duplicateNodeCount;
        }
        public bool IsValid => errors.Length == 0;
        public IReadOnlyList<string> Errors => errors;
        public int CandidateCount { get; }
        public int UnsupportedSolidCount { get; }
        public int DuplicateNodeCount { get; }
        public string Summary => IsValid
            ? $"Active Colony Node registry validated {CandidateCount} Nodes and observed {UnsupportedSolidCount} unsupported Solids."
            : $"Active Colony Node registry rejected: {string.Join(" ", errors)}";
    }

    public readonly struct ColonyResourceNodeRegistryDiagnostics
    {
        internal ColonyResourceNodeRegistryDiagnostics(bool available,
            ColonyResourceNodeScenarioIdentity scenario,
            ColonyResourceNodeRegistryLifecycle lifecycle, long revision,
            int nodeCount, int soilCount, int stoneCount, int totalInitialSoil,
            int totalRemainingSoil, int totalInitialStone, int totalRemainingStone,
            int invalidCount, int mismatchedCount, int unsupportedSolidCount,
            int duplicateNodeCount, int deferredVariableNodeCount)
        {
            Available = available; Scenario = scenario; Lifecycle = lifecycle;
            Revision = revision; NodeCount = nodeCount; SoilCount = soilCount;
            StoneCount = stoneCount; TotalInitialSoil = totalInitialSoil;
            TotalRemainingSoil = totalRemainingSoil; TotalInitialStone = totalInitialStone;
            TotalRemainingStone = totalRemainingStone; InvalidOrStaleCount = invalidCount;
            MismatchedNodeCount = mismatchedCount; UnsupportedSolidCount = unsupportedSolidCount;
            DuplicateNodeCount = duplicateNodeCount; DeferredVariableNodeCount = deferredVariableNodeCount;
        }
        public bool Available { get; }
        public ColonyResourceNodeScenarioIdentity Scenario { get; }
        public ColonyResourceNodeRegistryLifecycle Lifecycle { get; }
        public long Revision { get; }
        public int NodeCount { get; }
        public int SoilCount { get; }
        public int StoneCount { get; }
        public int TotalInitialSoil { get; }
        public int TotalRemainingSoil { get; }
        public int TotalInitialStone { get; }
        public int TotalRemainingStone { get; }
        public int InvalidOrStaleCount { get; }
        public int MismatchedNodeCount { get; }
        public int UnsupportedSolidCount { get; }
        public int DuplicateNodeCount { get; }
        public int DeferredVariableNodeCount { get; }
    }

    /// <summary>
    /// Scenario-local authority for Colony Node resource quantities. Terrain
    /// occupancy remains in ChunkedWorldData; this registry guards Solid writes
    /// so quantity cannot be bypassed by legacy or development operations.
    /// </summary>
    public sealed class ColonyResourceNodeRegistry : IDisposable, IWorldSolidMutationGuard
    {
        public const long InitialRevision = 1L;
        public const string DevelopmentFixtureNamespace = "development-fixture";
        public const int DevelopmentFixtureVersion = 1;
        public const string VariableExtractionDeferredReason =
            "Variable-quantity Node awaiting whole-node Extraction support (WV-B3C.3B)";

        private readonly ChunkedWorldData world;
        private readonly string soilSolidId;
        private readonly string stoneSolidId;
        private readonly List<NodeRecord> nodes;
        private readonly Dictionary<RegionCellId, NodeRecord> nodesByCell;
        private readonly Dictionary<string, NodeRecord> nodesById;
        private readonly HashSet<string> authorizedTerrainTokens = new(StringComparer.Ordinal);
        private readonly int unsupportedSolidCount;
        private long registryRevision = InitialRevision;
        private long fixtureMutationSequence;
        private ColonyResourceNodeRegistryLifecycle lifecycle = ColonyResourceNodeRegistryLifecycle.Active;

        private ColonyResourceNodeRegistry(ChunkedWorldData world,
            ColonyResourceNodeScenarioIdentity scenario, string soilSolidId,
            string stoneSolidId, List<NodeRecord> nodes,
            Dictionary<RegionCellId, NodeRecord> nodesByCell,
            Dictionary<string, NodeRecord> nodesById, int unsupportedSolidCount)
        {
            this.world = world; Scenario = scenario; this.soilSolidId = soilSolidId;
            this.stoneSolidId = stoneSolidId; this.nodes = nodes;
            this.nodesByCell = nodesByCell; this.nodesById = nodesById;
            this.unsupportedSolidCount = unsupportedSolidCount;
            world.CellChanged += OnTerrainCellChanged;
            world.BindSolidMutationGuard(this);
        }

        public ColonyResourceNodeScenarioIdentity Scenario { get; }
        public ColonyResourceNodeRegistryLifecycle Lifecycle => lifecycle;
        public long Revision => registryRevision;
        public event Action<RegionCellId, long> NodeChanged;
        public IReadOnlyList<ColonyResourceNodeSnapshot> Nodes
        {
            get
            {
                var snapshots = new ColonyResourceNodeSnapshot[nodes.Count];
                for (int i = 0; i < nodes.Count; i++) snapshots[i] = new ColonyResourceNodeSnapshot(nodes[i]);
                return snapshots;
            }
        }

        public ColonyResourceNodeRegistryDiagnostics Diagnostics
        {
            get
            {
                int soil = 0, stone = 0, initialSoil = 0, remainingSoil = 0;
                int initialStone = 0, remainingStone = 0, invalid = 0, mismatched = 0, deferred = 0;
                foreach (NodeRecord node in nodes)
                {
                    if (node.OutputMaterial == ExcavatedMaterialType.Soil)
                    { soil++; initialSoil += node.InitialQuantity; remainingSoil += node.RemainingQuantity; }
                    else if (node.OutputMaterial == ExcavatedMaterialType.Stone)
                    { stone++; initialStone += node.InitialQuantity; remainingStone += node.RemainingQuantity; }
                    if (node.Lifecycle is ColonyResourceNodeLifecycle.Invalidated or
                        ColonyResourceNodeLifecycle.ScenarioRetired) invalid++;
                    if (IsMismatched(node)) mismatched++;
                    if (node.InitialQuantity > 1 && node.RemainingQuantity > 0) deferred++;
                }
                return new ColonyResourceNodeRegistryDiagnostics(
                    lifecycle != ColonyResourceNodeRegistryLifecycle.ContextInvalidated,
                    Scenario, lifecycle, registryRevision, nodes.Count, soil, stone,
                    initialSoil, remainingSoil, initialStone, remainingStone, invalid,
                    mismatched, unsupportedSolidCount, 0, deferred);
            }
        }

        public static bool TryCreate(ChunkedWorldData world,
            ColonyResourceNodeScenarioIdentity scenario, string soilSolidId,
            string stoneSolidId, GeneratedWorldSnapshot generated,
            out ColonyResourceNodeRegistry registry,
            out ColonyResourceNodeRegistryBuildReport report)
        {
            registry = null;
            var errors = new List<string>();
            var records = new List<NodeRecord>();
            var byCell = new Dictionary<RegionCellId, NodeRecord>();
            var byId = new Dictionary<string, NodeRecord>(StringComparer.Ordinal);
            int unsupported = 0, duplicate = 0;
            if (world == null)
            {
                errors.Add("Colony terrain is unavailable.");
                report = new ColonyResourceNodeRegistryBuildReport(errors, 0, 0);
                return false;
            }
            if (string.IsNullOrWhiteSpace(soilSolidId) || string.IsNullOrWhiteSpace(stoneSolidId) ||
                string.Equals(soilSolidId, stoneSolidId, StringComparison.Ordinal))
                errors.Add("Distinct registered Soil and Stone Solid IDs are required.");
            ValidateDefinition(world, soilSolidId, ExcavatedMaterialType.Soil, "Soil", errors);
            ValidateDefinition(world, stoneSolidId, ExcavatedMaterialType.Stone, "Stone", errors);
            if (generated != null && (generated.Metadata.Seed != scenario.Seed ||
                generated.Metadata.Version != scenario.Version ||
                !string.Equals(generated.Metadata.ConfigurationId, scenario.NamespaceId, StringComparison.Ordinal)))
                errors.Add("Generated metadata does not match the Node scenario identity.");

            Dictionary<RegionCellId, GeneratedResourceNodeRecord> generatedByCell =
                BuildGeneratedNodeLookup(generated, errors, out int generatedDuplicates);
            duplicate += generatedDuplicates;
            foreach (Vector2Int chunk in world.GetResidentChunkCoordinates())
            for (int y = 0; y < WorldTileCoordinates.ChunkSize; y++)
            for (int x = 0; x < WorldTileCoordinates.ChunkSize; x++)
            {
                Vector2Int worldCell = WorldTileCoordinates.GetWorldCellCoordinate(chunk, new Vector2Int(x, y));
                if (world.GetCell(worldCell, out WorldTileCellData cell) != WorldCellQueryResult.Available || !cell.HasSolidTile) continue;
                bool supported = string.Equals(cell.SolidMaterialId, soilSolidId, StringComparison.Ordinal) ||
                    string.Equals(cell.SolidMaterialId, stoneSolidId, StringComparison.Ordinal);
                if (!supported) { unsupported++; continue; }
                if (string.IsNullOrWhiteSpace(cell.BackgroundMaterialId))
                { errors.Add($"Eligible Solid at {worldCell} has no Background."); continue; }
                RegionCellId cellId = ToCellId(worldCell);
                if (byCell.ContainsKey(cellId))
                { duplicate++; errors.Add($"Duplicate Colony Node cell {cellId}."); continue; }
                GeneratedResourceNodeRecord generatedNode = null;
                if (generated != null && (!generatedByCell.TryGetValue(cellId, out generatedNode) ||
                    !string.Equals(generatedNode.DefinitionId, cell.SolidMaterialId, StringComparison.Ordinal)))
                { errors.Add($"Generated Node provenance is missing or mismatched at {cellId}."); continue; }
                if (!world.TryGetSolidMaterialDefinition(cell.SolidMaterialId, out SolidTileMaterialDefinition definition))
                { errors.Add($"Solid definition {cell.SolidMaterialId} is unavailable."); continue; }
                int quantity = generatedNode?.InitialQuantity ?? definition.ExcavationYield;
                if (quantity <= 0) { errors.Add($"Node at {cellId} requires a positive quantity."); continue; }
                string nodeId = CreateNodeId(scenario, cellId, cell.SolidMaterialId, generatedNode?.Id, 0);
                if (byId.ContainsKey(nodeId))
                { duplicate++; errors.Add($"Duplicate Colony Node ID {nodeId}."); continue; }
                var record = new NodeRecord(nodeId, cellId, cell.SolidMaterialId, quantity,
                    generatedNode?.Provenance ?? $"fixture:{scenario.NamespaceId}.v{scenario.Version}",
                    definition, generatedNode == null);
                records.Add(record); byCell.Add(cellId, record); byId.Add(nodeId, record);
            }
            records.Sort((a, b) => a.CellId.CompareTo(b.CellId));
            report = new ColonyResourceNodeRegistryBuildReport(errors, records.Count, unsupported, duplicate);
            if (!report.IsValid) return false;
            registry = new ColonyResourceNodeRegistry(world, scenario, soilSolidId,
                stoneSolidId, records, byCell, byId, unsupported);
            return true;
        }

        public ColonyResourceNodeReadResult TryGet(RegionCellId cellId,
            out ColonyResourceNodeSnapshot node)
        {
            node = null;
            if (lifecycle == ColonyResourceNodeRegistryLifecycle.ContextInvalidated)
                return ColonyResourceNodeReadResult.ContextInvalidated;
            if (cellId.RegionId != AntRegionIds.Colony) return ColonyResourceNodeReadResult.InvalidRegion;
            if (cellId.Kind != RegionLocationKind.Colony) return ColonyResourceNodeReadResult.InvalidCoordinateKind;
            if (!nodesByCell.TryGetValue(cellId, out NodeRecord record)) return ColonyResourceNodeReadResult.NotRegistered;
            node = new ColonyResourceNodeSnapshot(record);
            return ColonyResourceNodeReadResult.Success;
        }

        public ColonyResourceNodeReadResult TryGet(string nodeId, out ColonyResourceNodeSnapshot node)
        {
            node = null;
            if (lifecycle == ColonyResourceNodeRegistryLifecycle.ContextInvalidated)
                return ColonyResourceNodeReadResult.ContextInvalidated;
            if (string.IsNullOrWhiteSpace(nodeId) || !nodesById.TryGetValue(nodeId, out NodeRecord record))
                return ColonyResourceNodeReadResult.NotRegistered;
            node = new ColonyResourceNodeSnapshot(record);
            return ColonyResourceNodeReadResult.Success;
        }

        public bool CanUseLegacyExtraction(Vector2Int cell, out string reason)
        {
            if (TryGet(ToCellId(cell), out ColonyResourceNodeSnapshot node) != ColonyResourceNodeReadResult.Success)
            { reason = "The designated Resource Node is not registered."; return false; }
            if (node.Lifecycle != ColonyResourceNodeLifecycle.Active)
            { reason = $"The Resource Node is {node.Lifecycle}."; return false; }
            if (!node.IsLegacyQuantityOneFixture)
            { reason = VariableExtractionDeferredReason; return false; }
            reason = string.Empty;
            return true;
        }

        public bool TryPrepareLegacyExtractionRemoval(Vector2Int cell,
            string operationId, out string authorityToken, out string error)
        {
            authorityToken = string.Empty;
            if (!CanUseLegacyExtraction(cell, out error)) return false;
            if (string.IsNullOrWhiteSpace(operationId))
            { error = "Extraction requires a stable operation identity."; return false; }
            authorityToken = $"node-extraction:{Scenario.StableKey}:{cell.x}:{cell.y}:{operationId.Trim()}";
            authorizedTerrainTokens.Add(authorityToken);
            return true;
        }

        public bool TryPrepareDevelopmentFixtureMutation(Vector2Int cell,
            out string authorityToken, out string error)
        {
            authorityToken = string.Empty;
            if (!IsFixtureScenario)
            { error = "Direct Solid mutation is disabled for generated Node scenarios."; return false; }
            authorityToken = $"fixture-terrain:{Scenario.StableKey}:{cell.x}:{cell.y}:{++fixtureMutationSequence}";
            authorizedTerrainTokens.Add(authorityToken);
            error = string.Empty;
            return true;
        }

        public bool TryReserve(string nodeId, string ownerId, long expectedRevision, out string error)
        {
            if (!TryGetActiveRecord(nodeId, out NodeRecord record, out error)) return false;
            if (record.Revision != expectedRevision) { error = "The Resource Node revision is stale."; return false; }
            if (string.IsNullOrWhiteSpace(ownerId)) { error = "A Node reservation owner is required."; return false; }
            if (!string.IsNullOrEmpty(record.ReservationOwner)) { error = "The Resource Node is already reserved."; return false; }
            record.ReservationOwner = ownerId.Trim(); record.Lifecycle = ColonyResourceNodeLifecycle.Reserved;
            CommitRecordChange(record); error = string.Empty; return true;
        }

        public bool TryReleaseReservation(string nodeId, string ownerId, out string error)
        {
            if (!TryGetActiveRecord(nodeId, out NodeRecord record, out error)) return false;
            if (!string.Equals(record.ReservationOwner, ownerId, StringComparison.Ordinal))
            { error = "The caller does not own the Node reservation."; return false; }
            record.ReservationOwner = string.Empty; record.Lifecycle = ColonyResourceNodeLifecycle.Active;
            CommitRecordChange(record); error = string.Empty; return true;
        }

        public bool TryBeginPendingExtraction(string nodeId, string ownerId,
            long expectedRevision, out string error)
        {
            if (!TryGetActiveRecord(nodeId, out NodeRecord record, out error)) return false;
            if (record.Revision != expectedRevision || record.Lifecycle != ColonyResourceNodeLifecycle.Reserved ||
                !string.Equals(record.ReservationOwner, ownerId, StringComparison.Ordinal))
            { error = "The Node reservation or expected revision is stale."; return false; }
            record.Lifecycle = ColonyResourceNodeLifecycle.PendingExtractionCommit;
            CommitRecordChange(record); error = string.Empty; return true;
        }

        /// <summary>
        /// Future transaction primitive used by authority tests. No current
        /// gameplay path calls it, so WV-B3C.3A does not add partial harvesting.
        /// </summary>
        public bool TryCommitQuantityReduction(string nodeId, string ownerId,
            string operationId, int quantity, long expectedRevision,
            out ColonyResourceNodeSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (lifecycle == ColonyResourceNodeRegistryLifecycle.ContextInvalidated)
            { error = "The Resource Node scenario has been retired."; return false; }
            if (string.IsNullOrWhiteSpace(nodeId) || !nodesById.TryGetValue(nodeId, out NodeRecord record))
            { error = "The Resource Node identity is unavailable."; return false; }
            if (!string.IsNullOrWhiteSpace(operationId) && record.CommittedOperations.Contains(operationId))
            { snapshot = new ColonyResourceNodeSnapshot(record); error = string.Empty; return true; }
            if (record.Revision != expectedRevision || quantity <= 0 || quantity > record.RemainingQuantity ||
                record.Lifecycle != ColonyResourceNodeLifecycle.PendingExtractionCommit ||
                !string.Equals(record.ReservationOwner, ownerId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(operationId))
            { error = "The Node quantity commit is invalid, unowned or stale."; return false; }
            record.RemainingQuantity -= quantity; record.LastCommittedOperationId = operationId;
            record.CommittedOperations.Add(operationId); record.ReservationOwner = string.Empty;
            record.Lifecycle = record.RemainingQuantity == 0
                ? ColonyResourceNodeLifecycle.Depleted : ColonyResourceNodeLifecycle.Active;
            CommitRecordChange(record); snapshot = new ColonyResourceNodeSnapshot(record);
            error = string.Empty; return true;
        }

        public void InvalidateContext()
        {
            if (lifecycle == ColonyResourceNodeRegistryLifecycle.ContextInvalidated) return;
            world.CellChanged -= OnTerrainCellChanged; world.UnbindSolidMutationGuard(this);
            authorizedTerrainTokens.Clear();
            foreach (NodeRecord node in nodes)
            {
                node.ReservationOwner = string.Empty;
                node.Lifecycle = ColonyResourceNodeLifecycle.ScenarioRetired;
                node.Revision++;
            }
            lifecycle = ColonyResourceNodeRegistryLifecycle.ContextInvalidated;
            registryRevision++;
        }
        public void Dispose() => InvalidateContext();

        bool IWorldSolidMutationGuard.CanReplaceSolid(Vector2Int cell, string currentSolidId,
            string replacementSolidId, string authorityToken, out string reason)
        {
            if (ConsumeTerrainToken(authorityToken)) { reason = string.Empty; return true; }
            reason = "Solid replacement rejected by active Resource Node authority."; return false;
        }
        bool IWorldSolidMutationGuard.CanRemoveSolid(Vector2Int cell, string currentSolidId,
            string authorityToken, out string reason)
        {
            if (ConsumeTerrainToken(authorityToken)) { reason = string.Empty; return true; }
            reason = nodesByCell.ContainsKey(ToCellId(cell))
                ? "Solid removal rejected by active Resource Node authority."
                : "Direct Solid removal is not authorised in this scenario.";
            return false;
        }
        private bool ConsumeTerrainToken(string token) =>
            !string.IsNullOrWhiteSpace(token) && authorizedTerrainTokens.Remove(token);

        private void OnTerrainCellChanged(WorldCellAuthorityChange change)
        {
            if (lifecycle == ColonyResourceNodeRegistryLifecycle.ContextInvalidated ||
                (change.Category & RuntimeCellChangeCategory.Node) == 0) return;
            RegionCellId cellId = ToCellId(change.WorldCell);
            world.GetCell(change.WorldCell, out WorldTileCellData cell);
            if (nodesByCell.TryGetValue(cellId, out NodeRecord record))
            {
                if (!cell.HasSolidTile)
                {
                    record.RemainingQuantity = 0; record.ReservationOwner = string.Empty;
                    record.Lifecycle = ColonyResourceNodeLifecycle.Depleted;
                    record.Revision++; registryRevision++; return;
                }
                if (!string.Equals(cell.SolidMaterialId, record.DefinitionId, StringComparison.Ordinal))
                {
                    record.ReservationOwner = string.Empty;
                    record.Lifecycle = ColonyResourceNodeLifecycle.Invalidated;
                    record.Revision++; registryRevision++;
                    lifecycle = ColonyResourceNodeRegistryLifecycle.ContainsInvalidatedRecords;
                    if (IsSupported(cell.SolidMaterialId) && IsFixtureScenario)
                        ReplaceWithFixtureRecord(cellId, cell.SolidMaterialId);
                }
                return;
            }
            if (cell.HasSolidTile && IsSupported(cell.SolidMaterialId) && IsFixtureScenario)
                AddFixtureRecord(cellId, cell.SolidMaterialId);
        }

        private bool TryGetActiveRecord(string nodeId, out NodeRecord record, out string error)
        {
            record = null;
            if (lifecycle == ColonyResourceNodeRegistryLifecycle.ContextInvalidated)
            { error = "The Resource Node scenario has been retired."; return false; }
            if (string.IsNullOrWhiteSpace(nodeId) || !nodesById.TryGetValue(nodeId, out record))
            { error = "The Resource Node identity is unavailable."; return false; }
            if (record.Lifecycle is ColonyResourceNodeLifecycle.Depleted or
                ColonyResourceNodeLifecycle.Invalidated or ColonyResourceNodeLifecycle.ScenarioRetired)
            { error = $"The Resource Node is {record.Lifecycle}."; return false; }
            error = string.Empty; return true;
        }

        private void CommitRecordChange(NodeRecord record)
        {
            record.Revision++; registryRevision++;
            NodeChanged?.Invoke(record.CellId, record.Revision);
        }

        private bool IsMismatched(NodeRecord node)
        {
            if (node.Lifecycle is not (ColonyResourceNodeLifecycle.Active or
                    ColonyResourceNodeLifecycle.Reserved or
                    ColonyResourceNodeLifecycle.PendingExtractionCommit)) return false;
            Vector2Int cell = new(node.CellId.ColonyCell.X, node.CellId.ColonyCell.Y);
            return world.GetCell(cell, out WorldTileCellData data) != WorldCellQueryResult.Available ||
                !data.HasSolidTile || !string.Equals(data.SolidMaterialId, node.DefinitionId, StringComparison.Ordinal);
        }

        private bool IsFixtureScenario => string.Equals(Scenario.NamespaceId,
            DevelopmentFixtureNamespace, StringComparison.Ordinal);
        private bool IsSupported(string solidId) =>
            string.Equals(solidId, soilSolidId, StringComparison.Ordinal) ||
            string.Equals(solidId, stoneSolidId, StringComparison.Ordinal);

        private void ReplaceWithFixtureRecord(RegionCellId cellId, string solidId)
        {
            if (nodesByCell.TryGetValue(cellId, out NodeRecord old))
            { nodesById.Remove(old.NodeId); nodes.Remove(old); }
            AddFixtureRecord(cellId, solidId);
        }
        private void AddFixtureRecord(RegionCellId cellId, string solidId)
        {
            if (!world.TryGetSolidMaterialDefinition(solidId, out SolidTileMaterialDefinition definition)) return;
            string id = CreateNodeId(Scenario, cellId, solidId, null, ++fixtureMutationSequence);
            var record = new NodeRecord(id, cellId, solidId, definition.ExcavationYield,
                $"fixture:{Scenario.NamespaceId}.v{Scenario.Version}:runtime", definition, true);
            nodes.Add(record); nodes.Sort((a, b) => a.CellId.CompareTo(b.CellId));
            nodesByCell[cellId] = record; nodesById[id] = record; registryRevision++;
        }

        private static RegionCellId ToCellId(Vector2Int cell) =>
            RegionCellId.Colony(AntRegionIds.Colony, new ColonyCell(cell.x, cell.y));
        private static void ValidateDefinition(ChunkedWorldData world, string stableId,
            ExcavatedMaterialType expected, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return;
            if (!world.TryGetSolidMaterialDefinition(stableId, out SolidTileMaterialDefinition definition) ||
                !definition.IsExcavatable || definition.ExcavatedMaterial != expected || definition.ExcavationYield <= 0)
                errors.Add($"{label} Solid {stableId} is not a valid registered quantity-producing Extraction definition.");
        }
        private static Dictionary<RegionCellId, GeneratedResourceNodeRecord>
            BuildGeneratedNodeLookup(GeneratedWorldSnapshot generated, List<string> errors,
                out int duplicateCount)
        {
            duplicateCount = 0;
            var result = new Dictionary<RegionCellId, GeneratedResourceNodeRecord>();
            if (generated == null) return result;
            foreach (GeneratedResourceNodeRecord node in generated.Nodes)
            {
                if (node.Location.Kind != RegionLocationKind.Colony || node.Location.RegionId != AntRegionIds.Colony) continue;
                RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony, node.Location.ColonyCell);
                if (!result.TryAdd(cellId, node))
                { duplicateCount++; errors.Add($"Generated Colony Node provenance is duplicated at {cellId}."); }
            }
            return result;
        }
        private static string CreateNodeId(ColonyResourceNodeScenarioIdentity scenario,
            RegionCellId cellId, string solidId, string generatedNodeId, long sequence)
        {
            string source = string.IsNullOrWhiteSpace(generatedNodeId)
                ? $"{solidId}.{cellId.ColonyCell.X}.{cellId.ColonyCell.Y}" +
                  (sequence > 0 ? $".{sequence}" : string.Empty)
                : generatedNodeId;
            return $"node.{AntRegionIds.Colony.Value}.{scenario.StableKey}.{source}";
        }
    }

    internal sealed class NodeRecord
    {
        public NodeRecord(string nodeId, RegionCellId cellId, string definitionId,
            int quantity, string provenance, SolidTileMaterialDefinition definition,
            bool legacyFixture)
        {
            NodeId = nodeId; CellId = cellId; DefinitionId = definitionId;
            InitialQuantity = quantity; RemainingQuantity = quantity;
            Revision = ColonyResourceNodeRegistry.InitialRevision; Provenance = provenance;
            BlocksMovement = definition.BlocksMovement; SupportsNavigation = definition.SupportsNavigation;
            ActivityId = "extraction"; SkillId = "extraction";
            OutputMaterial = definition.ExcavatedMaterial;
            OutputResourceId = MaterialResourceCatalog.GetStableId(OutputMaterial);
            ExtractionWorkQuantity = definition.ExcavationYield;
            Lifecycle = ColonyResourceNodeLifecycle.Active;
            IsLegacyQuantityOneFixture = legacyFixture && quantity == 1;
        }
        public string NodeId { get; }
        public RegionCellId CellId { get; }
        public string DefinitionId { get; }
        public int InitialQuantity { get; }
        public int RemainingQuantity { get; set; }
        public long Revision { get; set; }
        public string Provenance { get; }
        public bool BlocksMovement { get; }
        public bool SupportsNavigation { get; }
        public string ActivityId { get; }
        public string SkillId { get; }
        public ExcavatedMaterialType OutputMaterial { get; }
        public string OutputResourceId { get; }
        public int ExtractionWorkQuantity { get; }
        public ColonyResourceNodeLifecycle Lifecycle { get; set; }
        public string ReservationOwner { get; set; } = string.Empty;
        public string LastCommittedOperationId { get; set; } = string.Empty;
        public bool IsLegacyQuantityOneFixture { get; }
        public HashSet<string> CommittedOperations { get; } = new(StringComparer.Ordinal);
    }
}
