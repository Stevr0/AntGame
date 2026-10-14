using System.Collections;
using System.Linq;
using AntColony.Commands;
using AntColony.Development;
using AntColony.PlayerUI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class PlayerHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredHudLoadsWithAuthoritativeRosterAndHiddenDeveloperUi()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            PlayerHudController hud = host.PlayerHud;

            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.IsReady, Is.True);
            Assert.That(hud.CurrentView, Is.EqualTo(PlayerViewContext.Colony));
            Assert.That(host.SelectionController.DevelopmentUiVisible, Is.False);
            Assert.That(hud.AntRoster.DisplayedRecords.Select(record => record.DisplayName),
                Is.EquivalentTo(new[] { "Mara", "Pip", "Tala" }));
            Assert.That(Object.FindObjectsByType<EventSystem>(
                FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RosterAndWorldSelectionShareTheSameAntRecord()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            PlayerHudController hud = host.PlayerHud;
            var pip = hud.AntRoster.DisplayedRecords.Single(
                record => record.DisplayName == "Pip");

            Assert.That(hud.AntRoster.SelectRecord(pip), Is.True);
            Assert.That(host.SelectionController.SelectedAnt.Record, Is.SameAs(pip));
            Assert.That(host.AntRegistry.Count, Is.EqualTo(3));
            hud.SelectedAntInspector.Refresh();
            Assert.That(hud.SelectedAntInspector.DisplayedText, Does.Contain("Pip"));

            SelectableGridAnt tala = host.SelectionController.SelectableAnts.Single(
                ant => ant.DisplayName == "Tala");
            host.SelectionController.Select(tala);
            hud.AntRoster.Refresh();
            Assert.That(host.SelectionController.SelectedAnt.Record,
                Is.SameAs(tala.Record));
            Assert.That(host.AntRegistry.Count, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator PlayerControlsUseExistingClockAndExposeSixColonyRecipes()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            PlayerHudController hud = host.PlayerHud;

            Assert.That(hud.SimulationControls.SimulationClock,
                Is.SameAs(host.SimulationClock));
            Assert.That(hud.SimulationControls.DisplayedTime,
                Does.Contain("Day 1").And.Contain("08:00"));
            Assert.That(hud.CommandBar.VisibleRecipeCount, Is.EqualTo(6));
            Assert.That(hud.CommandBar.ViewContext,
                Is.EqualTo(PlayerViewContext.Colony));
        }

        [UnityTest]
        public IEnumerator ResourceSummarySeparatesPhysicalOwnershipCategories()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            ColonyResourceSummaryView view = host.PlayerHud.ResourceSummary;
            view.Refresh();

            Assert.That(view.DisplayedText, Does.Contain("stored"));
            Assert.That(view.DisplayedText, Does.Contain("carried"));
            Assert.That(view.DisplayedText, Does.Contain("loose"));
            Assert.That(view.DisplayedText, Does.Contain("staged"));
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
        }

        private static AntGameDevelopmentWorldHost FindHost()
        {
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host, Is.Not.Null);
            return host;
        }
    }
}
