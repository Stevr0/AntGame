using System.Collections.Generic;
using System.Text.RegularExpressions;
using AntColony.World;
using AntColony.WorldPresentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Verifies that Tilemaps remain a one-way projection of authoritative world
    /// data and preserve the project's integer-centred coordinate convention.
    /// </summary>
    public sealed class WorldTilemapRendererTests
    {
        private const string BackgroundId = "background.tilemap-test";
        private const string SolidId = "solid.tilemap-test";
        private const string NonBlockingSolidId = "solid.non-blocking-test";

        private readonly List<Object> temporaryObjects = new();
        private GameObject projectionRoot;
        private Tilemap backgroundTilemap;
        private Tilemap foregroundTilemap;
        private WorldTilemapRenderer renderer;
        private Tile backgroundVisual;
        private Tile solidVisual;
        private Tile nonBlockingVisual;
        private BackgroundTileMaterialDefinition backgroundDefinition;
        private SolidTileMaterialDefinition solidDefinition;
        private SolidTileMaterialDefinition nonBlockingDefinition;
        private ChunkedWorldData worldData;

        [SetUp]
        public void SetUp()
        {
            backgroundVisual = CreateTile("Background Test Tile");
            solidVisual = CreateTile("Solid Test Tile");
            nonBlockingVisual = CreateTile("Non-blocking Solid Test Tile");
            backgroundDefinition = CreateDefinition<BackgroundTileMaterialDefinition>(
                BackgroundId,
                backgroundVisual);
            solidDefinition = CreateDefinition<SolidTileMaterialDefinition>(
                SolidId,
                solidVisual);
            nonBlockingDefinition = CreateDefinition<SolidTileMaterialDefinition>(
                NonBlockingSolidId,
                nonBlockingVisual);
            SetBoolean(nonBlockingDefinition, "blocksMovement", false);

            worldData = CreateWorld(
                backgroundDefinition,
                solidDefinition,
                nonBlockingDefinition);
            Assert.That(
                worldData.CreateChunk(Vector2Int.zero, BackgroundId),
                Is.EqualTo(WorldChunkCreationResult.Success));

            projectionRoot = new GameObject(
                "Tilemap Projection Test",
                typeof(Grid),
                typeof(WorldTilemapRenderer));
            temporaryObjects.Add(projectionRoot);
            projectionRoot.transform.position = new Vector3(-0.5f, -0.5f, 0f);

            backgroundTilemap = CreateTilemapChild("Background Tilemap", 0);
            foregroundTilemap = CreateTilemapChild("Foreground Solid Tilemap", 1);
            renderer = projectionRoot.GetComponent<WorldTilemapRenderer>();
            renderer.BindWorldData(worldData);
            renderer.SetTilemapReferences(backgroundTilemap, foregroundTilemap);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = temporaryObjects.Count - 1; index >= 0; index--)
            {
                if (temporaryObjects[index] != null)
                {
                    Object.DestroyImmediate(temporaryObjects[index]);
                }
            }

            temporaryObjects.Clear();
        }

        [Test]
        public void BackgroundVisual_MapsFromAuthoritativeBackgroundMaterial()
        {
            WorldTilemapRefreshResult result = renderer.RefreshCell(Vector2Int.zero);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(backgroundTilemap.GetTile(Vector3Int.zero), Is.SameAs(backgroundVisual));
            Assert.That(foregroundTilemap.GetTile(Vector3Int.zero), Is.Null);
        }

        [Test]
        public void ForegroundVisual_MapsFromSolidPresence()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, SolidId);

            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(backgroundTilemap.GetTile(Vector3Int.zero), Is.SameAs(backgroundVisual));
            Assert.That(foregroundTilemap.GetTile(Vector3Int.zero), Is.SameAs(solidVisual));
        }

        [Test]
        public void RemovingSolid_ClearsOnlyForegroundAndPreservesBackground()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, SolidId);
            renderer.RefreshCell(Vector2Int.zero);

            worldData.RemoveSolidMaterial(Vector2Int.zero);
            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(backgroundTilemap.GetTile(Vector3Int.zero), Is.SameAs(backgroundVisual));
            Assert.That(foregroundTilemap.GetTile(Vector3Int.zero), Is.Null);
            worldData.GetCell(Vector2Int.zero, out WorldTileCellData cell);
            Assert.That(cell.BackgroundMaterialId, Is.EqualTo(BackgroundId));
            Assert.That(cell.HasSolidTile, Is.False);
        }

        [Test]
        public void NonBlockingSolid_StillReceivesForegroundVisual()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, NonBlockingSolidId);
            var classifier = new WorldNavigationClassifier(worldData);

            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(
                foregroundTilemap.GetTile(Vector3Int.zero),
                Is.SameAs(nonBlockingVisual));
            Assert.That(
                classifier.Classify(Vector2Int.zero).Occupancy,
                Is.EqualTo(NavigationCellOccupancy.Exposed));
        }

        [Test]
        public void DifferentSolidIds_ResolveConfiguredVisuals()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, SolidId);
            worldData.SetSolidMaterial(Vector2Int.right, NonBlockingSolidId);

            renderer.RefreshCell(Vector2Int.zero);
            renderer.RefreshCell(Vector2Int.right);

            Assert.That(foregroundTilemap.GetTile(Vector3Int.zero), Is.SameAs(solidVisual));
            Assert.That(
                foregroundTilemap.GetTile(Vector3Int.right),
                Is.SameAs(nonBlockingVisual));
        }

        [Test]
        public void MissingSolidVisual_IsDiagnosedWithoutChangingWorldData()
        {
            SolidTileMaterialDefinition missingVisual =
                CreateDefinition<SolidTileMaterialDefinition>("solid.no-visual", null);
            ChunkedWorldData missingVisualWorld = CreateWorld(
                backgroundDefinition,
                solidDefinition,
                missingVisual);
            missingVisualWorld.CreateChunk(Vector2Int.zero, BackgroundId);
            missingVisualWorld.SetSolidMaterial(Vector2Int.zero, "solid.no-visual");
            renderer.BindWorldData(missingVisualWorld);
            LogAssert.Expect(
                LogType.Warning,
                new Regex("MissingSolidVisual.*solid.no-visual"));

            WorldTilemapRefreshResult result = renderer.RefreshCell(Vector2Int.zero);

            Assert.That(result.Status, Is.EqualTo(WorldTilemapRefreshStatus.MissingSolidVisual));
            Assert.That(foregroundTilemap.GetTile(Vector3Int.zero), Is.Null);
            missingVisualWorld.GetCell(Vector2Int.zero, out WorldTileCellData cell);
            Assert.That(cell.SolidMaterialId, Is.EqualTo("solid.no-visual"));
        }

        [Test]
        public void MissingBackgroundVisual_IsDiagnosedWithoutChangingWorldData()
        {
            BackgroundTileMaterialDefinition missingVisual =
                CreateDefinition<BackgroundTileMaterialDefinition>(
                    "background.no-visual",
                    null);
            ChunkedWorldData missingVisualWorld = CreateWorld(
                missingVisual,
                solidDefinition);
            missingVisualWorld.CreateChunk(Vector2Int.zero, "background.no-visual");
            renderer.BindWorldData(missingVisualWorld);
            LogAssert.Expect(
                LogType.Warning,
                new Regex("MissingBackgroundVisual.*background.no-visual"));

            WorldTilemapRefreshResult result = renderer.RefreshCell(Vector2Int.zero);

            Assert.That(
                result.Status,
                Is.EqualTo(WorldTilemapRefreshStatus.MissingBackgroundVisual));
            Assert.That(backgroundTilemap.GetTile(Vector3Int.zero), Is.Null);
            missingVisualWorld.GetCell(Vector2Int.zero, out WorldTileCellData cell);
            Assert.That(cell.BackgroundMaterialId, Is.EqualTo("background.no-visual"));
        }

        [Test]
        public void UnknownMaterialId_CannotResolveOrRenderAsValidVisual()
        {
            WorldTilemapRefreshResult result = WorldTilemapRenderer.ResolveSolidVisual(
                worldData,
                "solid.unknown",
                Vector2Int.zero,
                out TileBase visual);

            Assert.That(result.Status, Is.EqualTo(WorldTilemapRefreshStatus.UnknownSolidMaterial));
            Assert.That(visual, Is.Null);
            Assert.That(
                worldData.SetSolidMaterial(Vector2Int.zero, "solid.unknown"),
                Is.EqualTo(WorldCellMutationResult.InvalidMaterial));
        }

        [Test]
        public void MissingChunkRefresh_DoesNotGenerateWorldData()
        {
            int chunksBefore = worldData.ResidentChunkCount;
            LogAssert.Expect(LogType.Warning, new Regex(@"ChunkUnavailable.*\(32, 0\)"));

            WorldTilemapRefreshResult result = renderer.RefreshCell(new Vector2Int(32, 0));

            Assert.That(result.Status, Is.EqualTo(WorldTilemapRefreshStatus.ChunkUnavailable));
            Assert.That(worldData.ResidentChunkCount, Is.EqualTo(chunksBefore));
            Assert.That(worldData.ContainsChunk(Vector2Int.right), Is.False);
        }

        [TestCase(0, 0)]
        [TestCase(5, 7)]
        [TestCase(-5, -7)]
        [TestCase(31, 31)]
        [TestCase(32, 32)]
        [TestCase(-1, -1)]
        [TestCase(-32, -32)]
        [TestCase(-33, -33)]
        public void GridOffset_AlignsTileCentresWithIntegerWorldCells(int x, int y)
        {
            var worldCell = new Vector2Int(x, y);
            Vector3 actualCentre = backgroundTilemap.GetCellCenterWorld(
                WorldTilemapRenderer.WorldCellToTilemapCell(worldCell));
            Vector2 expectedCentre = WorldTileCoordinates.CellToWorldPosition(worldCell);

            Assert.That(actualCentre.x, Is.EqualTo(expectedCentre.x).Within(0.0001f));
            Assert.That(actualCentre.y, Is.EqualTo(expectedCentre.y).Within(0.0001f));
        }

        [TestCase(31)]
        [TestCase(32)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(-32)]
        [TestCase(-33)]
        public void BoundaryCells_RenderAtTheirExactSignedCoordinates(int worldX)
        {
            var worldCell = new Vector2Int(worldX, 0);
            Vector2Int chunk = WorldTileCoordinates.GetChunkCoordinate(worldCell);
            if (!worldData.ContainsChunk(chunk))
            {
                worldData.CreateChunk(chunk, BackgroundId);
            }

            renderer.RefreshCell(worldCell);

            Vector3Int tilemapCell = WorldTilemapRenderer.WorldCellToTilemapCell(worldCell);
            Assert.That(backgroundTilemap.GetTile(tilemapCell), Is.SameAs(backgroundVisual));
            Assert.That(tilemapCell.x, Is.EqualTo(worldX));
        }

        [Test]
        public void CellRefresh_LeavesUnrequestedVisualsUntouched()
        {
            Vector3Int unrequestedCell = new Vector3Int(8, 8, 0);
            foregroundTilemap.SetTile(unrequestedCell, nonBlockingVisual);

            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(
                foregroundTilemap.GetTile(unrequestedCell),
                Is.SameAs(nonBlockingVisual));
        }

        [Test]
        public void ChunkRefresh_UsesBulkProjectionAcrossNegativeCoordinates()
        {
            Vector2Int chunk = new Vector2Int(-1, -1);
            worldData.CreateChunk(chunk, BackgroundId);
            Vector2Int minimum = WorldTileCoordinates.GetWorldCellCoordinate(
                chunk,
                Vector2Int.zero);
            Vector2Int maximum = WorldTileCoordinates.GetWorldCellCoordinate(
                chunk,
                new Vector2Int(31, 31));
            worldData.SetSolidMaterial(minimum, SolidId);
            worldData.SetSolidMaterial(maximum, NonBlockingSolidId);

            WorldTilemapRefreshResult result = renderer.RefreshChunk(chunk);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                foregroundTilemap.GetTile(
                    WorldTilemapRenderer.WorldCellToTilemapCell(minimum)),
                Is.SameAs(solidVisual));
            Assert.That(
                foregroundTilemap.GetTile(
                    WorldTilemapRenderer.WorldCellToTilemapCell(maximum)),
                Is.SameAs(nonBlockingVisual));
        }

        [Test]
        public void RepeatedRefreshes_DoNotCreateSceneObjects()
        {
            int objectCountBefore = projectionRoot
                .GetComponentsInChildren<Transform>(true).Length;

            renderer.RefreshChunk(Vector2Int.zero);
            renderer.RefreshChunk(Vector2Int.zero);

            int objectCountAfter = projectionRoot
                .GetComponentsInChildren<Transform>(true).Length;
            Assert.That(objectCountAfter, Is.EqualTo(objectCountBefore));
        }

        [Test]
        public void Rendering_DoesNotAlterNavigationClassification()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, SolidId);
            var classifier = new WorldNavigationClassifier(worldData);
            WorldNavigationClassification before = classifier.Classify(Vector2Int.up);

            renderer.RefreshChunk(Vector2Int.zero);

            WorldNavigationClassification after = classifier.Classify(Vector2Int.up);
            Assert.That(after.Occupancy, Is.EqualTo(before.Occupancy));
            Assert.That(after.Support, Is.EqualTo(before.Support));
        }

        [Test]
        public void Projection_CreatesNoGameplayCollidersOrRigidbodies()
        {
            renderer.RefreshChunk(Vector2Int.zero);

            Assert.That(
                projectionRoot.GetComponentsInChildren<Collider2D>(true),
                Is.Empty);
            Assert.That(
                projectionRoot.GetComponentsInChildren<Collider>(true),
                Is.Empty);
            Assert.That(
                projectionRoot.GetComponentsInChildren<Rigidbody2D>(true),
                Is.Empty);
        }

        private Tilemap CreateTilemapChild(string objectName, int sortingOrder)
        {
            var child = new GameObject(objectName, typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(projectionRoot.transform, false);
            Tilemap tilemap = child.GetComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            child.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return tilemap;
        }

        private Tile CreateTile(string tileName)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = tileName;
            temporaryObjects.Add(tile);
            return tile;
        }

        private TDefinition CreateDefinition<TDefinition>(
            string materialId,
            TileBase visualTile)
            where TDefinition : WorldTileMaterialDefinition
        {
            TDefinition definition = ScriptableObject.CreateInstance<TDefinition>();
            temporaryObjects.Add(definition);

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stableMaterialId").stringValue = materialId;
            serializedDefinition.FindProperty("displayName").stringValue = materialId;
            serializedDefinition.FindProperty("visualTile").objectReferenceValue = visualTile;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetBoolean(Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ChunkedWorldData CreateWorld(
            BackgroundTileMaterialDefinition background,
            params SolidTileMaterialDefinition[] solids)
        {
            return new ChunkedWorldData(
                new WorldMaterialRegistry(new[] { background }, solids));
        }
    }
}
