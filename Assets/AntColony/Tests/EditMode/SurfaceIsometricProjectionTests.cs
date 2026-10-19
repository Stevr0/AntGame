using System.Collections.Generic;
using AntColony.World;
using AntColony.WorldPresentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    public sealed class SurfaceIsometricProjectionTests
    {
        private const float Tolerance = 0.00001f;
        private const string SpriteFolder =
            "Assets/AntColony/Art/WorldView/Isometric/Terrain/";
        private const string PreviewScenePath =
            "Assets/AntColony/Scenes/IsometricCalibrationPreview.unity";

        private static readonly string[] SelectedSpriteNames =
        {
            "tile_101", "tile_022", "tile_104_flatWater", "tile_060", "tile_044"
        };

        private GameObject projectionObject;
        private SurfaceIsometricProjection projection;

        [SetUp]
        public void SetUp()
        {
            projectionObject = new GameObject("Projection Test");
            projection = projectionObject.AddComponent<SurfaceIsometricProjection>();
            projection.Configure(new Vector2(10f, 20f), 32f, 16f, 32f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(projectionObject);
        }

        [Test]
        public void KnownCellsUseDownRightAndDownLeftOrientation()
        {
            AssertVector(projection.CellToWorld(new SurfaceCell(0, 0)), 10f, 20f);
            AssertVector(projection.CellToWorld(new SurfaceCell(1, 0)), 10.5f, 19.75f);
            AssertVector(projection.CellToWorld(new SurfaceCell(0, 1)), 9.5f, 19.75f);
            AssertVector(projection.CellToWorld(new SurfaceCell(-1, 0)), 9.5f, 20.25f);
            AssertVector(projection.CellToWorld(new SurfaceCell(0, -1)), 10.5f, 20.25f);
        }

        [Test]
        public void WorldToCellResolvesPositiveAndNegativeCentres()
        {
            Assert.That(projection.WorldToCell(new Vector2(11f, 18.5f)),
                Is.EqualTo(new SurfaceCell(4, 2)));
            Assert.That(projection.WorldToCell(new Vector2(9f, 21.5f)),
                Is.EqualTo(new SurfaceCell(-4, -2)));
        }

        [Test]
        public void CellCentresRoundTripAcrossFutureSignedCoordinates()
        {
            for (int y = -40; y <= 40; y++)
            {
                for (int x = -40; x <= 40; x++)
                {
                    var cell = new SurfaceCell(x, y);
                    Assert.That(projection.WorldToCell(projection.CellToWorld(cell)),
                        Is.EqualTo(cell));
                }
            }
        }

        [Test]
        public void AdjacentCellsUseHalfDiamondSpacing()
        {
            Vector2 centre = projection.CellToWorld(new SurfaceCell(0, 0));
            AssertVector(projection.CellToWorld(new SurfaceCell(1, 0)) - centre,
                0.5f, -0.25f);
            AssertVector(projection.CellToWorld(new SurfaceCell(0, 1)) - centre,
                -0.5f, -0.25f);
        }

        [Test]
        public void DiamondBoundaryUsesDocumentedHalfOpenRule()
        {
            Vector2 centre = projection.CellToWorld(new SurfaceCell(0, 0));
            Vector2 nextX = projection.CellToWorld(new SurfaceCell(1, 0));
            Vector2 sharedEdge = Vector2.Lerp(centre, nextX, 0.5f);

            Assert.That(projection.WorldToCell(sharedEdge),
                Is.EqualTo(new SurfaceCell(1, 0)));
            Assert.That(projection.WorldToCell(
                    Vector2.Lerp(centre, nextX, 0.499f)),
                Is.EqualTo(new SurfaceCell(0, 0)));
        }

        [Test]
        public void ProjectionIsDeterministic()
        {
            var cell = new SurfaceCell(-17, 29);
            Vector2 expected = projection.CellToWorld(cell);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                Assert.That(projection.CellToWorld(cell), Is.EqualTo(expected));
                Assert.That(projection.WorldToCell(expected), Is.EqualTo(cell));
            }
        }

        [Test]
        public void ContinuousLogicalPositionsUseTheSameProjectionAuthority()
        {
            Vector2 start = projection.CellToWorld(new SurfaceCell(2, 3));
            Vector2 end = projection.CellToWorld(new SurfaceCell(3, 3));

            Assert.That(projection.LogicalToWorld(new Vector2(2.5f, 3f)),
                Is.EqualTo(Vector2.Lerp(start, end, 0.5f)));
        }

        [Test]
        public void FullSurfaceBoundsAndPointerRejectionUseProjectedDiamond()
        {
            Rect bounds = projection.GetWorldBounds(new SurfaceCell(0, 0), 64, 64);

            Assert.That(bounds.xMin, Is.EqualTo(-22f).Within(Tolerance));
            Assert.That(bounds.xMax, Is.EqualTo(42f).Within(Tolerance));
            Assert.That(bounds.yMin, Is.EqualTo(-11.75f).Within(Tolerance));
            Assert.That(bounds.yMax, Is.EqualTo(20.25f).Within(Tolerance));
            Assert.That(projection.TryWorldToCell(
                projection.CellToWorld(new SurfaceCell(63, 63)),
                new SurfaceCell(0, 0), 64, 64, out SurfaceCell last), Is.True);
            Assert.That(last, Is.EqualTo(new SurfaceCell(63, 63)));
            Assert.That(projection.TryWorldToCell(
                projection.CellToWorld(new SurfaceCell(64, 63)),
                new SurfaceCell(0, 0), 64, 64, out _), Is.False);
        }

        [Test]
        public void SelectedSpritesUseCalibratedPresentationImportSettings()
        {
            foreach (string spriteName in SelectedSpriteNames)
            {
                string path = SpriteFolder + spriteName + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                Assert.That(textureSettings.spriteGenerateFallbackPhysicsShape,
                    Is.False, path);
                Assert.That(textureSettings.spriteMeshType,
                    Is.EqualTo(SpriteMeshType.FullRect), path);
                Assert.That(importer.spritePivot,
                    Is.EqualTo(new Vector2(0.5f, 0.25f)), path);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(32f, 32f)), path);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(16f, 8f)), path);
            }
        }

        [Test]
        public void CalibrationSceneIsIsolatedAndContainsEightByEightPreview()
        {
            Scene scene = EditorSceneManager.OpenScene(
                PreviewScenePath,
                OpenSceneMode.Additive);

            try
            {
                IsometricCalibrationPreview preview = null;
                Camera previewCamera = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    preview ??= root.GetComponentInChildren<IsometricCalibrationPreview>();
                    previewCamera ??= root.GetComponentInChildren<Camera>();
                }

                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.PreviewRoot, Is.Not.Null);
                Assert.That(previewCamera, Is.Not.Null);
                Assert.That(previewCamera.orthographic, Is.True);

                int terrainCount = 0;
                foreach (Transform child in preview.PreviewRoot)
                {
                    if (child.name.StartsWith("Terrain ("))
                    {
                        terrainCount++;
                    }
                }

                Assert.That(terrainCount, Is.EqualTo(64));
                Assert.That(preview.WaterPatternRoot, Is.Not.Null);
                Assert.That(preview.WaterPatternRoot.childCount, Is.EqualTo(5));
                foreach (Transform pattern in preview.WaterPatternRoot)
                {
                    int patternTileCount = 0;
                    foreach (Transform child in pattern)
                    {
                        if (child.name.StartsWith("Pattern Tile ("))
                            patternTileCount++;
                    }
                    Assert.That(patternTileCount, Is.EqualTo(16), pattern.name);
                }
                Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(2),
                    "The preview must contain only its calibration root and camera.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertVector(Vector2 actual, float x, float y)
        {
            Assert.That(actual.x, Is.EqualTo(x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(y).Within(Tolerance));
        }
    }
}
