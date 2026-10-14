using System.Collections;
using System.Linq;
using AntColony.Ants;
using AntColony.Development;
using AntColony.PlayerUI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class ColonyManagementPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScreenOpensWithAuthoritativeActiveAntRowsAndCloses()
        {
            yield return LoadScene();
            ColonyManagementView management = FindHost().PlayerHud.ColonyManagement;

            Assert.That(management.IsOpen, Is.False);
            management.Open();
            Assert.That(management.IsOpen, Is.True);
            Assert.That(management.DisplayedRecords.Select(record => record.DisplayName),
                Is.EquivalentTo(new[] { "Mara", "Pip", "Tala" }));
            Assert.That(management.Rows, Has.Count.EqualTo(3));
            management.Close();
            Assert.That(management.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator PriorityChangesAreIndependentAndSurviveReopening()
        {
            yield return LoadScene();
            ColonyManagementView management = FindHost().PlayerHud.ColonyManagement;
            management.Open();
            WorkPriorityRowView mara = management.Rows.Single(
                row => row.Record.DisplayName == "Mara");
            WorkPriorityRowView pip = management.Rows.Single(
                row => row.Record.DisplayName == "Pip");

            Assert.That(mara.TrySetPriority(AntWorkCategory.Excavation,
                AntWorkPriority.Highest, out string message), Is.True, message);
            Assert.That(mara.TrySetPriority(AntWorkCategory.Construction,
                AntWorkPriority.Disabled, out message), Is.True, message);
            Assert.That(mara.Record.WorkPriorities.Get(AntWorkCategory.Excavation),
                Is.EqualTo(AntWorkPriority.Highest));
            Assert.That(mara.Record.WorkPriorities.Get(AntWorkCategory.Construction),
                Is.EqualTo(AntWorkPriority.Disabled));
            Assert.That(pip.Record.WorkPriorities.Get(AntWorkCategory.Excavation),
                Is.EqualTo(AntWorkPriority.Normal));

            management.Close();
            management.Open();
            Assert.That(mara.Record.WorkPriorities.Get(AntWorkCategory.Excavation),
                Is.EqualTo(AntWorkPriority.Highest));
            Assert.That(mara.Record.WorkPriorities.Get(AntWorkCategory.Construction),
                Is.EqualTo(AntWorkPriority.Disabled));
        }

        [UnityTest]
        public IEnumerator OpeningDoesNotPauseClockAndUnloadedRecordsRemainManageable()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            ColonyManagementView management = host.PlayerHud.ColonyManagement;
            host.SimulationClock.Resume();
            Assert.That(host.SimulationClock.IsRunning, Is.True);

            var unloaded = new AntRecord(new AntId("ant-unloaded-test"), "Nia",
                host.AntRegistry.ColonyId, AntCaste.Worker,
                new AntAttributes(5, 5, 5));
            Assert.That(host.AntRegistry.TryRegister(unloaded, out string error),
                Is.True, error);
            management.RebuildRows();
            management.Open();

            Assert.That(host.SimulationClock.IsRunning, Is.True);
            WorkPriorityRowView nia = management.Rows.Single(
                row => row.Record == unloaded);
            Assert.That(nia.DisplayedActivity,
                Does.Contain("Not loaded in Colony View"));
            Assert.That(nia.TrySetPriority(AntWorkCategory.Hauling,
                AntWorkPriority.High, out string message), Is.True, message);
            Assert.That(unloaded.WorkPriorities.Get(AntWorkCategory.Hauling),
                Is.EqualTo(AntWorkPriority.High));
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
