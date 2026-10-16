using System.Collections.Generic;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused authoritative-data checks for material handling.</summary>
    public sealed class InventoryMaterialHandlingTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private readonly List<Object> temporaryDefinitions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object definition in temporaryDefinitions)
                Object.DestroyImmediate(definition);
            temporaryDefinitions.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TestCase(0, 2, 2)]
        [TestCase(19, 2, 2)]
        [TestCase(20, 3, 2)]
        [TestCase(39, 3, 2)]
        [TestCase(40, 3, 3)]
        [TestCase(59, 3, 3)]
        [TestCase(60, 4, 4)]
        [TestCase(74, 4, 4)]
        [TestCase(75, 5, 5)]
        [TestCase(89, 5, 5)]
        [TestCase(90, 6, 6)]
        [TestCase(100, 6, 6)]
        public void HaulingLevelDeterminesApprovedCapacityBand(int level,
            int expectedSlots, int expectedUnitsPerSlot)
        {
            AntRecord ant = CreateRecord($"level-{level}");
            SetExactHaulingLevel(ant, level);

            Assert.That(ant.Inventory.NaturalSlotCapacity, Is.EqualTo(expectedSlots));
            Assert.That(ant.Inventory.BaseUnitsPerSlot,
                Is.EqualTo(expectedUnitsPerSlot));
            Assert.That(ant.Inventory.NormalTotalCapacity,
                Is.EqualTo(expectedSlots * expectedUnitsPerSlot));
        }

        [Test]
        public void NewQueenAndWorkerStartWithIndependentEmptyInventories()
        {
            AntRecord queen = CreateRecord("queen", caste: AntCaste.Queen);
            AntRecord worker = CreateRecord("worker");

            Assert.That(queen.Inventory, Is.Not.SameAs(worker.Inventory));
            Assert.That(queen.Inventory.TotalQuantity, Is.Zero);
            Assert.That(worker.Inventory.TotalQuantity, Is.Zero);
            Assert.That(queen.Inventory.NaturalSlotCapacity,
                Is.EqualTo(worker.Inventory.NaturalSlotCapacity));
        }

        [Test]
        public void InventoryMergesThenSplitsIntoBoundedStacks()
        {
            AntRecord ant = CreateRecord("stacking");
            SetExactHaulingLevel(ant, 90);

            Assert.That(ant.Inventory.TryAdd(MaterialResourceCatalog.GetStableId(
                ExcavatedMaterialType.Soil), 14, 10, false, out int added,
                out string error), Is.True, error);
            Assert.That(added, Is.EqualTo(14));
            Assert.That(ant.Inventory.OccupiedSlots.Select(slot => slot.Quantity),
                Is.EqualTo(new[] { 6, 6, 2 }));
            Assert.That(ant.Inventory.GetAvailableCapacity(
                MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil), 10),
                Is.EqualTo(22));
        }

        [Test]
        public void IncompatibleMaterialNeedsItsOwnSlotAndFullAddIsAtomic()
        {
            AntRecord ant = CreateRecord("incompatible");
            Assert.That(ant.Inventory.TryAdd("material.soil", 2, 10, false,
                out _, out _), Is.True);

            Assert.That(ant.Inventory.TryAdd("material.stone", 3, 10, false,
                out int added, out _), Is.False);
            Assert.That(added, Is.Zero);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(2));
        }

        [Test]
        public void PartialAddUsesOnlyAvailableCapacity()
        {
            AntRecord ant = CreateRecord("partial");
            Assert.That(ant.Inventory.TryAdd("material.soil", 3, 10, false,
                out _, out _), Is.True);

            Assert.That(ant.Inventory.TryAdd("material.soil", 5, 10, true,
                out int added, out string error), Is.True, error);
            Assert.That(added, Is.EqualTo(1));
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(4));
        }

        [Test]
        public void InvalidInventoryQuantitiesAreRejectedWithoutMutation()
        {
            AntRecord ant = CreateRecord("invalid-quantity");

            Assert.That(ant.Inventory.TryAdd("material.soil", 0, 10, true,
                out _, out _), Is.False);
            Assert.That(ant.Inventory.TryAdd(" material.soil", 1, 10, true,
                out _, out _), Is.False);
            Assert.That(ant.Inventory.TryRemove(0, -1,
                out _, out _, out _), Is.False);
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
        }

        [Test]
        public void CapacityDecayRetainsOverflowAndBlocksNewAdditions()
        {
            var settings = new AntSkillProgressionSettings(
                100d, 10d, inactivityGraceHours: 0d, decayLevelsPerHour: 10d);
            AntRecord ant = CreateRecord("decay", settings);
            SetExactHaulingLevel(ant, 60);
            Assert.That(ant.Inventory.TryAdd("material.soil", 16, 10, false,
                out _, out _), Is.True);

            Assert.That(ant.TryAdvanceSkillDecay(1d, out _), Is.True);
            Assert.That(ant.Inventory.NaturalSlotCapacity, Is.EqualTo(3));
            Assert.That(ant.Inventory.OccupiedSlotCount, Is.EqualTo(4));
            Assert.That(ant.Inventory.BaseUnitsPerSlot, Is.EqualTo(3));
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(16));
            Assert.That(ant.Inventory.IsOverCapacity, Is.True);
            Assert.That(ant.Inventory.TryAdd("material.soil", 1, 10, true,
                out _, out _), Is.False);
        }

        [Test]
        public void DecayFromNinetyToEightyNinePreservesCargoUntilSafeDelivery()
        {
            var settings = new AntSkillProgressionSettings(
                100d, 10d, inactivityGraceHours: 0d, decayLevelsPerHour: 1d);
            AntRecord ant = CreateRecord("decay-90", settings);
            SetExactHaulingLevel(ant, 90);
            Assert.That(ant.Inventory.TryAdd("material.soil", 36, 10, false,
                out _, out string error), Is.True, error);

            Assert.That(ant.TryAdvanceSkillDecay(1d, out error), Is.True, error);
            Assert.That(ant.Skills.Get(AntSkillId.Hauling).Level, Is.EqualTo(89));
            Assert.That(ant.Inventory.NaturalSlotCapacity, Is.EqualTo(5));
            Assert.That(ant.Inventory.BaseUnitsPerSlot, Is.EqualTo(5));
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(36));
            Assert.That(ant.Inventory.IsOverCapacity, Is.True);
            Assert.That(ant.Inventory.TryAdd("material.soil", 1, 10, true,
                out _, out _), Is.False);

            Assert.That(ant.Inventory.TryRemoveResource("material.soil", 11, 10,
                out error), Is.True, error);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(25));
            Assert.That(ant.Inventory.IsOverCapacity, Is.False);
        }

        [Test]
        public void TransferAllocationProtectsCargoUntilDeliveryOrRecovery()
        {
            AntRecord ant = CreateRecord("allocated");
            Assert.That(ant.Inventory.TryAddAllocated("transfer.1", "material.soil",
                3, 10, out string error), Is.True, error);
            Assert.That(ant.Inventory.AllocatedQuantity, Is.EqualTo(3));
            Assert.That(ant.Inventory.TryRemoveResource("material.soil", 1, 10,
                out _), Is.False);
            Assert.That(ant.Inventory.TryRemoveAllocated("transfer.1",
                "material.soil", 3, 10, out error), Is.True, error);
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
            Assert.That(ant.Inventory.AllocatedQuantity, Is.Zero);

            Assert.That(ant.Inventory.TryAddAllocated("transfer.2", "material.soil",
                2, 10, out error), Is.True, error);
            ant.Inventory.ReleaseAllocation("transfer.2");
            Assert.That(ant.Inventory.GetUnallocatedQuantity("material.soil", 10),
                Is.EqualTo(2));
        }

        [Test]
        public void ExcavationWithEmptyInventoryCreatesLooseDropOnly()
        {
            WorldMaterialDropRegistry drops = CreateExcavationWorld(1,
                out ChunkedWorldData world,
                out ExcavationWorldTransaction transaction);
            AntRecord ant = CreateRecord("excavator");

            Assert.That(transaction.TryCommit("excavate.full", Vector2Int.zero,
                "solid.test", out WorldMaterialDrop drop,
                out string error), Is.True, error);
            Assert.That(drop.WorldCell, Is.EqualTo(Vector2Int.zero));
            Assert.That(drop.Quantity, Is.EqualTo(1));
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Soil), Is.EqualTo(1));
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
            Assert.That(world.GetCell(Vector2Int.zero, out WorldTileCellData cell),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(cell.HasSolidTile, Is.False);
        }

        [Test]
        public void ExcavationWithPartialInventoryLeavesInventoryUnchanged()
        {
            WorldMaterialDropRegistry drops = CreateExcavationWorld(1,
                out _, out ExcavationWorldTransaction transaction);
            AntRecord ant = CreateRecord("partial");
            SetExactHaulingLevel(ant, 40);
            Assert.That(ant.Inventory.TryAdd("material.soil", 5, 10, false,
                out _, out _), Is.True);

            Assert.That(transaction.TryCommit("excavate.partial", Vector2Int.zero,
                "solid.test", out _, out string error), Is.True, error);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(5));
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Soil), Is.EqualTo(1));
        }

        [Test]
        public void ExcavationWithFullInventoryCreatesDropAndStableKeyIsIdempotent()
        {
            WorldMaterialDropRegistry drops = CreateExcavationWorld(1,
                out _, out ExcavationWorldTransaction transaction);
            AntRecord ant = CreateRecord("fallback");
            SetExactHaulingLevel(ant, 90);
            ant.Inventory.TryAdd("material.soil", 10, 10, false, out _, out _);

            Assert.That(transaction.TryCommit("excavate.fallback", Vector2Int.zero,
                "solid.test", out _, out string error), Is.True, error);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(10));
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Soil), Is.EqualTo(1));
            Assert.That(transaction.TryCommit("excavate.fallback", Vector2Int.zero,
                "solid.test", out _, out _), Is.False);
        }

        [Test]
        public void ExcavationMergesCompatibleDropIntoOneCanonicalStack()
        {
            WorldMaterialDropRegistry drops = CreateExcavationWorld(3,
                out _, out ExcavationWorldTransaction transaction);
            Assert.That(drops.TryAdd("existing", ExcavatedMaterialType.Soil, 7,
                Vector2Int.zero, out WorldMaterialDrop existing,
                out string setupError), Is.True, setupError);

            Assert.That(transaction.TryCommit("excavate.merge", Vector2Int.zero,
                "solid.test", out WorldMaterialDrop result,
                out string error), Is.True, error);
            Assert.That(drops.Count, Is.EqualTo(1));
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Soil), Is.EqualTo(10));
            Assert.That(result.Quantity, Is.EqualTo(10));
            Assert.That(result.StackId, Is.EqualTo(existing.StackId));
            Assert.That(result.WorldCell, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void UnrepresentableExcavationOutputLeavesTerrainAndInventoryIntact()
        {
            WorldMaterialDropRegistry drops = CreateExcavationWorld(11,
                out ChunkedWorldData world,
                out ExcavationWorldTransaction transaction);
            Assert.That(transaction.TryCommit("excavate.invalid", Vector2Int.zero,
                "solid.test", out _, out _), Is.False);
            world.GetCell(Vector2Int.zero, out WorldTileCellData cell);
            Assert.That(cell.HasSolidTile, Is.True);
            Assert.That(drops.Count, Is.Zero);
        }

        [Test]
        public void PickupReservationPreventsCompetitionAndCancellationPreservesDrop()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop drop), Is.True);
            AntRecord first = CreateRecord("first");
            AntRecord second = CreateRecord("second");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, drop.WorldCell,
                out Vector2Int approach, out _, out string error), Is.True, error);

            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, first, drop, approach,
                out MaterialPickupOrder order, out error), Is.True, error);
            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, second, drop, approach,
                out _, out _), Is.False);
            order.Cancel("test cancellation");
            Assert.That(host.MaterialDrops.GetReservedQuantity(drop.OwnershipKey),
                Is.Zero);
            Assert.That(host.MaterialDrops.TryGet(drop.OwnershipKey, out _), Is.True);
        }

        [Test]
        public void PickupRejectsNonAdjacentAnchorAndInactiveAnts()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.TryFindAtCell(AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop drop);
            AntRecord active = CreateRecord("active");
            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, active, drop, drop.WorldCell,
                out _, out _), Is.False);

            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, drop.WorldCell, out Vector2Int approach, out _, out _);
            AntRecord inactive = CreateRecord("inactive");
            inactive.MarkInactive();
            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, inactive, drop, approach,
                out _, out _), Is.False);
            AntRecord deceased = CreateRecord("deceased");
            deceased.MarkDeceased();
            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, deceased, drop, approach,
                out _, out _), Is.False);
        }

        [Test]
        public void FailedPickupRevalidationPreservesDropAndReleasesReservation()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.TryFindAtCell(AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop drop);
            AntRecord ant = CreateRecord("revalidation");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, drop.WorldCell, out Vector2Int approach, out _, out _);
            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, ant, drop, approach,
                out MaterialPickupOrder order, out string error), Is.True, error);
            ant.Inventory.TryAdd("material.stone", 4, 10, false, out _, out _);

            Assert.That(order.TryComplete(approach), Is.False);
            Assert.That(host.MaterialDrops.TryGet(drop.OwnershipKey,
                out WorldMaterialDrop retained), Is.True);
            Assert.That(retained.Quantity, Is.EqualTo(drop.Quantity));
            Assert.That(host.MaterialDrops.GetReservedQuantity(drop.OwnershipKey),
                Is.Zero);
        }

        [Test]
        public void PickupCommitsPartialQuantityAndLeavesRemainder()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.TryFindAtCell(AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop drop);
            AntRecord ant = CreateRecord("pickup");
            ant.Inventory.TryAdd("material.soil", 2, 10, false, out _, out _);
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, drop.WorldCell, out Vector2Int approach, out _, out _);
            Assert.That(MaterialPickupOrder.TryCreate(host.MaterialDrops,
                host.GridNavigationMap, ant, drop, approach,
                out MaterialPickupOrder order, out string error), Is.True, error);

            Assert.That(order.TryComplete(approach), Is.True);
            Assert.That(order.CollectedQuantity, Is.EqualTo(2));
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(4));
            Assert.That(host.MaterialDrops.TryGet(drop.OwnershipKey,
                out WorldMaterialDrop remainder), Is.True);
            Assert.That(remainder.Quantity, Is.EqualTo(drop.Quantity - 2));
        }

        [Test]
        public void DeveloperDropMergesAndConservesQuantity()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            var drops = new WorldMaterialDropRegistry();
            Vector2Int cell = new(-10, SampleWorldGenerator.MainPassageY);
            drops.TryAdd("existing", ExcavatedMaterialType.Soil, 5, cell,
                out _, out _);
            AntRecord ant = CreateRecord("dropper");
            SetExactHaulingLevel(ant, 90);
            ant.Inventory.TryAdd("material.soil", 4, 10, false, out _, out _);

            Assert.That(MaterialDropTransaction.TryDrop(ant.Inventory, 0, 4,
                cell, host.GridNavigationMap, drops, "manual.drop",
                out _, out string error), Is.True, error);
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Soil), Is.EqualTo(9));
            Assert.That(drops.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidDropPlacementLeavesInventoryUnchanged()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            var drops = new WorldMaterialDropRegistry();
            AntRecord ant = CreateRecord("blocked-drop");
            SetExactHaulingLevel(ant, 90);
            ant.Inventory.TryAdd("material.soil", 4, 10, false, out _, out _);

            Assert.That(MaterialDropTransaction.TryDrop(ant.Inventory, 0, 4,
                new Vector2Int(10000, 10000), host.GridNavigationMap, drops,
                "invalid.drop", out _, out _), Is.False);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(4));
            Assert.That(drops.Count, Is.Zero);
        }

        private WorldMaterialDropRegistry CreateExcavationWorld(int yield,
            out ChunkedWorldData world, out ExcavationWorldTransaction transaction)
        {
            BackgroundTileMaterialDefinition background =
                ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>();
            background.name = "Test Background";
            var serializedBackground = new SerializedObject(background);
            serializedBackground.FindProperty("stableMaterialId").stringValue =
                "background.test";
            serializedBackground.FindProperty("displayName").stringValue =
                "Test Background";
            serializedBackground.ApplyModifiedPropertiesWithoutUndo();
            temporaryDefinitions.Add(background);

            SolidTileMaterialDefinition solid =
                ScriptableObject.CreateInstance<SolidTileMaterialDefinition>();
            solid.name = "Test Soil";
            var serialized = new SerializedObject(solid);
            serialized.FindProperty("stableMaterialId").stringValue = "solid.test";
            serialized.FindProperty("displayName").stringValue = "Test Soil";
            serialized.FindProperty("isExcavatable").boolValue = true;
            serialized.FindProperty("excavatedMaterial").enumValueIndex =
                (int)ExcavatedMaterialType.Soil;
            serialized.FindProperty("excavationYield").intValue = yield;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            temporaryDefinitions.Add(solid);
            world = new ChunkedWorldData(new WorldMaterialRegistry(
                new[] { background }, new[] { solid }));
            Assert.That(world.CreateChunk(Vector2Int.zero, "background.test"),
                Is.EqualTo(WorldChunkCreationResult.Success));
            Assert.That(world.SetSolidMaterial(Vector2Int.zero, "solid.test"),
                Is.EqualTo(WorldCellMutationResult.Success));
            var drops = new WorldMaterialDropRegistry();
            transaction = new ExcavationWorldTransaction(world, drops);
            return drops;
        }

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    AntGameDevelopmentWorldHost>(true)).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }

        private static AntRecord CreateRecord(string id,
            AntSkillProgressionSettings settings = null,
            AntCaste caste = AntCaste.Worker) => new(
                new AntId(id), $"Test {id}", new ColonyId("test-colony"),
                caste, new AntAttributes(5, 5, 5),
                settings ?? AntSkillProgressionSettings.Provisional, 0d);

        private static void SetExactHaulingLevel(AntRecord ant, int level)
        {
            if (level == 0) return;
            Assert.That(ant.TryAwardSkillExperience(AntSkillId.Hauling,
                ant.Skills.Settings.TotalExperienceForLevel(level), 1d, 0d,
                out _, out string error), Is.True, error);
        }
    }
}
