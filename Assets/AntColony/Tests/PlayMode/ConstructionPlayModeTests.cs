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

namespace AntColony.Tests.PlayMode
{
    public sealed class ConstructionPlayModeTests
    {
        [UnityTest]
        public IEnumerator DevelopmentSceneProvidesAllConversionRecipesAndModeSwitching()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.ConstructionRecipes.Recipes.Count, Is.EqualTo(6));
            Assert.That(host.ConstructionRecipes.Recipes.Select(
                recipe => recipe.Result.Function), Does.Contain(BackgroundFunction.Nursery));
            host.SelectionController.SetDesignationMode(
                WorldDesignationMode.BackgroundConversion);
            Assert.That(host.SelectionController.DesignationMode,
                Is.EqualTo(WorldDesignationMode.BackgroundConversion));
            Assert.That(host.SelectionController.TryHandleLeftClick(Vector2.zero),
                Is.False, "Designation mode must not issue selection commands.");
            host.SelectionController.SetDesignationMode(WorldDesignationMode.Excavate);
            Assert.That(host.SelectionController.DesignationMode,
                Is.EqualTo(WorldDesignationMode.Excavate));
        }

        [UnityTest]
        public IEnumerator AutonomousBuilderClaimsRealConstructionExclusively()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            Vector2Int site = new(11, SampleWorldGenerator.MainPassageY);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(
                item => item.Result.Function == BackgroundFunction.Sleeping);
            Assert.That(host.ConstructionDesignations.TryDesignate(site,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            foreach (SelectableGridAnt ant in controller.SelectableAnts)
            {
                ant.Record.WorkPriorities.Reset(AntWorkPriority.Disabled);
                ant.Record.WorkPriorities.TrySet(AntWorkCategory.Construction,
                    AntWorkPriority.Highest, out _);
            }
            controller.SelectableAnts[0].Record.Inventory.TryAdd("material.soil", 1,
                MaterialResourceCatalog.ProvisionalMaximumStack, false, out _, out _);
            Assert.That(designation.Site.TryStage(
                controller.SelectableAnts[0].Record.Inventory, 1,
                MaterialResourceCatalog.ProvisionalMaximumStack, out error),
                Is.True, error);
            controller.SetAutonomousColonyWorkEnabled(true);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(
                controller.SelectableAnts[0], 1d), Is.True);
            Assert.That(controller.SelectableAnts[0].ConstructionOrder, Is.Not.Null);
            Assert.That(designation.Job.ReservationOwner,
                Is.EqualTo(controller.SelectableAnts[0].Record.Id));
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(
                controller.SelectableAnts[1], 1d), Is.False);
        }

        [UnityTest]
        public IEnumerator RebuildClearsConstructionAndRestoresDeterministicWorld()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Vector2Int site = new(11, SampleWorldGenerator.MainPassageY);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(
                item => item.Result.Function == BackgroundFunction.Sleeping);
            Assert.That(host.ConstructionDesignations.TryDesignate(site,
                recipe.Result.StableMaterialId, out _, out _), Is.True);
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.ConstructionDesignations.Count, Is.Zero);
            host.WorldData.GetCell(site, out WorldTileCellData restored);
            Assert.That(restored.BackgroundMaterialId,
                Is.Not.EqualTo(recipe.Result.StableMaterialId));
        }

        [UnityTest]
        public IEnumerator CancelledConstructionCanBeDesignatedAgain()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Vector2Int site = new(11, SampleWorldGenerator.MainPassageY);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(
                item => item.Result.Function == BackgroundFunction.Sleeping);
            Assert.That(host.ConstructionDesignations.TryDesignate(site,
                recipe.Result.StableMaterialId, out _, out string error), Is.True, error);
            Assert.That(host.ConstructionDesignations.TryCancel(site, out error),
                Is.True, error);
            Assert.That(host.ConstructionDesignations.TryDesignate(site,
                recipe.Result.StableMaterialId, out ConstructionDesignation replacement,
                out error), Is.True, error);
            yield return null;
            Assert.That(replacement.State,
                Is.EqualTo(ConstructionDesignationState.Pending));
        }

        [UnityTest]
        public IEnumerator UnsuppliedConstructionDoesNotClaimAntOrCarriedMaterial()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            Vector2Int site = new(11, SampleWorldGenerator.MainPassageY);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(
                item => item.Result.Function == BackgroundFunction.Sleeping);
            Assert.That(host.ConstructionDesignations.TryDesignate(site,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.WorkPriorities.Reset(AntWorkPriority.Disabled);
            ant.Record.WorkPriorities.TrySet(AntWorkCategory.Construction,
                AntWorkPriority.Highest, out _);
            ant.Record.Inventory.Clear();
            ant.Record.Inventory.TryAdd("material.stone", 3,
                MaterialResourceCatalog.ProvisionalMaximumStack, false, out _, out _);
            controller.SetAutonomousColonyWorkEnabled(true);

            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 1d), Is.False);
            Assert.That(ant.ConstructionOrder, Is.Null);
            Assert.That(ant.IssueMoveOrder(SampleWorldGenerator.GridDirectDestination),
                Is.True);
            Assert.That(ant.Record.Inventory.TotalQuantity, Is.EqualTo(3));
            Assert.That(designation.Job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(designation.Job.ReservationOwner.IsValid, Is.False);
            Assert.That(host.MaterialStorage.Slots.All(slot =>
                slot.ReservationOwnerId != ant.Record.Id.Value), Is.True);
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
    }
}
