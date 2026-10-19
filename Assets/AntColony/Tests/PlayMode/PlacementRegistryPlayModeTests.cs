using System.Collections;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AntColony.Tests.PlayMode
{
    public sealed class PlacementRegistryPlayModeTests
    {
        [UnityTest]
        public IEnumerator FixturePublishesDefinitionsAndReadOnlyLegacyProjections()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();

            Assert.That(host.Placements, Is.Not.Null);
            Assert.That(host.Placements.DefinitionCount, Is.EqualTo(6));
            Assert.That(host.Placements.Diagnostics.LegacyProjectionCount,
                Is.GreaterThan(0));
            foreach (ConstructionRecipe recipe in host.ConstructionRecipes.Recipes)
            {
                Assert.That(host.Placements.TryGetDefinition(recipe.StableId,
                    out PlacementDefinition definition), Is.True);
                Assert.That(definition.ConstructionRecipeId,
                    Is.EqualTo(recipe.StableId));
                Assert.That(definition.ConstructionWorkHours,
                    Is.EqualTo(recipe.BaseDurationHours));
                Assert.That(definition.ConstructionRequirements.Select(
                    cost => (cost.ResourceDefinitionId, cost.Quantity)),
                    Is.EqualTo(recipe.MaterialRequirements.Select(
                        cost => (cost.ResourceTypeId, cost.Quantity))));
            }

            Vector2Int storageCell =
                AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture;
            Assert.That(host.MaterialStorage.TryGet(storageCell,
                out MaterialStorageSlot storage), Is.True);
            int storedQuantity = storage.Quantity;
            Assert.That(host.RuntimeCells.TryGetCell(ColonyCell(storageCell),
                out RuntimeCellSnapshot cell), Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(cell.Placement, Is.Not.Null);
            Assert.That(cell.Placement.AuthorityState,
                Is.EqualTo(PlacementAuthorityState.LegacyBackgroundProjection));
            Assert.That(cell.Placement.ContentsArePlacementOwned, Is.False);
            Assert.That(cell.Placement.FunctionalCapacity, Is.Null);
            Assert.That(cell.Placement.CurrentHitPoints, Is.Null);
            Assert.That(cell.Placement.MaximumHitPoints, Is.Null);
            Assert.That(cell.NaturalBackgroundProjectionState,
                Is.EqualTo(RuntimeOccupantProjectionState.DetailsUnavailable));
            Assert.That(storage.Quantity, Is.EqualTo(storedQuantity));
        }

        [UnityTest]
        public IEnumerator LegacyConstructionCompletionCreatesOneProjectionOnly()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            var site = new Vector2Int(11, SampleWorldGenerator.MainPassageY);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(
                item => item.Result.Function == BackgroundFunction.Sleeping);
            int beforeCount = host.Placements.Count;
            Assert.That(host.Placements.TryGetByCell(ColonyCell(site), out _),
                Is.EqualTo(PlacementReadResult.NotRegistered));
            Assert.That(host.ConstructionDesignations.TryDesignate(site,
                recipe.Result.StableMaterialId,
                out ConstructionDesignation designation, out string error),
                Is.True, error);
            AntRecord stager = CreateRecord("placement-play-stager");
            stager.Inventory.TryAdd(recipe.ResourceTypeId, recipe.RequiredQuantity,
                10, false, out _, out _);
            Assert.That(designation.Site.TryStage(stager.Inventory,
                recipe.RequiredQuantity, 10, out error), Is.True, error);
            Vector2Int approach = MeasureApproach(host, site);
            AntRecord builder = CreateRecord("placement-play-builder");
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                builder, designation, approach, approach,
                host.ConstructionTransaction, out ConstructionJobOrder order,
                out error), Is.True, error);
            order.TryArrive(approach, out _);
            order.Advance(4d, 4d);
            yield return null;

            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.Completed));
            Assert.That(host.Placements.Count, Is.EqualTo(beforeCount + 1));
            Assert.That(host.Placements.TryGetByCell(ColonyCell(site),
                out PlacementSnapshot projection),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(projection.DefinitionId, Is.EqualTo(recipe.StableId));
            Assert.That(projection.AuthorityState,
                Is.EqualTo(PlacementAuthorityState.LegacyBackgroundProjection));
            Assert.That(projection.CurrentHitPoints, Is.Null);
            Assert.That(host.SurvivalInteractionWorld.SleepingPositions,
                Does.Contain(site),
                "The existing Sleeping authority remains the only functional owner.");
        }

        [UnityTest]
        public IEnumerator ViewsPreserveIdentityAndScenarioReplacementRetiresContext()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            RegionCellId cell = ColonyCell(
                AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture);
            PlacementRegistry fixtureRegistry = host.Placements;
            Assert.That(fixtureRegistry.TryGetByCell(cell,
                out PlacementSnapshot fixture), Is.EqualTo(PlacementReadResult.Success));

            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Surface);
            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Colony);
            Assert.That(host.Placements, Is.SameAs(fixtureRegistry));
            Assert.That(fixtureRegistry.TryGetByCell(cell,
                out PlacementSnapshot afterViews), Is.EqualTo(PlacementReadResult.Success));
            Assert.That(afterViews.PlacementId, Is.EqualTo(fixture.PlacementId));
            Assert.That(afterViews.Revision, Is.EqualTo(fixture.Revision));

            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203L, out string message), Is.True, message);
            yield return null;
            Assert.That(fixtureRegistry.Lifecycle,
                Is.EqualTo(PlacementRegistryLifecycle.ContextInvalidated));
            Assert.That(fixtureRegistry.TryGet(fixture.Handle, out _),
                Is.EqualTo(PlacementReadResult.ContextInvalidated));
            Assert.That(host.Placements.Count, Is.Zero);

            Assert.That(host.TryStartScenario(
                GeneratedWorldScenario.DevelopmentFixture, 2203L, out message),
                Is.True, message);
            yield return null;
            Assert.That(host.Placements.TryGetByCell(cell,
                out PlacementSnapshot restored), Is.EqualTo(PlacementReadResult.Success));
            Assert.That(restored.PlacementId, Is.EqualTo(fixture.PlacementId),
                "Identical fixture state has deterministic projected identity.");
            Assert.That(restored.ScenarioContextId,
                Is.Not.EqualTo(fixture.ScenarioContextId));
            Assert.That(host.Placements.TryGet(fixture.Handle, out _),
                Is.EqualTo(PlacementReadResult.StaleScenario));
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
            PlayModeSimulationTestUtility.Resume();
        }

        private static AntGameDevelopmentWorldHost FindHost() =>
            Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();

        private static RegionCellId ColonyCell(Vector2Int cell) =>
            RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(cell.x, cell.y));

        private static Vector2Int MeasureApproach(
            AntGameDevelopmentWorldHost host, Vector2Int cell)
        {
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, cell, out Vector2Int approach,
                out _, out string error), Is.True, error);
            return approach;
        }

        private static AntRecord CreateRecord(string id) => new(
            new AntId(id), "Placement Test Ant", new ColonyId("test-colony"),
            AntCaste.Worker, new AntAttributes(5, 5, 5));
    }
}
