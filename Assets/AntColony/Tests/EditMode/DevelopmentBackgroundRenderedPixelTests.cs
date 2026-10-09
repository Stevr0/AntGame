using System.Linq;
using AntColony.Development;
using AntColony.World;
using AntColony.WorldPresentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Graphics-backed verification that checks the pixel emitted by the authored
    /// camera rather than only checking authoritative or Tilemap object state.
    /// </summary>
    public sealed class DevelopmentBackgroundRenderedPixelTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        private RenderTexture renderTexture;
        private Texture2D readbackTexture;
        private Camera testCamera;

        [TearDown]
        public void TearDown()
        {
            RenderTexture.active = null;
            if (testCamera != null)
            {
                testCamera.targetTexture = null;
            }
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
            }
            if (readbackTexture != null)
            {
                Object.DestroyImmediate(readbackTexture);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void PreparedToggle_ChangesRenderedPixelAndRevertsToEarthColour()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = FindOne<AntGameDevelopmentWorldHost>(scene);
            WorldTilemapRenderer renderer = FindOne<WorldTilemapRenderer>(scene);
            Camera camera = FindOne<Camera>(scene);
            testCamera = camera;
            Assert.That(host.RebuildWorld(), Is.True);

            var worldCell = new Vector2Int(-1, 15);
            Vector2 worldPosition = WorldTileCoordinates.CellToWorldPosition(worldCell);
            camera.transform.position = new Vector3(worldPosition.x, worldPosition.y, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.magenta;

            // URP 2D's render-graph path requires a depth/stencil attachment for
            // camera output textures, even though these tiles are visually 2D.
            renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            readbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            camera.targetTexture = renderTexture;

            Color earthPixel = RenderCentrePixel(camera);
            Assert.That(host.TogglePreparedBackground(worldCell), Is.True);
            Color preparedPixel = RenderCentrePixel(camera);
            Vector3Int tilemapCell = WorldTilemapRenderer.WorldCellToTilemapCell(worldCell);
            Tile preparedTile = renderer.BackgroundTilemap.GetTile<Tile>(tilemapCell);
            Color cellColourAfterSetTile = renderer.BackgroundTilemap.GetColor(tilemapCell);
            TileFlags preparedFlags = renderer.BackgroundTilemap.GetTileFlags(tilemapCell);

            renderer.SetForegroundVisible(false);
            Color isolatedPreparedPixel = RenderCentrePixel(camera);
            Vector3 renderedCellCentre = renderer.BackgroundTilemap.GetCellCenterWorld(
                WorldTilemapRenderer.WorldCellToTilemapCell(worldCell));
            TestContext.WriteLine(
                $"Earth={earthPixel}; Prepared={preparedPixel}; " +
                $"Prepared isolated={isolatedPreparedPixel}; " +
                $"Tile colour={preparedTile.color}; Cell colour={cellColourAfterSetTile}; " +
                $"Cell flags={preparedFlags}; Cell centre={renderedCellCentre}");
            AssertColoursClose(preparedPixel, isolatedPreparedPixel, 0.02f,
                "Foreground visuals must not cover the reported exposed cell.");

            AssertColoursClose(preparedTile.color, cellColourAfterSetTile, 0.01f,
                "The replacement cell must receive the prepared Tile's authored colour.");
            Assert.That(ColourDistance(earthPixel, preparedPixel), Is.GreaterThan(0.1f),
                "The actual rendered pixel must change after preparation.");
            Assert.That(preparedPixel.g, Is.GreaterThan(preparedPixel.r),
                "The prepared pixel must visibly favour green over red.");

            Assert.That(host.TogglePreparedBackground(worldCell), Is.True);
            Color revertedPixel = RenderCentrePixel(camera);
            AssertColoursClose(earthPixel, revertedPixel, 0.02f,
                "The rendered pixel must return to its original Earth colour.");
        }

        private Color RenderCentrePixel(Camera camera)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            readbackTexture.ReadPixels(new Rect(64f, 64f, 1f, 1f), 0, 0, false);
            readbackTexture.Apply(false, false);
            Color pixel = readbackTexture.GetPixel(0, 0);
            RenderTexture.active = previous;
            return pixel;
        }

        private static float ColourDistance(Color first, Color second)
        {
            return Mathf.Sqrt(
                Mathf.Pow(first.r - second.r, 2f) +
                Mathf.Pow(first.g - second.g, 2f) +
                Mathf.Pow(first.b - second.b, 2f));
        }

        private static void AssertColoursClose(
            Color expected,
            Color actual,
            float tolerance,
            string message)
        {
            Assert.That(ColourDistance(expected, actual), Is.LessThan(tolerance), message);
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Single();
        }
    }
}
