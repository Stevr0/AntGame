using System.Collections.Generic;
using System.Linq;
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
    /// Exercises the real Unity TilemapCollider2D/CompositeCollider2D projection
    /// while keeping every terrain decision in authoritative world data.
    /// </summary>
    public sealed class WorldTerrainCollisionRendererTests
    {
        private const string BackgroundId = "background.collision-test";
        private const string BlockingSupportingId = "solid.blocking-supporting";
        private const string NonBlockingSupportingId = "solid.nonblocking-supporting";
        private const string BlockingNonSupportingId = "solid.blocking-nonsupporting";
        private const string NonBlockingNonSupportingId = "solid.nonblocking-nonsupporting";

        private readonly List<Object> temporaryObjects = new();
        private GameObject projectionRoot;
        private GameObject collisionObject;
        private Tilemap collisionTilemap;
        private Tile collisionTile;
        private TilemapCollider2D tilemapCollider;
        private CompositeCollider2D compositeCollider;
        private WorldTerrainCollisionRenderer renderer;
        private BackgroundTileMaterialDefinition background;
        private SolidTileMaterialDefinition blockingSupporting;
        private SolidTileMaterialDefinition nonBlockingSupporting;
        private SolidTileMaterialDefinition blockingNonSupporting;
        private SolidTileMaterialDefinition nonBlockingNonSupporting;
        private ChunkedWorldData worldData;

        [SetUp]
        public void SetUp()
        {
            Tile visualTile = CreateTile("Visual Tile", Tile.ColliderType.None);
            collisionTile = CreateTile("Collision Tile", Tile.ColliderType.Grid);
            background = CreateDefinition<BackgroundTileMaterialDefinition>(
                BackgroundId,
                visualTile);
            blockingSupporting = CreateSolidDefinition(
                BlockingSupportingId,
                visualTile,
                true,
                true);
            nonBlockingSupporting = CreateSolidDefinition(
                NonBlockingSupportingId,
                visualTile,
                false,
                true);
            blockingNonSupporting = CreateSolidDefinition(
                BlockingNonSupportingId,
                visualTile,
                true,
                false);
            nonBlockingNonSupporting = CreateSolidDefinition(
                NonBlockingNonSupportingId,
                visualTile,
                false,
                false);
            worldData = CreateWorld();
            worldData.CreateChunk(Vector2Int.zero, BackgroundId);

            projectionRoot = new GameObject("Collision Projection Test", typeof(Grid));
            temporaryObjects.Add(projectionRoot);
            projectionRoot.transform.position = new Vector3(-0.5f, -0.5f, 0f);

            collisionObject = new GameObject(
                "Foreground Collision Tilemap",
                typeof(Tilemap),
                typeof(TilemapRenderer),
                typeof(Rigidbody2D),
                typeof(TilemapCollider2D),
                typeof(CompositeCollider2D),
                typeof(WorldTerrainCollisionRenderer));
            collisionObject.transform.SetParent(projectionRoot.transform, false);
            collisionTilemap = collisionObject.GetComponent<Tilemap>();
            collisionTilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            collisionObject.GetComponent<TilemapRenderer>().enabled = false;
            collisionObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            tilemapCollider = collisionObject.GetComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            compositeCollider = collisionObject.GetComponent<CompositeCollider2D>();
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;

            renderer = collisionObject.GetComponent<WorldTerrainCollisionRenderer>();
            renderer.BindWorldData(worldData);
            renderer.SetCollisionReferences(
                collisionTilemap,
                collisionTile,
                tilemapCollider,
                compositeCollider);
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
            Physics2D.SyncTransforms();
        }

        [Test]
        public void BackgroundOnlyCell_NeverCreatesTerrainCollision()
        {
            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(collisionTilemap.GetTile(Vector3Int.zero), Is.Null);
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.Zero);
        }

        [Test]
        public void BlockingSolid_CreatesImmediatePhysicsCollision()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);

            WorldTerrainCollisionRefreshResult result = renderer.RefreshCell(Vector2Int.zero);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(collisionTilemap.GetTile(Vector3Int.zero), Is.SameAs(collisionTile));
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.EqualTo(1));
        }

        [Test]
        public void NonBlockingSolid_RemainsCollisionFreeWhileVisuallyRepresentable()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, NonBlockingSupportingId);

            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(nonBlockingSupporting.VisualTile, Is.Not.Null);
            Assert.That(collisionTilemap.GetTile(Vector3Int.zero), Is.Null);
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.Zero);
        }

        [TestCase(BlockingSupportingId, true, true)]
        [TestCase(NonBlockingSupportingId, false, true)]
        [TestCase(BlockingNonSupportingId, true, false)]
        [TestCase(NonBlockingNonSupportingId, false, false)]
        public void BlockingAndSupportFlags_RemainIndependent(
            string materialId,
            bool expectedCollision,
            bool expectedSupport)
        {
            var solidCell = new Vector2Int(5, 5);
            var exposedNeighbour = new Vector2Int(5, 6);
            worldData.SetSolidMaterial(solidCell, materialId);
            var classifier = new WorldNavigationClassifier(worldData);

            renderer.RefreshCell(solidCell);

            Assert.That(
                CountTerrainCollidersAt(solidCell) > 0,
                Is.EqualTo(expectedCollision));
            Assert.That(
                classifier.Classify(exposedNeighbour).HasConfirmedSupport,
                Is.EqualTo(expectedSupport));
        }

        [Test]
        public void RemovingBlockingSolid_RemovesCollisionAndPreservesBackground()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            renderer.RefreshCell(Vector2Int.zero);
            worldData.GetCell(Vector2Int.zero, out WorldTileCellData before);

            worldData.RemoveSolidMaterial(Vector2Int.zero);
            renderer.RefreshCell(Vector2Int.zero);

            worldData.GetCell(Vector2Int.zero, out WorldTileCellData after);
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.Zero);
            Assert.That(after.BackgroundMaterialId, Is.EqualTo(before.BackgroundMaterialId));
            Assert.That(after.HasSolidTile, Is.False);
        }

        [Test]
        public void RestoringBlockingSolid_RestoresCollision()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            renderer.RefreshCell(Vector2Int.zero);
            worldData.RemoveSolidMaterial(Vector2Int.zero);
            renderer.RefreshCell(Vector2Int.zero);

            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.EqualTo(1));
        }

        [Test]
        public void NonBlockingToBlockingMaterialChange_AddsCollision()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, NonBlockingSupportingId);
            renderer.RefreshCell(Vector2Int.zero);
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.Zero);

            worldData.SetSolidMaterial(Vector2Int.zero, BlockingNonSupportingId);
            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.EqualTo(1));
        }

        [Test]
        public void BlockingToNonBlockingMaterialChange_RemovesCollision()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            renderer.RefreshCell(Vector2Int.zero);
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.EqualTo(1));

            worldData.SetSolidMaterial(Vector2Int.zero, NonBlockingNonSupportingId);
            renderer.RefreshCell(Vector2Int.zero);

            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.Zero);
        }

        [Test]
        public void AdjacentBlockingTiles_MergeWithoutDuplicatePhysicsHits()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            worldData.SetSolidMaterial(Vector2Int.right, BlockingSupportingId);

            renderer.RefreshChunk(Vector2Int.zero);

            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.EqualTo(1));
            Assert.That(CountTerrainCollidersAt(Vector2Int.right), Is.EqualTo(1));
            Assert.That(compositeCollider.pathCount, Is.EqualTo(1));
        }

        [TestCase(0, 0)]
        [TestCase(31, 0)]
        [TestCase(32, 0)]
        [TestCase(-1, 0)]
        [TestCase(-32, 0)]
        [TestCase(-33, 0)]
        [TestCase(0, 31)]
        [TestCase(0, 32)]
        [TestCase(0, -1)]
        [TestCase(0, -32)]
        [TestCase(0, -33)]
        public void CollisionAlignsAcrossSignedChunkBoundaries(int x, int y)
        {
            var worldCell = new Vector2Int(x, y);
            EnsureChunk(WorldTileCoordinates.GetChunkCoordinate(worldCell));
            worldData.SetSolidMaterial(worldCell, BlockingSupportingId);

            renderer.RefreshCell(worldCell);

            Vector3 centre = collisionTilemap.GetCellCenterWorld(
                WorldTilemapRenderer.WorldCellToTilemapCell(worldCell));
            Assert.That(centre.x, Is.EqualTo(x).Within(0.0001f));
            Assert.That(centre.y, Is.EqualTo(y).Within(0.0001f));
            Assert.That(CountTerrainCollidersAt(worldCell), Is.EqualTo(1));
        }

        [Test]
        public void MissingChunk_DoesNotGenerateCollisionOrWorldData()
        {
            int chunksBefore = worldData.ResidentChunkCount;
            var unavailableCell = new Vector2Int(32, 0);
            collisionTilemap.SetTile(
                WorldTilemapRenderer.WorldCellToTilemapCell(unavailableCell),
                collisionTile);
            renderer.SynchronizeCollisionGeometry();
            LogAssert.Expect(
                LogType.Warning,
                new Regex(@"ChunkUnavailable.*\(32, 0\)"));

            WorldTerrainCollisionRefreshResult result = renderer.RefreshCell(unavailableCell);

            Assert.That(
                result.Status,
                Is.EqualTo(WorldTerrainCollisionRefreshStatus.ChunkUnavailable));
            Assert.That(worldData.ResidentChunkCount, Is.EqualTo(chunksBefore));
            Assert.That(worldData.ContainsChunk(Vector2Int.right), Is.False);
            Assert.That(CountTerrainCollidersAt(unavailableCell), Is.Zero);
        }

        [Test]
        public void CollisionRefresh_DoesNotAlterNavigationClassification()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            var classifier = new WorldNavigationClassifier(worldData);
            WorldNavigationClassification before = classifier.Classify(Vector2Int.up);

            renderer.RefreshChunk(Vector2Int.zero);

            WorldNavigationClassification after = classifier.Classify(Vector2Int.up);
            Assert.That(after.Occupancy, Is.EqualTo(before.Occupancy));
            Assert.That(after.Support, Is.EqualTo(before.Support));
        }

        [Test]
        public void RepeatedRefresh_DoesNotCreateExtraColliderComponents()
        {
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingSupportingId);
            int colliderCountBefore =
                projectionRoot.GetComponentsInChildren<Collider2D>(true).Length;
            int objectCountBefore =
                projectionRoot.GetComponentsInChildren<Transform>(true).Length;

            renderer.RefreshChunk(Vector2Int.zero);
            renderer.RefreshChunk(Vector2Int.zero);

            Assert.That(
                projectionRoot.GetComponentsInChildren<Collider2D>(true).Length,
                Is.EqualTo(colliderCountBefore));
            Assert.That(
                projectionRoot.GetComponentsInChildren<Transform>(true).Length,
                Is.EqualTo(objectCountBefore));
            Assert.That(colliderCountBefore, Is.EqualTo(2));
        }

        [Test]
        public void ChunkRefresh_DoesNotChangeUnrequestedCollisionRegion()
        {
            var otherChunkCell = new Vector2Int(32, 0);
            collisionTilemap.SetTile(
                WorldTilemapRenderer.WorldCellToTilemapCell(otherChunkCell),
                collisionTile);
            renderer.SynchronizeCollisionGeometry();

            renderer.RefreshChunk(Vector2Int.zero);

            Assert.That(
                collisionTilemap.HasTile(
                    WorldTilemapRenderer.WorldCellToTilemapCell(otherChunkCell)),
                Is.True);
        }

        [Test]
        public void ExplicitSynchronization_AppliesPendingColliderGeometry()
        {
            collisionTilemap.SetTile(Vector3Int.zero, collisionTile);

            bool synchronized = renderer.SynchronizeCollisionGeometry();

            Assert.That(synchronized, Is.True);
            Assert.That(CountTerrainCollidersAt(Vector2Int.zero), Is.EqualTo(1));
        }

        private int CountTerrainCollidersAt(Vector2Int worldCell)
        {
            Vector2 position = WorldTileCoordinates.CellToWorldPosition(worldCell);
            return Physics2D.OverlapPointAll(position)
                .Count(collider =>
                    collider != null && collider.gameObject == collisionObject);
        }

        private void EnsureChunk(Vector2Int chunkCoordinate)
        {
            if (!worldData.ContainsChunk(chunkCoordinate))
            {
                Assert.That(
                    worldData.CreateChunk(chunkCoordinate, BackgroundId),
                    Is.EqualTo(WorldChunkCreationResult.Success));
            }
        }

        private ChunkedWorldData CreateWorld()
        {
            return new ChunkedWorldData(
                new WorldMaterialRegistry(
                    new[] { background },
                    new[]
                    {
                        blockingSupporting,
                        nonBlockingSupporting,
                        blockingNonSupporting,
                        nonBlockingNonSupporting
                    }));
        }

        private Tile CreateTile(string tileName, Tile.ColliderType colliderType)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = tileName;
            tile.colliderType = colliderType;
            temporaryObjects.Add(tile);
            return tile;
        }

        private SolidTileMaterialDefinition CreateSolidDefinition(
            string materialId,
            TileBase visualTile,
            bool blocksMovement,
            bool supportsNavigation)
        {
            SolidTileMaterialDefinition definition =
                CreateDefinition<SolidTileMaterialDefinition>(materialId, visualTile);
            SetBoolean(definition, "blocksMovement", blocksMovement);
            SetBoolean(definition, "supportsNavigation", supportsNavigation);
            return definition;
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
    }
}
