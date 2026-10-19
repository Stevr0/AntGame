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
    public sealed class MaterialStorageHaulingPlayModeTests
    {
        [UnityTest]
        public IEnumerator DevelopmentSceneExposesStorageAndHaulingControls()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.MaterialStorage.Count, Is.EqualTo(5));
            Assert.That(host.SelectionController.AutonomousHaulingEnabled, Is.False);
            host.SelectionController.SetAutonomousHaulingEnabled(true);
            Assert.That(host.SelectionController.AutonomousHaulingEnabled, Is.True);
            Assert.That(host.SelectionController.ResetMaterialHaulingFixtures(
                out string error), Is.True, error);
            Assert.That(host.SelectionController.CreatePartialHaulingSoilFixture(
                out error), Is.True, error);
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntSelectionCommandController.PartialHaulingSoilFixture,
                out WorldMaterialDrop largeStack), Is.True);
            Assert.That(largeStack.Quantity, Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator CanonicalStackSurvivesViewSwitchAndResetInvalidatesIt()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop before), Is.True);
            string oldContext = host.MaterialDrops.ScenarioContextId;
            MaterialTransferRegistry oldTransfers = host.MaterialTransfers;

            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Surface);
            Assert.That(host.SelectionController.ColonyWorldPresentationEnabled, Is.False);
            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Colony);
            Assert.That(host.SelectionController.ColonyWorldPresentationEnabled, Is.True);
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop afterSwitch), Is.True);
            Assert.That(afterSwitch.StackId, Is.EqualTo(before.StackId));
            Assert.That(afterSwitch.Quantity, Is.EqualTo(before.Quantity));
            Assert.That(afterSwitch.Revision, Is.EqualTo(before.Revision));

            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(oldTransfers.IsActive, Is.False);
            Assert.That(host.MaterialDrops.ScenarioContextId, Is.Not.EqualTo(oldContext));
            Assert.That(host.MaterialDrops.TryGet(before.OwnershipKey, out _), Is.False);
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop restored), Is.True);
            Assert.That(restored.StackId, Is.Not.EqualTo(before.StackId));
        }

        [UnityTest]
        public IEnumerator AutonomousCarrierMovesLooseSoilIntoPhysicalStorage()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            int dropsBefore = host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil);
            host.MaterialStorage.TryGet(
                AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture,
                out MaterialStorageSlot general);
            int storedBefore = general.Quantity;
            controller.SetAutonomousHaulingEnabled(true);
            controller.SetAutonomousColonyWorkEnabled(true);

            float timeout = Time.time + 35f;
            while (general.Quantity == storedBefore && Time.time < timeout)
                yield return null;

            Assert.That(general.Quantity, Is.GreaterThan(storedBefore));
            Assert.That(host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil),
                Is.LessThan(dropsBefore));
            Assert.That(controller.SelectableAnts.Sum(
                ant => ant.Record.Inventory.TotalQuantity) +
                host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil) +
                host.MaterialStorage.Slots.Where(slot =>
                    slot.ResourceTypeId == MaterialResourceCatalog.GetStableId(
                        ExcavatedMaterialType.Soil)).Sum(slot => slot.Quantity),
                Is.EqualTo(dropsBefore + 5));
        }

        [UnityTest]
        public IEnumerator CarrierInventoryCreatesOwnerOnlySelfDepositWork()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            host.MaterialDrops.Clear();
            SelectableGridAnt carrier = controller.SelectableAnts[0];
            carrier.Record.Inventory.TryAdd(
                MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone),
                2, MaterialResourceCatalog.ProvisionalMaximumStack, false,
                out _, out _);
            host.MaterialStorage.TryGet(
                AntGameDevelopmentWorldHost.StoneMaterialStorageFixture,
                out MaterialStorageSlot storage);
            controller.SetAutonomousHaulingEnabled(true);
            controller.SetAutonomousColonyWorkEnabled(true);

            float timeout = Time.time + 35f;
            while (storage.Quantity == 0 && Time.time < timeout)
                yield return null;

            Assert.That(storage.Quantity, Is.EqualTo(2));
            Assert.That(carrier.Record.Inventory.TotalQuantity, Is.Zero);
            Assert.That(carrier.Record.Skills.Get(AntSkillId.Hauling).TotalExperience,
                Is.GreaterThan(0d));
        }

        [UnityTest]
        public IEnumerator GeneratedQueenCanHaulToAndBuildFirstStorageWithoutStorage()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203, out string error), Is.True, error);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out error), Is.True, error);
            Assert.That(host.MaterialStorage.Count, Is.Zero);
            Assert.That(host.MaterialDrops.TryAdd("play.bootstrap.soil",
                ExcavatedMaterialType.Soil, 1, new Vector2Int(-2, -8),
                out WorldMaterialDrop source, out error), Is.True, error);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(item =>
                item.Result.Function == BackgroundFunction.MaterialStorage);
            Vector2Int siteCell = new(1, -8);
            Assert.That(host.ConstructionDesignations.TryDesignate(siteCell,
                recipe.Result.StableMaterialId, out ConstructionDesignation site,
                out error), Is.True, error);
            SelectableGridAnt queen = host.SelectionController.SelectableAnts.Single();
            Vector2Int start = new(ProceduralWorldGenerator.QueenSpawn.X,
                ProceduralWorldGenerator.QueenSpawn.Y);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, source.WorldCell,
                out Vector2Int sourceApproach, out _, out error), Is.True, error);
            Assert.That(host.ColonyJobRegistry.TryAdd(new ColonyJobId("play.haul"),
                AntWorkCategory.Hauling, source.WorldCell, out ColonyJob haulJob,
                out error), Is.True, error);
            var plan = new MaterialHaulingPlan(haulJob,
                MaterialHaulSourceKind.WorldDrop, "material.soil", source.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                host.ConstructionDesignations, queen.Record, plan, sourceApproach,
                start, out MaterialHaulingOrder haul, out error), Is.True, error);
            Assert.That(haul.TryArrive(sourceApproach, 1d,
                out Vector2Int siteApproach), Is.True);
            Assert.That(haul.TryArrive(siteApproach, 2d, out _), Is.True);
            Assert.That(haul.Transfer.Phase,
                Is.EqualTo(MaterialTransferPhase.Completed));
            Assert.That(site.Site.IsFullySupplied, Is.True);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                queen.Record, site, siteApproach, siteApproach,
                host.ConstructionTransaction, out ConstructionJobOrder construction,
                out error), Is.True, error);
            construction.TryArrive(siteApproach, out _);
            construction.Advance(4d, 4d);
            yield return null;

            Assert.That(construction.Phase,
                Is.EqualTo(ConstructionOrderPhase.Completed));
            Assert.That(host.MaterialStorage.TryGet(siteCell,
                out MaterialStorageSlot storage), Is.True);
            Assert.That(storage.Quantity, Is.Zero);
            Assert.That(host.MaterialStorage.GetFunctionAuthority(siteCell),
                Is.EqualTo(MaterialStorageFunctionAuthority.LegacyBackground),
                "New completion remains on the documented WV-B3D.3B bridge.");
            RegionCellId runtimeCell = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(siteCell.x, siteCell.y));
            Assert.That(host.Placements.TryGetByCell(runtimeCell,
                out PlacementSnapshot projection),
                Is.EqualTo(PlacementReadResult.Success));
            Assert.That(projection.AuthorityState,
                Is.EqualTo(PlacementAuthorityState.LegacyBackgroundProjection));
            Assert.That(queen.Record.Inventory.TotalQuantity, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LevelZeroCarrierCombinesFourGroundSourcesInOneRun()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            host.MaterialDrops.Clear();
            WorldMaterialDrop first = default;
            int createdCount = 0;
            for (int x = -10; x <= 10 && createdCount < 4; x++)
            {
                Vector2Int cell = new(x, SampleWorldGenerator.MainPassageY);
                if (host.MaterialDrops.ValidatePlacement(cell).Compatibility !=
                    GroundStackPlacementCompatibility.Valid) continue;
                Assert.That(host.MaterialDrops.TryAdd($"play.multi.{createdCount}",
                    ExcavatedMaterialType.Soil, 1, cell,
                    out WorldMaterialDrop created, out string addError), Is.True, addError);
                if (createdCount == 0) first = created;
                createdCount++;
            }
            Assert.That(createdCount, Is.EqualTo(4));
            SelectableGridAnt carrier = host.SelectionController.SelectableAnts[0];
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, first.WorldCell,
                out Vector2Int approach, out _, out string error), Is.True, error);
            Assert.That(host.ColonyJobRegistry.TryAdd(new ColonyJobId("play.multi-haul"),
                AntWorkCategory.Hauling, first.WorldCell, out ColonyJob job,
                out error), Is.True, error);
            var plan = new MaterialHaulingPlan(job,
                MaterialHaulSourceKind.WorldDrop, "material.soil", first.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage, carrier.Record, plan, approach, start,
                out MaterialHaulingOrder order, out error), Is.True, error);

            Vector2Int arrival = order.InitialApproachCell;
            while (order.Phase == MaterialHaulingPhase.TravellingToSource)
                Assert.That(order.TryArrive(arrival, 1d,
                    out arrival), Is.True, order.FailureReason);
            Assert.That(order.SourceCount, Is.EqualTo(4));
            Assert.That(carrier.Record.Inventory.TotalQuantity, Is.EqualTo(4));
            Assert.That(carrier.Record.Inventory.OccupiedSlotCount, Is.EqualTo(2));
            Assert.That(order.TryArrive(order.DestinationApproachCell, 2d,
                out _), Is.True, order.FailureReason);
            Assert.That(carrier.Record.Inventory.TotalQuantity, Is.Zero);
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
