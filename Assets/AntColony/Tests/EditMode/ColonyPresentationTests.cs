using System.Linq;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused non-visual rules checks for Milestone 3.5A.</summary>
    public sealed class ColonyPresentationTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private static readonly Vector2Int ConstructionSite =
            new(11, SampleWorldGenerator.MainPassageY);

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void ExcavationHoverUsesAuthoritativeEligibilityAndReason()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.SetDesignationMode(WorldDesignationMode.Excavate);

            controller.UpdateDesignationPreview(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, false);
            Assert.That(controller.DesignationPreview.Single().State,
                Is.EqualTo(DesignationPreviewState.Valid));

            controller.UpdateDesignationPreview(
                new Vector2Int(-10, SampleWorldGenerator.MainPassageY), false);
            Assert.That(controller.DesignationPreview.Single().State,
                Is.EqualTo(DesignationPreviewState.Invalid));
            Assert.That(controller.PreviewDiagnostic, Does.Contain("excavatable"));
        }

        [Test]
        public void ExcavationDragPreviewIsBoundedAndShowsExistingDesignation()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.SetDesignationMode(WorldDesignationMode.Excavate);
            host.ExcavationDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.ExcavationAreaMinimum, out _, out _);

            controller.BeginDesignationDragPreview(
                AntGameDevelopmentWorldHost.ExcavationAreaMinimum);
            controller.UpdateDesignationPreview(
                AntGameDevelopmentWorldHost.ExcavationAreaMaximum, true);

            Assert.That(controller.DesignationPreview, Has.Count.EqualTo(3));
            Assert.That(controller.DesignationPreview.Count(cell =>
                cell.State == DesignationPreviewState.ExistingDesignation),
                Is.EqualTo(1));
        }

        [Test]
        public void OverlayGeometryCoversTheWholeCameraAlignedWorldCell()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Camera camera = host.SelectionController.WorldCamera;
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;

            Assert.That(DesignationOverlayGeometry.TryGetCellGuiRect(camera, cell,
                1000f, out Rect rectangle), Is.True);
            Vector3 minimum = camera.WorldToScreenPoint(
                new Vector3(cell.x, cell.y, 0f));
            Vector3 maximum = camera.WorldToScreenPoint(
                new Vector3(cell.x + 1f, cell.y + 1f, 0f));
            Assert.That(rectangle.width,
                Is.EqualTo(Mathf.Abs(maximum.x - minimum.x)).Within(0.01f));
            Assert.That(rectangle.height,
                Is.EqualTo(Mathf.Abs(maximum.y - minimum.y)).Within(0.01f));
            Assert.That(DesignationOverlayGeometry.BorderThickness, Is.GreaterThan(0f));
            Assert.That(Mathf.Min(rectangle.width, rectangle.height),
                Is.GreaterThan(DesignationOverlayGeometry.BorderThickness * 2f));
        }

        [Test]
        public void ConstructionPreviewReflectsRecipeCoverageAndOccupancy()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.First();
            AntSelectionCommandController controller = host.SelectionController;
            Assert.That(controller.TrySelectConstructionRecipe(recipe.StableId), Is.True);

            controller.UpdateDesignationPreview(ConstructionSite, false);
            Assert.That(controller.DesignationPreview.Single().State,
                Is.EqualTo(DesignationPreviewState.Valid));
            Assert.That(controller.SelectedConstructionRecipe, Is.SameAs(recipe));

            controller.UpdateDesignationPreview(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, false);
            Assert.That(controller.DesignationPreview.Single().State,
                Is.EqualTo(DesignationPreviewState.Invalid));

            controller.UpdateDesignationPreview(host.SurvivalFixtureCells[0], false);
            Assert.That(controller.DesignationPreview.Single().State,
                Is.EqualTo(DesignationPreviewState.Invalid));
        }

        [Test]
        public void ToolChangeClearsPreview()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.SetDesignationMode(WorldDesignationMode.Excavate);
            controller.UpdateDesignationPreview(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, false);

            controller.SetDesignationMode(WorldDesignationMode.None);

            Assert.That(controller.DesignationPreview, Is.Empty);
            Assert.That(controller.HasHoveredDesignationCell, Is.False);
        }

        [Test]
        public void UiBlockedGestureClearsPreviewAndCannotRemainInDragState()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.SetDesignationMode(WorldDesignationMode.Excavate);
            controller.BeginDesignationDragPreview(
                AntGameDevelopmentWorldHost.ExcavationAreaMinimum);
            Assert.That(controller.IsDesignationDragActive, Is.True);
            Assert.That(controller.DesignationPreview, Is.Not.Empty);

            controller.CancelDesignationDragPreview();

            Assert.That(controller.IsDesignationDragActive, Is.False);
            Assert.That(controller.DesignationPreview, Is.Empty);
            Assert.That(controller.HasHoveredDesignationCell, Is.False);
        }

        [Test]
        public void PersistentOverlayRuleClearsCancelledExcavation()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            host.ExcavationDesignations.TryDesignate(cell,
                out ExcavationDesignation designation, out _);
            Assert.That(DesignationOverlayRules.IsVisible(designation), Is.True);

            host.ExcavationDesignations.TryCancel(cell, out _);

            Assert.That(DesignationOverlayRules.IsVisible(designation), Is.False);
        }

        [Test]
        public void WorldRebindClearsPreview()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            controller.SetDesignationMode(WorldDesignationMode.Excavate);
            controller.UpdateDesignationPreview(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture, false);

            Assert.That(host.RebuildWorld(), Is.True);

            Assert.That(controller.DesignationPreview, Is.Empty);
            Assert.That(controller.HasHoveredDesignationCell, Is.False);
        }

        [Test]
        public void RightClickCancelsOnlyCompatibleActiveDesignation()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ExcavationDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                out ExcavationDesignation excavation, out _);
            ConstructionRecipe recipe = host.ConstructionRecipes.Recipes.First();
            host.ConstructionDesignations.TryDesignate(ConstructionSite,
                recipe.Result.StableMaterialId,
                out ConstructionDesignation construction, out _);

            controller.SetDesignationMode(WorldDesignationMode.Excavate);
            Assert.That(controller.TryHandleDesignationRightClick(
                ConstructionSite), Is.False);
            Assert.That(construction.State,
                Is.Not.EqualTo(ConstructionDesignationState.Cancelled));
            Assert.That(controller.TryHandleDesignationRightClick(
                excavation.Cell), Is.True);
            Assert.That(excavation.State,
                Is.EqualTo(ExcavationDesignationState.Cancelled));
        }

        [Test]
        public void EmptyRightClickDoesNotChangeOtherJobs()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ExcavationDesignations.TryDesignate(
                AntGameDevelopmentWorldHost.ExcavationSoilFixture,
                out ExcavationDesignation excavation, out _);
            controller.SetDesignationMode(WorldDesignationMode.Excavate);

            Assert.That(controller.TryHandleDesignationRightClick(
                new Vector2Int(-10, SampleWorldGenerator.MainPassageY)), Is.False);
            Assert.That(excavation.State,
                Is.Not.EqualTo(ExcavationDesignationState.Cancelled));
        }

        [TestCase("Solid_Soil", "Colony_R02_C01")]
        [TestCase("Background_UnpreparedEarth", "Colony_R02_C01")]
        [TestCase("Solid_Stone", "Colony_R04_C01")]
        [TestCase("Background_UnpreparedStone", "Colony_R04_C01")]
        public void RuntimeArtMappingUsesOnlyReviewedColliderFreeCandidates(
            string tileName, string expectedSprite)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(
                $"Assets/AntColony/Art/WorldTiles/{tileName}.asset");

            Assert.That(tile, Is.Not.Null);
            Assert.That(tile.sprite.name, Is.EqualTo(expectedSprite));
            Assert.That(tile.colliderType, Is.EqualTo(Tile.ColliderType.None));
        }

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    AntGameDevelopmentWorldHost>(true)).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            return host;
        }
    }
}
