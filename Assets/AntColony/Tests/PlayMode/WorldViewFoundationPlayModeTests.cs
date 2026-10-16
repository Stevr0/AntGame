using System;
using System.Collections;
using AntColony.Commands;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.PlayerUI;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AntColony.Tests.PlayMode
{
    public sealed class WorldViewFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SurfaceMotorUsesSharedClockDeltaAndStopsWhilePaused()
        {
            var gameObject = new GameObject("Surface motor test");
            SurfaceNavigationMotor motor = gameObject.AddComponent<SurfaceNavigationMotor>();
            var clock = new SimulationClockState();
            Assert.That(motor.Initialize(new SurfaceWorldData(8, 8), clock,
                Vector2.zero, new SurfaceCell(1, 1)), Is.True);
            Assert.That(motor.MoveTo(new SurfaceCell(5, 1)), Is.True);
            Vector3 start = gameObject.transform.position;

            clock.Advance(1d, 1d);
            motor.Tick(clock.DeltaSeconds);
            Assert.That(gameObject.transform.position, Is.EqualTo(start));

            clock.Resume();
            clock.Advance(0.25d, 1d);
            motor.Tick(clock.DeltaSeconds);
            Assert.That(gameObject.transform.position.x, Is.GreaterThan(start.x));
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredDevelopmentSceneLoadsBothViewsWithoutStateMutation()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host, Is.Not.Null);
            TwoViewWorldCoordinator views = host.TwoViewWorld;
            Assert.That(views, Is.Not.Null);
            Assert.That(views.SurfaceWorld.Width, Is.EqualTo(64));
            Assert.That(views.Residencies.Count, Is.EqualTo(3));
            Assert.That(LayerMask.LayerToName(
                TwoViewWorldCoordinator.SurfacePresentationLayer),
                Is.EqualTo(TwoViewWorldCoordinator.SurfacePresentationLayerName));
            int surfaceMask = 1 << TwoViewWorldCoordinator.SurfacePresentationLayer;
            Assert.That(views.SurfaceCamera.cullingMask, Is.EqualTo(surfaceMask));
            Assert.That(views.ColonyCamera.cullingMask & surfaceMask, Is.Zero);
            foreach (Renderer renderer in
                views.SurfacePresentationRoot.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(renderer.gameObject.layer,
                    Is.EqualTo(TwoViewWorldCoordinator.SurfacePresentationLayer));
            }
            foreach (Renderer renderer in
                GameObject.Find("World Grid").GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(views.SurfaceCamera.cullingMask &
                    (1 << renderer.gameObject.layer), Is.Zero,
                    $"Colony renderer {renderer.name} leaked into World View.");
            }
            host.SimulationClock.Resume();
            double elapsed = host.SimulationClock.ElapsedSeconds;
            int residents = views.Residencies.Count;

            views.SwitchView(DevelopmentWorldView.Surface);
            Assert.That(host.PlayerHud.CurrentView, Is.EqualTo(PlayerViewContext.World));
            Assert.That(views.SurfaceCamera.enabled, Is.True);
            Assert.That(host.SelectionController.WorldInputEnabled, Is.False);
            Assert.That(host.SelectionController.ColonyWorldPresentationEnabled,
                Is.False);
            host.SelectionController.ToggleDevelopmentCategory(
                DevelopmentUiCategory.World);
            Assert.That(host.SelectionController.ExpandedDevelopmentCategory,
                Is.Not.EqualTo(DevelopmentUiCategory.World));
            yield return null;
            views.SurfaceCamera.transform.position += new Vector3(2f, 1f, 0f);
            Vector3 remembered = views.SurfaceCamera.transform.position;
            views.SwitchView(DevelopmentWorldView.Colony);
            views.SwitchView(DevelopmentWorldView.Surface);

            Assert.That(views.SurfaceCamera.transform.position, Is.EqualTo(remembered));
            Assert.That(views.Residencies.Count, Is.EqualTo(residents));
            Assert.That(host.SimulationClock.ElapsedSeconds, Is.GreaterThan(elapsed));
            Assert.That(host.SimulationClock.SelectedSpeed, Is.EqualTo(SimulationSpeed.Normal));
        }

        [UnityTest]
        public IEnumerator RepeatedViewSwitchesHideDynamicColonyOverlaysWithoutMutatingState()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            TwoViewWorldCoordinator views = host.TwoViewWorld;
            host.SimulationClock.Pause();

            Assert.That(host.SelectionController.TryDesignateExcavation(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, out _), Is.True);
            int excavationCount = host.ExcavationDesignations.Count;
            int constructionCount = host.ConstructionDesignations.Count;
            int dropCount = host.MaterialDrops.Count;
            int residentCount = views.Residencies.Count;

            for (int index = 0; index < 4; index++)
            {
                views.SwitchView(DevelopmentWorldView.Surface);
                Assert.That(host.SelectionController.ColonyWorldPresentationEnabled,
                    Is.False);
                Assert.That(host.PlayerHud.CommandBar.gameObject.activeSelf, Is.False);
                views.SwitchView(DevelopmentWorldView.Colony);
                Assert.That(host.SelectionController.ColonyWorldPresentationEnabled,
                    Is.True);
                Assert.That(host.PlayerHud.CommandBar.gameObject.activeSelf, Is.True);
            }

            Assert.That(host.ExcavationDesignations.Count, Is.EqualTo(excavationCount));
            Assert.That(host.ConstructionDesignations.Count, Is.EqualTo(constructionCount));
            Assert.That(host.MaterialDrops.Count, Is.EqualTo(dropCount));
            Assert.That(views.Residencies.Count, Is.EqualTo(residentCount));
            Assert.That(host.SimulationClock.IsRunning, Is.False);
        }

        [UnityTest]
        public IEnumerator GeneratedFoundingScenarioPublishesOnceAndPreservesNodeAcrossViews()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203L, out string message), Is.True, message);
            yield return null;

            Assert.That(host.ActiveScenario,
                Is.EqualTo(GeneratedWorldScenario.FoundingQueen));
            Assert.That(host.GenerationDiagnostics.IsValid, Is.True);
            Assert.That(host.WorldData.WorldSeed, Is.EqualTo(2203L));
            Assert.That(host.SelectionController.SelectableAnts.Count, Is.EqualTo(1));
            Assert.That(host.SelectionController.SelectableAnts[0].Record.Caste,
                Is.EqualTo(AntColony.Ants.AntCaste.Queen));
            Assert.That(host.MaterialStorage.Count, Is.Zero);
            Assert.That(host.MaterialDrops.Count, Is.Zero);
            Assert.That(host.TwoViewWorld.SurfaceWorld.Width, Is.EqualTo(64));
            Assert.That(host.TwoViewWorld.Residencies.Count, Is.EqualTo(1));

            var generatedSolid = new Vector2Int(4, -8);
            Assert.That(host.WorldData.GetCell(generatedSolid,
                out WorldTileCellData before), Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(before.HasSolidTile, Is.True);
            Assert.That(host.ToggleForegroundSolid(generatedSolid), Is.False,
                "Generated Node terrain cannot bypass the active Node authority.");
            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Surface);
            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Colony);
            Assert.That(host.WorldData.GetCell(generatedSolid,
                out WorldTileCellData after), Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(after.HasSolidTile, Is.True,
                "Rejected direct mutation and view switching must preserve the Node Solid.");

            SelectableGridAnt queen = host.SelectionController.SelectableAnts[0];
            AntColony.Ants.AntId queenId = queen.Record.Id;
            host.SimulationClock.Resume();
            Assert.That(host.TwoViewWorld.TryUseEntrance(queen), Is.True);
            yield return WaitFor(() => host.TwoViewWorld.Residencies.IsInRegion(
                queenId, AntRegionIds.Surface), 12f,
                "Generated queen did not reach the Surface.");
            SurfaceNavigationMotor surfaceMotor =
                queen.GetComponent<SurfaceNavigationMotor>();
            Assert.That(surfaceMotor.MoveTo(new SurfaceCell(28, 32)), Is.True);
            yield return WaitFor(() => surfaceMotor.HasArrived, 8f,
                "Generated queen did not clear the Surface arrival.");
            yield return null;
            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Surface);
            Assert.That(host.TwoViewWorld.TryUseEntrance(queen), Is.True);
            yield return WaitFor(() => host.TwoViewWorld.Residencies.IsInRegion(
                queenId, AntRegionIds.Colony), 12f,
                "Generated queen did not complete the entrance round trip.");
            Assert.That(queen.Record.Id, Is.EqualTo(queenId));
            Assert.That(host.TwoViewWorld.Entrance.IsReserved, Is.False);
        }

        [UnityTest]
        public IEnumerator EntranceCanBeReusedAfterEachArrivalCellIsCleared()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            TwoViewWorldCoordinator views = host.TwoViewWorld;
            SelectableGridAnt antA = host.SelectionController.SelectableAnts[0];
            SelectableGridAnt antB = host.SelectionController.SelectableAnts[1];
            host.SimulationClock.Resume();

            Assert.That(views.TryUseEntrance(antA), Is.True);
            yield return WaitFor(() => views.Residencies.IsInRegion(
                antA.Record.Id, AntRegionIds.Surface), 12f,
                "Ant A did not reach the Surface.");
            AssertAntPresentationLayer(antA, surface: true);
            Assert.That(views.Entrance.IsReserved, Is.False);

            SurfaceNavigationMotor surfaceA =
                antA.GetComponent<SurfaceNavigationMotor>();
            Assert.That(surfaceA.MoveTo(new SurfaceCell(10, 4)), Is.True);
            yield return WaitFor(() => surfaceA.HasArrived, 6f,
                "Ant A did not clear the Surface arrival.");
            yield return null;
            Assert.That(views.Residencies.TryGet(antA.Record.Id,
                out AntResidency movedA), Is.True);
            Assert.That(movedA.Location.SurfaceCell,
                Is.EqualTo(new SurfaceCell(10, 4)));

            views.SwitchView(DevelopmentWorldView.Colony);
            Assert.That(views.TryUseEntrance(antB), Is.True);
            yield return WaitFor(() => views.Residencies.IsInRegion(
                antB.Record.Id, AntRegionIds.Surface), 12f,
                "Ant B could not reuse the Surface-bound entrance.");
            Assert.That(views.Residencies.TryGet(antB.Record.Id,
                out AntResidency arrivedB), Is.True);
            Assert.That(movedA.AntId, Is.EqualTo(antA.Record.Id));
            Assert.That(arrivedB.AntId, Is.EqualTo(antB.Record.Id));
            Assert.That(movedA.Location, Is.Not.EqualTo(arrivedB.Location));

            SurfaceNavigationMotor surfaceB =
                antB.GetComponent<SurfaceNavigationMotor>();
            Assert.That(surfaceB.MoveTo(new SurfaceCell(11, 4)), Is.True);
            yield return WaitFor(() => surfaceB.HasArrived, 6f,
                "Ant B did not clear the Surface arrival.");
            yield return null;

            views.SwitchView(DevelopmentWorldView.Surface);
            Assert.That(views.TryUseEntrance(antA), Is.True);
            yield return WaitFor(() => views.Residencies.IsInRegion(
                antA.Record.Id, AntRegionIds.Colony), 12f,
                "Ant A did not return to the Colony.");
            AssertAntPresentationLayer(antA, surface: false);
            Assert.That(antA.IssueMoveOrder(new Vector2Int(-5, -8)), Is.True);
            yield return WaitFor(() => antA.Motor.HasArrived, 8f,
                "Ant A did not clear the Colony arrival.");
            yield return null;

            views.SwitchView(DevelopmentWorldView.Surface);
            Assert.That(views.TryUseEntrance(antB), Is.True);
            yield return WaitFor(() => views.Residencies.IsInRegion(
                antB.Record.Id, AntRegionIds.Colony), 12f,
                "Ant B could not reuse the Colony-bound entrance.");
            Assert.That(views.Entrance.IsReserved, Is.False);
            Assert.That(views.Residencies.Count, Is.EqualTo(3));
        }

        private static void AssertAntPresentationLayer(
            SelectableGridAnt ant, bool surface)
        {
            foreach (Renderer renderer in ant.GetComponentsInChildren<Renderer>(true))
            {
                if (surface)
                    Assert.That(renderer.gameObject.layer,
                        Is.EqualTo(TwoViewWorldCoordinator.SurfacePresentationLayer));
                else
                    Assert.That(renderer.gameObject.layer,
                        Is.Not.EqualTo(TwoViewWorldCoordinator.SurfacePresentationLayer));
            }
        }

        private static IEnumerator WaitFor(Func<bool> predicate, float timeout,
            string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(predicate(), Is.True, failureMessage);
        }
    }
}
