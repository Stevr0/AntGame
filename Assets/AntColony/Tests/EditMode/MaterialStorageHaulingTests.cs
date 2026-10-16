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
    /// <summary>Focused authoritative-data checks for Milestone 3.3D.</summary>
    public sealed class MaterialStorageHaulingTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void AuthoredStorageHasOnePhysicalSlotAndExpectedFilters()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.MaterialStorage.Count, Is.EqualTo(5));
            AssertSlot(host, AntGameDevelopmentWorldHost.SoilMaterialStorageFixture,
                MaterialStorageFilter.Soil, 0);
            AssertSlot(host, AntGameDevelopmentWorldHost.StoneMaterialStorageFixture,
                MaterialStorageFilter.Stone, 0);
            AssertSlot(host, AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture,
                MaterialStorageFilter.AllBuildingMaterials, 5);
            Assert.That(host.MaterialStorage.TryAdd(
                AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture,
                MaterialStorageFilter.Soil, string.Empty, 0, 10, out _), Is.False);
        }

        [Test]
        public void FiltersRejectWrongMaterialAndFullStorageHasNoCapacity()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            string soil = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil);
            string stone = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone);
            Assert.That(host.MaterialStorage.GetAvailableCapacity(
                AntGameDevelopmentWorldHost.SoilMaterialStorageFixture, soil, 10),
                Is.EqualTo(10));
            Assert.That(host.MaterialStorage.GetAvailableCapacity(
                AntGameDevelopmentWorldHost.SoilMaterialStorageFixture, stone, 10),
                Is.Zero);
            Assert.That(host.MaterialStorage.GetAvailableCapacity(
                AntGameDevelopmentWorldHost.FullMaterialStorageFixture, stone, 10),
                Is.Zero);
        }

        [Test]
        public void FilterChangePreservesContentsAndReleasesIncompatibleIncoming()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            string soil = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil);
            Vector2Int cell = AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture;
            Assert.That(host.MaterialStorage.TryReserveIncoming(
                cell, soil, 10, "carrier", 2, out _, out _), Is.True);
            Assert.That(host.MaterialStorage.TrySetFilter(
                cell, MaterialStorageFilter.Stone, out _), Is.True);
            host.MaterialStorage.TryGet(cell, out MaterialStorageSlot slot);
            Assert.That(slot.Quantity, Is.EqualTo(5));
            Assert.That(slot.ResourceTypeId, Is.EqualTo(soil));
            Assert.That(slot.ReservedIncomingQuantity, Is.Zero);
            Assert.That(host.MaterialStorage.GetAvailableCapacity(cell, soil, 10),
                Is.Zero);
        }

        [Test]
        public void DestinationUsesReachableActualPathAndStableTieBreak()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            string soil = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil);
            Assert.That(MaterialHaulingOrder.TryChooseDestination(
                host.MaterialStorage, host.GridNavigationMap,
                AntGameDevelopmentWorldHost.SoilDropFixture, soil, 10, 4,
                out Vector2Int chosen, out _, out int quantity, out string error),
                Is.True, error);
            Assert.That(chosen,
                Is.Not.EqualTo(AntGameDevelopmentWorldHost.UnreachableMaterialStorageFixture));
            Assert.That(chosen,
                Is.EqualTo(AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture));
            Assert.That(quantity, Is.EqualTo(4));
        }

        [Test]
        public void CompletedDropDeliveryConservesMaterialAndAwardsHaulingXp()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "delivery",
                out AntRecord ant, out WorldMaterialDrop source);
            int storageBefore = host.MaterialStorage.Slots.Sum(slot => slot.Quantity);
            int dropsBefore = host.MaterialDrops.GetQuantity(source.Material);
            Assert.That(order.TryArrive(order.SourceApproachCell, 0d, out _), Is.True);
            Assert.That(order.TryArrive(order.DestinationApproachCell, 0d, out _), Is.True);
            Assert.That(order.Phase, Is.EqualTo(MaterialHaulingPhase.Completed));
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
            Assert.That(host.MaterialStorage.Slots.Sum(slot => slot.Quantity),
                Is.EqualTo(storageBefore + order.Quantity));
            Assert.That(host.MaterialDrops.GetQuantity(source.Material),
                Is.EqualTo(dropsBefore - order.Quantity));
            Assert.That(ant.Skills.Get(AntSkillId.Hauling).TotalExperience,
                Is.EqualTo(order.Quantity * MaterialHaulingOrder.ExperiencePerDeliveredUnit));
        }

        [Test]
        public void CancellationBeforePickupReleasesBothReservations()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "cancel-before",
                out _, out WorldMaterialDrop source);
            order.Cancel("test");
            host.MaterialStorage.TryGet(order.DestinationCell,
                out MaterialStorageSlot destination);
            Assert.That(host.MaterialDrops.GetReservedQuantity(source.OwnershipKey),
                Is.Zero);
            Assert.That(destination.ReservedIncomingQuantity, Is.Zero);
            Assert.That(host.MaterialDrops.TryGet(source.OwnershipKey, out _), Is.True);
            Assert.That(host.MaterialTransfers.TryGet(order.TransferId,
                out MaterialTransferSnapshot transfer), Is.True);
            Assert.That(transfer.Phase,
                Is.EqualTo(MaterialTransferPhase.CancelledBeforePickup));
        }

        [Test]
        public void CancellationAfterPickupKeepsMaterialOnCarrier()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "cancel-after",
                out AntRecord ant, out _);
            Assert.That(order.TryArrive(order.SourceApproachCell, 0d, out _), Is.True);
            order.Cancel("test");
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(order.Quantity));
            host.MaterialStorage.TryGet(order.DestinationCell,
                out MaterialStorageSlot destination);
            Assert.That(destination.ReservedIncomingQuantity, Is.Zero);
            Assert.That(host.MaterialTransfers.TryGet(order.TransferId,
                out MaterialTransferSnapshot transfer), Is.True);
            Assert.That(transfer.Phase,
                Is.EqualTo(MaterialTransferPhase.RecoveryRequired));
        }

        [Test]
        public void CompletedTransferPhasesAreReplaySafe()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "replay",
                out AntRecord ant, out _);
            Assert.That(host.MaterialTransfers.TryGet(order.TransferId,
                out MaterialTransferSnapshot transfer), Is.True);
            Assert.That(transfer.Phase, Is.EqualTo(MaterialTransferPhase.Reserved));

            Assert.That(order.TryArrive(order.SourceApproachCell, 1d, out _), Is.True);
            Assert.That(host.MaterialTransfers.TryGet(order.TransferId,
                out transfer), Is.True);
            Assert.That(transfer.Phase, Is.EqualTo(MaterialTransferPhase.PickedUp));
            Assert.That(ant.Inventory.HasAllocation(order.TransferId.Value), Is.True);

            Assert.That(order.TryArrive(order.DestinationApproachCell, 2d, out _),
                Is.True);
            Assert.That(host.MaterialTransfers.TryGet(order.TransferId,
                out transfer), Is.True);
            Assert.That(transfer.Phase, Is.EqualTo(MaterialTransferPhase.Completed));
            Assert.That(ant.Inventory.HasAllocation(order.TransferId.Value), Is.False);
            double experience = ant.Skills.Get(AntSkillId.Hauling).TotalExperience;
            Assert.That(order.TryArrive(order.DestinationApproachCell, 3d, out _),
                Is.True);
            Assert.That(ant.Skills.Get(AntSkillId.Hauling).TotalExperience,
                Is.EqualTo(experience));

            int replayedCallbacks = 0;
            Assert.That(host.MaterialTransfers.TryCommitPickup(order.TransferId,
                () => { replayedCallbacks++; return true; }, out _, out _), Is.True);
            Assert.That(host.MaterialTransfers.TryCommitDelivery(order.TransferId,
                () => { replayedCallbacks++; return true; }, out _, out _), Is.True);
            Assert.That(replayedCallbacks, Is.Zero);
        }

        [Test]
        public void ScenarioRebuildInvalidatesOldTransferIdentity()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "old-scenario",
                out _, out _);
            MaterialTransferRegistry oldTransfers = host.MaterialTransfers;
            MaterialTransferId oldId = order.TransferId;

            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(oldTransfers.IsActive, Is.False);
            Assert.That(oldTransfers.TryGet(oldId, out _), Is.False);
            int callbacks = 0;
            Assert.That(oldTransfers.TryCommitPickup(oldId,
                () => { callbacks++; return true; }, out _, out _), Is.False);
            Assert.That(callbacks, Is.Zero);
            Assert.That(host.MaterialTransfers.ScenarioContextId,
                Is.Not.EqualTo(oldTransfers.ScenarioContextId));
        }

        [Test]
        public void InactiveCarrierRetainsPickedUpCargoForRecovery()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "inactive-carrier",
                out AntRecord ant, out _);
            Assert.That(order.TryArrive(order.SourceApproachCell, 0d, out _), Is.True);

            ant.MarkInactive();
            order.Cancel("carrier inactive");

            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(order.Quantity));
            Assert.That(host.MaterialTransfers.TryGet(order.TransferId,
                out MaterialTransferSnapshot transfer), Is.True);
            Assert.That(transfer.Phase,
                Is.EqualTo(MaterialTransferPhase.RecoveryRequired));
        }

        [Test]
        public void SevenUnitFixtureMovesAcrossSuccessiveCapacityBoundedTrips()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SelectionController.CreatePartialHaulingSoilFixture(
                out string error), Is.True, error);
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntSelectionCommandController.PartialHaulingSoilFixture,
                out WorldMaterialDrop source), Is.True);
            Assert.That(source.Quantity, Is.EqualTo(7));
            int storedBefore = host.MaterialStorage.Slots.Sum(slot => slot.Quantity);
            AntRecord ant = CreateRecord("successive-trips");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);

            for (int trip = 0; trip < 3 && host.MaterialDrops.TryGet(
                     source.OwnershipKey, out WorldMaterialDrop remaining); trip++)
            {
                Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                    host.GridNavigationMap, start, remaining.WorldCell,
                    out Vector2Int sourceApproach, out _, out error), Is.True, error);
                Assert.That(host.ColonyJobRegistry.TryAdd(
                    new ColonyJobId($"haul.successive.{trip}"),
                    AntWorkCategory.Hauling, remaining.WorldCell,
                    out ColonyJob job, out error), Is.True, error);
                var plan = new MaterialHaulingPlan(job,
                    MaterialHaulSourceKind.WorldDrop, "material.soil",
                    remaining.OwnershipKey);
                Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                    host.GridNavigationMap, host.MaterialDrops,
                    host.MaterialTransfers, host.MaterialStorage, ant, plan,
                    sourceApproach, start, out MaterialHaulingOrder order,
                    out error), Is.True, error);
                Assert.That(order.Quantity, Is.LessThanOrEqualTo(4));
                Assert.That(order.TryArrive(sourceApproach, trip * 2d,
                    out _), Is.True);
                Assert.That(order.TryArrive(order.DestinationApproachCell,
                    trip * 2d + 1d, out _), Is.True);
                start = order.DestinationApproachCell;
            }

            Assert.That(host.MaterialDrops.TryGet(source.OwnershipKey, out _),
                Is.False);
            Assert.That(host.MaterialStorage.Slots.Sum(slot => slot.Quantity),
                Is.EqualTo(storedBefore + 7));
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
        }

        [Test]
        public void PartialLoadLeavesSourceRemainderThenDepositsExactlyThatLoad()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialDrops.TryFindAtCell(AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop source);
            AntRecord ant = CreateRecord("partial-load");
            Assert.That(ant.Inventory.TryAdd("material.soil", 2, 10, false,
                out _, out string setupError), Is.True, setupError);
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, source.WorldCell, out Vector2Int approach, out _, out _);
            host.ColonyJobRegistry.TryAdd(new ColonyJobId("haul.partial"),
                AntWorkCategory.Hauling, source.WorldCell, out ColonyJob job, out _);
            var plan = new MaterialHaulingPlan(job, MaterialHaulSourceKind.WorldDrop,
                "material.soil", source.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                ant, plan, approach, start, out MaterialHaulingOrder order,
                out string error), Is.True, error);
            Assert.That(order.Quantity, Is.EqualTo(2));
            Assert.That(order.TryArrive(approach, 0d, out _), Is.True);
            Assert.That(host.MaterialDrops.TryGet(source.OwnershipKey,
                out WorldMaterialDrop remainder), Is.True);
            Assert.That(remainder.Quantity, Is.EqualTo(2));
            Assert.That(order.TryArrive(order.DestinationApproachCell, 0d, out _),
                Is.True);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(2));
        }

        [Test]
        public void RemovedDestinationFailsSafelyAfterPickupAndKeepsCargo()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "removed-destination",
                out AntRecord ant, out _);
            Assert.That(order.TryArrive(order.SourceApproachCell, 0d, out _), Is.True);
            Assert.That(host.MaterialStorage.Remove(order.DestinationCell), Is.True);
            Assert.That(order.TryArrive(order.DestinationApproachCell, 0d, out _),
                Is.False);
            Assert.That(order.Phase, Is.EqualTo(MaterialHaulingPhase.Failed));
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(order.Quantity));
        }

        [Test]
        public void IncompatibleFilterChangeInvalidatesDepositWithoutMaterialLoss()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder order = CreateDropOrder(host, "changed-filter",
                out AntRecord ant, out _);
            Assert.That(host.MaterialStorage.TrySetFilter(order.DestinationCell,
                MaterialStorageFilter.Stone, out _), Is.True);
            Assert.That(order.TryArrive(order.SourceApproachCell, 0d, out _), Is.True);
            Assert.That(order.TryArrive(order.DestinationApproachCell, 0d, out _),
                Is.False);
            Assert.That(ant.Inventory.TotalQuantity, Is.EqualTo(order.Quantity));
        }

        [Test]
        public void CompetingCarrierCannotClaimReservedSourceOrDestination()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            MaterialHaulingOrder first = CreateDropOrder(host, "first",
                out _, out WorldMaterialDrop source);
            AntRecord second = CreateRecord("second");
            ColonyJobId id = new("haul.second");
            host.ColonyJobRegistry.TryAdd(id, AntWorkCategory.Hauling,
                source.WorldCell, out ColonyJob job, out _);
            var plan = new MaterialHaulingPlan(job, MaterialHaulSourceKind.WorldDrop,
                MaterialResourceCatalog.GetStableId(source.Material), source.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                second, plan, first.SourceApproachCell, first.SourceApproachCell,
                out _, out _), Is.False);
        }

        [Test]
        public void SelfDepositCanOnlyBeClaimedByInventoryOwner()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            string soil = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil);
            AntRecord owner = CreateRecord("owner");
            owner.Inventory.TryAdd(soil, 2, 10, false, out _, out _);
            Vector2Int target = AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture;
            host.ColonyJobRegistry.TryAdd(new ColonyJobId("self"),
                AntWorkCategory.Hauling, target, out ColonyJob job, out _);
            var plan = new MaterialHaulingPlan(job,
                MaterialHaulSourceKind.PersonalInventory, soil,
                inventoryOwner: owner.Id);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                CreateRecord("intruder"), plan, target,
                new Vector2Int(-10, SampleWorldGenerator.MainPassageY),
                out _, out _), Is.False);
        }

        [Test]
        public void HaulingWithoutStorageStaysPendingAndLeavesDropUnreserved()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialStorage.Clear();
            host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop source);
            AntRecord ant = CreateRecord("no-storage");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            host.ColonyJobRegistry.TryAdd(new ColonyJobId("haul.no-storage"),
                AntWorkCategory.Hauling, source.WorldCell,
                out ColonyJob job, out _);
            var plan = new MaterialHaulingPlan(job,
                MaterialHaulSourceKind.WorldDrop, "material.soil",
                source.OwnershipKey);
            int quantityBefore = source.Quantity;

            Assert.That(MaterialHaulingOrder.CanStart(host.GridNavigationMap,
                host.MaterialDrops, host.MaterialStorage, ant, plan, start,
                out string reason), Is.False);
            Assert.That(reason, Does.Contain("Construction Site or Material Storage"));
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, source.WorldCell, out Vector2Int approach, out _, out _);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                ant, plan, approach, start, out _, out _), Is.False);
            Assert.That(host.MaterialDrops.TryGet(
                source.OwnershipKey, out WorldMaterialDrop unchanged), Is.True);
            Assert.That(unchanged.Quantity, Is.EqualTo(quantityBefore));
            Assert.That(host.MaterialDrops.GetReservedQuantity(source.OwnershipKey),
                Is.Zero);
            Assert.That(job.ReservationOwner.IsValid, Is.False);
        }

        [Test]
        public void GeneratedQueenBootstrapsFirstStorageFromLooseSoil()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.TryStartScenario(
                GeneratedWorldScenario.FoundingQueen, 2203, out string error),
                Is.True, error);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out error), Is.True, error);
            Assert.That(host.MaterialStorage.Count, Is.Zero);
            Assert.That(host.MaterialDrops.TryAdd("bootstrap.soil",
                ExcavatedMaterialType.Soil, 1, new Vector2Int(-2, -8),
                out WorldMaterialDrop source, out error), Is.True, error);

            ConstructionRecipe storageRecipe = host.ConstructionRecipes.Recipes
                .Single(recipe =>
                    recipe.Result.Function == BackgroundFunction.MaterialStorage);
            Assert.That(host.ConstructionDesignations.TryDesignate(
                new Vector2Int(1, -8), storageRecipe.Result.StableMaterialId,
                out ConstructionDesignation first, out error), Is.True, error);
            SelectableGridAnt queen = host.SelectionController.SelectableAnts.Single();
            Vector2Int start = new(ProceduralWorldGenerator.QueenSpawn.X,
                ProceduralWorldGenerator.QueenSpawn.Y);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, source.WorldCell,
                out Vector2Int sourceApproach, out _, out error), Is.True, error);
            Assert.That(host.ColonyJobRegistry.TryAdd(
                new ColonyJobId("haul.bootstrap"), AntWorkCategory.Hauling,
                source.WorldCell, out ColonyJob haulJob, out error), Is.True, error);
            var plan = new MaterialHaulingPlan(haulJob,
                MaterialHaulSourceKind.WorldDrop, "material.soil", source.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                host.ConstructionDesignations, queen.Record, plan, sourceApproach,
                start, out MaterialHaulingOrder haul, out error), Is.True, error);
            Assert.That(haul.DestinationKind,
                Is.EqualTo(MaterialHaulDestinationKind.ConstructionSite));
            Assert.That(haul.DestinationCell, Is.EqualTo(first.Cell));
            Assert.That(host.MaterialDrops.GetReservedQuantity(source.OwnershipKey),
                Is.EqualTo(1));
            Assert.That(haul.TryArrive(haul.SourceApproachCell, 1d,
                out Vector2Int destinationApproach), Is.True);
            Assert.That(haul.TryArrive(destinationApproach, 2d, out _), Is.True);
            Assert.That(first.Site.IsFullySupplied, Is.True);

            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, destinationApproach, first.Cell,
                out Vector2Int siteApproach, out _, out error), Is.True, error);
            Assert.That(ConstructionJobOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialStorage,
                queen.Record, first, siteApproach, first.Cell,
                host.ConstructionTransaction, out ConstructionJobOrder order,
                out error), Is.True, error);
            Assert.That(order.TryArrive(siteApproach, out _), Is.False);
            order.Advance(4d, 4d);

            Assert.That(order.Phase, Is.EqualTo(ConstructionOrderPhase.Completed));
            Assert.That(host.MaterialDrops.TryGet(source.OwnershipKey, out _), Is.False);
            Assert.That(queen.Record.Inventory.TotalQuantity, Is.Zero);
            Assert.That(first.Site.DeliveredQuantity, Is.Zero);
            Assert.That(host.MaterialStorage.TryGet(first.Cell,
                out MaterialStorageSlot completedStorage), Is.True);
            Assert.That(completedStorage.Quantity, Is.Zero);
            Assert.That(host.ConstructionTransaction.TryCommit(first, out _), Is.False,
                "Construction material must not be consumed twice.");

            Assert.That(host.MaterialDrops.TryAdd("bootstrap.second-soil",
                ExcavatedMaterialType.Soil, 1, new Vector2Int(-1, -7),
                out WorldMaterialDrop secondSource, out error), Is.True, error);
            host.ColonyJobRegistry.TryAdd(new ColonyJobId("haul.after-bootstrap"),
                AntWorkCategory.Hauling, secondSource.WorldCell,
                out ColonyJob secondJob, out error);
            var secondPlan = new MaterialHaulingPlan(secondJob,
                MaterialHaulSourceKind.WorldDrop, "material.soil",
                secondSource.OwnershipKey);
            Assert.That(MaterialHaulingOrder.CanStart(host.GridNavigationMap,
                host.MaterialDrops, host.MaterialStorage,
                host.ConstructionDesignations, queen.Record, secondPlan, first.Cell,
                out error), Is.True, error);
        }

        [Test]
        public void DisabledHaulingBlocksDynamicClaimAndPriorityReenablesIt()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithAnts();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.SetAutonomousColonyWorkEnabled(true);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 1d), Is.False);

            controller.SetAutonomousHaulingEnabled(true);
            ant.Record.WorkPriorities.TrySet(AntWorkCategory.Hauling,
                AntWorkPriority.Disabled, out _);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 10d), Is.False);
            ant.Record.WorkPriorities.TrySet(AntWorkCategory.Hauling,
                AntWorkPriority.Highest, out _);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 20d), Is.True);
            Assert.That(ant.HaulingOrder, Is.Not.Null);
        }

        [Test]
        public void ExplicitPlayerMovePreemptsHaulingAndReleasesReservations()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithAnts();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            SelectableGridAnt ant = controller.SelectableAnts[0];
            ant.Record.WorkPriorities.TrySet(AntWorkCategory.Hauling,
                AntWorkPriority.Highest, out _);
            controller.SetAutonomousHaulingEnabled(true);
            controller.SetAutonomousColonyWorkEnabled(true);
            Assert.That(controller.EvaluateAutonomousColonyWorkNow(ant, 1d), Is.True);
            MaterialHaulingOrder haul = ant.HaulingOrder;
            controller.Select(ant);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            Assert.That(haul.Phase, Is.EqualTo(MaterialHaulingPhase.Cancelled));
            host.MaterialStorage.TryGet(haul.DestinationCell,
                out MaterialStorageSlot destination);
            Assert.That(destination.ReservedIncomingQuantity, Is.Zero);
            Assert.That(host.MaterialDrops.GetReservedQuantity(haul.DropKey), Is.Zero);
        }

        [Test]
        public void CancellingSiteBoundHaulReleasesBothReservations()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            host.MaterialStorage.Clear();
            host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop source);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.Single(
                item => item.Result.Function == BackgroundFunction.Sleeping);
            Vector2Int siteCell = new(11, SampleWorldGenerator.MainPassageY);
            Assert.That(host.ConstructionDesignations.TryDesignate(siteCell,
                recipe.Result.StableMaterialId, out ConstructionDesignation site,
                out string error), Is.True, error);
            AntRecord ant = CreateRecord("site-cancel");
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            SurvivalInteractionOrder.TryMeasureApproach(host.GridNavigationMap,
                start, source.WorldCell, out Vector2Int sourceApproach, out _, out _);
            host.ColonyJobRegistry.TryAdd(new ColonyJobId("haul.site.cancel"),
                AntWorkCategory.Hauling, source.WorldCell, out ColonyJob job, out _);
            var plan = new MaterialHaulingPlan(job, MaterialHaulSourceKind.WorldDrop,
                "material.soil", source.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                host.ConstructionDesignations, ant, plan, sourceApproach, start,
                out MaterialHaulingOrder order, out error), Is.True, error);

            order.Cancel("test cancellation");

            Assert.That(host.MaterialDrops.GetReservedQuantity(source.OwnershipKey),
                Is.Zero);
            Assert.That(site.Site.GetAvailableCapacity("material.soil"),
                Is.EqualTo(recipe.RequiredQuantity));
            Assert.That(ant.Inventory.TotalQuantity, Is.Zero);
        }

        [Test]
        public void LooseGroundCellRejectsASecondResourceType()
        {
            var drops = new WorldMaterialDropRegistry();
            Vector2Int cell = new(3, -4);
            Assert.That(drops.TryAdd("soil", ExcavatedMaterialType.Soil, 1, cell,
                out _, out string error), Is.True, error);
            Assert.That(drops.TryAdd("stone", ExcavatedMaterialType.Stone, 1, cell,
                out _, out error), Is.False);
            Assert.That(error, Does.Contain("different resource types"));
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Soil), Is.EqualTo(1));
            Assert.That(drops.GetQuantity(ExcavatedMaterialType.Stone), Is.Zero);
        }

        [Test]
        public void DevelopmentResetRestoresStorageDropsAndEmptyInventories()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithAnts();
            host.MaterialStorage.TrySetFilter(
                AntGameDevelopmentWorldHost.SoilMaterialStorageFixture,
                MaterialStorageFilter.Stone, out _);
            host.MaterialDrops.Clear();
            host.SelectionController.SelectableAnts[0].Record.Inventory.TryAdd(
                "material.soil", 2, 10, false, out _, out _);
            Assert.That(host.SelectionController.ResetMaterialHaulingFixtures(
                out string message), Is.True, message);
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(2));
            Assert.That(host.SelectionController.SelectableAnts.All(
                ant => ant.Record.Inventory.TotalQuantity == 0), Is.True);
            AssertSlot(host, AntGameDevelopmentWorldHost.SoilMaterialStorageFixture,
                MaterialStorageFilter.Soil, 0);
        }

        private static MaterialHaulingOrder CreateDropOrder(
            AntGameDevelopmentWorldHost host, string id, out AntRecord ant,
            out WorldMaterialDrop source)
        {
            host.MaterialDrops.TryFindAtCell(AntGameDevelopmentWorldHost.SoilDropFixture,
                out source);
            ant = CreateRecord(id);
            Vector2Int start = new(-10, SampleWorldGenerator.MainPassageY);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, source.WorldCell,
                out Vector2Int approach, out _, out string error), Is.True, error);
            host.ColonyJobRegistry.TryAdd(new ColonyJobId($"haul.{id}"),
                AntWorkCategory.Hauling, source.WorldCell, out ColonyJob job, out _);
            var plan = new MaterialHaulingPlan(job, MaterialHaulSourceKind.WorldDrop,
                MaterialResourceCatalog.GetStableId(source.Material), source.OwnershipKey);
            Assert.That(MaterialHaulingOrder.TryCreate(host.ColonyJobRegistry,
                host.GridNavigationMap, host.MaterialDrops, host.MaterialTransfers,
                host.MaterialStorage,
                ant, plan, approach, start, out MaterialHaulingOrder order,
                out error), Is.True, error);
            return order;
        }

        private static void AssertSlot(AntGameDevelopmentWorldHost host,
            Vector2Int cell, MaterialStorageFilter filter, int quantity)
        {
            Assert.That(host.MaterialStorage.TryGet(cell,
                out MaterialStorageSlot slot), Is.True);
            Assert.That(slot.Filter, Is.EqualTo(filter));
            Assert.That(slot.Quantity, Is.EqualTo(quantity));
        }

        private static AntRecord CreateRecord(string id) => new(
            new AntId(id), "Test Ant", new ColonyId("test-colony"), AntCaste.Worker,
            new AntAttributes(5, 5, 5));

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    AntGameDevelopmentWorldHost>(true)).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }

        private static AntGameDevelopmentWorldHost OpenHostWithAnts()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string error),
                Is.True, error);
            return host;
        }
    }
}
