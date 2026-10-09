using System.Collections.Generic;
using AntColony.NavigationDebug;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Verifies debug presentation data without relying on a graphics device or
    /// screenshot comparison.
    /// </summary>
    public sealed class NavigationOverlayTests
    {
        private const string BackgroundId = "background.overlay-test";
        private const string SolidId = "solid.overlay-test";

        private readonly List<Object> temporaryObjects = new();
        private BackgroundTileMaterialDefinition background;
        private SolidTileMaterialDefinition solid;
        private ChunkedWorldData worldData;
        private WorldNavigationClassifier classifier;
        private NavigationOverlayModel overlayModel;

        [SetUp]
        public void SetUp()
        {
            background = CreateDefinition<BackgroundTileMaterialDefinition>(BackgroundId);
            solid = CreateDefinition<SolidTileMaterialDefinition>(SolidId);
            var registry = new WorldMaterialRegistry(
                new[] { background },
                new[] { solid });
            worldData = new ChunkedWorldData(registry);
            worldData.CreateChunk(Vector2Int.zero, BackgroundId);
            classifier = new WorldNavigationClassifier(worldData);
            overlayModel = new NavigationOverlayModel(classifier);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = temporaryObjects.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(temporaryObjects[index]);
            }

            temporaryObjects.Clear();
        }

        [Test]
        public void OverlayCells_MatchClassifierResultsAndWorldPositions()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, SolidId);
            var cells = new List<NavigationOverlayCell>();

            overlayModel.BuildCells(new Vector2Int(-1, -1), new Vector2Int(3, 3), cells);

            Assert.That(cells, Has.Count.EqualTo(9));
            foreach (NavigationOverlayCell cell in cells)
            {
                WorldNavigationClassification expected = classifier.Classify(cell.WorldCell);
                Assert.That(cell.Classification.Occupancy, Is.EqualTo(expected.Occupancy));
                Assert.That(cell.Classification.Support, Is.EqualTo(expected.Support));
                Assert.That(
                    cell.WorldPosition,
                    Is.EqualTo(WorldTileCoordinates.CellToWorldPosition(cell.WorldCell)));
            }
        }

        [Test]
        public void Palette_UsesDistinctRequiredClassificationColours()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, SolidId);
            var cells = new List<NavigationOverlayCell>();
            overlayModel.BuildCells(Vector2Int.zero, new Vector2Int(1, 1), cells);
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Covered));

            overlayModel.BuildCells(Vector2Int.up, new Vector2Int(1, 1), cells);
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Supported));

            overlayModel.BuildCells(new Vector2Int(5, 5), new Vector2Int(1, 1), cells);
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Unsupported));

            overlayModel.BuildCells(new Vector2Int(31, 5), new Vector2Int(1, 1), cells);
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Unknown));

            overlayModel.BuildCells(new Vector2Int(32, 5), new Vector2Int(1, 1), cells);
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Unavailable));

            var uniqueColours = new HashSet<Color32>
            {
                NavigationOverlayPalette.Covered,
                NavigationOverlayPalette.Supported,
                NavigationOverlayPalette.Unsupported,
                NavigationOverlayPalette.Unknown,
                NavigationOverlayPalette.Unavailable
            };
            Assert.That(uniqueColours, Has.Count.EqualTo(5));
        }

        [Test]
        public void NegativeCells_UseIntegerCentresForVisualPositions()
        {
            var cells = new List<NavigationOverlayCell>();
            overlayModel.BuildCells(new Vector2Int(-17, -9), Vector2Int.one, cells);

            Assert.That(cells[0].WorldPosition, Is.EqualTo(new Vector2(-17f, -9f)));
        }

        [TestCase(-2, -64.5f)]
        [TestCase(-1, -32.5f)]
        [TestCase(0, -0.5f)]
        [TestCase(1, 31.5f)]
        [TestCase(2, 63.5f)]
        public void ChunkBoundaryPositions_FollowIntegerCentreConvention(
            int chunkCoordinate,
            float expectedPosition)
        {
            Assert.That(
                NavigationOverlayModel.GetChunkBoundaryPosition(chunkCoordinate),
                Is.EqualTo(expectedPosition));
        }

        [Test]
        public void RebuildingAfterSolidMutation_ChangesDisplayedClassification()
        {
            var cells = new List<NavigationOverlayCell>();
            var supportCell = new Vector2Int(5, 4);
            var inspectedCell = new Vector2Int(5, 5);
            worldData.SetSolidMaterial(supportCell, SolidId);
            overlayModel.BuildCells(inspectedCell, Vector2Int.one, cells);
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Supported));

            worldData.RemoveSolidMaterial(supportCell);
            overlayModel.BuildCells(inspectedCell, Vector2Int.one, cells);

            Assert.That(cells[0].Classification.Support,
                Is.EqualTo(NavigationSupportStatus.Unsupported));
            Assert.That(cells[0].Colour, Is.EqualTo(NavigationOverlayPalette.Unsupported));
        }

        [Test]
        public void DisplayQueries_DoNotGenerateUnavailableChunks()
        {
            int chunksBefore = worldData.ResidentChunkCount;
            var cells = new List<NavigationOverlayCell>();

            overlayModel.BuildCells(new Vector2Int(30, 4), new Vector2Int(5, 3), cells);

            Assert.That(worldData.ResidentChunkCount, Is.EqualTo(chunksBefore));
            Assert.That(worldData.ContainsChunk(Vector2Int.right), Is.False);
            Assert.That(cells.Exists(cell =>
                cell.Classification.Occupancy == NavigationCellOccupancy.Unavailable), Is.True);
        }

        [Test]
        public void DisablingRenderer_DoesNotChangeWorldOrAddColliders()
        {
            GameObject debugObject = new GameObject("Navigation Overlay Test");
            temporaryObjects.Add(debugObject);
            NavigationTestWorldHost host = debugObject.AddComponent<NavigationTestWorldHost>();
            NavigationDebugRenderer renderer = debugObject.AddComponent<NavigationDebugRenderer>();

            var serializedHost = new SerializedObject(host);
            serializedHost.FindProperty("backgroundMaterial").objectReferenceValue = background;
            serializedHost.FindProperty("solidMaterial").objectReferenceValue = solid;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            var serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("worldHost").objectReferenceValue = host;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(host.TryRebuildFixture(), Is.True);
            renderer.RefreshOverlay();
            int chunksBefore = host.WorldData.ResidentChunkCount;
            host.WorldData.GetCell(Vector2Int.zero, out WorldTileCellData cellBefore);

            renderer.SetOverlayEnabled(false);

            host.WorldData.GetCell(Vector2Int.zero, out WorldTileCellData cellAfter);
            Assert.That(host.WorldData.ResidentChunkCount, Is.EqualTo(chunksBefore));
            Assert.That(cellAfter.SolidMaterialId, Is.EqualTo(cellBefore.SolidMaterialId));
            Assert.That(debugObject.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(debugObject.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        private TDefinition CreateDefinition<TDefinition>(string materialId)
            where TDefinition : WorldTileMaterialDefinition
        {
            TDefinition definition = ScriptableObject.CreateInstance<TDefinition>();
            temporaryObjects.Add(definition);

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stableMaterialId").stringValue = materialId;
            serializedDefinition.FindProperty("displayName").stringValue = materialId;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
