using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Describes the deterministic scenario namespace used by inactive Colony
    /// Node IDs. A later save format can preserve this value without relying on
    /// Unity object identity or process-specific hashes.
    /// </summary>
    public readonly struct ColonyResourceNodeScenarioIdentity :
        IEquatable<ColonyResourceNodeScenarioIdentity>
    {
        public ColonyResourceNodeScenarioIdentity(
            string namespaceId,
            int version,
            long seed)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                throw new ArgumentException(
                    "A Node scenario namespace cannot be empty.",
                    nameof(namespaceId));
            if (version <= 0)
                throw new ArgumentOutOfRangeException(nameof(version));

            NamespaceId = namespaceId.Trim();
            Version = version;
            Seed = seed;
        }

        public string NamespaceId { get; }
        public int Version { get; }
        public long Seed { get; }
        public string StableKey =>
            $"{NamespaceId}.v{Version}.{unchecked((ulong)Seed):X16}";

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

    public enum InactiveColonyNodeLifecycle
    {
        ValidInactiveCandidate,
        StaleInvalidated,
        DepletedOrAbsent
    }

    public enum InactiveColonyNodeRegistryLifecycle
    {
        ValidInactiveCandidate,
        ContainsInvalidatedRecords,
        ContextInvalidated
    }

    public enum InactiveColonyNodeReadResult
    {
        Success,
        InvalidRegion,
        InvalidCoordinateKind,
        NotRegistered,
        ContextInvalidated
    }

    /// <summary>
    /// Immutable diagnostic view of one quantity-one Colony Solid. The record is
    /// deliberately not an active gameplay authority in WV-B3C.1.
    /// </summary>
    public sealed class InactiveColonyResourceNodeSnapshot
    {
        internal InactiveColonyResourceNodeSnapshot(
            string nodeId,
            RegionCellId cellId,
            string definitionId,
            long revision,
            string provenance,
            bool blocksMovement,
            bool supportsNavigation,
            ExcavatedMaterialType outputMaterial,
            int outputQuantity,
            InactiveColonyNodeLifecycle lifecycle)
        {
            NodeId = nodeId;
            CellId = cellId;
            DefinitionId = definitionId;
            InitialQuantity = 1;
            RemainingQuantity = 1;
            Revision = revision;
            Provenance = provenance;
            BlocksMovement = blocksMovement;
            SupportsNavigation = supportsNavigation;
            ActivityId = "extraction";
            SkillId = "extraction";
            OutputMaterial = outputMaterial;
            OutputResourceId = MaterialResourceCatalog.GetStableId(outputMaterial);
            OutputQuantity = outputQuantity;
            Lifecycle = lifecycle;
        }

        public string NodeId { get; }
        public RegionCellId CellId { get; }
        public string DefinitionId { get; }
        public int InitialQuantity { get; }
        public int RemainingQuantity { get; }
        public long Revision { get; }
        public bool IsDepletedOrAbsent =>
            Lifecycle == InactiveColonyNodeLifecycle.DepletedOrAbsent;
        public string Provenance { get; }
        public bool BlocksMovement { get; }
        public bool SupportsNavigation { get; }
        public string ActivityId { get; }
        public string SkillId { get; }
        public ExcavatedMaterialType OutputMaterial { get; }
        public string OutputResourceId { get; }
        public int OutputQuantity { get; }
        public InactiveColonyNodeLifecycle Lifecycle { get; }
    }

    /// <summary>Bounded construction result; an invalid registry is never returned.</summary>
    public sealed class ColonyResourceNodeRegistryBuildReport
    {
        private readonly string[] errors;

        internal ColonyResourceNodeRegistryBuildReport(
            IEnumerable<string> errors,
            int candidateCount,
            int unsupportedSolidCount,
            int duplicateNodeCount = 0)
        {
            this.errors = errors == null
                ? Array.Empty<string>()
                : new List<string>(errors).ToArray();
            CandidateCount = candidateCount;
            UnsupportedSolidCount = unsupportedSolidCount;
            DuplicateNodeCount = duplicateNodeCount;
        }

        public bool IsValid => errors.Length == 0;
        public IReadOnlyList<string> Errors => errors;
        public int CandidateCount { get; }
        public int UnsupportedSolidCount { get; }
        public int DuplicateNodeCount { get; }
        public string Summary => IsValid
            ? $"Inactive Colony Node registry validated {CandidateCount} Nodes " +
              $"and observed {UnsupportedSolidCount} unsupported Solids."
            : $"Inactive Colony Node registry rejected: {string.Join(" ", errors)}";
    }

    /// <summary>Read-only development diagnostics for the inactive registry.</summary>
    public readonly struct ColonyResourceNodeRegistryDiagnostics
    {
        internal ColonyResourceNodeRegistryDiagnostics(
            bool available,
            ColonyResourceNodeScenarioIdentity scenario,
            InactiveColonyNodeRegistryLifecycle lifecycle,
            long revision,
            int nodeCount,
            int soilCount,
            int stoneCount,
            int staleCount,
            int depletedOrAbsentCount,
            int unsupportedSolidCount,
            int duplicateNodeCount)
        {
            Available = available;
            Scenario = scenario;
            Lifecycle = lifecycle;
            Revision = revision;
            NodeCount = nodeCount;
            SoilCount = soilCount;
            StoneCount = stoneCount;
            StaleCount = staleCount;
            DepletedOrAbsentCount = depletedOrAbsentCount;
            UnsupportedSolidCount = unsupportedSolidCount;
            DuplicateNodeCount = duplicateNodeCount;
        }

        public bool Available { get; }
        public ColonyResourceNodeScenarioIdentity Scenario { get; }
        public InactiveColonyNodeRegistryLifecycle Lifecycle { get; }
        public long Revision { get; }
        public int NodeCount { get; }
        public int SoilCount { get; }
        public int StoneCount { get; }
        public int StaleCount { get; }
        public int DepletedOrAbsentCount { get; }
        public int InvalidOrStaleCount => Available
            ? StaleCount + DepletedOrAbsentCount
            : NodeCount;
        public int UnsupportedSolidCount { get; }
        public int DuplicateNodeCount { get; }
    }

    /// <summary>
    /// Inactive WV-B3C.1 candidate registry. ChunkedWorldData remains the live
    /// terrain and effective quantity-one authority. This registry observes
    /// terrain changes only to stop stale diagnostic records being presented as
    /// valid; it never writes terrain, output, jobs, reservations or quantity.
    /// </summary>
    public sealed class InactiveColonyResourceNodeRegistry : IDisposable
    {
        public const long InitialRevision = 1L;
        public const string DevelopmentFixtureNamespace = "development-fixture";
        public const int DevelopmentFixtureVersion = 1;

        private readonly ChunkedWorldData world;
        private readonly string soilSolidId;
        private readonly string stoneSolidId;
        private readonly List<InactiveColonyResourceNodeSnapshot> nodes;
        private readonly ReadOnlyCollection<InactiveColonyResourceNodeSnapshot>
            nodeView;
        private readonly Dictionary<RegionCellId, int> nodeIndexByCell;
        private readonly int soilCount;
        private readonly int stoneCount;
        private readonly int unsupportedSolidCount;
        private int staleCount;
        private int depletedOrAbsentCount;
        private long revision = InitialRevision;
        private InactiveColonyNodeRegistryLifecycle lifecycle =
            InactiveColonyNodeRegistryLifecycle.ValidInactiveCandidate;

        private InactiveColonyResourceNodeRegistry(
            ChunkedWorldData world,
            ColonyResourceNodeScenarioIdentity scenario,
            string soilSolidId,
            string stoneSolidId,
            List<InactiveColonyResourceNodeSnapshot> nodes,
            Dictionary<RegionCellId, int> nodeIndexByCell,
            int soilCount,
            int stoneCount,
            int unsupportedSolidCount)
        {
            this.world = world;
            Scenario = scenario;
            this.soilSolidId = soilSolidId;
            this.stoneSolidId = stoneSolidId;
            this.nodes = nodes;
            nodeView = nodes.AsReadOnly();
            this.nodeIndexByCell = nodeIndexByCell;
            this.soilCount = soilCount;
            this.stoneCount = stoneCount;
            this.unsupportedSolidCount = unsupportedSolidCount;
            world.CellChanged += OnTerrainCellChanged;
        }

        public ColonyResourceNodeScenarioIdentity Scenario { get; }
        public InactiveColonyNodeRegistryLifecycle Lifecycle => lifecycle;
        public long Revision => revision;
        public IReadOnlyList<InactiveColonyResourceNodeSnapshot> Nodes => nodeView;
        public ColonyResourceNodeRegistryDiagnostics Diagnostics =>
            new(lifecycle != InactiveColonyNodeRegistryLifecycle.ContextInvalidated,
                Scenario, lifecycle, revision, nodes.Count, soilCount, stoneCount,
                staleCount, depletedOrAbsentCount, unsupportedSolidCount, 0);

        /// <summary>
        /// Builds a complete candidate from already-prepared Colony terrain.
        /// Failure returns diagnostics and leaves the supplied world untouched.
        /// </summary>
        public static bool TryCreate(
            ChunkedWorldData world,
            ColonyResourceNodeScenarioIdentity scenario,
            string soilSolidId,
            string stoneSolidId,
            GeneratedWorldSnapshot generated,
            out InactiveColonyResourceNodeRegistry registry,
            out ColonyResourceNodeRegistryBuildReport report)
        {
            registry = null;
            var errors = new List<string>();
            var records = new List<InactiveColonyResourceNodeSnapshot>();
            var indices = new Dictionary<RegionCellId, int>();
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            int soilCount = 0;
            int stoneCount = 0;
            int unsupportedCount = 0;
            int duplicateCount = 0;

            if (world == null)
            {
                errors.Add("Colony terrain is unavailable.");
                report = new ColonyResourceNodeRegistryBuildReport(
                    errors, 0, 0);
                return false;
            }
            if (string.IsNullOrWhiteSpace(soilSolidId) ||
                string.IsNullOrWhiteSpace(stoneSolidId) ||
                string.Equals(soilSolidId, stoneSolidId, StringComparison.Ordinal))
            {
                errors.Add("Distinct registered Soil and Stone Solid IDs are required.");
            }

            ValidateExtractionDefinition(world, soilSolidId,
                ExcavatedMaterialType.Soil, "Soil", errors);
            ValidateExtractionDefinition(world, stoneSolidId,
                ExcavatedMaterialType.Stone, "Stone", errors);
            if (generated != null &&
                (generated.Metadata.Seed != scenario.Seed ||
                 generated.Metadata.Version != scenario.Version ||
                 !string.Equals(generated.Metadata.ConfigurationId,
                     scenario.NamespaceId, StringComparison.Ordinal)))
            {
                errors.Add("Generated metadata does not match the Node scenario identity.");
            }

            Dictionary<RegionCellId, GeneratedResourceNodeRecord> generatedByCell =
                BuildGeneratedNodeLookup(generated, errors,
                    out int generatedDuplicateCount);
            duplicateCount += generatedDuplicateCount;
            foreach (Vector2Int chunk in world.GetResidentChunkCoordinates())
            {
                for (int localY = 0; localY < WorldTileCoordinates.ChunkSize; localY++)
                for (int localX = 0; localX < WorldTileCoordinates.ChunkSize; localX++)
                {
                    Vector2Int worldCell = WorldTileCoordinates.GetWorldCellCoordinate(
                        chunk, new Vector2Int(localX, localY));
                    if (world.GetCell(worldCell, out WorldTileCellData cell) !=
                            WorldCellQueryResult.Available || !cell.HasSolidTile)
                        continue;

                    bool isSoil = string.Equals(cell.SolidMaterialId,
                        soilSolidId, StringComparison.Ordinal);
                    bool isStone = string.Equals(cell.SolidMaterialId,
                        stoneSolidId, StringComparison.Ordinal);
                    if (!isSoil && !isStone)
                    {
                        unsupportedCount++;
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(cell.BackgroundMaterialId))
                    {
                        errors.Add($"Eligible Solid at {worldCell} has no Background.");
                        continue;
                    }

                    RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                        new ColonyCell(worldCell.x, worldCell.y));
                    if (indices.ContainsKey(cellId))
                    {
                        duplicateCount++;
                        errors.Add($"Duplicate Colony Node cell {cellId}.");
                        continue;
                    }

                    GeneratedResourceNodeRecord generatedNode = null;
                    if (generated != null &&
                        (!generatedByCell.TryGetValue(cellId, out generatedNode) ||
                         !string.Equals(generatedNode.DefinitionId,
                             cell.SolidMaterialId, StringComparison.Ordinal)))
                    {
                        errors.Add($"Generated Node provenance is missing or mismatched at {cellId}.");
                        continue;
                    }

                    string nodeId = CreateNodeId(scenario, cellId,
                        cell.SolidMaterialId, generatedNode?.Id);
                    if (!nodeIds.Add(nodeId))
                    {
                        duplicateCount++;
                        errors.Add($"Duplicate Colony Node ID {nodeId}.");
                        continue;
                    }
                    if (!world.TryGetSolidMaterialDefinition(cell.SolidMaterialId,
                            out SolidTileMaterialDefinition definition))
                    {
                        errors.Add($"Solid definition {cell.SolidMaterialId} is unavailable.");
                        continue;
                    }

                    var record = new InactiveColonyResourceNodeSnapshot(
                        nodeId, cellId, cell.SolidMaterialId, InitialRevision,
                        generatedNode?.Provenance ??
                            $"fixture:{scenario.NamespaceId}.v{scenario.Version}",
                        definition.BlocksMovement, definition.SupportsNavigation,
                        definition.ExcavatedMaterial, definition.ExcavationYield,
                        InactiveColonyNodeLifecycle.ValidInactiveCandidate);
                    indices.Add(cellId, records.Count);
                    records.Add(record);
                    if (isSoil) soilCount++; else stoneCount++;
                }
            }

            records.Sort((left, right) => left.CellId.CompareTo(right.CellId));
            indices.Clear();
            for (int index = 0; index < records.Count; index++)
                indices.Add(records[index].CellId, index);

            report = new ColonyResourceNodeRegistryBuildReport(
                errors, records.Count, unsupportedCount, duplicateCount);
            if (!report.IsValid) return false;

            registry = new InactiveColonyResourceNodeRegistry(world, scenario,
                soilSolidId, stoneSolidId, records, indices, soilCount,
                stoneCount, unsupportedCount);
            return true;
        }

        public InactiveColonyNodeReadResult TryGet(
            RegionCellId cellId,
            out InactiveColonyResourceNodeSnapshot node)
        {
            node = null;
            if (lifecycle == InactiveColonyNodeRegistryLifecycle.ContextInvalidated)
                return InactiveColonyNodeReadResult.ContextInvalidated;
            if (cellId.RegionId != AntRegionIds.Colony)
                return InactiveColonyNodeReadResult.InvalidRegion;
            if (cellId.Kind != RegionLocationKind.Colony)
                return InactiveColonyNodeReadResult.InvalidCoordinateKind;
            if (!nodeIndexByCell.TryGetValue(cellId, out int index))
                return InactiveColonyNodeReadResult.NotRegistered;
            node = nodes[index];
            return InactiveColonyNodeReadResult.Success;
        }

        /// <summary>
        /// Invalidates this scenario handle. Reads then fail explicitly rather
        /// than exposing records from terrain that is no longer the active world.
        /// </summary>
        public void InvalidateContext()
        {
            if (lifecycle == InactiveColonyNodeRegistryLifecycle.ContextInvalidated)
                return;
            world.CellChanged -= OnTerrainCellChanged;
            lifecycle = InactiveColonyNodeRegistryLifecycle.ContextInvalidated;
            revision++;
        }

        public void Dispose() => InvalidateContext();

        private void OnTerrainCellChanged(WorldCellAuthorityChange change)
        {
            if (lifecycle == InactiveColonyNodeRegistryLifecycle.ContextInvalidated ||
                (change.Category & RuntimeCellChangeCategory.Node) == 0)
                return;

            RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(change.WorldCell.x, change.WorldCell.y));
            if (!nodeIndexByCell.TryGetValue(cellId, out int index)) return;

            InactiveColonyResourceNodeSnapshot current = nodes[index];
            InactiveColonyNodeLifecycle nextLifecycle;
            if (world.GetCell(change.WorldCell, out WorldTileCellData cell) !=
                    WorldCellQueryResult.Available || !cell.HasSolidTile)
            {
                nextLifecycle = InactiveColonyNodeLifecycle.DepletedOrAbsent;
            }
            else if (!string.Equals(cell.SolidMaterialId, current.DefinitionId,
                         StringComparison.Ordinal) ||
                     (!string.Equals(cell.SolidMaterialId, soilSolidId,
                          StringComparison.Ordinal) &&
                      !string.Equals(cell.SolidMaterialId, stoneSolidId,
                          StringComparison.Ordinal)))
            {
                nextLifecycle = InactiveColonyNodeLifecycle.StaleInvalidated;
            }
            else
            {
                // A remove/re-add sequence must not silently revive an already
                // invalid record as a new active authority.
                nextLifecycle = current.Lifecycle;
            }

            if (current.Lifecycle == InactiveColonyNodeLifecycle.ValidInactiveCandidate)
            {
                if (nextLifecycle == InactiveColonyNodeLifecycle.StaleInvalidated)
                    staleCount++;
                else if (nextLifecycle == InactiveColonyNodeLifecycle.DepletedOrAbsent)
                    depletedOrAbsentCount++;
            }
            nodes[index] = new InactiveColonyResourceNodeSnapshot(
                current.NodeId, current.CellId, current.DefinitionId,
                current.Revision + 1L, current.Provenance,
                current.BlocksMovement, current.SupportsNavigation,
                current.OutputMaterial, current.OutputQuantity, nextLifecycle);
            revision++;
            lifecycle = InactiveColonyNodeRegistryLifecycle.ContainsInvalidatedRecords;
        }

        private static void ValidateExtractionDefinition(
            ChunkedWorldData world,
            string stableId,
            ExcavatedMaterialType expectedMaterial,
            string label,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return;
            if (!world.TryGetSolidMaterialDefinition(stableId,
                    out SolidTileMaterialDefinition definition) ||
                !definition.IsExcavatable ||
                definition.ExcavatedMaterial != expectedMaterial ||
                definition.ExcavationYield <= 0)
            {
                errors.Add($"{label} Solid {stableId} is not a valid registered " +
                    "quantity-producing Extraction definition.");
            }
        }

        private static Dictionary<RegionCellId, GeneratedResourceNodeRecord>
            BuildGeneratedNodeLookup(
                GeneratedWorldSnapshot generated,
                List<string> errors,
                out int duplicateCount)
        {
            duplicateCount = 0;
            var result = new Dictionary<RegionCellId, GeneratedResourceNodeRecord>();
            if (generated == null) return result;
            foreach (GeneratedResourceNodeRecord node in generated.Nodes)
            {
                if (node.Location.Kind != RegionLocationKind.Colony ||
                    node.Location.RegionId != AntRegionIds.Colony) continue;
                RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                    node.Location.ColonyCell);
                if (!result.TryAdd(cellId, node))
                {
                    duplicateCount++;
                    errors.Add($"Generated Colony Node provenance is duplicated at {cellId}.");
                }
            }
            return result;
        }

        private static string CreateNodeId(
            ColonyResourceNodeScenarioIdentity scenario,
            RegionCellId cellId,
            string solidId,
            string generatedNodeId)
        {
            string source = string.IsNullOrWhiteSpace(generatedNodeId)
                ? $"{solidId}.{cellId.ColonyCell.X}.{cellId.ColonyCell.Y}"
                : generatedNodeId;
            return $"inactive-node.{AntRegionIds.Colony.Value}." +
                $"{scenario.StableKey}.{source}";
        }
    }
}
