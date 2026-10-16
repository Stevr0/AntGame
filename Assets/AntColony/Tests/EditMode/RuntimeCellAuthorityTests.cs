using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    public sealed class RuntimeCellAuthorityTests
    {
        private const string EarthId = "background.test-earth";
        private const string SoilId = "solid.test-soil";
        private readonly List<ScriptableObject> definitions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject definition in definitions)
                UnityEngine.Object.DestroyImmediate(definition);
            definitions.Clear();
        }

        [Test]
        public void TypedIdentityNeverErasesTheCoordinateDomain()
        {
            RegionCellId colony = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(4, -8));
            RegionCellId surface = RegionCellId.Surface(AntRegionIds.Surface,
                new SurfaceCell(4, 8));

            Assert.That(colony.Kind, Is.EqualTo(RegionLocationKind.Colony));
            Assert.That(surface.Kind, Is.EqualTo(RegionLocationKind.Surface));
            Assert.That(colony, Is.Not.EqualTo(surface));
            Assert.That(new[] { surface, colony }.OrderBy(id => id).First(),
                Is.EqualTo(colony));
        }

        [Test]
        public void SnapshotIsImmutableAndRejectsWrongRegionOrCoordinateKind()
        {
            ColonyRuntimeCellRegionAdapter adapter = CreateColonyAdapter(
                out _, out _);
            RegionCellId origin = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(0, 0));

            Assert.That(adapter.TryGetCell(origin, out RuntimeCellSnapshot snapshot),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(snapshot.BackgroundDefinitionId, Is.EqualTo(EarthId));
            Assert.That(adapter.TryGetCell(origin, out RuntimeCellSnapshot repeated),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(repeated.CellRevision, Is.EqualTo(snapshot.CellRevision));
            Assert.That(typeof(RuntimeCellSnapshot).GetProperties()
                .All(property => !property.CanWrite), Is.True);
            Assert.That(adapter.TryGetCell(RegionCellId.Surface(AntRegionIds.Colony,
                new SurfaceCell(0, 0)), out _),
                Is.EqualTo(RuntimeCellReadResult.InvalidCoordinateKind));
            Assert.That(adapter.TryGetCell(RegionCellId.Colony(AntRegionIds.Surface,
                new ColonyCell(0, 0)), out _),
                Is.EqualTo(RuntimeCellReadResult.InvalidRegion));
            Assert.That(adapter.TryGetCell(RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(32, 0)), out _),
                Is.EqualTo(RuntimeCellReadResult.CellUnavailable));
            adapter.Dispose();
        }

        [Test]
        public void TerrainAndDropsAdvanceOneCellRevisionAndSharedChangeFeed()
        {
            ColonyRuntimeCellRegionAdapter adapter = CreateColonyAdapter(
                out ChunkedWorldData world, out WorldMaterialDropRegistry drops);
            RegionCellId origin = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(0, 0));
            adapter.TryGetCell(origin, out RuntimeCellSnapshot before);

            Assert.That(world.SetSolidMaterial(Vector2Int.zero, SoilId),
                Is.EqualTo(WorldCellMutationResult.Success));
            Assert.That(world.SetSolidMaterial(Vector2Int.zero, SoilId),
                Is.EqualTo(WorldCellMutationResult.Success),
                "An identical authoritative write remains a successful no-op.");
            Assert.That(drops.TryAdd("drop.test", ExcavatedMaterialType.Soil, 2,
                Vector2Int.zero, out _, out string error), Is.True, error);
            adapter.TryGetCell(origin, out RuntimeCellSnapshot after);

            Assert.That(after.CellRevision, Is.EqualTo(before.CellRevision + 2));
            Assert.That(after.Node.DefinitionId, Is.EqualTo(SoilId));
            Assert.That(after.LooseStack.Quantity, Is.EqualTo(2));
            Assert.That(after.LooseStack.ProjectionState,
                Is.EqualTo(RuntimeOccupantProjectionState.Authoritative));
            Assert.That(after.LooseStack.ReservedQuantity, Is.Zero);
            Assert.That(after.LooseStack.AvailableQuantity, Is.EqualTo(2));
            Assert.That(after.LooseStack.StackRevision, Is.EqualTo(1));
            Assert.That(after.LooseStack.CellId, Is.EqualTo(origin));
            Assert.That(adapter.Changes.CurrentSequence, Is.EqualTo(2));
            Assert.That(adapter.Changes.ReadAfter(0, out var firstConsumer),
                Is.EqualTo(RuntimeCellChangeReadResult.Success));
            Assert.That(adapter.Changes.ReadAfter(0, out var secondConsumer),
                Is.EqualTo(RuntimeCellChangeReadResult.Success));
            Assert.That(firstConsumer.Count, Is.EqualTo(secondConsumer.Count));
            Assert.That(firstConsumer[0].Changes[0].Category,
                Is.EqualTo(RuntimeCellChangeCategory.Node));
            Assert.That(firstConsumer[1].Changes[0].Category,
                Is.EqualTo(RuntimeCellChangeCategory.LooseResource));
            adapter.Dispose();
        }

        [Test]
        public void ReservationChangesAdvanceOnlyTheAffectedStackAndCell()
        {
            ColonyRuntimeCellRegionAdapter adapter = CreateColonyAdapter(
                out _, out WorldMaterialDropRegistry drops);
            RegionCellId origin = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(0, 0));
            drops.TryAdd("drop.reservation", ExcavatedMaterialType.Soil, 3,
                Vector2Int.zero, out WorldMaterialDrop stack, out _);
            adapter.TryGetCell(origin, out RuntimeCellSnapshot before);

            Assert.That(drops.TryReserve(stack.OwnershipKey, "ant", 2,
                out _, out string error), Is.True, error);
            adapter.TryGetCell(origin, out RuntimeCellSnapshot held);
            Assert.That(held.CellRevision, Is.EqualTo(before.CellRevision + 1));
            Assert.That(held.LooseStack.StackRevision,
                Is.EqualTo(before.LooseStack.StackRevision + 1));
            Assert.That(held.LooseStack.ReservedQuantity, Is.EqualTo(2));
            Assert.That(held.LooseStack.AvailableQuantity, Is.EqualTo(1));

            drops.ReleaseReservation(stack.OwnershipKey, "ant");
            adapter.TryGetCell(origin, out RuntimeCellSnapshot released);
            Assert.That(released.CellRevision, Is.EqualTo(held.CellRevision + 1));
            Assert.That(released.LooseStack.ReservedQuantity, Is.Zero);
            adapter.Dispose();
        }

        [Test]
        public void CompatibilityRulesArePureAndConservative()
        {
            RegionCellId id = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(1, -1));
            var clear = new RuntimeCellSnapshot(id, EarthId, 1, 1);
            var obstructed = new RuntimeCellSnapshot(id, EarthId, 1, 1,
                new RuntimeNodeSnapshot(
                    RuntimeOccupantProjectionState.CompatibilityProjected,
                    "node", SoilId, true, null, null,
                    RuntimeNodeInteractionState.CompatibilityOnly));
            var unknown = new RuntimeCellSnapshot(id, EarthId, 1, 1,
                looseStack: new RuntimeLooseStackSnapshot(
                    RuntimeOccupantProjectionState.DetailsUnavailable,
                    string.Empty, string.Empty, null, null, true));
            var occupiedStack = new RuntimeCellSnapshot(id, EarthId, 1, 1,
                looseStack: new RuntimeLooseStackSnapshot(
                    RuntimeOccupantProjectionState.CompatibilityProjected,
                    "drop", "material.soil", 8, 10, false));
            var storagePlacement = new RuntimeCellSnapshot(id, EarthId, 1, 1,
                placement: new RuntimePlacementSnapshot(
                    RuntimeOccupantProjectionState.CompatibilityProjected,
                    "storage", "background.storage", "MaterialStorage", true));

            Assert.That(RuntimeCellCompatibilityValidator
                .ValidateStandardConstruction(clear).Result,
                Is.EqualTo(RuntimeOccupancyCompatibility.Valid));
            Assert.That(RuntimeCellCompatibilityValidator
                .ValidateStandardConstruction(obstructed).Result,
                Is.EqualTo(RuntimeOccupancyCompatibility.Invalid));
            Assert.That(RuntimeCellCompatibilityValidator
                .ValidateObserved(unknown).Result,
                Is.EqualTo(RuntimeOccupancyCompatibility.Unknown));
            Assert.That(RuntimeCellCompatibilityValidator.ValidateLooseMerge(
                clear, "material.soil", 3, 10).Result,
                Is.EqualTo(RuntimeOccupancyCompatibility.Valid));
            Assert.That(RuntimeCellCompatibilityValidator.ValidateLooseMerge(
                occupiedStack, "material.stone", 1, 10).Result,
                Is.EqualTo(RuntimeOccupancyCompatibility.Invalid));
            Assert.That(RuntimeCellCompatibilityValidator.ValidateLooseMerge(
                occupiedStack, "material.soil", 3, 10).Result,
                Is.EqualTo(RuntimeOccupancyCompatibility.Invalid));
            Assert.That(storagePlacement.Placement.ContentsArePlacementOwned,
                Is.True);
            Assert.That(storagePlacement.LooseStack, Is.Null,
                "Placement-owned contents and site staging are not ground stacks.");
        }

        [Test]
        public void PublicationIsAtomicAndKeepsPreviousPairAfterValidationFailure()
        {
            ColonyRuntimeCellRegionAdapter colony = CreateColonyAdapter(
                out _, out _);
            var surface = new SurfaceRuntimeCellRegionAdapter(
                new SurfaceWorldData(2, 2));
            var registry = new RuntimeCellPublicationRegistry();

            Assert.That(registry.TryPublish(colony, surface, out string error),
                Is.True, error);
            RuntimeCellRegionSet first = registry.Current;
            Assert.That(first.PublicationRevision, Is.EqualTo(1));
            Assert.That(registry.TryGetCell(RegionCellId.Surface(
                AntRegionIds.Surface, new SurfaceCell(0, 0)), out var snapshot),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(snapshot.RegionPublication, Is.EqualTo(1));

            var invalid = new MissingBackgroundRegion();
            ColonyRuntimeCellRegionAdapter rejectedColony =
                CreateColonyAdapter(out _, out _);
            Assert.That(registry.TryPublish(rejectedColony, invalid, out error),
                Is.False);
            Assert.That(error, Does.Contain("invalid cell"));
            Assert.That(registry.Current, Is.SameAs(first));
            Assert.That(registry.PublicationRevision, Is.EqualTo(1));
            rejectedColony.Dispose();
            registry.Current.Dispose();
        }

        [Test]
        public void FixtureObstacleExplicitlyReportsUnavailableNodeDetails()
        {
            var adapter = new SurfaceRuntimeCellRegionAdapter(
                SurfaceWorldData.CreateDevelopmentFixture());
            Assert.That(adapter.TryGetCell(RegionCellId.Surface(
                AntRegionIds.Surface, new SurfaceCell(18, 8)), out var snapshot),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(snapshot.Node.ProjectionState,
                Is.EqualTo(RuntimeOccupantProjectionState.DetailsUnavailable));
            Assert.That(snapshot.Node.InteractionState,
                Is.EqualTo(RuntimeNodeInteractionState.Unsupported));
        }

        private ColonyRuntimeCellRegionAdapter CreateColonyAdapter(
            out ChunkedWorldData world, out WorldMaterialDropRegistry drops)
        {
            BackgroundTileMaterialDefinition earth =
                CreateDefinition<BackgroundTileMaterialDefinition>(EarthId);
            SolidTileMaterialDefinition soil =
                CreateDefinition<SolidTileMaterialDefinition>(SoilId);
            world = new ChunkedWorldData(new WorldMaterialRegistry(
                new[] { earth }, new[] { soil }));
            Assert.That(world.CreateChunk(Vector2Int.zero, EarthId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            drops = new WorldMaterialDropRegistry();
            return new ColonyRuntimeCellRegionAdapter(world, drops);
        }

        private T CreateDefinition<T>(string stableId)
            where T : WorldTileMaterialDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = stableId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            definitions.Add(definition);
            return definition;
        }

        private sealed class MissingBackgroundRegion : IReadOnlyRuntimeCellRegion
        {
            private readonly RegionCellId onlyCell = RegionCellId.Surface(
                AntRegionIds.Surface, new SurfaceCell(0, 0));
            public RegionId RegionId => AntRegionIds.Surface;
            public RegionLocationKind CoordinateKind => RegionLocationKind.Surface;
            public long PublicationRevision => 0;
            public RuntimeCellChangeFeed Changes { get; } =
                new(AntRegionIds.Surface);
            public RuntimeCellReadResult TryGetCell(RegionCellId cellId,
                out RuntimeCellSnapshot snapshot)
            {
                snapshot = null;
                return RuntimeCellReadResult.MissingBackground;
            }
            public IEnumerable<RegionCellId> EnumerateResidentCells()
            {
                yield return onlyCell;
            }
        }
    }
}
