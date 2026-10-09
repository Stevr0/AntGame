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
    /// Verifies the complete authored-scene path for the manually reported
    /// Background refresh cell, including world and Tilemap coordinate mapping.
    /// </summary>
    public sealed class DevelopmentSceneBackgroundRefreshTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void PreparedToggleAtReportedCell_RefreshesExactBackgroundTilemapCell()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = FindOne<AntGameDevelopmentWorldHost>(scene);
            WorldTilemapRenderer renderer = FindOne<WorldTilemapRenderer>(scene);
            Assert.That(host, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(host.RebuildWorld(), Is.True);

            var worldCell = new Vector2Int(-1, 15);
            var expectedTilemapCell = new Vector3Int(-1, 15, 0);
            Vector2 worldPosition = WorldTileCoordinates.CellToWorldPosition(worldCell);
            Tilemap backgroundTilemap = renderer.BackgroundTilemap;

            Assert.That(
                WorldTileCoordinates.WorldPositionToCell(worldPosition),
                Is.EqualTo(worldCell),
                "Click/world-position conversion must return the displayed cell.");
            Assert.That(
                renderer.WorldPositionToWorldCell(worldPosition),
                Is.EqualTo(worldCell),
                "Development selection must use the displayed Tilemap Grid cell.");
            Assert.That(
                WorldTilemapRenderer.WorldCellToTilemapCell(worldCell),
                Is.EqualTo(expectedTilemapCell),
                "Authoritative and Tilemap cell coordinates must remain identical.");
            Assert.That(
                backgroundTilemap.WorldToCell(worldPosition),
                Is.EqualTo(expectedTilemapCell),
                "The authored Grid transform must map the cell centre identically.");
            Assert.That(
                (Vector2)backgroundTilemap.GetCellCenterWorld(expectedTilemapCell),
                Is.EqualTo(worldPosition),
                "The rendered Tile must be centred on the inspected world position.");

            Assert.That(host.TryGetCell(worldCell, out WorldTileCellData before), Is.True);
            Assert.That(before.HasSolidTile, Is.False);
            Assert.That(host.TogglePreparedBackground(worldCell), Is.True);
            Assert.That(host.TryGetCell(worldCell, out WorldTileCellData after), Is.True);
            Assert.That(after.BackgroundMaterialId,
                Is.EqualTo("background.prepared.grippable"));
            Assert.That(
                host.WorldData.TryGetBackgroundMaterialDefinition(
                    after.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition prepared),
                Is.True);

            TileBase renderedTile = backgroundTilemap.GetTile(expectedTilemapCell);
            Assert.That(renderedTile, Is.SameAs(prepared.VisualTile),
                "The exact reported cell must contain the prepared visual Tile.");
            Assert.That(renderer.ForegroundTilemap.HasTile(expectedTilemapCell), Is.False,
                "No foreground visual may obscure this exposed test cell.");
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .SingleOrDefault();
        }
    }
}
