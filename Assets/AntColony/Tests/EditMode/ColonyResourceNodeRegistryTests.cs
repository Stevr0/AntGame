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
        public void FixtureCreatesUniqueActiveQuantityOneNodes()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);
            SetSolid(world, Vector2Int.right, StoneId);
            Assert.That(TryCreate(world, FixtureIdentity(2203), null,
                out ColonyResourceNodeRegistry registry,
                out ColonyResourceNodeRegistryBuildReport report), Is.True,
                report.Summary);

            Assert.That(registry.Nodes.Count, Is.EqualTo(2));
            Assert.That(registry.Nodes.Select(n => n.NodeId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(registry.Nodes.All(n => n.InitialQuantity == 1 &&
                n.RemainingQuantity == 1 && n.Revision == ColonyResourceNodeRegistry.InitialRevision &&
                n.Lifecycle == ColonyResourceNodeLifecycle.Active &&
                n.IsLegacyQuantityOneFixture), Is.True);
            Assert.That(registry.Diagnostics.TotalInitialSoil, Is.EqualTo(1));
            Assert.That(registry.Diagnostics.TotalInitialStone, Is.EqualTo(1));
            registry.Dispose();
        }

        [Test]
        public void GeneratedRegistryOwnsGeneratedVariableQuantities()
        {
            CreateGeneratedRegistry(2203L, out GeneratedWorldSnapshot generated,
                out _, out ColonyResourceNodeRegistry registry);
            foreach (ColonyResourceNodeSnapshot node in registry.Nodes)
            {
                GeneratedResourceNodeRecord source = generated.Nodes.Single(n =>
                    n.Location.Kind == RegionLocationKind.Colony &&
                    n.Location.ColonyCell.Equals(node.CellId.ColonyCell));
                Assert.That(node.InitialQuantity, Is.EqualTo(source.InitialQuantity));
                Assert.That(node.RemainingQuantity, Is.EqualTo(source.InitialQuantity));
                Assert.That(node.IsLegacyQuantityOneFixture, Is.False);
                if (node.OutputMaterial == ExcavatedMaterialType.Soil)
                    Assert.That(node.InitialQuantity, Is.InRange(8, 12));
                else Assert.That(node.InitialQuantity, Is.InRange(5, 15));
            }
            Assert.That(registry.Diagnostics.MismatchedNodeCount, Is.Zero);
            registry.Dispose();
        }

        [Test]
        public void IdentityIsDeterministicAndScenarioScoped()
        {
            ChunkedWorldData firstWorld = CreateWorld();
            SetSolid(firstWorld, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(firstWorld, FixtureIdentity(2203), null,
                out ColonyResourceNodeRegistry first, out _), Is.True);
            ChunkedWorldData repeatedWorld = CreateWorld();
            SetSolid(repeatedWorld, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(repeatedWorld, FixtureIdentity(2203), null,
                out ColonyResourceNodeRegistry repeated, out _), Is.True);
            ChunkedWorldData otherWorld = CreateWorld();
            SetSolid(otherWorld, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(otherWorld, FixtureIdentity(2204), null,
                out ColonyResourceNodeRegistry other, out _), Is.True);
            Assert.That(first.Nodes[0].NodeId, Is.EqualTo(repeated.Nodes[0].NodeId));
            Assert.That(other.Nodes[0].NodeId, Is.Not.EqualTo(first.Nodes[0].NodeId));
            first.Dispose(); repeated.Dispose(); other.Dispose();
        }

        [Test]
        public void ReadsDoNotAdvanceRevisionOrQuantity()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out ColonyResourceNodeRegistry registry, out _), Is.True);
            RegionCellId cell = ColonyCell(Vector2Int.zero);
            long registryRevision = registry.Revision;
            Assert.That(registry.TryGet(cell, out var first), Is.EqualTo(ColonyResourceNodeReadResult.Success));
            Assert.That(registry.TryGet(cell, out var second), Is.EqualTo(ColonyResourceNodeReadResult.Success));
            Assert.That(second.NodeId, Is.EqualTo(first.NodeId));
            Assert.That(second.RemainingQuantity, Is.EqualTo(1));
            Assert.That(registry.Revision, Is.EqualTo(registryRevision));
            Assert.That(registry.TryGet(RegionCellId.Surface(AntRegionIds.Colony,
                new SurfaceCell(0, 0)), out _), Is.EqualTo(ColonyResourceNodeReadResult.InvalidCoordinateKind));
            registry.Dispose();
        }

        [Test]
        public void DirectSolidRemovalAndReplacementAreRejected()
        {
            CreateGeneratedRegistry(2203L, out _, out ChunkedWorldData world,
                out ColonyResourceNodeRegistry registry);
            ColonyResourceNodeSnapshot node = registry.Nodes.First();
            Vector2Int cell = new(node.CellId.ColonyCell.X, node.CellId.ColonyCell.Y);
            long terrainRevision = world.GetCellRevision(cell);
            Assert.That(world.RemoveSolidMaterial(cell), Is.EqualTo(WorldCellMutationResult.AuthorityRejected));
            Assert.That(world.SetSolidMaterial(cell, node.OutputMaterial == ExcavatedMaterialType.Soil
                ? StoneId : SoilId), Is.EqualTo(WorldCellMutationResult.AuthorityRejected));
            Assert.That(world.GetCellRevision(cell), Is.EqualTo(terrainRevision));
            registry.TryGet(node.NodeId, out ColonyResourceNodeSnapshot after);
            Assert.That(after.RemainingQuantity, Is.EqualTo(node.RemainingQuantity));
            registry.Dispose();
        }

        [Test]
        public void AuthorizedFixtureRemovalDepletesNodeAndPreservesBackground()
        {
            ChunkedWorldData world = CreateWorld();
            SetSolid(world, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(world, FixtureIdentity(7), null,
                out ColonyResourceNodeRegistry registry, out _), Is.True);
            Assert.That(registry.TryPrepareLegacyExtractionRemoval(Vector2Int.zero,
                "job.1", out string token, out string error), Is.True, error);
            Assert.That(world.RemoveSolidMaterial(Vector2Int.zero, token),
                Is.EqualTo(WorldCellMutationResult.Success));
            Assert.That(registry.TryGet(ColonyCell(Vector2Int.zero), out var after),
                Is.EqualTo(ColonyResourceNodeReadResult.Success));
            Assert.That(after.RemainingQuantity, Is.Zero);
            Assert.That(after.Lifecycle, Is.EqualTo(ColonyResourceNodeLifecycle.Depleted));
            Assert.That(world.GetCell(Vector2Int.zero, out WorldTileCellData cell),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(cell.BackgroundMaterialId, Is.EqualTo(EarthId));
            registry.Dispose();
        }

        [Test]
        public void QuantityMutationValidatesRevisionOwnerBoundsAndReplay()
        {
            CreateGeneratedRegistry(2203L, out _, out _, out ColonyResourceNodeRegistry registry);
            ColonyResourceNodeSnapshot initial = registry.Nodes.First(n => n.InitialQuantity > 2);
            Assert.That(registry.TryReserve(initial.NodeId, "operation.owner",
                initial.Revision, out string error), Is.True, error);
            registry.TryGet(initial.NodeId, out ColonyResourceNodeSnapshot reserved);
            Assert.That(registry.TryBeginPendingExtraction(initial.NodeId,
                "operation.owner", reserved.Revision, out error), Is.True, error);
            registry.TryGet(initial.NodeId, out ColonyResourceNodeSnapshot pending);
            long beforeFailedRevision = pending.Revision;
            Assert.That(registry.TryCommitQuantityReduction(initial.NodeId,
                "wrong.owner", "operation.bad", 1, pending.Revision,
                out _, out _), Is.False);
            registry.TryGet(initial.NodeId, out ColonyResourceNodeSnapshot unchanged);
            Assert.That(unchanged.Revision, Is.EqualTo(beforeFailedRevision));
            Assert.That(unchanged.RemainingQuantity, Is.EqualTo(initial.RemainingQuantity));
            Assert.That(registry.TryCommitQuantityReduction(initial.NodeId,
                "operation.owner", "operation.good", 2, pending.Revision,
                out ColonyResourceNodeSnapshot committed, out error), Is.True, error);
            long committedRevision = committed.Revision;
            Assert.That(committed.RemainingQuantity, Is.EqualTo(initial.RemainingQuantity - 2));
            Assert.That(registry.TryCommitQuantityReduction(initial.NodeId,
                "operation.owner", "operation.good", 2, pending.Revision,
                out ColonyResourceNodeSnapshot replay, out error), Is.True, error);
            Assert.That(replay.RemainingQuantity, Is.EqualTo(committed.RemainingQuantity));
            Assert.That(replay.Revision, Is.EqualTo(committedRevision));
            registry.Dispose();
        }

        [Test]
        public void CompetingReservationAndStaleRevisionFailWithoutMutation()
        {
            CreateGeneratedRegistry(2203L, out _, out _, out ColonyResourceNodeRegistry registry);
            ColonyResourceNodeSnapshot node = registry.Nodes.First();
            Assert.That(registry.TryReserve(node.NodeId, "first", node.Revision, out _), Is.True);
            registry.TryGet(node.NodeId, out ColonyResourceNodeSnapshot reserved);
            Assert.That(registry.TryReserve(node.NodeId, "second", node.Revision, out _), Is.False);
            Assert.That(registry.TryBeginPendingExtraction(node.NodeId, "first",
                node.Revision, out _), Is.False);
            registry.TryGet(node.NodeId, out ColonyResourceNodeSnapshot unchanged);
            Assert.That(unchanged.Revision, Is.EqualTo(reserved.Revision));
            Assert.That(unchanged.ReservationOwner, Is.EqualTo("first"));
            Assert.That(registry.TryReleaseReservation(node.NodeId, "first", out _), Is.True);
            registry.Dispose();
        }

        [Test]
        public void RuntimeSnapshotUsesActiveAuthorityAndPublishesOneChangePerMutation()
        {
            CreateGeneratedRegistry(2203L, out GeneratedWorldSnapshot generated,
                out ChunkedWorldData world, out ColonyResourceNodeRegistry registry);
            var drops = new WorldMaterialDropRegistry(world);
            var adapter = new ColonyRuntimeCellRegionAdapter(world, drops,
                generated, registry);
            ColonyResourceNodeSnapshot node = registry.Nodes.First();

            Assert.That(adapter.TryGetCell(node.CellId,
                out RuntimeCellSnapshot before), Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(before.Node.ProjectionState,
                Is.EqualTo(RuntimeOccupantProjectionState.Authoritative));
            Assert.That(before.Node.StableId, Is.EqualTo(node.NodeId));
            Assert.That(before.Node.ResourceDefinitionId,
                Is.EqualTo(node.OutputResourceId));
            Assert.That(before.Node.InitialQuantity, Is.EqualTo(node.InitialQuantity));
            Assert.That(before.Node.RemainingQuantity, Is.EqualTo(node.RemainingQuantity));
            Assert.That(before.Node.NodeRevision, Is.EqualTo(node.Revision));
            Assert.That(before.Node.Lifecycle,
                Is.EqualTo(ColonyResourceNodeLifecycle.Active));
            Assert.That(before.Node.ExtractionStatus, Is.EqualTo(
                ColonyResourceNodeExtractionStatus.Interactive));

            long sequence = adapter.Changes.CurrentSequence;
            Assert.That(registry.TryReserve(node.NodeId, "worker",
                node.Revision, out string error), Is.True, error);
            Assert.That(adapter.Changes.CurrentSequence, Is.EqualTo(sequence + 1));
            Assert.That(adapter.Changes.ReadAfter(sequence, out var changes),
                Is.EqualTo(RuntimeCellChangeReadResult.Success));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].Changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].Changes[0].Category,
                Is.EqualTo(RuntimeCellChangeCategory.Node));
            Assert.That(adapter.TryGetCell(node.CellId, out RuntimeCellSnapshot reserved),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(reserved.Node.NodeRevision, Is.EqualTo(node.Revision + 1));
            Assert.That(reserved.Node.Lifecycle,
                Is.EqualTo(ColonyResourceNodeLifecycle.Reserved));

            long afterReserve = adapter.Changes.CurrentSequence;
            Assert.That(registry.TryReserve(node.NodeId, "other",
                node.Revision, out _), Is.False);
            Assert.That(adapter.Changes.CurrentSequence, Is.EqualTo(afterReserve),
                "A failed stale mutation must not publish a change set.");
            adapter.Dispose();
            registry.Dispose();
        }

        [Test]
        public void ScenarioReplacementInvalidatesOldIdentityAndReservation()
        {
            ChunkedWorldData oldWorld = CreateWorld();
            SetSolid(oldWorld, Vector2Int.zero, SoilId);
            Assert.That(TryCreate(oldWorld, FixtureIdentity(7), null,
                out ColonyResourceNodeRegistry oldRegistry, out _), Is.True);
            ColonyResourceNodeSnapshot oldNode = oldRegistry.Nodes.Single();
            Assert.That(oldRegistry.TryReserve(oldNode.NodeId, "owner",
                oldNode.Revision, out _), Is.True);
            oldRegistry.InvalidateContext();
            Assert.That(oldRegistry.Lifecycle,
                Is.EqualTo(ColonyResourceNodeRegistryLifecycle.ContextInvalidated));
            Assert.That(oldRegistry.TryGet(oldNode.NodeId, out _),
                Is.EqualTo(ColonyResourceNodeReadResult.ContextInvalidated));
            Assert.That(oldRegistry.TryReleaseReservation(oldNode.NodeId,
                "owner", out _), Is.False);
        }

        [Test]
        public void UnsupportedSolidIsReportedWithoutBecomingNode()
        {
            ChunkedWorldData world = CreateWorld(includeUnsupported: true);
            SetSolid(world, Vector2Int.zero, SoilId);
            SetSolid(world, Vector2Int.right, UnsupportedId);
            Assert.That(TryCreate(world, FixtureIdentity(1), null,
                out ColonyResourceNodeRegistry registry,
                out ColonyResourceNodeRegistryBuildReport report), Is.True,
                report.Summary);
            Assert.That(registry.Nodes.Count, Is.EqualTo(1));
            Assert.That(registry.Diagnostics.UnsupportedSolidCount, Is.EqualTo(1));
            registry.Dispose();
        }

        private void CreateGeneratedRegistry(long seed,
            out GeneratedWorldSnapshot generated, out ChunkedWorldData world,
            out ColonyResourceNodeRegistry registry)
        {
            WorldMaterialRegistry materials = CreateMaterialRegistry();
            Assert.That(ProceduralWorldGenerator.TryGenerate(seed,
                new WorldGenerationConfiguration(), out generated,
                out GeneratedWorldValidationReport generationReport), Is.True,
                generationReport.Summary);
            world = generated.CreateColonyWorld(materials,
                new SampleWorldMaterialIds(EarthId, EarthId, EarthId, EarthId,
                    SoilId, StoneId));
            var identity = new ColonyResourceNodeScenarioIdentity(
                generated.Metadata.ConfigurationId, generated.Metadata.Version,
                generated.Metadata.Seed);
            Assert.That(TryCreate(world, identity, generated, out registry,
                out ColonyResourceNodeRegistryBuildReport report), Is.True,
                report.Summary);
        }

        private bool TryCreate(ChunkedWorldData world,
            ColonyResourceNodeScenarioIdentity identity,
            GeneratedWorldSnapshot generated, out ColonyResourceNodeRegistry registry,
            out ColonyResourceNodeRegistryBuildReport report) =>
            ColonyResourceNodeRegistry.TryCreate(world, identity, SoilId,
                StoneId, generated, out registry, out report);

        private ChunkedWorldData CreateWorld(bool includeUnsupported = false)
        {
            var world = new ChunkedWorldData(CreateMaterialRegistry(includeUnsupported), 2203L);
            Assert.That(world.CreateChunk(Vector2Int.zero, EarthId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            return world;
        }

        private WorldMaterialRegistry CreateMaterialRegistry(bool includeUnsupported = false)
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(EarthId);
            var solids = new List<SolidTileMaterialDefinition>
            {
                CreateSolid(SoilId, ExcavatedMaterialType.Soil, 1),
                CreateSolid(StoneId, ExcavatedMaterialType.Stone, 1)
            };
            if (includeUnsupported)
                solids.Add(CreateSolid(UnsupportedId, ExcavatedMaterialType.None, 0, false));
            return new WorldMaterialRegistry(new[] { earth }, solids);
        }

        private BackgroundTileMaterialDefinition CreateBackground(string stableId)
        {
            var definition = ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>();
            SetBaseFields(definition, stableId); definitions.Add(definition); return definition;
        }
        private SolidTileMaterialDefinition CreateSolid(string stableId,
            ExcavatedMaterialType material, int yield, bool isExcavatable = true)
        {
            var definition = ScriptableObject.CreateInstance<SolidTileMaterialDefinition>();
            SetBaseFields(definition, stableId);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("isExcavatable").boolValue = isExcavatable;
            serialized.FindProperty("excavatedMaterial").enumValueIndex = (int)material;
            serialized.FindProperty("excavationYield").intValue = yield;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            definitions.Add(definition); return definition;
        }
        private static void SetBaseFields(WorldTileMaterialDefinition definition, string stableId)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = stableId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetSolid(ChunkedWorldData world, Vector2Int cell, string solidId) =>
            Assert.That(world.SetSolidMaterial(cell, solidId), Is.EqualTo(WorldCellMutationResult.Success));
        private static RegionCellId ColonyCell(Vector2Int cell) =>
            RegionCellId.Colony(AntRegionIds.Colony, new ColonyCell(cell.x, cell.y));
        private static ColonyResourceNodeScenarioIdentity FixtureIdentity(long seed) =>
            new(ColonyResourceNodeRegistry.DevelopmentFixtureNamespace,
                ColonyResourceNodeRegistry.DevelopmentFixtureVersion, seed);
    }
}
