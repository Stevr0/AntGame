using AntColony.Development;
using NUnit.Framework;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused tests for the integrated world's developer camera.</summary>
    public sealed class DevelopmentCameraControllerTests
    {
        private GameObject cameraObject;
        private Camera testCamera;
        private DevelopmentCameraController controller;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Development Camera Test", typeof(Camera));
            testCamera = cameraObject.GetComponent<Camera>();
            testCamera.orthographic = true;
            testCamera.orthographicSize = 30f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            controller = cameraObject.AddComponent<DevelopmentCameraController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
        }

        [TestCase(1f, 1f)]
        [TestCase(120f, 1f)]
        [TestCase(-1f, -1f)]
        [TestCase(-120f, -1f)]
        [TestCase(0f, 0f)]
        public void ScrollNormalisation_HandlesSmallAndWindowsStyleDeltas(
            float rawScroll,
            float expectedSteps)
        {
            Assert.That(
                DevelopmentCameraController.NormaliseScrollSteps(rawScroll),
                Is.EqualTo(expectedSteps).Within(0.0001f));
        }

        [Test]
        public void ScrollUpZoomsInAndScrollDownZoomsOutSmoothly()
        {
            controller.ApplyScrollZoom(1f);
            Assert.That(controller.TargetOrthographicSize, Is.EqualTo(26f));

            controller.AdvanceZoom(0.05f);
            Assert.That(testCamera.orthographicSize, Is.LessThan(30f));
            Assert.That(testCamera.orthographicSize, Is.GreaterThan(26f));

            controller.ApplyScrollZoom(-120f);
            Assert.That(controller.TargetOrthographicSize, Is.EqualTo(30f));
            controller.AdvanceZoom(0.2f);
            Assert.That(testCamera.orthographicSize, Is.GreaterThan(26f));
        }

        [Test]
        public void ZoomTarget_IsClampedToAuthoredLimits()
        {
            for (int index = 0; index < 100; index++)
            {
                controller.ApplyScrollZoom(120f);
            }

            Assert.That(
                controller.TargetOrthographicSize,
                Is.EqualTo(controller.MinimumOrthographicSize));

            for (int index = 0; index < 100; index++)
            {
                controller.ApplyScrollZoom(-120f);
            }

            Assert.That(
                controller.TargetOrthographicSize,
                Is.EqualTo(controller.MaximumOrthographicSize));
        }

        [Test]
        public void PanMovement_DoesNotChangeZoom()
        {
            controller.ApplyPan(Vector2.right, 0.5f);

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(12f));
            Assert.That(cameraObject.transform.position.y, Is.Zero);
            Assert.That(testCamera.orthographicSize, Is.EqualTo(30f));
            Assert.That(controller.TargetOrthographicSize, Is.EqualTo(30f));
        }

        [Test]
        public void ReturnHome_RestoresPositionCurrentZoomAndZoomTarget()
        {
            controller.ApplyPan(new Vector2(1f, 1f), 1f);
            controller.ApplyScrollZoom(120f);
            controller.AdvanceZoom(0.05f);

            controller.ReturnHome();

            Assert.That(cameraObject.transform.position, Is.EqualTo(
                new Vector3(0f, 0f, -10f)));
            Assert.That(testCamera.orthographicSize, Is.EqualTo(30f));
            Assert.That(controller.TargetOrthographicSize, Is.EqualTo(30f));
        }

        [Test]
        public void ViewportAwareBoundsConstrainPanAndRememberedState()
        {
            testCamera.aspect = 2f;
            controller.ConfigureBounds(new Vector2(-20f, -10f),
                new Vector2(20f, 10f), true);
            controller.SetCameraState(new Vector3(100f, 100f, -10f), 4f);

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(12f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(6f));
            Assert.That(testCamera.orthographicSize, Is.EqualTo(4f));
            Assert.That(controller.TargetOrthographicSize, Is.EqualTo(4f));
        }
    }
}
