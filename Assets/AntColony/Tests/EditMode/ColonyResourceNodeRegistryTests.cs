using System.Collections.Generic;
using System.Linq;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    public sealed class ColonyResourceNodeRegistryTests
    {
        private const string EarthId = "background.test-earth";
        private const string SoilId = "solid.soil";
        private const string StoneId = "solid.stone";
        private const string UnsupportedId = "solid.test-blocker";
        private readonly List<ScriptableObject> definitions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject definition in definitions)
                Object.DestroyImmediate(definition);
            definitions.Clear();
        }

        [Test]
        public void SoilAndStoneCreateUniqueQuantityOneInactiveRecords()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, new Vector2Int(0, 0), SoilId);
            SetSolid(world, new Vector2Int(1, 0), StoneId);

            Assert.That(TryCreate(world, FixtureIdentity(2203), null,
                out InactiveColonyResourceNodeRegistry registry,
                out ColonyResourceNodeRegistryBuildReport report), Is.True,
                report.Summary);

            Assert.That(registry.Nodes.Count, Is.EqualTo(2));
            Assert.That(registry.Nodes.Select(node => node.NodeId).Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(registry.Nodes.All(node => node.InitialQuantity == 1 &&
                node.RemainingQuantity == 1 &&
                node.Revision == InactiveColonyResourceNodeRegistry.InitialRevision &&
                node.Lifecycle ==
                    InactiveColonyNodeLifecycle.ValidInactiveCandidate), Is.True);
            Assert.That(registry.Diagnostics.SoilCount, Is.EqualTo(1));
            Assert.That(registry.Diagnostics.StoneCount, Is.EqualTo(1));
            Assert.That(registry.Nodes.Single(node => node.DefinitionId == SoilId)
                .OutputMaterial, Is.EqualTo(ExcavatedMaterialType.Soil));
            Assert.That(registry.Nodes.Single(node => node.DefinitionId == StoneId)
                .OutputMaterial, Is.EqualTo(ExcavatedMaterialType.Stone));
            registry.Dispose();
        }

        [Test]
        public void IdentityIsDeterministicAndScenarioScoped()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);

            Assert.That(TryCreate(world, FixtureIdentity(2203), null,
                out InactiveColonyResourceNodeRegistry first, out _), Is.True);
            Assert.That(TryCreate(world, FixtureIdentity(2203), null,
                out InactiveColonyResourceNodeRegistry repeated, out _), Is.True);
            Assert.That(TryCreate(world, FixtureIdentity(2204), null,
                out InactiveColonyResourceNodeRegistry otherScenario, out _), Is.True);

            Assert.That(first.Nodes[0].NodeId,
                Is.EqualTo(repeated.Nodes[0].NodeId));
            Assert.That(otherScenario.Nodes[0].NodeId,
                Is.Not.EqualTo(first.Nodes[0].NodeId));
            Assert.That(first.Nodes[0].CellId.Kind,
                Is.EqualTo(RegionLocationKind.Colony));
            first.Dispose(); repeated.Dispose(); otherScenario.Dispose();
        }

        [Test]
        public void GeneratedRegistryReusesVersionedGenerationProvenance()
        {
            WorldMaterialRegistry materials = CreateMaterialRegistry();
            Assert.That(ProceduralWorldGenerator.TryGenerate(2203L,
                new WorldGenerationConfiguration(), out GeneratedWorldSnapshot generated,
                out GeneratedWorldValidationReport generationReport), Is.True,
                generationReport.Summary);
            ChunkedWorldData world = generated.CreateColonyWorld(materials,
                new SampleWorldMaterialIds(EarthId, EarthId, EarthId, EarthId,
                    SoilId, StoneId));
            var identity = new ColonyResourceNodeScenarioIdentity(
                generated.Metadata.ConfigurationId, generated.Metadata.Version,
                generated.Metadata.Seed);

            Assert.That(TryCreate(world, identity, generated,
                out InactiveColonyResourceNodeRegistry registry,
                out ColonyResourceNodeRegistryBuildReport report), Is.True,
                report.Summary);
            GeneratedResourceNodeRecord source = generated.Nodes.First(node =>
                node.Location.Kind == RegionLocationKind.Colony &&
                node.Location.ColonyCell.Equals(
                    ProceduralWorldGenerator.FoundingStoneFace));
            RegionCellId cell = RegionCellId.Colony(AntRegionIds.Colony,
                source.Location.ColonyCell);
            Assert.That(registry.TryGet(cell, out var record),
                Is.EqualTo(InactiveColonyNodeReadResult.Success));
            Assert.That(record.NodeId, Does.Contain(source.Id));
            Assert.That(record.NodeId, Does.Contain(
                $"{generated.Metadata.ConfigurationId}.v{generated.Metadata.Version}"));
            Assert.That(registry.TryGet(RegionCellId.Colony(AntRegionIds.Colony,
                generated.QueenSpawn), out _),
                Is.EqualTo(InactiveColonyNodeReadResult.NotRegistered));
            registry.Dispose();
        }

        [Test]
        public void InvalidAndUnsupportedSolidsAreHandledWithoutTerrainMutation()
        {
            ChunkedWorldData world = CreateWorld(includeUnsupported: true);
            SetSolid(world, Vector2Int.zero, SoilId);
            SetSolid(world, Vector2Int.right, UnsupportedId);
            long soilRevision = world.GetCellRevision(Vector2Int.zero);
            long unsupportedRevision = world.GetCellRevision(Vector2Int.right);

            Assert.That(TryCreate(world, FixtureIdentity(1), null,
                out InactiveColonyResourceNodeRegistry registry,
                out ColonyResourceNodeRegistryBuildReport report), Is.True,
                report.Summary);
            Assert.That(registry.Nodes.Count, Is.EqualTo(1));
            Assert.That(registry.Diagnostics.UnsupportedSolidCount, Is.EqualTo(1));
            Assert.That(world.GetCellRevision(Vector2Int.zero), Is.EqualTo(soilRevision));
            Assert.That(world.GetCellRevision(Vector2Int.right),
                Is.EqualTo(unsupportedRevision));
            Assert.That(world.GetSolidMaterialId(Vector2Int.right, out string solid),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(solid, Is.EqualTo(UnsupportedId));
            registry.Dispose();

            Assert.That(InactiveColonyResourceNodeRegistry.TryCreate(world,
                FixtureIdentity(1), SoilId, "solid.missing", null,
                out registry, out report), Is.False);
            Assert.That(registry, Is.Null);
            Assert.That(report.Summary, Does.Contain("not a valid registered"));
            Assert.That(world.GetSolidMaterialId(Vector2Int.zero, out solid),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(solid, Is.EqualTo(SoilId));
        }

        [Test]
        public void ReadsAreTypedAndDoNotAdvanceRevisionsOrQuantity()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out InactiveColonyResourceNodeRegistry registry, out _), Is.True);
            RegionCellId cell = ColonyCell(Vector2Int.zero);
            long registryRevision = registry.Revision;

            Assert.That(registry.TryGet(cell, out var first),
                Is.EqualTo(InactiveColonyNodeReadResult.Success));
            Assert.That(registry.TryGet(cell, out var second),
                Is.EqualTo(InactiveColonyNodeReadResult.Success));
            Assert.That(second.NodeId, Is.EqualTo(first.NodeId));
            Assert.That(second.RemainingQuantity, Is.EqualTo(1));
            Assert.That(registry.Revision, Is.EqualTo(registryRevision));
            Assert.That(registry.TryGet(RegionCellId.Surface(AntRegionIds.Colony,
                new SurfaceCell(0, 0)), out _),
                Is.EqualTo(InactiveColonyNodeReadResult.InvalidCoordinateKind));
            Assert.That(registry.TryGet(RegionCellId.Colony(AntRegionIds.Surface,
                new ColonyCell(0, 0)), out _),
                Is.EqualTo(InactiveColonyNodeReadResult.InvalidRegion));
            Assert.That(registry.TryGet(ColonyCell(Vector2Int.one), out _),
                Is.EqualTo(InactiveColonyNodeReadResult.NotRegistered));
            registry.Dispose();
        }

        [Test]
        public void TerrainMutationInvalidatesOnlyTheAffectedRecord()
        {
            ChunkedWorldData world = CreateWorld();
            Vector2Int affected = Vector2Int.zero;
            Vector2Int unaffected = Vector2Int.right;
            SetSolid(world, affected, SoilId);
            SetSolid(world, unaffected, StoneId);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out InactiveColonyResourceNodeRegistry registry, out _), Is.True);
            registry.TryGet(ColonyCell(affected), out var beforeAffected);
            registry.TryGet(ColonyCell(unaffected), out var beforeUnaffected);

            Assert.That(world.RemoveSolidMaterial(affected),
                Is.EqualTo(WorldCellMutationResult.Success));
            Assert.That(registry.TryGet(ColonyCell(affected), out var afterAffected),
                Is.EqualTo(InactiveColonyNodeReadResult.Success));
            Assert.That(afterAffected.Lifecycle,
                Is.EqualTo(InactiveColonyNodeLifecycle.DepletedOrAbsent));
            Assert.That(afterAffected.Revision, Is.EqualTo(beforeAffected.Revision + 1));
            Assert.That(afterAffected.RemainingQuantity, Is.EqualTo(1),
                "The inactive registry must not perform a gameplay quantity write.");
            Assert.That(registry.TryGet(ColonyCell(unaffected), out var afterUnaffected),
                Is.EqualTo(InactiveColonyNodeReadResult.Success));
            Assert.That(afterUnaffected.NodeId, Is.EqualTo(beforeUnaffected.NodeId));
            Assert.That(afterUnaffected.Revision, Is.EqualTo(beforeUnaffected.Revision));
            Assert.That(registry.Diagnostics.InvalidOrStaleCount, Is.EqualTo(1));
            registry.Dispose();
        }

        [Test]
        public void BackgroundMutationDoesNotInvalidateNodeAndRemovedNodeCannotRevive()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out InactiveColonyResourceNodeRegistry registry, out _), Is.True);
            registry.TryGet(ColonyCell(Vector2Int.zero), out var before);

            Assert.That(world.SetBackgroundMaterial(Vector2Int.zero, EarthId),
                Is.EqualTo(WorldCellMutationResult.Success));
            registry.TryGet(ColonyCell(Vector2Int.zero), out var unchanged);
            Assert.That(unchanged.Revision, Is.EqualTo(before.Revision));
            Assert.That(world.RemoveSolidMaterial(Vector2Int.zero),
                Is.EqualTo(WorldCellMutationResult.Success));
            Assert.That(world.SetSolidMaterial(Vector2Int.zero, SoilId),
                Is.EqualTo(WorldCellMutationResult.Success));
            registry.TryGet(ColonyCell(Vector2Int.zero), out var restored);
            Assert.That(restored.Lifecycle,
                Is.EqualTo(InactiveColonyNodeLifecycle.DepletedOrAbsent));
            Assert.That(restored.NodeId, Is.EqualTo(before.NodeId));
            registry.Dispose();
        }

        [Test]
        public void ScenarioReplacementInvalidatesOldHandleAndRepeatedBuildDoesNotDuplicate()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out InactiveColonyResourceNodeRegistry oldRegistry, out _), Is.True);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out InactiveColonyResourceNodeRegistry replacement, out _), Is.True);
            string replacementId = replacement.Nodes.Single().NodeId;

            oldRegistry.InvalidateContext();
            Assert.That(oldRegistry.Lifecycle,
                Is.EqualTo(InactiveColonyNodeRegistryLifecycle.ContextInvalidated));
            Assert.That(oldRegistry.TryGet(ColonyCell(Vector2Int.zero), out _),
                Is.EqualTo(InactiveColonyNodeReadResult.ContextInvalidated));
            Assert.That(oldRegistry.Diagnostics.InvalidOrStaleCount, Is.EqualTo(1));
            Assert.That(replacement.Nodes.Count, Is.EqualTo(1));
            Assert.That(replacement.Nodes.Single().NodeId, Is.EqualTo(replacementId));
            replacement.Dispose();
        }

        private bool TryCreate(ChunkedWorldData world,
            ColonyResourceNodeScenarioIdentity identity,
            GeneratedWorldSnapshot generated,
            out InactiveColonyResourceNodeRegistry registry,
            out ColonyResourceNodeRegistryBuildReport report) =>
            InactiveColonyResourceNodeRegistry.TryCreate(world, identity,
                SoilId, StoneId, generated, out registry, out report);

        private ChunkedWorldData CreateWorld(bool includeUnsupported = false)
        {
            WorldMaterialRegistry registry = CreateMaterialRegistry(includeUnsupported);
            var world = new ChunkedWorldData(registry, 2203L);
            Assert.That(world.CreateChunk(Vector2Int.zero, EarthId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            return world;
        }

        private WorldMaterialRegistry CreateMaterialRegistry(
            bool includeUnsupported = false)
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(EarthId);
            SolidTileMaterialDefinition soil = CreateSolid(
                SoilId, ExcavatedMaterialType.Soil, 1);
            SolidTileMaterialDefinition stone = CreateSolid(
                StoneId, ExcavatedMaterialType.Stone, 1);
            var solids = new List<SolidTileMaterialDefinition> { soil, stone };
            if (includeUnsupported)
                solids.Add(CreateSolid(UnsupportedId, ExcavatedMaterialType.None, 0,
                    isExcavatable: false));
            return new WorldMaterialRegistry(new[] { earth }, solids);
        }

        private BackgroundTileMaterialDefinition CreateBackground(string stableId)
        {
            BackgroundTileMaterialDefinition definition =
                ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>();
            SetBaseFields(definition, stableId);
            definitions.Add(definition);
            return definition;
        }

        private SolidTileMaterialDefinition CreateSolid(string stableId,
            ExcavatedMaterialType material, int extractionYield,
            bool isExcavatable = true)
        {
            SolidTileMaterialDefinition definition =
                ScriptableObject.CreateInstance<SolidTileMaterialDefinition>();
            SetBaseFields(definition, stableId);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("isExcavatable").boolValue = isExcavatable;
            serialized.FindProperty("excavatedMaterial").enumValueIndex = (int)material;
            serialized.FindProperty("excavationYield").intValue = extractionYield;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            definitions.Add(definition);
            return definition;
        }

        private static void SetBaseFields(WorldTileMaterialDefinition definition,
            string stableId)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = stableId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSolid(ChunkedWorldData world,
            Vector2Int cell, string solidId) =>
            Assert.That(world.SetSolidMaterial(cell, solidId),
                Is.EqualTo(WorldCellMutationResult.Success));

        private static RegionCellId ColonyCell(Vector2Int cell) =>
            RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(cell.x, cell.y));

        private static ColonyResourceNodeScenarioIdentity FixtureIdentity(long seed) =>
            new(InactiveColonyResourceNodeRegistry.DevelopmentFixtureNamespace,
                InactiveColonyResourceNodeRegistry.DevelopmentFixtureVersion, seed);
    }
}
