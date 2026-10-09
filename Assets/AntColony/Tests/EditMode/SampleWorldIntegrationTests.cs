using System.Collections.Generic;
using System.Reflection;
using AntColony.World;
using AntColony.WorldPresentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AntColony.Tests.EditMode
{
    /// <summary>Regression coverage for the bounded integrated sample world.</summary>
    public sealed class SampleWorldIntegrationTests
    {
        private readonly List<Object> createdObjects = new();
        private BackgroundTileMaterialDefinition sky;
        private BackgroundTileMaterialDefinition earth;
        private BackgroundTileMaterialDefinition stoneBackground;
        private BackgroundTileMaterialDefinition prepared;
        private SolidTileMaterialDefinition soil;
        private SolidTileMaterialDefinition stone;
        private Tile skyTile;
        private Tile earthTile;
        private Tile stoneBackgroundTile;
        private Tile preparedTile;
        private Tile soilTile;
        private Tile stoneTile;
        private Tile collisionTile;
        private WorldMaterialRegistry registry;
        private SampleWorldMaterialIds materialIds;

        [SetUp]
        public void SetUp()
        {
            skyTile = CreateTile("Sky");
            earthTile = CreateTile("Earth");
            stoneBackgroundTile = CreateTile("Stone Background");
            preparedTile = CreateTile("Prepared");
            soilTile = CreateTile("Soil");
            stoneTile = CreateTile("Stone");
            collisionTile = CreateTile("Collision", Tile.ColliderType.Grid);

            sky = CreateBackground("background.sky", skyTile, true, false, false, false, true);
            earth = CreateBackground("background.earth", earthTile, true, false, false, false, true);
            stoneBackground = CreateBackground(
                "background.stone",
                stoneBackgroundTile,
                true,
                false,
                false,
                false,
                true);
            prepared = CreateBackground(
                "background.prepared",
                preparedTile,
                true,
                true,
                true,
                true,
                false);
            soil = CreateSolid("solid.soil", soilTile);
            stone = CreateSolid("solid.stone", stoneTile);
            registry = new WorldMaterialRegistry(
                new[] { sky, earth, stoneBackground, prepared },
                new[] { soil, stone });
            materialIds = new SampleWorldMaterialIds(
                sky.StableMaterialId,
                earth.StableMaterialId,
                stoneBackground.StableMaterialId,
                prepared.StableMaterialId,
                soil.StableMaterialId,
                stone.StableMaterialId);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }
        }

        [Test]
        public void SameSeed_GeneratesIdenticalAuthoritativeCells()
        {
            GeneratedSampleWorld first = Generate(7182);
            GeneratedSampleWorld second = Generate(7182);

            for (int y = -64; y <= 63; y++)
            {
                for (int x = -64; x <= 63; x++)
                {
                    var cell = new Vector2Int(x, y);
                    first.WorldData.GetCell(cell, out WorldTileCellData firstData);
                    second.WorldData.GetCell(cell, out WorldTileCellData secondData);
                    Assert.That(secondData.BackgroundMaterialId,
                        Is.EqualTo(firstData.BackgroundMaterialId));
                    Assert.That(secondData.SolidMaterialId,
                        Is.EqualTo(firstData.SolidMaterialId));
                }
            }
        }

        [Test]
        public void GeneratedRegion_HasBackgroundsSignedChunksSkyAndUndergroundLayers()
        {
            GeneratedSampleWorld generated = Generate(2203);
            ChunkedWorldData world = generated.WorldData;

            Assert.That(world.ResidentChunkCount, Is.EqualTo(16));
            Assert.That(world.ContainsChunk(new Vector2Int(-2, -2)), Is.True);
            Assert.That(world.ContainsChunk(new Vector2Int(1, 1)), Is.True);
            for (int y = -64; y <= 63; y++)
            {
                for (int x = -64; x <= 63; x++)
                {
                    Assert.That(
                        world.GetCell(new Vector2Int(x, y), out WorldTileCellData cell),
                        Is.EqualTo(WorldCellQueryResult.Available));
                    Assert.That(cell.BackgroundMaterialId, Is.Not.Null.And.Not.Empty);
                }
            }

            world.GetCell(new Vector2Int(40, 50), out WorldTileCellData skyCell);
            world.GetCell(new Vector2Int(40, -40), out WorldTileCellData stoneCell);
            world.GetCell(new Vector2Int(40, 10), out WorldTileCellData soilCell);
            Assert.That(skyCell.BackgroundMaterialId, Is.EqualTo(sky.StableMaterialId));
            Assert.That(skyCell.HasSolidTile, Is.False);
            Assert.That(stoneCell.BackgroundMaterialId,
                Is.EqualTo(stoneBackground.StableMaterialId));
            Assert.That(stoneCell.SolidMaterialId, Is.EqualTo(stone.StableMaterialId));
            Assert.That(soilCell.SolidMaterialId, Is.EqualTo(soil.StableMaterialId));
        }

        [Test]
        public void GeneratedGeometry_ContainsPassagesChambersShaftAndPreparedStrip()
        {
            ChunkedWorldData world = Generate(2203).WorldData;
            AssertExposed(world, new Vector2Int(0, SampleWorldGenerator.MainPassageY));
            AssertExposed(world, new Vector2Int(-27, -16));
            AssertExposed(world, new Vector2Int(27, -27));
            AssertExposed(world, new Vector2Int(0, 10));

            var preparedCell = new Vector2Int(-13, SampleWorldGenerator.MainPassageY);
            AssertExposed(world, preparedCell);
            world.GetCell(preparedCell, out WorldTileCellData data);
            Assert.That(data.BackgroundMaterialId,
                Is.EqualTo(prepared.StableMaterialId));
        }

        [Test]
        public void MissingChunkQuery_DoesNotGenerateTerrain()
        {
            ChunkedWorldData world = Generate(2203).WorldData;
            int countBefore = world.ResidentChunkCount;

            Assert.That(
                world.GetCell(new Vector2Int(1000, -1000), out _),
                Is.EqualTo(WorldCellQueryResult.ChunkUnavailable));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(countBefore));
        }

        [Test]
        public void ProjectionCollisionAndMutations_RefreshFromAuthoritativeData()
        {
            ChunkedWorldData world = Generate(2203).WorldData;
            GameObject root = Track(new GameObject("Sample Projection", typeof(Grid)));
            Tilemap background = CreateTilemap(root.transform, "Background");
            Tilemap foreground = CreateTilemap(root.transform, "Foreground");
            GameObject rendererObject = Track(new GameObject(
                "Visual Renderer",
                typeof(WorldTilemapRenderer)));
            WorldTilemapRenderer visual =
                rendererObject.GetComponent<WorldTilemapRenderer>();
            visual.BindWorldData(world);
            visual.SetTilemapReferences(background, foreground);

            GameObject collisionObject = Track(new GameObject(
                "Collision",
                typeof(Tilemap),
                typeof(TilemapRenderer),
                typeof(Rigidbody2D),
                typeof(TilemapCollider2D),
                typeof(CompositeCollider2D),
                typeof(WorldTerrainCollisionRenderer)));
            collisionObject.transform.SetParent(root.transform, false);
            Tilemap collision = collisionObject.GetComponent<Tilemap>();
            TilemapCollider2D sourceCollider =
                collisionObject.GetComponent<TilemapCollider2D>();
            sourceCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite =
                collisionObject.GetComponent<CompositeCollider2D>();
            composite.generationType = CompositeCollider2D.GenerationType.Manual;
            WorldTerrainCollisionRenderer collisionRenderer =
                collisionObject.GetComponent<WorldTerrainCollisionRenderer>();
            collisionRenderer.BindWorldData(world);
            collisionRenderer.SetCollisionReferences(
                collision,
                collisionTile,
                sourceCollider,
                composite);

            var solidCell = new Vector2Int(40, 10);
            visual.RefreshCell(solidCell);
            collisionRenderer.RefreshCell(solidCell);
            Vector3Int tileCell = WorldTilemapRenderer.WorldCellToTilemapCell(solidCell);
            Assert.That(foreground.GetTile(tileCell), Is.SameAs(soilTile));
            Assert.That(collision.HasTile(tileCell), Is.True);

            Assert.That(
                world.RemoveSolidMaterial(solidCell),
                Is.EqualTo(WorldCellMutationResult.Success));
            visual.RefreshCell(solidCell);
            collisionRenderer.RefreshCell(solidCell);
            Assert.That(foreground.HasTile(tileCell), Is.False);
            Assert.That(collision.HasTile(tileCell), Is.False);
            world.GetCell(solidCell, out WorldTileCellData exposed);
            Assert.That(exposed.BackgroundMaterialId, Is.EqualTo(earth.StableMaterialId));

            var backgroundCell = new Vector2Int(-13, SampleWorldGenerator.MainPassageY);
            Vector3Int backgroundTileCell =
                WorldTilemapRenderer.WorldCellToTilemapCell(backgroundCell);
            visual.RefreshCell(backgroundCell);
            Assert.That(background.GetTile(backgroundTileCell), Is.SameAs(preparedTile));
            Assert.That(
                world.SetBackgroundMaterial(backgroundCell, earth.StableMaterialId),
                Is.EqualTo(WorldCellMutationResult.Success));
            visual.RefreshCell(backgroundCell);
            Assert.That(background.GetTile(backgroundTileCell), Is.SameAs(earthTile));

            int objectCount = root.GetComponentsInChildren<Transform>(true).Length;
            visual.RefreshChunk(Vector2Int.zero);
            collisionRenderer.RefreshChunk(Vector2Int.zero);
            Assert.That(
                root.GetComponentsInChildren<Transform>(true).Length,
                Is.EqualTo(objectCount),
                "Tile projection must not create one GameObject per tile.");
        }

        private GeneratedSampleWorld Generate(int seed)
        {
            return SampleWorldGenerator.Generate(registry, materialIds, seed);
        }

        private void AssertExposed(ChunkedWorldData world, Vector2Int cell)
        {
            Assert.That(world.GetCell(cell, out WorldTileCellData data),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(data.HasSolidTile, Is.False, $"Expected {cell} to be excavated.");
        }

        private Tilemap CreateTilemap(Transform parent, string name)
        {
            GameObject child = new(name, typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Tilemap>();
        }

        private Tile CreateTile(
            string name,
            Tile.ColliderType colliderType = Tile.ColliderType.None)
        {
            Tile tile = Track(ScriptableObject.CreateInstance<Tile>());
            tile.name = name;
            tile.colliderType = colliderType;
            return tile;
        }

        private BackgroundTileMaterialDefinition CreateBackground(
            string id,
            Tile tile,
            bool horizontal,
            bool up,
            bool down,
            bool grip,
            bool gravity)
        {
            BackgroundTileMaterialDefinition definition =
                Track(ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>());
            SetField(definition, "stableMaterialId", id);
            SetField(definition, "displayName", id);
            SetField(definition, "visualTile", tile);
            SetField(definition, "allowsHorizontalMovement", horizontal);
            SetField(definition, "allowsUpwardMovement", up);
            SetField(definition, "allowsDownwardMovement", down);
            SetField(definition, "providesClimbingGrip", grip);
            SetField(definition, "gravityAppliesWhenUnsupported", gravity);
            return definition;
        }

        private SolidTileMaterialDefinition CreateSolid(string id, Tile tile)
        {
            SolidTileMaterialDefinition definition =
                Track(ScriptableObject.CreateInstance<SolidTileMaterialDefinition>());
            SetField(definition, "stableMaterialId", id);
            SetField(definition, "displayName", id);
            SetField(definition, "visualTile", tile);
            SetField(definition, "blocksMovement", true);
            SetField(definition, "supportsNavigation", true);
            SetField(definition, "isExcavatable", true);
            return definition;
        }

        private T Track<T>(T created) where T : Object
        {
            createdObjects.Add(created);
            return created;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            Assert.Fail($"Serialized field '{fieldName}' was not found.");
        }
    }
}
