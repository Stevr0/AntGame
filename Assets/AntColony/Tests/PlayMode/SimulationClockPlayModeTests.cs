using System.Collections;
using System.Linq;
using AntColony.Commands;
using AntColony.Development;
using AntColony.PlayerUI;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class SimulationClockPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator RestoreUnityTime()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Time.maximumDeltaTime = 0.3333333f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneStartsPausedWithNormalGameplayBundleEnabled()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();

            Assert.That(host.SimulationClock, Is.Not.Null);
            Assert.That(host.SimulationClock.IsRunning, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(host.SelectionController.AutonomousSurvivalEnabled, Is.True);
            Assert.That(host.SelectionController.AutonomousColonyWorkEnabled, Is.True);
            Assert.That(host.SelectionController.AutonomousHaulingEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator SpeedSelectionRunsAndPauseRetainsTheSelection()
        {
            yield return LoadPausedScene();
            SimulationClockController clock = FindHost().SimulationClock;

            clock.SetRunningSpeed(SimulationSpeed.VeryFast);
            yield return null;
            yield return null;
            double elapsed = clock.ElapsedHours;

            Assert.That(Time.timeScale, Is.EqualTo(3f));
            Assert.That(elapsed, Is.GreaterThan(0d));
            clock.Pause();
            yield return null;
            Assert.That(clock.SelectedSpeed, Is.EqualTo(SimulationSpeed.VeryFast));
            Assert.That(clock.ElapsedHours, Is.EqualTo(elapsed).Within(0.000001d));
            clock.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(3f));
        }

        [UnityTest]
        public IEnumerator PausedDirectOrderIsAcceptedButSchedulerDoesNotExecuteIt()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            long updateBefore = host.GridNavigationScheduler.Diagnostics.UpdateIndex;

            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            Assert.That(controller.SelectedAnt.Motor.HasPendingRequest, Is.True);
            yield return null;
            yield return null;

            Assert.That(host.GridNavigationScheduler.Diagnostics.UpdateIndex,
                Is.EqualTo(updateBefore));
            Assert.That(controller.SelectedAnt.Motor.HasPendingRequest, Is.True);
        }

        [UnityTest]
        public IEnumerator PausedDirectOrderExecutesAfterResume()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            Vector2 start = controller.SelectedAnt.Motor.Position;
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);

            host.SimulationClock.Resume();
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((controller.SelectedAnt.Motor.Position - start).sqrMagnitude < 0.01f &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That((controller.SelectedAnt.Motor.Position - start).sqrMagnitude,
                Is.GreaterThanOrEqualTo(0.01f));
        }

        [UnityTest]
        public IEnumerator PagedJobSelectionFreezesWhilePausedAndAdvancesAtThreeTimesSpeed()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            ColonyJobRegistry jobs = controller.ColonyJobs;
            jobs.Clear();
            for (int index = 0; index < 32; index++)
            {
                Assert.That(jobs.TryAdd(new ColonyJobId($"blocked-{index:00}"),
                    AntColony.Ants.AntWorkCategory.Hauling,
                    SampleWorldGenerator.GridUnreachableDestination,
                    out _, out string addError), Is.True, addError);
            }
            Assert.That(jobs.TryAdd(new ColonyJobId("reachable-33"),
                AntColony.Ants.AntWorkCategory.Hauling, new Vector2Int(-10, -8),
                out _, out string reachableError), Is.True, reachableError);

            yield return null;
            yield return null;
            Assert.That(controller.SelectableAnts.Any(ant => ant.HasActiveOrder), Is.False);

            host.SimulationClock.SetRunningSpeed(SimulationSpeed.VeryFast);
            float deadline = Time.realtimeSinceStartup + 8f;
            ColonyJob job = null;
            while (!jobs.TryGet(new ColonyJobId("reachable-33"), out job) ||
                   !job.ReservationOwner.IsValid)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "The reachable job starved after the paged scan resumed.");
                yield return null;
            }

            Assert.That(job.ReservationOwner.IsValid, Is.True);
            Assert.That(controller.SelectableAnts.Count(ant =>
                ant.JobOrder?.Job?.Id.Equals(job.Id) == true), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PausePreservesNeedsAndSimulationTime()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();
            double hunger = host.SelectionController.SelectableAnts[0]
                .Record.Needs.Hunger;
            double elapsed = host.SimulationClock.ElapsedHours;

            yield return null;
            yield return null;

            Assert.That(host.SelectionController.SelectableAnts[0]
                .Record.Needs.Hunger, Is.EqualTo(hunger));
            Assert.That(host.SimulationClock.ElapsedHours, Is.EqualTo(elapsed));
        }

        [UnityTest]
        public IEnumerator PauseDoesNotOverwriteDeveloperAutonomyOverride()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();
            GameplayModeCoordinator mode = host.GameplayMode;
            mode.SetOverride(
                GameplayAutonomySubsystem.ColonyWork,
                GameplayAutonomyOverride.ForcedDisabled);

            host.SimulationClock.Resume();
            host.SimulationClock.Pause();

            Assert.That(mode.ColonyWorkOverride,
                Is.EqualTo(GameplayAutonomyOverride.ForcedDisabled));
            Assert.That(host.SelectionController.AutonomousColonyWorkEnabled,
                Is.False);
            Assert.That(host.SelectionController.AutonomousSurvivalEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PlayerHudReplacesTimeStripAndBlocksWorldInputWhenDevUiIsHidden()
        {
            yield return LoadPausedScene();
            AntGameDevelopmentWorldHost host = FindHost();
            PlayerHudController hud = host.PlayerHud;
            Assert.That(hud, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<SimulationTimeControlStrip>(),
                Is.Null);

            host.SelectionController.SetDevelopmentUiVisible(false);
            yield return null;
            RectTransform panel = hud.SimulationControls.GetComponent<RectTransform>();
            var corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            Vector2 centre = (corners[0] + corners[2]) * 0.5f;

            Assert.That(host.SelectionController.IsScreenPointBlocked(centre), Is.True);
            Assert.That(host.SelectionController.DevelopmentUiVisible, Is.False);
        }

        private static IEnumerator LoadPausedScene()
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
