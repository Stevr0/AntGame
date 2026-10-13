using AntColony.Commands;
using NUnit.Framework;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused layout/state checks for the development IMGUI toolbar.</summary>
    public sealed class DevelopmentUiLayoutTests
    {
        private GameObject root;
        private AntSelectionCommandController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Development UI Test");
            controller = root.AddComponent<AntSelectionCommandController>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void CategorySwitchingKeepsOnlyOnePanelExpanded()
        {
            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Ant);
            Assert.That(controller.ExpandedDevelopmentCategory,
                Is.EqualTo(DevelopmentUiCategory.Ant));

            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Resources);
            Assert.That(controller.ExpandedDevelopmentCategory,
                Is.EqualTo(DevelopmentUiCategory.Resources));
        }

        [Test]
        public void SelectingExpandedCategoryCollapsesPanel()
        {
            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Jobs);
            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Jobs);

            Assert.That(controller.ExpandedDevelopmentCategory,
                Is.EqualTo(DevelopmentUiCategory.None));
        }

        [Test]
        public void HideAllCollapsesEverythingAndReopenControlRemainsBlocked()
        {
            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.Debug);
            controller.SetDevelopmentUiVisible(false);

            Assert.That(controller.DevelopmentUiVisible, Is.False);
            Assert.That(controller.ExpandedDevelopmentCategory,
                Is.EqualTo(DevelopmentUiCategory.None));
            Assert.That(controller.IsGuiPointBlocked(
                controller.DevelopmentReopenGuiRect.center), Is.True);
            Assert.That(controller.IsGuiPointBlocked(new Vector2(300f, 300f)),
                Is.False);

            controller.SetDevelopmentUiVisible(true);
            Assert.That(controller.DevelopmentUiVisible, Is.True);
        }

        [TestCase(320, 200)]
        [TestCase(1024, 768)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1080)]
        public void ToolbarSummaryAndPanelStayInsideGameView(int width, int height)
        {
            AssertInside(AntSelectionCommandController.CalculateToolbarRect(
                width, height), width, height);
            AssertInside(AntSelectionCommandController.CalculateSummaryRect(
                width, height), width, height);
            AssertInside(AntSelectionCommandController.CalculatePanelRect(
                width, height), width, height);
        }

        [Test]
        public void VisibleUiBlocksToolbarSummaryAndOnlyTheActivePanel()
        {
            Assert.That(controller.IsGuiPointBlocked(
                controller.DevelopmentToolbarGuiRect.center), Is.True);
            Assert.That(controller.IsGuiPointBlocked(
                controller.DevelopmentSummaryGuiRect.center), Is.True);
            Assert.That(controller.IsGuiPointBlocked(
                controller.DevelopmentPanelGuiRect.center), Is.False);

            controller.ToggleDevelopmentCategory(DevelopmentUiCategory.World);
            Assert.That(controller.IsGuiPointBlocked(
                controller.DevelopmentPanelGuiRect.center), Is.True);
        }

        private static void AssertInside(Rect rect, int width, int height)
        {
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(height));
            Assert.That(rect.width, Is.GreaterThan(0f));
            Assert.That(rect.height, Is.GreaterThan(0f));
        }
    }
}
