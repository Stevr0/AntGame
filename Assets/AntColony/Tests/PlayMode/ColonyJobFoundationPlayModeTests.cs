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
    public sealed class ColonyJobFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneProvidesIndependentPrioritiesAndSyntheticFixtures()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.ColonyJobRegistry.Count, Is.EqualTo(5));
            SelectableGridAnt mara = host.SelectionController.SelectableAnts[0];
            SelectableGridAnt pip = host.SelectionController.SelectableAnts[1];
            mara.Record.WorkPriorities.TrySet(
                AntWorkCategory.Excavation, AntWorkPriority.Highest, out _);
            Assert.That(pip.Record.WorkPriorities.Get(AntWorkCategory.Excavation),
                Is.EqualTo(AntWorkPriority.Normal));
        }

        [UnityTest]
        public IEnumerator AutonomousAntClaimsNavigatesAndCompletesSyntheticWork()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            double xpBefore = ant.Record.Skills.Get(AntSkillId.Excavation).TotalExperience;
            controller.SetAutonomousColonyWorkEnabled(true);
            float timeout = Time.time + 35f;
            while (!host.ColonyJobRegistry.Jobs.Any(job =>
                       job.Status == ColonyJobStatus.Completed) && Time.time < timeout)
                yield return null;
            ColonyJob completed = host.ColonyJobRegistry.Jobs.FirstOrDefault(job =>
                job.Status == ColonyJobStatus.Completed);
            Assert.That(completed, Is.Not.Null);
            Assert.That(completed.Progress, Is.EqualTo(1));
            if (completed.Category == AntWorkCategory.Excavation)
                Assert.That(ant.Record.Skills.Get(AntSkillId.Excavation).TotalExperience,
                    Is.GreaterThan(xpBefore));
        }

        [UnityTest]
        public IEnumerator MultipleAntsReserveDifferentJobs()
        {
            yield return LoadScene();
            AntSelectionCommandController controller = FindHost().SelectionController;
            controller.SetAutonomousColonyWorkEnabled(true);
            float timeout = Time.time + 4f;
            while (controller.SelectableAnts.Count(ant =>
                       ant.OrderSource == AntOrderSource.AutonomousWork) < 2 &&
                   Time.time < timeout)
                yield return null;
            SelectableGridAnt[] workers = controller.SelectableAnts.Where(ant =>
                ant.OrderSource == AntOrderSource.AutonomousWork).ToArray();
            Assert.That(workers.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(workers[0].JobOrder.Job.Id,
                Is.Not.EqualTo(workers[1].JobOrder.Job.Id));
        }

        [UnityTest]
        public IEnumerator CriticalHungerInterruptsWorkAndPreservesJobProgress()
        {
            yield return LoadScene();
            AntSelectionCommandController controller = FindHost().SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.SetAutonomousColonyWorkEnabled(true);
            float timeout = Time.time + 4f;
            while (ant.OrderSource != AntOrderSource.AutonomousWork && Time.time < timeout)
                yield return null;
            ColonyJob job = ant.JobOrder.Job;
            ant.Record.TrySetNeedsForDevelopment(100, 0, 100, 20, out _);
            controller.SetAutonomousSurvivalEnabled(true);
            timeout = Time.time + 4f;
            while (ant.OrderSource != AntOrderSource.AutonomousSurvival &&
                   Time.time < timeout)
                yield return null;
            Assert.That(ant.OrderSource, Is.EqualTo(AntOrderSource.AutonomousSurvival));
            Assert.That(job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(job.ReservationOwner.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator PlayerMovePreemptsWorkAndResetRestoresFixtures()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            controller.SetAutonomousColonyWorkEnabled(true);
            float timeout = Time.time + 4f;
            while (ant.OrderSource != AntOrderSource.AutonomousWork && Time.time < timeout)
                yield return null;
            ColonyJob job = ant.JobOrder.Job;
            controller.Select(ant);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            Assert.That(job.Status, Is.EqualTo(ColonyJobStatus.Paused));
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.ColonyJobRegistry.Count, Is.EqualTo(5));
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
