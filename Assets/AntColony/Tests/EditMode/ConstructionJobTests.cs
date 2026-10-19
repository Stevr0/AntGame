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
    /// <summary>Focused construction-work and site-staging checks.</summary>
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
        public void ValidDesignationCreatesSharedJobAndSiteRequirement()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionRecipe recipe = GetRecipe(host, BackgroundFunction.Sleeping);
            Assert.That(host.ConstructionDesignations.TryDesignate(Site,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            Assert.That(designation.Job.Category, Is.EqualTo(AntWorkCategory.Construction));
            Assert.That(designation.Site.Requirements.Single().ResourceTypeId,
                Is.EqualTo(recipe.ResourceTypeId));
            Assert.That(designation.Site.Requirements.Single().RequiredQuantity,
                Is.EqualTo(recipe.RequiredQuantity));
            Assert.That(designation.Site.IsFullySupplied, Is.False);
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
        public void ConstructionCannotStartUntilHaulingHasFullyStagedSite()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.Sleeping);
            AntRecord ant = CreateRecord("waiting-builder");
            Vector2Int approach = MeasureApproach(host, Site);
            Assert.That(ConstructionJobOrder.CanObtainRequiredMaterial(
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, approach, out string reason), Is.False);
            Assert.That(reason, Does.Contain("Hauling"));
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, approach, approach, host.ConstructionTransaction,
                out _, out reason), Is.False);
            Assert.That(designation.Job.ReservationOwner.IsValid, Is.False);

            StageFromTestInventory(designation, ant);
            Assert.That(ConstructionJobOrder.CanObtainRequiredMaterial(
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, approach, out string error), Is.True, error);
        }

        [Test]
        public void TimedConstructionConsumesStagingOnceAndAwardsConstructionXp()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.WorldData.GetCell(Site, out WorldTileCellData before);
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.Sleeping);
            StageFromTestInventory(designation, CreateRecord("stager"));
            AntRecord builder = CreateRecord("builder");
            ConstructionJobOrder order = CreateSuppliedOrder(host, builder, designation);
            Assert.That(order.TryArrive(order.SiteApproachCell, out _), Is.False);
            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.Working));
            order.Advance(4d, 4d);

            host.WorldData.GetCell(Site, out WorldTileCellData after);
            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.Completed));
            Assert.That(after.BackgroundMaterialId,
                Is.EqualTo(designation.Recipe.Result.StableMaterialId));
            Assert.That(after.SolidMaterialId, Is.EqualTo(before.SolidMaterialId));
            Assert.That(designation.Site.DeliveredQuantity, Is.Zero);
            Assert.That(builder.Skills.Get(AntSkillId.Construction).TotalExperience,
                Is.EqualTo(20d).Within(0.001d));
            Assert.That(host.ConstructionTransaction.TryCommit(designation, out _),
                Is.False, "Completion must not consume staged material twice.");
        }

        [Test]
        public void CancellingConstructionRecoversStagedMaterialAsGroundResource()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.Clear();
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.Sleeping);
            StageFromTestInventory(designation, CreateRecord("stager"));
            Assert.That(host.ConstructionDesignations.TryCancel(Site,
                out string error), Is.True, error);
            Assert.That(designation.Site.DeliveredQuantity, Is.Zero);
            Assert.That(host.MaterialDrops.TryFindAtCell(Site,
                out WorldMaterialDrop recovered), Is.True);
            Assert.That(recovered.Material, Is.EqualTo(ExcavatedMaterialType.Soil));
            Assert.That(recovered.Quantity,
                Is.EqualTo(designation.Recipe.RequiredQuantity));
        }

        [Test]
        public void SiteReservationsPreventDuplicateDeliveryAndReleaseOnCancellation()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.Sleeping);
            ConstructionSite site = designation.Site;
            Assert.That(site.TryReserveIncoming(designation.Recipe.ResourceTypeId,
                "carrier-a", 1, out int reserved, out string error), Is.True, error);
            Assert.That(reserved, Is.EqualTo(1));
            Assert.That(site.TryReserveIncoming(designation.Recipe.ResourceTypeId,
                "carrier-b", 1, out _, out _), Is.False);
            site.ReleaseIncoming(designation.Recipe.ResourceTypeId, "carrier-a");
            Assert.That(site.TryReserveIncoming(designation.Recipe.ResourceTypeId,
                "carrier-b", 1, out _, out error), Is.True, error);
        }

        [Test]
        public void ConstructionCancellationDoesNotRemoveAnotherSitesStaging()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionDesignation first = Designate(host, Site,
                BackgroundFunction.Sleeping);
            ConstructionDesignation second = Designate(host,
                Site + Vector2Int.right, BackgroundFunction.Sleeping);
            StageFromTestInventory(first, CreateRecord("first-stager"));
            StageFromTestInventory(second, CreateRecord("second-stager"));
            Assert.That(host.ConstructionDesignations.TryCancel(first.Cell,
                out string error), Is.True, error);
            Assert.That(first.Site.DeliveredQuantity, Is.Zero);
            Assert.That(second.Site.DeliveredQuantity,
                Is.EqualTo(second.Recipe.RequiredQuantity));
        }

        [Test]
        public void SiteCanStageMultipleRecipeResourcesWithoutLooseStackMixing()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.Clear();
            ConstructionRecipe authored = GetRecipe(host, BackgroundFunction.Sleeping);
            var recipe = new ConstructionRecipe("test.multi-resource", string.Empty,
                authored.Result, new[]
                {
                    new ConstructionMaterialCost("material.soil", 1),
                    new ConstructionMaterialCost("material.stone", 1)
                }, authored.BaseDurationHours);
            var catalog = new ConstructionRecipeCatalog(new[] { recipe });
            var registry = new ConstructionDesignationRegistry(host.WorldData,
                host.GridNavigationMap, host.ColonyJobRegistry, catalog,
                host.SurvivalInteractionWorld, host.MaterialStorage,
                host.MaterialDrops);
            Assert.That(registry.TryDesignate(Site,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            foreach (ConstructionMaterialRequirement requirement in
                designation.Site.Requirements)
            {
                AntRecord carrier = CreateRecord($"carrier-{requirement.ResourceTypeId}");
                carrier.Inventory.TryAdd(requirement.ResourceTypeId, 1, 10,
                    false, out _, out _);
                Assert.That(designation.Site.TryReserveIncoming(
                    requirement.ResourceTypeId, carrier.Id.Value, 1,
                    out _, out error), Is.True, error);
                Assert.That(designation.Site.TryCommitReservedDelivery(
                    requirement.ResourceTypeId, carrier.Id.Value, 1,
                    () => carrier.Inventory.TryRemoveResource(
                        requirement.ResourceTypeId, 1, 10, out _), out error),
                    Is.True, error);
            }
            Assert.That(designation.Site.IsFullySupplied, Is.True);
            Assert.That(registry.TryCancel(Site, out error), Is.True, error);
            WorldMaterialDrop[] recovered = host.MaterialDrops.Drops.ToArray();
            Assert.That(recovered.Length, Is.EqualTo(2));
            Assert.That(recovered.Select(drop => drop.WorldCell).Distinct().Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void CompletedMaterialStorageRegistersAuthoritativePlacementAndEmptyCapacityOnce()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.WorldData.GetCell(Site, out WorldTileCellData before);
            int placementCount = host.Placements.Count;
            int storageCount = host.MaterialStorage.Count;
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.MaterialStorage);
            Assert.That(designation.Recipe.RequiredQuantity, Is.EqualTo(1));
            Assert.That(designation.Recipe.BaseDurationHours, Is.EqualTo(4d));
            StageFromTestInventory(designation, CreateRecord("storage-stager"));
            AntRecord builder = CreateRecord("storage-builder");
            ConstructionJobOrder order = CreateSuppliedOrder(host,
                builder, designation);
            order.TryArrive(order.SiteApproachCell, out _);
            order.Advance(4d, 4d);

            host.WorldData.GetCell(Site, out WorldTileCellData after);
            Assert.That(after.BackgroundMaterialId,
                Is.EqualTo(before.BackgroundMaterialId),
                "Direct Placement completion preserves the natural Background.");
            Assert.That(host.MaterialStorage.TryGet(Site,
                out MaterialStorageSlot slot), Is.True);
            Assert.That(slot.Quantity, Is.Zero);
            Assert.That(slot.MaximumStack, Is.EqualTo(10));
            Assert.That(slot.Filter,
                Is.EqualTo(MaterialStorageFilter.AllBuildingMaterials));
            Assert.That(host.MaterialStorage.Count, Is.EqualTo(storageCount + 1));
            Assert.That(host.MaterialStorage.GetFunctionAuthority(Site),
                Is.EqualTo(MaterialStorageFunctionAuthority.AuthoritativePlacement));
            Assert.That(host.Placements.TryGetByCell(ColonyCell(Site),
                out PlacementSnapshot placement), Is.EqualTo(PlacementReadResult.Success));
            Assert.That(placement.AuthorityState,
                Is.EqualTo(PlacementAuthorityState.AuthoritativePlacement));
            Assert.That(placement.Lifecycle, Is.EqualTo(PlacementLifecycle.Completed));
            Assert.That(placement.Footprint.Count, Is.EqualTo(1));
            Assert.That(placement.CurrentHitPoints, Is.Null);
            Assert.That(placement.MaximumHitPoints, Is.Null);
            Assert.That(host.Placements.Count, Is.EqualTo(placementCount + 1));
            Assert.That(designation.Site.DeliveredQuantity, Is.Zero);
            Assert.That(builder.Skills.Get(AntSkillId.Construction).TotalExperience,
                Is.EqualTo(20d).Within(0.001d));

            PlacementId identity = placement.PlacementId;
            long placementRevision = placement.Revision;
            double constructionExperience = builder.Skills.Get(
                AntSkillId.Construction).TotalExperience;
            Assert.That(host.ConstructionTransaction.TryCommit(designation,
                out string replayError), Is.True, replayError);
            Assert.That(host.Placements.Count, Is.EqualTo(placementCount + 1));
            Assert.That(host.MaterialStorage.Count, Is.EqualTo(storageCount + 1));
            host.Placements.TryGetByCell(ColonyCell(Site), out placement);
            Assert.That(placement.PlacementId, Is.EqualTo(identity));
            Assert.That(placement.Revision, Is.EqualTo(placementRevision));
            Assert.That(builder.Skills.Get(AntSkillId.Construction).TotalExperience,
                Is.EqualTo(constructionExperience));
            Assert.That(host.ConstructionTransaction.LastPhase,
                Is.EqualTo(ConstructionCompletionPhase.ReplaySuppressed));

            Assert.That(host.RuntimeCells.TryGetCell(ColonyCell(Site),
                out RuntimeCellSnapshot runtimeBefore),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(host.MaterialStorage.TryReserveIncoming(Site,
                "material.soil", 10, "test.deposit", 1,
                out _, out replayError), Is.True, replayError);
            Assert.That(host.MaterialStorage.TryCommitReservedDeposit(Site,
                "material.soil", 10, "test.deposit", 1, () => true,
                out replayError), Is.True, replayError);
            Assert.That(host.RuntimeCells.TryGetCell(ColonyCell(Site),
                out RuntimeCellSnapshot runtimeAfter),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(runtimeAfter.Placement.StableId,
                Is.EqualTo(runtimeBefore.Placement.StableId));
            Assert.That(runtimeAfter.Placement.PlacementRevision,
                Is.EqualTo(runtimeBefore.Placement.PlacementRevision));
            Assert.That(runtimeAfter.Placement.FunctionRevision,
                Is.GreaterThan(runtimeBefore.Placement.FunctionRevision));
            Assert.That(runtimeAfter.Placement.StoredQuantity, Is.EqualTo(1));
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.ConstructionDesignations.Count, Is.Zero);
        }

        [Test]
        public void BlockedMaterialStorageCompletionPreservesSiteAndCanRetry()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.MaterialStorage);
            StageFromTestInventory(designation, CreateRecord("blocked-stager"));
            Assert.That(host.MaterialDrops.TryAdd("test.storage-conflict",
                ExcavatedMaterialType.Soil, 1, Site,
                out _, out string error), Is.True, error);
            ConstructionJobOrder blocked = CreateSuppliedOrder(host,
                CreateRecord("blocked-builder"), designation);
            blocked.TryArrive(blocked.SiteApproachCell, out _);
            blocked.Advance(4d, 4d);

            Assert.That(blocked.Phase, Is.EqualTo(ConstructionOrderPhase.Failed));
            Assert.That(designation.Site.DeliveredQuantity, Is.EqualTo(1));
            Assert.That(designation.Site.AcceptsDeliveries, Is.True);
            Assert.That(host.MaterialStorage.TryGet(Site, out _), Is.False);
            Assert.That(host.Placements.TryGetByCell(ColonyCell(Site), out _),
                Is.EqualTo(PlacementReadResult.NotRegistered));
            Assert.That(host.ConstructionTransaction.LastPhase,
                Is.EqualTo(ConstructionCompletionPhase.Blocked));

            host.MaterialDrops.Clear();
            ConstructionJobOrder retry = CreateSuppliedOrder(host,
                CreateRecord("retry-builder"), designation);
            retry.TryArrive(retry.SiteApproachCell, out _);
            retry.Advance(4d, 8d);
            Assert.That(retry.Phase, Is.EqualTo(ConstructionOrderPhase.Completed));
            Assert.That(host.MaterialStorage.TryGet(Site, out _), Is.True);
            Assert.That(designation.Site.DeliveredQuantity, Is.Zero);
        }

        [Test]
        public void ConstructionProgressStillUsesSkillAndNeedsMultipliers()
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

        private static double AdvanceOneHour(AntGameDevelopmentWorldHost host,
            AntRecord ant)
        {
            ConstructionDesignation designation = Designate(host, Site,
                BackgroundFunction.Sleeping);
            StageFromTestInventory(designation, CreateRecord($"{ant.Id.Value}-stager"));
            ConstructionJobOrder order = CreateSuppliedOrder(host, ant, designation);
            order.TryArrive(order.SiteApproachCell, out _);
            order.Advance(1d, 1d);
            return order.Job.Progress;
        }

        private static ConstructionJobOrder CreateSuppliedOrder(
            AntGameDevelopmentWorldHost host, AntRecord ant,
            ConstructionDesignation designation)
        {
            Vector2Int approach = MeasureApproach(host, designation.Cell);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                ant, designation, approach, approach, host.ConstructionTransaction,
                out ConstructionJobOrder order, out string error), Is.True, error);
            return order;
        }

        private static ConstructionDesignation Designate(
            AntGameDevelopmentWorldHost host, Vector2Int cell,
            BackgroundFunction targetFunction)
        {
            ConstructionRecipe recipe = GetRecipe(host, targetFunction);
            Assert.That(host.ConstructionDesignations.TryDesignate(cell,
                recipe.Result.StableMaterialId, out ConstructionDesignation designation,
                out string error), Is.True, error);
            return designation;
        }

        private static void StageFromTestInventory(ConstructionDesignation designation,
            AntRecord carrier)
        {
            carrier.Inventory.TryAdd(designation.Recipe.ResourceTypeId,
                designation.Recipe.RequiredQuantity, 10, false, out _, out _);
            Assert.That(designation.Site.TryStage(carrier.Inventory,
                designation.Recipe.RequiredQuantity, 10, out string error), Is.True,
                error);
        }

        private static Vector2Int MeasureApproach(AntGameDevelopmentWorldHost host,
            Vector2Int cell)
        {
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, cell, out Vector2Int approach,
                out _, out string error), Is.True, error);
            return approach;
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

        private static RegionCellId ColonyCell(Vector2Int cell) =>
            RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(cell.x, cell.y));

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
