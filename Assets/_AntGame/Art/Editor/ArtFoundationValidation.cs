using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AntColony.EditorTools
{
    /// <summary>
    /// Checks that the committed art foundation imports exactly as documented.
    /// This validator is Editor-only and does not create or modify gameplay assets.
    /// </summary>
    public static class ArtFoundationValidation
    {
        private const string ColonySource =
            "Assets/_AntGame/Art/Colony/Source/Colony_Tileset_Source.png";
        private const string ColonyPrepared =
            "Assets/_AntGame/Art/Colony/Prepared/Colony_Tile_Candidates_32.png";
        private const string WorldSource =
            "Assets/_AntGame/Art/World/Source/World_Tileset_Source.png";
        private const string WorldPrepared =
            "Assets/_AntGame/Art/World/Prepared/World_Tile_Candidates_32.png";

        [MenuItem("Ant Colony/Validation/Validate Art Foundation")]
        public static void ValidateFromMenu()
        {
            ValidateAll();
            Debug.Log("Art foundation validation passed.");
        }

        /// <summary>
        /// Batch-mode entry point used by repository validation commands.
        /// An exception makes Unity return a failed process result.
        /// </summary>
        public static void ValidateBatch()
        {
            ValidateAll();
            Debug.Log("Art foundation batch validation passed: 170 candidate sprites.");
        }

        private static void ValidateAll()
        {
            ValidateSource(ColonySource);
            ValidateSource(WorldSource);
            ValidatePrepared(ColonyPrepared, 320, 320, 100);
            ValidatePrepared(WorldPrepared, 320, 224, 70);
            ValidateColonyRuntimeMapping();
        }

        private static void ValidateSource(string assetPath)
        {
            Texture2D texture = LoadTexture(assetPath);
            Require(
                texture.width == 1254 && texture.height == 1254,
                $"{assetPath} must remain the original 1254x1254 source.");

            TextureImporter importer = LoadImporter(assetPath);
            Require(importer.textureType == TextureImporterType.Sprite, assetPath);
            Require(importer.spriteImportMode == SpriteImportMode.Single, assetPath);
            ValidateSharedImportSettings(importer, assetPath);
        }

        private static void ValidatePrepared(
            string assetPath,
            int expectedWidth,
            int expectedHeight,
            int expectedSpriteCount)
        {
            Texture2D texture = LoadTexture(assetPath);
            Require(
                texture.width == expectedWidth && texture.height == expectedHeight,
                $"{assetPath} has unexpected prepared dimensions.");

            TextureImporter importer = LoadImporter(assetPath);
            Require(importer.textureType == TextureImporterType.Sprite, assetPath);
            Require(importer.spriteImportMode == SpriteImportMode.Multiple, assetPath);
            ValidateSharedImportSettings(importer, assetPath);

            int spriteCount = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .Count();
            Require(
                spriteCount == expectedSpriteCount,
                $"{assetPath} imported {spriteCount} sprites; expected {expectedSpriteCount}.");
        }

        private static void ValidateSharedImportSettings(
            TextureImporter importer,
            string assetPath)
        {
            Require(Mathf.Approximately(importer.spritePixelsPerUnit, 32f), assetPath);
            Require(importer.filterMode == FilterMode.Point, assetPath);
            Require(importer.textureCompression == TextureImporterCompression.Uncompressed, assetPath);
            Require(!importer.mipmapEnabled, assetPath);
            Require(importer.alphaIsTransparency, assetPath);
        }

        private static void ValidateColonyRuntimeMapping()
        {
            ValidateMappedTile(
                "Assets/AntColony/Art/WorldTiles/Solid_Soil.asset",
                "Colony_R02_C01");
            ValidateMappedTile(
                "Assets/AntColony/Art/WorldTiles/Background_UnpreparedEarth.asset",
                "Colony_R02_C01");
            ValidateMappedTile(
                "Assets/AntColony/Art/WorldTiles/Solid_Stone.asset",
                "Colony_R04_C01");
            ValidateMappedTile(
                "Assets/AntColony/Art/WorldTiles/Background_UnpreparedStone.asset",
                "Colony_R04_C01");
        }

        private static void ValidateMappedTile(string assetPath, string spriteName)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            Require(tile != null, $"Missing mapped Tile asset: {assetPath}");
            Require(tile.sprite != null && tile.sprite.name == spriteName,
                $"{assetPath} must use reviewed candidate {spriteName}.");
            Require(tile.colliderType == Tile.ColliderType.None,
                $"{assetPath} must not create sprite collision authority.");
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Missing art texture: {assetPath}");
            }

            return texture;
        }

        private static TextureImporter LoadImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Missing TextureImporter: {assetPath}");
            }

            return importer;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    $"Art foundation validation failed: {message}");
            }
        }
    }
}
