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
        public IEnumerator AutonomousAntCompletesRealExcavationAndCreatesDrop()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            // Remove synthetic demand so this assertion exercises the real job path.
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            Assert.That(controller.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _), Is.True);
            controller.SetAutonomousColonyWorkEnabled(true);
            float timeout = Time.time + 35f;
            while (host.MaterialDrops.Count == 0 && Time.time < timeout)
                yield return null;
            Assert.That(host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil),
                Is.EqualTo(1));
            host.WorldData.GetCell(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                out WorldTileCellData cell);
            Assert.That(cell.HasSolidTile, Is.False);
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
            Assert.That(host.MaterialDrops.Count, Is.Zero);
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
        }

        private static AntGameDevelopmentWorldHost FindHost() =>
            Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
    }
}
