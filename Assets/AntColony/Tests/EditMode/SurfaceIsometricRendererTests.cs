using System.Collections.Generic;
using AntColony.World;
using AntColony.WorldPresentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace AntColony.Tests.EditMode
{
    public sealed class SurfaceIsometricRendererTests
    {
        private readonly List<GameObject> objects = new();
        private GeneratedWorldSnapshot generated;
        private SurfaceWorldData surface;
        private SurfaceRuntimeCellRegionAdapter region;

        [SetUp]
        public void SetUp()
        {
            Assert.That(ProceduralWorldGenerator.TryGenerate(
                2203L, new WorldGenerationConfiguration(),
                out generated, out GeneratedWorldValidationReport report),
                Is.True, report?.Summary);
            surface = generated.CreateSurfaceNavigationWorld();
            region = new SurfaceRuntimeCellRegionAdapter(surface, generated);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in objects)
                if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
            region?.Dispose();
        }

        [Test]
        public void AuthoritativeWindowMapsExactlyEightByEightBackgrounds()
        {
            SurfaceIsometricRenderer renderer = CreateRenderer(region);
            var minimum = new SurfaceCell(29, 29);

            SurfaceIsometricRenderResult result = renderer.RenderWindow(minimum);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RenderedCellCount, Is.EqualTo(64));
            Assert.That(renderer.BackgroundTilemap.GetUsedTilesCount(), Is.EqualTo(2));
            Assert.That(renderer.transform.childCount, Is.EqualTo(1),
                "Terrain must use one Grid/Tilemap hierarchy, not one object per cell.");

            for (int y = 0; y < SurfaceIsometricRenderer.PreviewSize; y++)
            for (int x = 0; x < SurfaceIsometricRenderer.PreviewSize; x++)
            {
                var cell = new SurfaceCell(minimum.X + x, minimum.Y + y);
                Assert.That(region.TryGetCell(
                    RegionCellId.Surface(AntRegionIds.Surface, cell),
                    out RuntimeCellSnapshot snapshot),
                    Is.EqualTo(RuntimeCellReadResult.Success));
                Assert.That(renderer.TryGetRenderedBackground(cell,
                    out string renderedId), Is.True);
                Assert.That(renderedId, Is.EqualTo(snapshot.BackgroundDefinitionId));
                Assert.That(renderer.BackgroundTilemap.GetTile(
                    SurfaceIsometricRenderer.LogicalToTilemapCell(cell)),
                    Is.Not.Null);
            }
        }

        [Test]
        public void UnityGridMatchesAcceptedProjectionForSignedCells()
        {
            SurfaceIsometricRenderer renderer = CreateRenderer(region);
            SurfaceIsometricProjection projection = renderer.Projection;
            Assert.That(renderer.Grid.cellSize,
                Is.EqualTo(new Vector3(1f, 0.5f, 1f)));
            var cells = new[]
            {
                new SurfaceCell(0, 0),
                new SurfaceCell(1, 0),
                new SurfaceCell(0, 1),
                new SurfaceCell(-4, 7),
                new SurfaceCell(29, 36)
            };

            foreach (SurfaceCell cell in cells)
            {
                Vector3 tileWorld = renderer.Grid.CellToWorld(
                    SurfaceIsometricRenderer.LogicalToTilemapCell(cell));
                Vector2 projected = projection.CellToWorld(cell);
                Assert.That(tileWorld.x, Is.EqualTo(projected.x).Within(0.00001f));
                Assert.That(tileWorld.y, Is.EqualTo(projected.y).Within(0.00001f));
            }
        }

        [Test]
        public void VisualCatalogUsesFinalGrassWaterAndFallbackSprites()
        {
            SurfaceIsometricVisualCatalog catalog =
                Resources.Load<SurfaceIsometricVisualCatalog>(
                    "SurfaceIsometricVisualCatalog");
            Assert.That(catalog, Is.Not.Null);
            SurfaceIsometricVisualCatalog.BackgroundEntry grass = null;
            SurfaceIsometricVisualCatalog.BackgroundEntry water = null;
            foreach (SurfaceIsometricVisualCatalog.BackgroundEntry entry
                     in catalog.Backgrounds)
            {
                if (entry.GameplayDefinitionId == "background.surface.grass")
                    grass = entry;
                if (entry.GameplayDefinitionId == "background.surface.water")
                    water = entry;
            }

            Assert.That(grass, Is.Not.Null);
            Assert.That(water, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(grass.Sprite), Is.EqualTo(
                "Assets/AntColony/Art/WorldView/Isometric/Terrain/" +
                "tile_040_GrassMain.png"));
            Assert.That(AssetDatabase.GetAssetPath(water.Sprite), Is.EqualTo(
                "Assets/AntColony/Art/WorldView/Isometric/Terrain/" +
                "tile_104_flatWater.png"));
            Assert.That(AssetDatabase.GetAssetPath(catalog.FallbackSprite), Is.EqualTo(
                "Assets/AntColony/Art/WorldView/Isometric/Terrain/tile_009.png"));
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/AntColony/Art/WorldView/Isometric/Terrain/tile_040.png"),
                Is.Not.Null, "The original raised grass sprite must remain available.");

            var importer = (TextureImporter)AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(grass.Sprite));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f));
            Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.25f)));
            Assert.That(grass.Sprite.rect.size, Is.EqualTo(new Vector2(32f, 32f)));

            importer = (TextureImporter)AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(water.Sprite));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f));
            Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.25f)));
            Assert.That(water.Sprite.rect.size, Is.EqualTo(new Vector2(32f, 32f)));
            Assert.That(water.Sprite.bounds.size.x, Is.EqualTo(1f));
            Assert.That(water.Sprite.bounds.size.y, Is.EqualTo(1f));
        }

        [TestCase(WaterLayout.Continuous, 16)]
        [TestCase(WaterLayout.SurroundedByGrass, 4)]
        [TestCase(WaterLayout.StraightShoreline, 8)]
        [TestCase(WaterLayout.IrregularShoreline, 10)]
        [TestCase(WaterLayout.Alternating, 8)]
        public void WaterLayoutsMapEveryLogicalCellToExactlyOneTile(
            WaterLayout layout, int expectedWaterCount)
        {
            var patternRegion = new WaterPatternSurfaceRegion(layout);
            SurfaceIsometricRenderer renderer = CreateRenderer(patternRegion);

            SurfaceIsometricRenderResult result =
                renderer.RenderWindow(new SurfaceCell(0, 0));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RenderedCellCount, Is.EqualTo(64));
            Assert.That(renderer.BackgroundTilemapRenderer.sortingOrder,
                Is.EqualTo(-20));

            int waterCount = 0;
            for (int y = 0; y < SurfaceIsometricRenderer.PreviewSize; y++)
            for (int x = 0; x < SurfaceIsometricRenderer.PreviewSize; x++)
            {
                var logicalCell = new SurfaceCell(x, y);
                TileBase tileBase = renderer.BackgroundTilemap.GetTile(
                    SurfaceIsometricRenderer.LogicalToTilemapCell(logicalCell));
                Assert.That(tileBase, Is.Not.Null,
                    $"Logical cell {logicalCell} must have exactly one tile.");
                var tile = tileBase as Tile;
                Assert.That(tile, Is.Not.Null);
                if (AssetDatabase.GetAssetPath(tile.sprite)
                    .EndsWith("tile_104_flatWater.png"))
                    waterCount++;
            }

            Assert.That(waterCount, Is.EqualTo(expectedWaterCount));
        }

        [Test]
        public void RenderingIsDeterministicAndDoesNotModifyAuthority()
        {
            string fingerprint = generated.CreateFingerprint();
            int obstacleCount = CountObstacles(surface);
            SurfaceIsometricRenderer renderer = CreateRenderer(region);
            var minimum = new SurfaceCell(29, 29);

            Assert.That(renderer.RenderWindow(minimum).Succeeded, Is.True);
            var first = Capture(renderer, minimum);
            Assert.That(renderer.RenderWindow(minimum).Succeeded, Is.True);
            var second = Capture(renderer, minimum);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(generated.CreateFingerprint(), Is.EqualTo(fingerprint));
            Assert.That(CountObstacles(surface), Is.EqualTo(obstacleCount));
        }

        [Test]
        public void WindowOutsideAuthorityFailsWithoutPartialTiles()
        {
            var smallRegion = new SurfaceRuntimeCellRegionAdapter(
                new SurfaceWorldData(8, 8));
            SurfaceIsometricRenderer renderer = CreateRenderer(smallRegion);

            SurfaceIsometricRenderResult result =
                renderer.RenderWindow(new SurfaceCell(1, 1));

            Assert.That(result.Status,
                Is.EqualTo(SurfaceIsometricRenderStatus.WindowOutsideAuthority));
            Assert.That(renderer.RenderedCellCount, Is.Zero);
            Assert.That(renderer.BackgroundTilemap.GetUsedTilesCount(), Is.Zero);
            smallRegion.Dispose();
        }

        [Test]
        public void UnsupportedBackgroundUsesVisibleFallback()
        {
            var unknown = new UnknownSurfaceRegion(8, 8);
            SurfaceIsometricRenderer renderer = CreateRenderer(unknown);

            SurfaceIsometricRenderResult result =
                renderer.RenderWindow(new SurfaceCell(0, 0));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FallbackCellCount, Is.EqualTo(64));
            Vector3Int tileCell = SurfaceIsometricRenderer.LogicalToTilemapCell(
                new SurfaceCell(0, 0));
            Assert.That(renderer.BackgroundTilemap.GetTile(tileCell), Is.Not.Null);
            Assert.That(renderer.BackgroundTilemap.GetColor(tileCell),
                Is.EqualTo((Color)new Color32(255, 0, 220, 255)));
        }

        private SurfaceIsometricRenderer CreateRenderer(
            IReadOnlyRuntimeCellRegion authoritativeRegion)
        {
            var root = new GameObject(
                "Surface Isometric Renderer Test",
                typeof(SurfaceIsometricProjection),
                typeof(SurfaceIsometricRenderer));
            objects.Add(root);
            SurfaceIsometricProjection projection =
                root.GetComponent<SurfaceIsometricProjection>();
            projection.Configure(new Vector2(10f, 20f), 32f, 16f, 32f);
            SurfaceIsometricRenderer renderer =
                root.GetComponent<SurfaceIsometricRenderer>();
            renderer.Configure(
                authoritativeRegion,
                projection,
                Resources.Load<SurfaceIsometricVisualCatalog>(
                    "SurfaceIsometricVisualCatalog"),
                0);
            return renderer;
        }

        private static string Capture(SurfaceIsometricRenderer renderer,
            SurfaceCell minimum)
        {
            var values = new List<string>();
            for (int y = 0; y < SurfaceIsometricRenderer.PreviewSize; y++)
            for (int x = 0; x < SurfaceIsometricRenderer.PreviewSize; x++)
            {
                renderer.TryGetRenderedBackground(
                    new SurfaceCell(minimum.X + x, minimum.Y + y),
                    out string value);
                values.Add(value);
            }
            return string.Join("|", values);
        }

        private static int CountObstacles(SurfaceWorldData world)
        {
            int count = 0;
            foreach (SurfaceCell ignored in world.Obstacles) count++;
            return count;
        }

        private sealed class UnknownSurfaceRegion : IReadOnlyRuntimeCellRegion
        {
            private readonly int width;
            private readonly int height;

            public UnknownSurfaceRegion(int width, int height)
            {
                this.width = width;
                this.height = height;
                Changes = new RuntimeCellChangeFeed(AntRegionIds.Surface);
            }

            public RegionId RegionId => AntRegionIds.Surface;
            public RegionLocationKind CoordinateKind => RegionLocationKind.Surface;
            public long PublicationRevision => 1;
            public RuntimeCellChangeFeed Changes { get; }

            public RuntimeCellReadResult TryGetCell(RegionCellId cellId,
                out RuntimeCellSnapshot snapshot)
            {
                SurfaceCell cell = cellId.SurfaceCell;
                if (cell.X < 0 || cell.Y < 0 || cell.X >= width || cell.Y >= height)
                {
                    snapshot = null;
                    return RuntimeCellReadResult.CellUnavailable;
                }
                snapshot = new RuntimeCellSnapshot(
                    cellId, "background.surface.unsupported", 0, 1);
                return RuntimeCellReadResult.Success;
            }

            public IEnumerable<RegionCellId> EnumerateResidentCells()
            {
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    yield return RegionCellId.Surface(
                        AntRegionIds.Surface, new SurfaceCell(x, y));
            }
        }

        public enum WaterLayout
        {
            Continuous,
            SurroundedByGrass,
            StraightShoreline,
            IrregularShoreline,
            Alternating
        }

        private sealed class WaterPatternSurfaceRegion : IReadOnlyRuntimeCellRegion
        {
            private readonly WaterLayout layout;

            public WaterPatternSurfaceRegion(WaterLayout layout)
            {
                this.layout = layout;
                Changes = new RuntimeCellChangeFeed(AntRegionIds.Surface);
            }

            public RegionId RegionId => AntRegionIds.Surface;
            public RegionLocationKind CoordinateKind => RegionLocationKind.Surface;
            public long PublicationRevision => 1;
            public RuntimeCellChangeFeed Changes { get; }

            public RuntimeCellReadResult TryGetCell(RegionCellId cellId,
                out RuntimeCellSnapshot snapshot)
            {
                SurfaceCell cell = cellId.SurfaceCell;
                if (cell.X < 0 || cell.Y < 0 ||
                    cell.X >= SurfaceIsometricRenderer.PreviewSize ||
                    cell.Y >= SurfaceIsometricRenderer.PreviewSize)
                {
                    snapshot = null;
                    return RuntimeCellReadResult.CellUnavailable;
                }

                bool isWater = cell.X < 4 && cell.Y < 4 && IsWater(cell.X, cell.Y);
                snapshot = new RuntimeCellSnapshot(
                    cellId,
                    isWater
                        ? ProceduralWorldGenerator.WaterBackgroundId
                        : ProceduralWorldGenerator.GrassBackgroundId,
                    0,
                    1);
                return RuntimeCellReadResult.Success;
            }

            public IEnumerable<RegionCellId> EnumerateResidentCells()
            {
                for (int y = 0; y < SurfaceIsometricRenderer.PreviewSize; y++)
                for (int x = 0; x < SurfaceIsometricRenderer.PreviewSize; x++)
                    yield return RegionCellId.Surface(
                        AntRegionIds.Surface, new SurfaceCell(x, y));
            }

            private bool IsWater(int x, int y)
            {
                return layout switch
                {
                    WaterLayout.Continuous => true,
                    WaterLayout.SurroundedByGrass => x > 0 && x < 3 && y > 0 && y < 3,
                    WaterLayout.StraightShoreline => x < 2,
                    WaterLayout.IrregularShoreline => y == 0 || x + y < 4,
                    WaterLayout.Alternating => (x + y) % 2 == 0,
                    _ => false
                };
            }
        }
    }
}

