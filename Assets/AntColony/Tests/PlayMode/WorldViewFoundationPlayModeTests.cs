using System.Collections;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.PlayerUI;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
            double elapsed = host.SimulationClock.ElapsedSeconds;
            int residents = views.Residencies.Count;

            views.SwitchView(DevelopmentWorldView.Surface);
            Assert.That(host.PlayerHud.CurrentView, Is.EqualTo(PlayerViewContext.World));
            Assert.That(views.SurfaceCamera.enabled, Is.True);
            views.SurfaceCamera.transform.position += new Vector3(2f, 1f, 0f);
            Vector3 remembered = views.SurfaceCamera.transform.position;
            views.SwitchView(DevelopmentWorldView.Colony);
            views.SwitchView(DevelopmentWorldView.Surface);

            Assert.That(views.SurfaceCamera.transform.position, Is.EqualTo(remembered));
            Assert.That(views.Residencies.Count, Is.EqualTo(residents));
            Assert.That(host.SimulationClock.ElapsedSeconds, Is.GreaterThanOrEqualTo(elapsed));
            Assert.That(host.SimulationClock.SelectedSpeed, Is.EqualTo(SimulationSpeed.Normal));
        }
    }
}
