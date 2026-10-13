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
    /// <summary>Focused deterministic coverage for Milestone 3.2B.</summary>
    public sealed class SurvivalInteractionTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TestCase("Background_FoodStorage", BackgroundFunction.FoodStorage)]
        [TestCase("Background_WaterStorage", BackgroundFunction.WaterStorage)]
        [TestCase("Background_Sleeping", BackgroundFunction.Sleeping)]
        public void FunctionalBackgrounds_HaveExactlyOneCapacity(
            string assetName, BackgroundFunction expected)
        {
            BackgroundTileMaterialDefinition definition = LoadBackground(assetName);
            Assert.That(definition.Function, Is.EqualTo(expected));
            Assert.That(definition.FunctionalCapacity, Is.EqualTo(1));
        }

        [Test]
        public void FunctionalBackgrounds_AreGrippableTraversableAndNormalSpeed()
        {
            foreach (string name in new[]
                { "Background_FoodStorage", "Background_WaterStorage", "Background_Sleeping" })
            {
                BackgroundTileMaterialDefinition definition = LoadBackground(name);
                Assert.That(definition.ProvidesClimbingGrip, Is.True);
                Assert.That(definition.AllowsHorizontalMovement, Is.True);
                Assert.That(definition.AllowsUpwardMovement, Is.True);
                Assert.That(definition.AllowsDownwardMovement, Is.True);
                Assert.That(definition.MovementSpeedMultiplier, Is.EqualTo(1f));
            }
        }

        [Test]
        public void Fixtures_ContainTwoFoodTwoWaterThreeSleepingAndCoveredFailure()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SurvivalFixtureCells, Has.Count.EqualTo(8));
            Assert.That(host.SurvivalInteractionWorld.StorageSlots.Count(
                slot => slot.Kind == SurvivalResourceKind.Food), Is.EqualTo(3));
            Assert.That(host.SurvivalInteractionWorld.StorageSlots.Count(
                slot => slot.Kind == SurvivalResourceKind.Water), Is.EqualTo(2));
            Assert.That(host.TryGetCell(host.SurvivalFixtureCells[7], out WorldTileCellData covered), Is.True);
            Assert.That(covered.HasSolidTile, Is.False);
        }

        [Test]
        public void FoodStorage_RejectsIncompatibleWaterStack()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SurvivalInteractionWorld.TryAddStorage(
                host.SurvivalFixtureCells[4], SurvivalResourceKind.Water,
                "resource.water", 1, out string error), Is.False);
            Assert.That(error, Does.Contain("WaterStorage"));
        }

        [Test]
        public void EmptyStorage_RemainsValidButCannotReserve()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = host.SurvivalFixtureCells[4];
            Assert.That(host.WorldData.SetBackgroundMaterial(
                cell, LoadBackground("Background_FoodStorage").StableMaterialId),
                Is.EqualTo(WorldCellMutationResult.Success));
            var world = new SurvivalInteractionWorld(host.WorldData);
            Assert.That(world.TryAddStorage(cell, SurvivalResourceKind.Food,
                "resource.food.empty", 0, out _), Is.True);
            Assert.That(world.TryReserveResource(cell, SurvivalResourceKind.Food,
                "ant-a", 1, out _), Is.False);
        }

        [Test]
        public void ResourceReservation_PreventsDoubleConsumption()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = host.SurvivalFixtureCells[0];
            SurvivalInteractionWorld world = host.SurvivalInteractionWorld;
            Assert.That(world.TryReserveResource(cell, SurvivalResourceKind.Food,
                "ant-a", 6, out _), Is.True);
            Assert.That(world.TryReserveResource(cell, SurvivalResourceKind.Food,
                "ant-b", 1, out _), Is.False);
            Assert.That(world.TryConsumeReserved(cell, "ant-a", 6, out _), Is.True);
            Assert.That(world.TryConsumeReserved(cell, "ant-a", 1, out _), Is.False);
            Assert.That(world.TryGetStorage(cell, out SurvivalStorageSlot slot), Is.True);
            Assert.That(slot.Quantity, Is.Zero);
        }

        [Test]
        public void CancelledReservation_IsReleased()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            order.Cancel("test");
            host.SurvivalInteractionWorld.TryGetStorage(
                host.SurvivalFixtureCells[0], out SurvivalStorageSlot slot);
            Assert.That(slot.ReservedQuantity, Is.Zero);
        }

        [Test]
        public void Eat_ConsumesOnePhysicalUnitAndRestoresHunger()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            ant.TrySetNeedsForDevelopment(100, 20, 100, 20, out _);
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            Assert.That(order.TryBeginInteraction(order.ApproachCell), Is.True);
            order.Advance(SurvivalInteractionOrder.ConsumptionDurationHours);
            host.SurvivalInteractionWorld.TryGetStorage(
                host.SurvivalFixtureCells[0], out SurvivalStorageSlot slot);
            Assert.That(slot.Quantity, Is.EqualTo(5));
            Assert.That(ant.Needs.Hunger, Is.EqualTo(60));
            Assert.That(order.Phase, Is.EqualTo(SurvivalInteractionPhase.Completed));
        }

        [Test]
        public void Drink_ConsumesOnePhysicalUnitRestoresThirstAndXpEligibility()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            ant.TrySetNeedsForDevelopment(100, 100, 0, 20, out _);
            SurvivalInteractionOrder order = CreateOrder(host, 2,
                SurvivalInteractionKind.Drink, out _);
            order.TryBeginInteraction(order.ApproachCell);
            order.Advance(SurvivalInteractionOrder.ConsumptionDurationHours);
            host.SurvivalInteractionWorld.TryGetStorage(
                host.SurvivalFixtureCells[2], out SurvivalStorageSlot slot);
            Assert.That(slot.Quantity, Is.EqualTo(7));
            Assert.That(ant.Needs.Thirst, Is.EqualTo(50));
            Assert.That(ant.Needs.IsDehydrated, Is.False);
        }

        [Test]
        public void IncompleteConsumption_ConsumesNothing()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            order.TryBeginInteraction(order.ApproachCell);
            order.Advance(0.5d);
            host.SurvivalInteractionWorld.TryGetStorage(
                host.SurvivalFixtureCells[0], out SurvivalStorageSlot slot);
            Assert.That(slot.Quantity, Is.EqualTo(6));
        }

        [Test]
        public void Eating_NeverRaisesHungerAboveMaximum()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            ant.TrySetNeedsForDevelopment(100, 90, 100, 20, out _);
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            order.TryBeginInteraction(order.ApproachCell);
            order.Advance(1d);
            Assert.That(ant.Needs.Hunger, Is.EqualTo(100));
        }

        [Test]
        public void Drinking_NeverRaisesThirstAboveMaximum()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            ant.TrySetNeedsForDevelopment(100, 100, 90, 20, out _);
            SurvivalInteractionOrder order = CreateOrder(host, 2,
                SurvivalInteractionKind.Drink, out _);
            order.TryBeginInteraction(order.ApproachCell);
            order.Advance(1d);
            Assert.That(ant.Needs.Thirst, Is.EqualTo(100));
        }

        [Test]
        public void SleepingPosition_IsExclusive()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = host.SurvivalFixtureCells[4];
            Assert.That(host.SurvivalInteractionWorld.TryReserveSleeping(
                cell, "ant-a", out _), Is.True);
            Assert.That(host.SurvivalInteractionWorld.TryReserveSleeping(
                cell, "ant-b", out _), Is.False);
        }

        [Test]
        public void Rest_ReducesFatigueAndCompletesAtAcceptedQualification()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            ant.TrySetNeedsForDevelopment(100, 100, 100, 44, out _);
            SurvivalInteractionOrder order = CreateOrder(host, 4,
                SurvivalInteractionKind.Rest, out _);
            order.TryBeginInteraction(order.ApproachCell);
            order.Advance(2d);
            Assert.That(ant.Needs.Fatigue, Is.EqualTo(20));
            Assert.That(ant.Needs.HasCompletedRest, Is.True);
            Assert.That(ant.Needs.WorkSpeedMultiplier, Is.GreaterThan(1d));
            Assert.That(order.Phase, Is.EqualTo(SurvivalInteractionPhase.Completed));
        }

        [Test]
        public void InterruptedRest_RetainsRecoveryAndReleasesPosition()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            ant.TrySetNeedsForDevelopment(100, 100, 100, 80, out _);
            SurvivalInteractionOrder order = CreateOrder(host, 4,
                SurvivalInteractionKind.Rest, out _);
            order.TryBeginInteraction(order.ApproachCell);
            order.Advance(1d);
            order.Cancel("interrupted");
            Assert.That(ant.Needs.Fatigue, Is.EqualTo(68));
            Assert.That(host.SurvivalInteractionWorld.GetSleepingReservation(
                host.SurvivalFixtureCells[4]), Is.Empty);
        }

        [TestCase(AntLifecycleStatus.Inactive)]
        [TestCase(AntLifecycleStatus.Deceased)]
        public void InactiveOrDeceasedAnt_ReleasesReservation(AntLifecycleStatus status)
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = FirstAnt(host);
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            order.TryBeginInteraction(order.ApproachCell);
            if (status == AntLifecycleStatus.Inactive) ant.MarkInactive();
            else ant.MarkDeceased();
            order.Advance(0.1d);
            host.SurvivalInteractionWorld.TryGetStorage(
                host.SurvivalFixtureCells[0], out SurvivalStorageSlot slot);
            Assert.That(slot.ReservedQuantity, Is.Zero);
        }

        [Test]
        public void Interaction_RejectsNonAdjacentExecution()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            Assert.That(order.TryBeginInteraction(new Vector2Int(60, 60)), Is.False);
            Assert.That(order.FailureReason, Does.Contain("adjacent"));
        }

        [Test]
        public void SurroundedFunctionalTarget_IsRejectedMeaningfully()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(TryCreate(host, 7, SurvivalInteractionKind.Eat,
                out _, out string error), Is.False);
            Assert.That(error, Does.Contain("No reachable adjacent"));
        }

        [Test]
        public void RemovedFunction_InvalidatesBeforeCommitAndConsumesNothing()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = host.SurvivalFixtureCells[0];
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            order.TryBeginInteraction(order.ApproachCell);
            host.WorldData.GetCell(order.ApproachCell, out WorldTileCellData ordinary);
            host.WorldData.SetBackgroundMaterial(cell, ordinary.BackgroundMaterialId);
            order.Advance(1d);
            host.SurvivalInteractionWorld.TryGetStorage(cell, out SurvivalStorageSlot slot);
            Assert.That(slot.Quantity, Is.EqualTo(6));
            Assert.That(order.Phase, Is.EqualTo(SurvivalInteractionPhase.Failed));
        }

        [Test]
        public void DifferentAnts_CanReserveIndependentStorage()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SurvivalInteractionWorld.TryReserveResource(
                host.SurvivalFixtureCells[0], SurvivalResourceKind.Food,
                "ant-a", 1, out _), Is.True);
            Assert.That(host.SurvivalInteractionWorld.TryReserveResource(
                host.SurvivalFixtureCells[1], SurvivalResourceKind.Food,
                "ant-b", 1, out _), Is.True);
        }

        [Test]
        public void WorldReset_ReplacesAllResourceAndReservationState()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SurvivalInteractionWorld before = host.SurvivalInteractionWorld;
            before.TryReserveResource(host.SurvivalFixtureCells[0],
                SurvivalResourceKind.Food, "ant-a", 1, out _);
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SurvivalInteractionWorld, Is.Not.SameAs(before));
            Assert.That(host.SurvivalInteractionWorld.StorageSlots.All(
                slot => slot.ReservedQuantity == 0), Is.True);
        }

        [Test]
        public void ActualPathCost_SelectsAReachableAdjacentAnchor()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            SurvivalInteractionOrder order = CreateOrder(host, 0,
                SurvivalInteractionKind.Eat, out _);
            Assert.That(host.GridNavigationMap.TryGetAnchor(order.ApproachCell, out _), Is.True);
            Assert.That(new GridPathfinder(host.GridNavigationMap).FindPath(
                WorldTileCoordinates.WorldPositionToCell(host.SelectableGridAnts[0].Position),
                order.ApproachCell).Succeeded, Is.True);
        }

        private static SurvivalInteractionOrder CreateOrder(
            AntGameDevelopmentWorldHost host,
            int fixtureIndex,
            SurvivalInteractionKind kind,
            out string error)
        {
            Assert.That(TryCreate(host, fixtureIndex, kind, out SurvivalInteractionOrder order,
                out error), Is.True, error);
            return order;
        }

        private static bool TryCreate(
            AntGameDevelopmentWorldHost host,
            int fixtureIndex,
            SurvivalInteractionKind kind,
            out SurvivalInteractionOrder order,
            out string error)
        {
            return SurvivalInteractionOrder.TryCreate(
                host.SurvivalInteractionWorld,
                host.GridNavigationMap,
                FirstAnt(host),
                kind,
                WorldTileCoordinates.WorldPositionToCell(host.SelectableGridAnts[0].Position),
                host.SurvivalFixtureCells[fixtureIndex],
                out order,
                out error);
        }

        private static AntRecord FirstAnt(AntGameDevelopmentWorldHost host) =>
            host.SelectionController.SelectableAnts[0].Record;

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AntGameDevelopmentWorldHost>(true))
                .Single();
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message), Is.True, message);
            return host;
        }

        private static BackgroundTileMaterialDefinition LoadBackground(string name) =>
            AssetDatabase.LoadAssetAtPath<BackgroundTileMaterialDefinition>(
                $"Assets/AntColony/Data/WorldMaterials/{name}.asset");
    }
}
