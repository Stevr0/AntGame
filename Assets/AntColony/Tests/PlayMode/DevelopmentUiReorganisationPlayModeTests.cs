using System.Collections;
using AntColony.Commands;
using AntColony.Development;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class DevelopmentUiReorganisationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SharedToolbarSwitchesWorldAndDebugWithoutOverlap()
        {
            yield return LoadScene();
            AntSelectionCommandController controller = FindController();
            DevelopmentWorldInspector inspector =
                Object.FindFirstObjectByType<DevelopmentWorldInspector>();

            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.World);
            Assert.That(inspector.DeveloperPanelGuiRect,
                Is.EqualTo(controller.DevelopmentPanelGuiRect));
            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Debug);
            Assert.That(controller.ExpandedDevelopmentCategory,
                Is.EqualTo(DevelopmentUiCategory.Debug));
            Assert.That(inspector.DeveloperPanelGuiRect,
                Is.EqualTo(controller.DevelopmentPanelGuiRect));
        }

        [UnityTest]
        public IEnumerator HideAllLeavesOnlyReopenControlAndUiClicksPreserveSelection()
        {
            yield return LoadScene();
            AntSelectionCommandController controller = FindController();
            SelectableGridAnt selected = controller.SelectableAnts[0];
            controller.Select(selected);
            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Resources);

            Vector2 guiPoint = controller.DevelopmentPanelGuiRect.center;
            Vector2 screenPoint = new(guiPoint.x, Screen.height - guiPoint.y);
            Assert.That(controller.IsScreenPointBlocked(screenPoint), Is.True);
            Assert.That(controller.TryHandleLeftClick(screenPoint), Is.False);
            Assert.That(controller.SelectedAnt, Is.SameAs(selected));

            controller.SetDevelopmentUiVisible(false);
            Assert.That(controller.ExpandedDevelopmentCategory,
                Is.EqualTo(DevelopmentUiCategory.None));
            Assert.That(controller.IsGuiPointBlocked(new Vector2(
                Screen.width * 0.5f, Screen.height * 0.5f)), Is.False);
            Assert.That(controller.IsGuiPointBlocked(
                controller.DevelopmentReopenGuiRect.center), Is.True);
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
            PlayModeSimulationTestUtility.Resume();
        }

        private static AntSelectionCommandController FindController() =>
            Object.FindFirstObjectByType<AntSelectionCommandController>();
    }
}
