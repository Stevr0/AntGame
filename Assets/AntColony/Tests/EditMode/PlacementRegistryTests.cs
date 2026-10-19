using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Commands;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    public sealed class PlacementRegistryTests
    {
        private const string EarthId = "background.placement-test-earth";
        private const string PreparedId = "background.placement-test-prepared";
        private const string SleepingId = "background.placement-test-sleeping";
        private const string StorageId = "background.placement-test-storage";
        private readonly List<ScriptableObject> assets = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject asset in assets)
                UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test]
        public void DefinitionValidatesDurabilityAndCopiesMutableInputs()
        {
            var backgrounds = new List<string> { EarthId };
            var definition = CreatePlacementDefinition("placement.test", 100,
                compatibleBackgrounds: backgrounds);
            backgrounds[0] = "changed-after-construction";

            Assert.That(definition.MaximumHitPoints, Is.EqualTo(100));
            Assert.That(definition.Destructible, Is.True);
            Assert.That(definition.Repairable, Is.True);
            Assert.That(definition.CompatibleBackgroundDefinitionIds,
                Is.EqualTo(new[] { EarthId }));
            Assert.That(typeof(PlacementDefinition).GetProperties()
                .All(property => !property.CanWrite), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreatePlacementDefinition("placement.invalid-zero", 0));
            Assert.Throws<ArgumentException>(() => new PlacementDefinition(
                "placement.invalid-unknown", "Unknown durability",
                PlacementCategory.Infrastructure,
                PlacementRegionSupport.Colony,
                PlacementFootprintDefinition.OneCell,
                Array.Empty<string>(), false, false, string.Empty,
                Array.Empty<PlacementConstructionRequirement>(), 0d, 0,
                true, false, PlacementInteractionRule.None, 0,
                Array.Empty<string>(), string.Empty, false, null,
                destructible: true, repairable: false));
        }

        [Test]
        public void DefinitionsRequireUniqueStableIds()
        {
            PlacementDefinition first = CreatePlacementDefinition(
                "placement.duplicate", 10);
            PlacementDefinition second = CreatePlacementDefinition(
                "placement.duplicate", 20);

            Assert.Throws<ArgumentException>(() => new PlacementRegistry(
                "scenario.test.v1", new[] { first, second }));
        }

        [Test]
        public void FootprintKeepsOneIdentityAndSupportsFutureRotation()
        {
            var footprint = new PlacementFootprintDefinition(new[]
            {
                Vector2Int.zero,
                Vector2Int.right
            });
            RegionCellId anchor = ColonyCell(3, -4);

            IReadOnlyList<RegionCellId> east = footprint.Resolve(anchor,
                PlacementOrientation.East);

            Assert.That(east.Count, Is.EqualTo(2));
            Assert.That(east[0], Is.EqualTo(anchor));
            Assert.That(east[1], Is.EqualTo(ColonyCell(3, -5)));
            Assert.Throws<ArgumentException>(() =>
                new PlacementFootprintDefinition(new[]
                {
                    Vector2Int.zero,
                    Vector2Int.zero
                }));
        }

        [Test]
        public void CompletedPlacementStartsAtFullHpAndSnapshotsAreImmutable()
        {
            PlacementDefinition definition = CreatePlacementDefinition(
                "placement.durable", 80);
            var registry = new PlacementRegistry("scenario.test.v1",
                new[] { definition });

            Assert.That(registry.TryRegisterCompleted(definition.StableId,
                ColonyCell(1, -1), PlacementOrientation.North,
                out PlacementSnapshot snapshot, out string error),
                Is.EqualTo(PlacementMutationResult.Success), error);

            Assert.That(snapshot.CurrentHitPoints, Is.EqualTo(80));
            Assert.That(snapshot.MaximumHitPoints, Is.EqualTo(80));
            Assert.That(snapshot.ConditionPercentage, Is.EqualTo(100f));
            Assert.That(snapshot.Revision, Is.EqualTo(1));
            Assert.That(typeof(PlacementSnapshot).GetProperties()
                .All(property => !property.CanWrite), Is.True);
            Assert.That(snapshot.Footprint.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<RegionCellId>)snapshot.Footprint).Add(ColonyCell(2, -1)));
        }

        [Test]
        public void HpMutationClampsAndAdvancesRevisionOnlyWhenCommitted()
        {
            var registry = CreateRegistryWithDurablePlacement(
                out PlacementSnapshot initial);

            Assert.That(registry.TryCommitHitPoints(initial.Handle, -20,
                initial.Revision, out PlacementSnapshot zero),
                Is.EqualTo(PlacementMutationResult.Success));
            Assert.That(zero.CurrentHitPoints, Is.Zero);
            Assert.That(zero.ConditionPercentage, Is.Zero);
            Assert.That(zero.Revision, Is.EqualTo(initial.Revision + 1));

            Assert.That(registry.TryCommitHitPoints(zero.Handle, -1,
                zero.Revision, out PlacementSnapshot unchanged),
                Is.EqualTo(PlacementMutationResult.NoChange));
            Assert.That(unchanged.Revision, Is.EqualTo(zero.Revision));

            Assert.That(registry.TryCommitHitPoints(unchanged.Handle, 999,
                unchanged.Revision, out PlacementSnapshot full),
                Is.EqualTo(PlacementMutationResult.Success));
            Assert.That(full.CurrentHitPoints, Is.EqualTo(full.MaximumHitPoints));
            Assert.That(full.Revision, Is.EqualTo(unchanged.Revision + 1));

            Assert.That(registry.TryCommitHitPoints(full.Handle, 50,
                expectedRevision: 1, out _),
                Is.EqualTo(PlacementMutationResult.RevisionMismatch));
            Assert.That(registry.TryGet(full.PlacementId, out PlacementSnapshot after),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(after.Revision, Is.EqualTo(full.Revision));
        }

        [Test]
        public void ScenarioContextRejectsStaleHpOperationsAndRetiresIdentity()
        {
            PlacementRegistry first = CreateRegistryWithDurablePlacement(
                out PlacementSnapshot original);
            first.InvalidateContext();
            var replacement = new PlacementRegistry(first.ScenarioStableId,
                new[] { CreatePlacementDefinition("placement.durable", 100) });

            Assert.That(first.TryGet(original.Handle, out _),
                Is.EqualTo(PlacementReadResult.ContextInvalidated));
            Assert.That(replacement.TryCommitHitPoints(original.Handle, 25,
                original.Revision, out _),
                Is.EqualTo(PlacementMutationResult.StaleScenario));
            Assert.That(first.Lifecycle,
                Is.EqualTo(PlacementRegistryLifecycle.ContextInvalidated));
        }

        [Test]
        public void RegistryRejectsDuplicateFootprintOccupancy()
        {
            PlacementDefinition definition = CreatePlacementDefinition(
                "placement.occupancy", 10);
            var registry = new PlacementRegistry("scenario.occupancy.v1",
                new[] { definition });
            RegionCellId cell = ColonyCell(0, 0);

            Assert.That(registry.TryRegisterCompleted(definition.StableId, cell,
                PlacementOrientation.North, out PlacementSnapshot first,
                out string error), Is.EqualTo(PlacementMutationResult.Success), error);
            Assert.That(registry.TryRegisterCompleted(definition.StableId, cell,
                PlacementOrientation.North, out _, out error),
                Is.EqualTo(PlacementMutationResult.Occupied));
            Assert.That(registry.TryGetByCell(cell, out PlacementSnapshot occupied),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(occupied.PlacementId, Is.EqualTo(first.PlacementId));
        }

        [Test]
        public void LegacyProjectionIsDeterministicReadOnlyAndReportsUnknownHp()
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(
                EarthId, BackgroundFunction.None);
            BackgroundTileMaterialDefinition prepared = CreateBackground(
                PreparedId, BackgroundFunction.None, speedMultiplier: 1.25f);
            ChunkedWorldData world = CreateWorld(earth, prepared);
            Assert.That(world.SetBackgroundMaterial(Vector2Int.zero, PreparedId),
                Is.EqualTo(WorldCellMutationResult.Success));
            PlacementDefinition definition = CreateLegacyDefinition(
                "conversion.prepared", PreparedId,
                PlacementCategory.MovementImprovement, capacity: 0);
            var mappings = new Dictionary<string, string>
            {
                [PreparedId] = definition.StableId
            };
            var first = new PlacementRegistry("fixture.v1.seed", new[] { definition },
                mappings);
            var second = new PlacementRegistry("fixture.v1.seed", new[] { definition },
                mappings);
            Assert.That(first.TryBuildLegacyProjections(world, out string error),
                Is.True, error);
            Assert.That(second.TryBuildLegacyProjections(world, out error),
                Is.True, error);

            Assert.That(first.TryGetByCell(ColonyCell(0, 0),
                out PlacementSnapshot projected), Is.EqualTo(PlacementReadResult.Success));
            Assert.That(second.TryGetByCell(ColonyCell(0, 0),
                out PlacementSnapshot repeated), Is.EqualTo(PlacementReadResult.Success));
            Assert.That(projected.PlacementId, Is.EqualTo(repeated.PlacementId));
            Assert.That(projected.CurrentHitPoints, Is.Null);
            Assert.That(projected.MaximumHitPoints, Is.Null);
            Assert.That(projected.ConditionPercentage, Is.Null);
            Assert.That(first.TryCommitHitPoints(projected.Handle, 0,
                projected.Revision, out _),
                Is.EqualTo(PlacementMutationResult.LegacyProjectionReadOnly));
            Assert.That(prepared.MovementSpeedMultiplier, Is.EqualTo(1.25f));
        }

        [Test]
        public void ProjectionDoesNotDuplicateStorageOrSleepingAuthority()
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(
                EarthId, BackgroundFunction.None);
            BackgroundTileMaterialDefinition storageBackground = CreateBackground(
                StorageId, BackgroundFunction.MaterialStorage);
            BackgroundTileMaterialDefinition sleepingBackground = CreateBackground(
                SleepingId, BackgroundFunction.Sleeping);
            ChunkedWorldData world = CreateWorld(earth, storageBackground,
                sleepingBackground);
            var storageCell = new Vector2Int(0, 0);
            var sleepingCell = new Vector2Int(1, 0);
            world.SetBackgroundMaterial(storageCell, StorageId);
            world.SetBackgroundMaterial(sleepingCell, SleepingId);
            var storage = new MaterialStorageWorld(world);
            Assert.That(storage.TryAdd(storageCell,
                MaterialStorageFilter.AllBuildingMaterials, "material.soil", 4,
                10, out string error), Is.True, error);
            var survival = new SurvivalInteractionWorld(world);
            Assert.That(survival.TryAddSleepingPosition(sleepingCell, out error),
                Is.True, error);
            Assert.That(survival.TryReserveSleeping(sleepingCell, "ant.test",
                out error), Is.True, error);

            PlacementDefinition storageDefinition = CreateLegacyDefinition(
                "conversion.material-storage", StorageId,
                PlacementCategory.Storage, capacity: 1);
            PlacementDefinition sleepingDefinition = CreateLegacyDefinition(
                "conversion.sleeping", SleepingId,
                PlacementCategory.Rest, capacity: 1);
            var registry = new PlacementRegistry("fixture.authority.v1",
                new[] { storageDefinition, sleepingDefinition },
                new Dictionary<string, string>
                {
                    [StorageId] = storageDefinition.StableId,
                    [SleepingId] = sleepingDefinition.StableId
                });
            Assert.That(registry.TryBuildLegacyProjections(world, out error),
                Is.True, error);
            var adapter = new ColonyRuntimeCellRegionAdapter(world,
                new WorldMaterialDropRegistry(world), placements: registry);
            Assert.That(adapter.TryGetCell(ColonyCell(0, 0),
                out RuntimeCellSnapshot cell), Is.EqualTo(RuntimeCellReadResult.Success));

            Assert.That(cell.Placement.ContentsArePlacementOwned, Is.False);
            Assert.That(cell.Placement.FunctionalCapacity, Is.Null);
            Assert.That(cell.Placement.CurrentHitPoints, Is.Null);
            Assert.That(storage.TryGet(storageCell, out MaterialStorageSlot slot),
                Is.True);
            Assert.That(slot.Quantity, Is.EqualTo(4));
            Assert.That(survival.GetSleepingReservation(sleepingCell),
                Is.EqualTo("ant.test"));
            adapter.Dispose();
        }

        [Test]
        public void CompatibilityRejectsNodeStackPlacementAndSiteAndKeepsUnknownExplicit()
        {
            PlacementDefinition definition = CreatePlacementDefinition(
                "placement.compatibility", 10,
                compatibleBackgrounds: new[] { EarthId });
            RegionCellId id = ColonyCell(0, 0);
            var clear = new RuntimeCellSnapshot(id, EarthId, 1, 1);
            var node = new RuntimeCellSnapshot(id, EarthId, 1, 1,
                node: new RuntimeNodeSnapshot(
                    RuntimeOccupantProjectionState.Authoritative,
                    "node", "node.soil", true, 2, 2,
                    RuntimeNodeInteractionState.Interactive));
            var stack = new RuntimeCellSnapshot(id, EarthId, 1, 1,
                looseStack: new RuntimeLooseStackSnapshot(
                    RuntimeOccupantProjectionState.Authoritative,
                    "stack", "material.soil", 1, 10, false));
            var unknown = new RuntimeCellSnapshot(id, SleepingId, 1, 1,
                naturalBackgroundDefinitionId: string.Empty,
                naturalBackgroundProjectionState:
                    RuntimeOccupantProjectionState.DetailsUnavailable,
                legacyFunctionalBackgroundDefinitionId: SleepingId);

            Assert.That(PlacementCompatibilityValidator.Validate(definition,
                clear, false).Result, Is.EqualTo(RuntimeOccupancyCompatibility.Valid));
            Assert.That(PlacementCompatibilityValidator.Validate(definition,
                node, false).Result, Is.EqualTo(RuntimeOccupancyCompatibility.Invalid));
            Assert.That(PlacementCompatibilityValidator.Validate(definition,
                stack, false).Result, Is.EqualTo(RuntimeOccupancyCompatibility.Invalid));
            Assert.That(PlacementCompatibilityValidator.Validate(definition,
                clear, true).Result, Is.EqualTo(RuntimeOccupancyCompatibility.Invalid));
            Assert.That(PlacementCompatibilityValidator.Validate(definition,
                unknown, false).Result, Is.EqualTo(RuntimeOccupancyCompatibility.Unknown));
        }

        [Test]
        public void RefreshChangesOnlyAffectedProjectionAndReadDoesNotAdvanceRevision()
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(
                EarthId, BackgroundFunction.None);
            BackgroundTileMaterialDefinition sleeping = CreateBackground(
                SleepingId, BackgroundFunction.Sleeping);
            ChunkedWorldData world = CreateWorld(earth, sleeping);
            PlacementDefinition definition = CreateLegacyDefinition(
                "conversion.sleeping", SleepingId, PlacementCategory.Rest, 1);
            var registry = new PlacementRegistry("fixture.revision.v1",
                new[] { definition }, new Dictionary<string, string>
                {
                    [SleepingId] = definition.StableId
                });
            registry.TryBuildLegacyProjections(world, out _);
            var adapter = new ColonyRuntimeCellRegionAdapter(world,
                new WorldMaterialDropRegistry(world), placements: registry);
            RegionCellId affected = ColonyCell(0, 0);
            RegionCellId unaffected = ColonyCell(1, 0);
            adapter.TryGetCell(unaffected, out RuntimeCellSnapshot beforeUnaffected);
            long registryBefore = registry.Revision;
            long feedBefore = adapter.Changes.CurrentSequence;
            Assert.That(adapter.TryGetCell(unaffected, out _),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(registry.Revision, Is.EqualTo(registryBefore));
            Assert.That(adapter.Changes.CurrentSequence, Is.EqualTo(feedBefore));

            Assert.That(world.SetBackgroundMaterial(Vector2Int.zero, SleepingId),
                Is.EqualTo(WorldCellMutationResult.Success));
            Assert.That(registry.TryGetByCell(affected, out PlacementSnapshot projection),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(projection.DefinitionId, Is.EqualTo(definition.StableId));
            adapter.TryGetCell(unaffected, out RuntimeCellSnapshot afterUnaffected);
            Assert.That(afterUnaffected.CellRevision,
                Is.EqualTo(beforeUnaffected.CellRevision));
            Assert.That(adapter.Changes.CurrentSequence, Is.EqualTo(feedBefore + 1));
            Assert.That(adapter.Changes.ReadAfter(feedBefore, out var changes),
                Is.EqualTo(RuntimeCellChangeReadResult.Success));
            Assert.That(changes[0].Changes[0].Category,
                Is.EqualTo(RuntimeCellChangeCategory.Background |
                    RuntimeCellChangeCategory.Placement));

            PlacementId firstId = projection.PlacementId;
            world.SetBackgroundMaterial(Vector2Int.zero, EarthId);
            world.SetBackgroundMaterial(Vector2Int.zero, SleepingId);
            Assert.That(registry.TryGetByCell(affected,
                out PlacementSnapshot replacement),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(replacement.PlacementId, Is.Not.EqualTo(firstId),
                "A retired Placement identity must not be reused in one scenario.");
            adapter.Dispose();
        }

        [Test]
        public void MaterialStorageCutoverPromotesIdentityWithoutCopyingMutableState()
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(
                EarthId, BackgroundFunction.None);
            BackgroundTileMaterialDefinition storageBackground = CreateBackground(
                StorageId, BackgroundFunction.MaterialStorage);
            ChunkedWorldData world = CreateWorld(earth, storageBackground);
            var cell = new Vector2Int(0, 0);
            world.SetBackgroundMaterial(cell, StorageId);
            var storage = new MaterialStorageWorld(world);
            Assert.That(storage.TryAdd(cell,
                MaterialStorageFilter.AllBuildingMaterials, "material.soil", 4,
                10, out string error), Is.True, error);
            Assert.That(storage.TryReserveIncoming(cell, "material.soil", 10,
                "incoming", 2, out _, out error), Is.True, error);
            Assert.That(storage.TryReserveWithdrawal(cell, "material.soil",
                "outgoing", 1, out _, out error), Is.True, error);
            PlacementDefinition definition = CreateLegacyDefinition(
                "conversion.material-storage", StorageId,
                PlacementCategory.Storage, 1);
            var registry = new PlacementRegistry("fixture.storage-cutover.v1",
                new[] { definition }, new Dictionary<string, string>
                {
                    [StorageId] = definition.StableId
                });
            Assert.That(registry.TryBuildLegacyProjections(world, out error),
                Is.True, error);
            RegionCellId cellId = ColonyCell(0, 0);
            registry.TryGetByCell(cellId, out PlacementSnapshot before);

            Assert.That(storage.TryMigrateExistingSlotsToPlacements(registry,
                definition.StableId, out error), Is.True, error);
            Assert.That(registry.TryGetByCell(cellId, out PlacementSnapshot after),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(after.PlacementId, Is.EqualTo(before.PlacementId));
            Assert.That(after.AuthorityState,
                Is.EqualTo(PlacementAuthorityState.AuthoritativePlacement));
            Assert.That(after.CurrentHitPoints, Is.Null,
                "Migration must not invent unapproved structural HP.");
            Assert.That(storage.GetFunctionAuthority(cell),
                Is.EqualTo(MaterialStorageFunctionAuthority.AuthoritativePlacement));
            Assert.That(storage.TryGet(cell, out MaterialStorageSlot slot), Is.True);
            Assert.That(slot.Quantity, Is.EqualTo(4));
            Assert.That(slot.Filter,
                Is.EqualTo(MaterialStorageFilter.AllBuildingMaterials));
            Assert.That(slot.MaximumStack, Is.EqualTo(10));
            Assert.That(slot.ReservationOwnerId, Is.EqualTo("incoming"));
            Assert.That(slot.ReservedIncomingQuantity, Is.EqualTo(2));
            Assert.That(slot.WithdrawalOwnerId, Is.EqualTo("outgoing"));
            Assert.That(slot.ReservedWithdrawalQuantity, Is.EqualTo(1));
            Assert.That(storage.AuthoritativePlacementCount, Is.EqualTo(1));
            Assert.That(storage.TryMigrateExistingSlotsToPlacements(registry,
                definition.StableId, out _), Is.False,
                "A second authority cutover must not duplicate the function.");
        }

        [Test]
        public void MaterialStorageCutoverFailureLeavesLegacySlotUntouched()
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(
                EarthId, BackgroundFunction.None);
            BackgroundTileMaterialDefinition storageBackground = CreateBackground(
                StorageId, BackgroundFunction.MaterialStorage);
            ChunkedWorldData world = CreateWorld(earth, storageBackground);
            var cell = new Vector2Int(0, 0);
            world.SetBackgroundMaterial(cell, StorageId);
            var storage = new MaterialStorageWorld(world);
            Assert.That(storage.TryAdd(cell, MaterialStorageFilter.Soil,
                "material.soil", 3, 10, out string error), Is.True, error);
            PlacementDefinition definition = CreateLegacyDefinition(
                "conversion.material-storage", StorageId,
                PlacementCategory.Storage, 1);
            var registry = new PlacementRegistry("fixture.failed-cutover.v1",
                new[] { definition });

            Assert.That(storage.TryMigrateExistingSlotsToPlacements(registry,
                definition.StableId, out error), Is.False);
            Assert.That(error, Does.Contain("no matching legacy Placement"));
            Assert.That(storage.GetFunctionAuthority(cell),
                Is.EqualTo(MaterialStorageFunctionAuthority.LegacyBackground));
            Assert.That(storage.TryGet(cell, out MaterialStorageSlot slot), Is.True);
            Assert.That(slot.Quantity, Is.EqualTo(3));
            Assert.That(storage.AuthoritativePlacementCount, Is.Zero);
        }

        [Test]
        public void RuntimeSnapshotReadsDelegatedStorageAndPublishesOneCommitChange()
        {
            BackgroundTileMaterialDefinition earth = CreateBackground(
                EarthId, BackgroundFunction.None);
            BackgroundTileMaterialDefinition storageBackground = CreateBackground(
                StorageId, BackgroundFunction.MaterialStorage);
            ChunkedWorldData world = CreateWorld(earth, storageBackground);
            var cell = new Vector2Int(0, 0);
            world.SetBackgroundMaterial(cell, StorageId);
            var storage = new MaterialStorageWorld(world);
            storage.TryAdd(cell, MaterialStorageFilter.Soil, "material.soil", 3,
                10, out _);
            PlacementDefinition definition = CreateLegacyDefinition(
                "conversion.material-storage", StorageId,
                PlacementCategory.Storage, 1);
            var registry = new PlacementRegistry("fixture.runtime-storage.v1",
                new[] { definition }, new Dictionary<string, string>
                {
                    [StorageId] = definition.StableId
                });
            registry.TryBuildLegacyProjections(world, out _);
            Assert.That(storage.TryMigrateExistingSlotsToPlacements(registry,
                definition.StableId, out string error), Is.True, error);
            var adapter = new ColonyRuntimeCellRegionAdapter(world,
                new WorldMaterialDropRegistry(world), placements: registry,
                materialStorage: storage);
            RegionCellId cellId = ColonyCell(0, 0);
            adapter.TryGetCell(cellId, out RuntimeCellSnapshot before);
            Assert.That(before.Placement.FunctionAuthority,
                Is.EqualTo(RuntimePlacementFunctionAuthority.DelegatedMaterialStorage));
            Assert.That(before.Placement.StoredQuantity, Is.EqualTo(3));
            Assert.That(before.Placement.ContentsArePlacementOwned, Is.False);
            Assert.That(before.NaturalBackgroundProjectionState,
                Is.EqualTo(RuntimeOccupantProjectionState.DetailsUnavailable));
            PlacementId identity = before.Placement.StableId.Length > 0
                ? new PlacementId(before.Placement.StableId) : default;

            Assert.That(storage.TryReserveIncoming(cell, "material.soil", 10,
                "transfer", 2, out _, out error), Is.True, error);
            long beforeCommitFeed = adapter.Changes.CurrentSequence;
            Assert.That(storage.TryCommitReservedDeposit(cell, "material.soil", 10,
                "transfer", 2, () => true, out error), Is.True, error);
            Assert.That(adapter.Changes.CurrentSequence,
                Is.EqualTo(beforeCommitFeed + 1));
            adapter.TryGetCell(cellId, out RuntimeCellSnapshot after);
            Assert.That(after.Placement.StableId, Is.EqualTo(identity.Value));
            Assert.That(after.Placement.StoredQuantity, Is.EqualTo(5));
            Assert.That(after.Placement.FunctionRevision,
                Is.GreaterThan(before.Placement.FunctionRevision));
            adapter.Dispose();
        }

        [Test]
        public void VisualStateResolverKeepsUnknownHpNormalAndDoesNotMutatePlacement()
        {
            PlacementRegistry registry = CreateRegistryWithDurablePlacement(
                out PlacementSnapshot placement);
            long registryRevision = registry.Revision;
            Assert.That(PlacementVisualStateResolver.Resolve(false,
                PlacementLifecycle.Completed, null, null, 0.5f),
                Is.EqualTo(PlacementVisualState.Completed));
            Assert.That(PlacementVisualStateResolver.Resolve(true,
                PlacementLifecycle.Completed, null, null, 0.5f),
                Is.EqualTo(PlacementVisualState.Construction));
            Assert.That(PlacementVisualStateResolver.Resolve(false,
                PlacementLifecycle.Completed, 40, 100, 0.5f),
                Is.EqualTo(PlacementVisualState.Damaged));
            Assert.That(PlacementVisualStateResolver.Resolve(false,
                PlacementLifecycle.Completed, 0, 100, 0.5f),
                Is.EqualTo(PlacementVisualState.Destroyed));
            Assert.That(registry.Revision, Is.EqualTo(registryRevision));
            Assert.That(registry.TryGet(placement.Handle, out PlacementSnapshot after),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(after.Revision, Is.EqualTo(placement.Revision));
        }

        private PlacementRegistry CreateRegistryWithDurablePlacement(
            out PlacementSnapshot snapshot)
        {
            PlacementDefinition definition = CreatePlacementDefinition(
                "placement.durable", 100);
            var registry = new PlacementRegistry("scenario.hp.v1",
                new[] { definition });
            Assert.That(registry.TryRegisterCompleted(definition.StableId,
                ColonyCell(0, 0), PlacementOrientation.North, out snapshot,
                out string error), Is.EqualTo(PlacementMutationResult.Success), error);
            return registry;
        }

        private static PlacementDefinition CreatePlacementDefinition(
            string id, int? maximumHitPoints,
            IEnumerable<string> compatibleBackgrounds = null)
        {
            return new PlacementDefinition(id, id,
                PlacementCategory.Infrastructure,
                PlacementRegionSupport.Colony,
                PlacementFootprintDefinition.OneCell,
                compatibleBackgrounds ?? Array.Empty<string>(),
                false, false, "recipe.test",
                new[]
                {
                    new PlacementConstructionRequirement("material.soil", 1)
                },
                4d, 0, true, false, PlacementInteractionRule.AdjacentCardinal,
                0, Array.Empty<string>(), "presentation.test", false,
                maximumHitPoints,
                destructible: maximumHitPoints.HasValue,
                repairable: maximumHitPoints.HasValue);
        }

        private static PlacementDefinition CreateLegacyDefinition(string id,
            string presentationId, PlacementCategory category, int capacity)
        {
            return new PlacementDefinition(id, id, category,
                PlacementRegionSupport.Colony,
                PlacementFootprintDefinition.OneCell,
                Array.Empty<string>(), false, false, id,
                new[]
                {
                    new PlacementConstructionRequirement("material.soil", 1)
                },
                4d, 0, true, false,
                category == PlacementCategory.MovementImprovement
                    ? PlacementInteractionRule.None
                    : PlacementInteractionRule.AdjacentCardinal,
                capacity, Array.Empty<string>(), presentationId, false,
                maximumHitPoints: null, destructible: false, repairable: false);
        }

        private ChunkedWorldData CreateWorld(
            params BackgroundTileMaterialDefinition[] backgrounds)
        {
            var world = new ChunkedWorldData(new WorldMaterialRegistry(
                backgrounds, Array.Empty<SolidTileMaterialDefinition>()));
            Assert.That(world.CreateChunk(Vector2Int.zero, EarthId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            return world;
        }

        private BackgroundTileMaterialDefinition CreateBackground(string id,
            BackgroundFunction function, float speedMultiplier = 1f)
        {
            var definition = ScriptableObject.CreateInstance<
                BackgroundTileMaterialDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = id;
            serialized.FindProperty("function").enumValueIndex = (int)function;
            serialized.FindProperty("movementSpeedMultiplier").floatValue =
                speedMultiplier;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            assets.Add(definition);
            return definition;
        }

        private static RegionCellId ColonyCell(int x, int y) =>
            RegionCellId.Colony(AntRegionIds.Colony, new ColonyCell(x, y));
    }
}
