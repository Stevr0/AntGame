using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused transaction and job checks for Milestone 3.4A.</summary>
    public sealed class ConstructionJobTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private static readonly Vector2Int Site =
            new(11, SampleWorldGenerator.MainPassageY);

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void ValidDesignationUsesSharedConstructionJobAndRejectsDuplicate()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionRecipe recipe = GetRecipe(host, BackgroundFunction.Sleeping);
            Assert.That(host.ConstructionDesignations.TryDesignate(Site,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            Assert.That(designation.Job.Category, Is.EqualTo(AntWorkCategory.Construction));
            Assert.That(host.ConstructionDesignations.TryDesignate(Site,
                recipe.Result.StableMaterialId, out _, out _), Is.False);
        }

        [Test]
        public void CoveredSameTypeInaccessibleAndOccupiedCellsAreRejected()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionRecipe prepared = GetRecipe(host, BackgroundFunction.None);
            Assert.That(host.ConstructionDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                prepared.Result.StableMaterialId, out _, out _), Is.False);
            Assert.That(host.ConstructionDesignations.TryDesignate(
                new Vector2Int(-16, SampleWorldGenerator.MainPassageY),
                prepared.Result.StableMaterialId, out _, out _), Is.False);
            Assert.That(host.ConstructionDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.UnreachableMaterialStorageFixture,
                prepared.Result.StableMaterialId, out _, out _), Is.False);
            Assert.That(host.ConstructionDesignations.TryDesignate(
                host.SurvivalFixtureCells[0], prepared.Result.StableMaterialId,
                out _, out _), Is.False);
        }

        [Test]
        public void AreaDesignationIsBoundedAndCreatesIndependentJobs()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionRecipe recipe = GetRecipe(host, BackgroundFunction.Sleeping);
            int added = host.ConstructionDesignations.DesignateArea(
                new Vector2Int(10, SampleWorldGenerator.MainPassageY),
                new Vector2Int(12, SampleWorldGenerator.MainPassageY),
                recipe.Result.StableMaterialId, out _);
            Assert.That(added, Is.GreaterThanOrEqualTo(2));
            Assert.That(host.ConstructionDesignations.Count, Is.EqualTo(added));
        }

        [Test]
        public void PriorityDisabledAndReachableCostUseExistingSelectionService()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            ConstructionRecipe recipe = GetRecipe(host, BackgroundFunction.Sleeping);
            Vector2Int near = new(11, SampleWorldGenerator.MainPassageY);
            Vector2Int far = new(18, SampleWorldGenerator.MainPassageY);
            host.ConstructionDesignations.TryDesignate(near,
                recipe.Result.StableMaterialId, out ConstructionDesignation nearJob,
                out _);
            host.ConstructionDesignations.TryDesignate(far,
                recipe.Result.StableMaterialId, out ConstructionDesignation farJob,
                out _);
            AntRecord ant = CreateRecord("priority");
            ant.WorkPriorities.Reset(AntWorkPriority.Disabled);
            var selector = new ColonyJobSelectionService();
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(selector.TryChoose(ant, host.ColonyJobRegistry,
                host.GridNavigationMap, start, out _, out _, out _), Is.False);
            ant.WorkPriorities.TrySet(AntWorkCategory.Construction,
                AntWorkPriority.Highest, out _);
            Assert.That(selector.TryChoose(ant, host.ColonyJobRegistry,
                host.GridNavigationMap, start, out ColonyJob selected,
                out _, out string error), Is.True, error);
            Assert.That(selected.Id, Is.EqualTo(nearJob.Job.Id));
            Assert.That(selected.Id, Is.Not.EqualTo(farJob.Job.Id));
        }

        [Test]
        public void ExistingInventoryIsStagedBeforeAnyExternalSource()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("inventory-first");
            ant.Inventory.TryAdd("material.soil", 1, 10, false, out _, out _);
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.Sleeping);
            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.TravellingToSite));
            Assert.That(order.TryArrive(order.SiteApproachCell, out _), Is.False);
            Assert.That(order.Designation.Site.DeliveredQuantity, Is.EqualTo(1));
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.Working));
        }

        [Test]
        public void BuilderWithdrawsReservedStorageAndSeparateCarrierCannotTakeIt()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.Clear();
            AntRecord ant = CreateRecord("storage-builder");
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.Sleeping);
            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.TravellingToSource));
            Assert.That(host.MaterialStorage.TryReserveWithdrawal(
                order.CurrentSourceCell, "material.soil", "other", 1,
                out _, out _), Is.False);
            Assert.That(order.TryArrive(order.CurrentSourceCell + Vector2Int.right,
                out Vector2Int siteApproach), Is.True);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(1));
            Assert.That(siteApproach, Is.EqualTo(order.SiteApproachCell));
        }

        [Test]
        public void BuilderCanCollectLooseDropAndCancellationKeepsCargo()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialStorage.Clear();
            AntRecord ant = CreateRecord("drop-builder");
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.Sleeping);
            Assert.That(order.TryArrive(order.CurrentSourceCell + Vector2Int.left,
                out _), Is.True);
            order.Cancel("player interruption");
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(1));
            Assert.That(order.Job.Status, Is.EqualTo(ColonyJobStatus.Paused));
        }

        [Test]
        public void MultiTripUsesSameBuilderAndPreservesStagedMaterial()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialStorage.Clear();
            host.MaterialDrops.Clear();
            host.MaterialDrops.TryAdd("trip.one", ExcavatedMaterialType.Soil, 1,
                new Vector2Int(8, SampleWorldGenerator.MainPassageY), out _, out _, 1);
            host.MaterialDrops.TryAdd("trip.two", ExcavatedMaterialType.Soil, 1,
                new Vector2Int(9, SampleWorldGenerator.MainPassageY), out _, out _, 1);
            ConstructionRecipe baseRecipe = GetRecipe(host, BackgroundFunction.Sleeping);
            var recipe = new ConstructionRecipe("test.multi", string.Empty,
                baseRecipe.Result, ExcavatedMaterialType.Soil, 2, 4d);
            var catalog = new ConstructionRecipeCatalog(new[] { recipe });
            var designations = new ConstructionDesignationRegistry(host.WorldData,
                host.GridNavigationMap, host.ColonyJobRegistry, catalog,
                host.SurvivalInteractionWorld, host.MaterialStorage);
            Assert.That(designations.TryDesignate(Site, recipe.Result.StableMaterialId,
                out ConstructionDesignation designation, out string error), Is.True, error);
            AntRecord ant = CreateRecord("multi-builder");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, Site, out Vector2Int siteApproach, out _, out _);
            var transaction = new ConstructionWorldTransaction(host.WorldData,
                host.SurvivalInteractionWorld, host.MaterialStorage);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, siteApproach, start, transaction,
                out ConstructionJobOrder order, out error), Is.True, error);
            Assert.That(order.TryArrive(order.CurrentSourceCell + Vector2Int.left,
                out _), Is.True);
            Assert.That(order.TryArrive(siteApproach, out _), Is.True);
            Assert.That(designation.Site.DeliveredQuantity, Is.EqualTo(1));
            Assert.That(order.Job.ReservationOwner, Is.EqualTo(ant.Id));
        }

        [Test]
        public void TimedCompletionConsumesOnceChangesOnlyBackgroundAndAwardsXp()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.WorldData.GetCell(Site, out WorldTileCellData before);
            AntRecord ant = CreateRecord("complete");
            ant.Inventory.TryAdd("material.soil", 1, 10, false, out _, out _);
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.Sleeping);
            order.TryArrive(order.SiteApproachCell, out _);
            order.Advance(4d, 4d);
            host.WorldData.GetCell(Site, out WorldTileCellData after);
            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.Completed));
            Assert.That(after.BackgroundMaterialId,
                Is.EqualTo(order.Designation.Recipe.Result.StableMaterialId));
            Assert.That(after.SolidMaterialId, Is.EqualTo(before.SolidMaterialId));
            Assert.That(order.Designation.Site.DeliveredQuantity, Is.Zero);
            Assert.That(ant.Skills.Get(AntSkillId.Construction).TotalExperience,
                Is.EqualTo(20d).Within(0.001d));
            Assert.That(host.ConstructionTransaction.TryCommit(
                order.Designation, out _), Is.False);
        }

        [Test]
        public void SkillAndRestedMultipliersIncreaseCommittedProgress()
        {
            double ordinary = AdvanceOneHour(OpenHost(), CreateRecord("ordinary"));
            AntGameDevelopmentWorldHost skilledHost = OpenHost();
            AntRecord skilled = CreateRecord("skilled");
            skilled.TryAwardSkillExperience(AntSkillId.Construction, 50000d, 1d,
                0d, out _, out _);
            double skilledProgress = AdvanceOneHour(skilledHost, skilled);
            AntGameDevelopmentWorldHost restedHost = OpenHost();
            AntRecord rested = CreateRecord("rested");
            rested.TryRest(0.1d, out _);
            double restedProgress = AdvanceOneHour(restedHost, rested);
            Assert.That(skilledProgress, Is.GreaterThan(ordinary));
            Assert.That(restedProgress, Is.GreaterThan(ordinary));
        }

        [Test]
        public void InsufficientMaterialLeavesDesignationPendingAndConsumesNothing()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialStorage.Clear();
            host.MaterialDrops.Clear();
            ConstructionRecipe recipe = GetRecipe(host, BackgroundFunction.Sleeping);
            host.ConstructionDesignations.TryDesignate(Site,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out _);
            AntRecord ant = CreateRecord("no-material");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, Site, out Vector2Int approach, out _, out _);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, approach, start, host.ConstructionTransaction,
                out _, out _), Is.False);
            Assert.That(designation.Job.Progress, Is.Zero);
            Assert.That(designation.Site.DeliveredQuantity, Is.Zero);
        }

        [Test]
        public void CancellationLeavesStagedMaterialForReplacementBuilder()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord first = CreateRecord("first-builder");
            first.Inventory.TryAdd("material.soil", 1, 10, false, out _, out _);
            ConstructionJobOrder firstOrder = CreateOrder(host, first, Site,
                BackgroundFunction.Sleeping);
            firstOrder.TryArrive(firstOrder.SiteApproachCell, out _);
            firstOrder.Cancel("replace builder");
            Assert.That(firstOrder.Designation.Site.DeliveredQuantity, Is.EqualTo(1));

            AntRecord second = CreateRecord("second-builder");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, Site, out Vector2Int approach, out _, out _);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                second, firstOrder.Designation, approach, start,
                host.ConstructionTransaction, out ConstructionJobOrder replacement,
                out string error), Is.True, error);
            Assert.That(replacement.TryArrive(approach, out _), Is.False);
            Assert.That(replacement.Phase, Is.EqualTo(ConstructionOrderPhase.Working));
        }

        [Test]
        public void CancellingDesignationTurnsStagedMaterialIntoRecoverableDrop()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("cancelled-site");
            ant.Inventory.TryAdd("material.soil", 1, 10, false, out _, out _);
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.Sleeping);
            order.TryArrive(order.SiteApproachCell, out _);
            order.Cancel("player cancellation");
            Assert.That(host.ConstructionDesignations.TryCancel(Site,
                out string error), Is.True, error);
            Assert.That(host.MaterialDrops.TryFindAtCell(Site,
                out WorldMaterialDrop recovered), Is.True);
            Assert.That(recovered.Material, Is.EqualTo(ExcavatedMaterialType.Soil));
            Assert.That(recovered.Quantity, Is.EqualTo(1));
            Assert.That(order.Designation.Site.DeliveredQuantity, Is.Zero);
        }

        [Test]
        public void ConvertedFunctionsRegisterCapacityAndWorldResetRestoresBaseline()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntRecord ant = CreateRecord("capacity");
            ant.Inventory.TryAdd("material.soil", 1, 10, false, out _, out _);
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.MaterialStorage);
            order.TryArrive(order.SiteApproachCell, out _);
            order.Advance(4d, 4d);
            Assert.That(host.MaterialStorage.TryGet(Site, out MaterialStorageSlot slot),
                Is.True);
            Assert.That(slot.Quantity, Is.Zero);
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.ConstructionDesignations.Count, Is.Zero);
            Assert.That(host.MaterialStorage.TryGet(Site, out _), Is.False);
        }

        private static double AdvanceOneHour(AntGameDevelopmentWorldHost host,
            AntRecord ant)
        {
            ant.Inventory.TryAdd("material.soil", 1, 10, false, out _, out _);
            ConstructionJobOrder order = CreateOrder(host, ant, Site,
                BackgroundFunction.Sleeping);
            order.TryArrive(order.SiteApproachCell, out _);
            order.Advance(1d, 1d);
            return order.Job.Progress;
        }

        private static ConstructionJobOrder CreateOrder(
            AntGameDevelopmentWorldHost host, AntRecord ant, Vector2Int cell,
            BackgroundFunction targetFunction)
        {
            ConstructionRecipe recipe = GetRecipe(host, targetFunction);
            Assert.That(host.ConstructionDesignations.TryDesignate(cell,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, cell, out Vector2Int approach, out _, out error), Is.True, error);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, approach, start, host.ConstructionTransaction,
                out ConstructionJobOrder order, out error), Is.True, error);
            return order;
        }

        private static ConstructionRecipe GetRecipe(
            AntGameDevelopmentWorldHost host, BackgroundFunction function) =>
            host.ConstructionRecipes.Recipes.Single(recipe =>
                recipe.Result.Function == function &&
                (function != BackgroundFunction.None ||
                 recipe.Result.StableMaterialId.Contains("prepared")));

        private static AntRecord CreateRecord(string id) => new(
            new AntId(id), "Test Builder", new ColonyId("test-colony"),
            AntCaste.Worker, new AntAttributes(5, 5, 5));

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    AntGameDevelopmentWorldHost>(true)).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }
    }
}
