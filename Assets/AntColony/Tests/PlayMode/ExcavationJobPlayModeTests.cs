using System.Collections;
using System.Linq;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class ExcavationJobPlayModeTests
    {
        [UnityTest]
        public IEnumerator ExcavateModeDesignatesWithoutChangingSelection()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt selected = controller.SelectableAnts[0];
            controller.Select(selected);
            controller.SetDesignationMode(WorldDesignationMode.Excavate);
            Assert.That(controller.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _), Is.True);
            Assert.That(controller.SelectedAnt, Is.SameAs(selected));
            controller.SetDesignationMode(WorldDesignationMode.None);
            Assert.That(controller.DesignationMode, Is.EqualTo(WorldDesignationMode.None));
        }

        [UnityTest]
        public IEnumerator AutonomousAntCompletesExcavationAndLeavesLooseOutput()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            // Remove synthetic demand so this assertion exercises the real job path.
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            int initialSoil = host.MaterialDrops.GetQuantity(
                ExcavatedMaterialType.Soil);
            Assert.That(controller.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _), Is.True);
            controller.SetAutonomousColonyWorkEnabled(true);
            float timeout = Time.time + 35f;
            WorldTileCellData cell;
            host.WorldData.GetCell(AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                out cell);
            while (cell.HasSolidTile && Time.time < timeout)
            {
                yield return null;
                host.WorldData.GetCell(
                    AntGameDevelopmentWorldHost.ExcavationSoilFixture, out cell);
            }
            Assert.That(cell.HasSolidTile, Is.False);
            Assert.That(controller.SelectableAnts.All(
                ant => ant.Record.Inventory.TotalQuantity == 0), Is.True);
            Assert.That(host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil),
                Is.EqualTo(initialSoil + 1));
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                out WorldMaterialDrop drop), Is.True);
            Assert.That(drop.Material, Is.EqualTo(ExcavatedMaterialType.Soil));
            Assert.That(drop.Quantity, Is.EqualTo(1));
            Assert.That(host.ExcavationDesignations.Designations.Single().State,
                Is.EqualTo(ExcavationDesignationState.Completed));
        }

        [UnityTest]
        public IEnumerator CancellationBeforeCompletionCreatesNoDrop()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.SelectionController.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _), Is.True);
            Assert.That(host.SelectionController.TryCancelExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _), Is.True);
            yield return null;
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(2),
                "Cancellation must leave only the two authored pickup fixtures.");
        }

        [UnityTest]
        public IEnumerator CancelledSolidCanBeDesignatedAgainInTheRunningScene()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            Assert.That(host.SelectionController.TryDesignateExcavation(cell,
                out string error), Is.True, error);
            Assert.That(host.SelectionController.TryCancelExcavation(cell,
                out error), Is.True, error);
            Assert.That(host.SelectionController.TryDesignateExcavation(cell,
                out error), Is.True, error);
            yield return null;
            Assert.That(host.ExcavationDesignations.Designations.Single().State,
                Is.EqualTo(ExcavationDesignationState.Pending));
        }

        [UnityTest]
        public IEnumerator WorldResetRestoresIndependentEmptyInventoriesAndDropFixtures()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;

            Assert.That(controller.SelectableAnts.Count, Is.EqualTo(3));
            Assert.That(controller.SelectableAnts.All(
                ant => ant.Record.Inventory.TotalQuantity == 0), Is.True);
            Assert.That(controller.SelectableAnts.Select(
                ant => ant.Record.Inventory).Distinct().Count(), Is.EqualTo(3));
            controller.SelectableAnts[0].Record.Inventory.TryAdd(
                MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil),
                3, MaterialResourceCatalog.ProvisionalMaximumStack, false,
                out _, out _);
            host.MaterialDrops.Clear();

            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SelectionController.SelectableAnts.All(
                ant => ant.Record.Inventory.TotalQuantity == 0), Is.True);
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator PlayerPickupAndDeveloperDropConserveMaterial()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.Select(ant);
            Assert.That(host.MaterialDrops.TryFindAtCell(
                AntGameDevelopmentWorldHost.SoilDropFixture,
                out WorldMaterialDrop source), Is.True);
            int initialWorldQuantity = host.MaterialDrops.GetQuantity(
                ExcavatedMaterialType.Soil);

            Assert.That(controller.TryIssueMaterialPickupOrder(source), Is.True);
            float timeout = Time.time + 20f;
            while (ant.Record.Inventory.TotalQuantity == 0 && Time.time < timeout)
                yield return null;
            Assert.That(ant.Record.Inventory.TotalQuantity, Is.GreaterThan(0));
            int carried = ant.Record.Inventory.TotalQuantity;
            Assert.That(controller.TryDropSelectedInventory(0, carried,
                out string error), Is.True, error);
            Assert.That(ant.Record.Inventory.TotalQuantity, Is.Zero);
            Assert.That(host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil),
                Is.EqualTo(initialWorldQuantity));
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
