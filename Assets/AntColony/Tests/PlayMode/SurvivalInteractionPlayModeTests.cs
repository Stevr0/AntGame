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
    public sealed class SurvivalInteractionPlayModeTests
    {
        private const string SceneName = "AntGameDevelopment";

        [UnityTest]
        public IEnumerator SceneBuildsDeterministicPhysicalSurvivalFixtures()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.SurvivalFixtureCells, Has.Count.EqualTo(8));
            Assert.That(host.SurvivalInteractionWorld.StorageSlots.Count(), Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator ContextEatOrder_UsesProductionMotorAndReachesInteraction()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            controller.SelectableAnts[0].Record.TrySetNeedsForDevelopment(
                100, 20, 100, 20, out _);
            Assert.That(controller.TryIssueContextOrder(host.SurvivalFixtureCells[0]), Is.True);

            float timeout = Time.time + 20f;
            while (controller.SelectedAnt.HasActiveOrder && Time.time < timeout)
                yield return null;

            Assert.That(controller.SelectedAnt.HasActiveOrder, Is.False);
            Assert.That(controller.SelectedAnt.Record.Needs.Hunger, Is.GreaterThan(20));
        }

        [UnityTest]
        public IEnumerator ReplacingInteractionWithMove_ReleasesResourceReservation()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            Assert.That(controller.TryIssueContextOrder(host.SurvivalFixtureCells[0]), Is.True);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            host.SurvivalInteractionWorld.TryGetStorage(
                host.SurvivalFixtureCells[0], out SurvivalStorageSlot slot);
            Assert.That(slot.ReservedQuantity, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SelectionChange_DoesNotCancelAnotherAntsInteraction()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt mara = controller.SelectableAnts[0];
            controller.Select(mara);
            Assert.That(controller.TryIssueContextOrder(host.SurvivalFixtureCells[0]), Is.True);
            controller.Select(controller.SelectableAnts[1]);
            Assert.That(mara.HasActiveOrder, Is.True);
        }

        [UnityTest]
        public IEnumerator OrdinaryContextOrder_RemainsAMoveOrder()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            Assert.That(controller.TryIssueContextOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            Assert.That(controller.SelectedAnt.InteractionOrder, Is.Null);
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        private static AntGameDevelopmentWorldHost FindHost() =>
            Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
    }
}
