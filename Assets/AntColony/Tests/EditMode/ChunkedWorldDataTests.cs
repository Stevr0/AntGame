using System;
using System.Collections.Generic;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Verifies the data-only invariants for sparse resident chunks. These tests
    /// deliberately create no GameObjects and require no ant movement component.
    /// </summary>
    public sealed class ChunkedWorldDataTests
    {
        private const string DefaultBackgroundId = "background.default";
        private const string AlternateBackgroundId = "background.alternate";
        private const string SoilSolidId = "solid.soil";
        private const string RockSolidId = "solid.rock";

        private readonly List<ScriptableObject> temporaryDefinitions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject definition in temporaryDefinitions)
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }

            temporaryDefinitions.Clear();
        }

        [TestCase(0, 0, 0, 0, 0, 0)]
        [TestCase(31, 31, 0, 0, 31, 31)]
        [TestCase(32, 0, 1, 0, 0, 0)]
        [TestCase(-1, 0, -1, 0, 31, 0)]
        [TestCase(-32, -32, -1, -1, 0, 0)]
        [TestCase(-33, 5, -2, 0, 31, 5)]
        [TestCase(0, -1, 0, -1, 0, 31)]
        [TestCase(0, -33, 0, -2, 0, 31)]
        public void CoordinateConversion_UsesFloorDivisionForEveryQuadrant(
            int worldX,
            int worldY,
            int chunkX,
            int chunkY,
            int localX,
            int localY)
        {
            var worldCell = new Vector2Int(worldX, worldY);
            var expectedChunk = new Vector2Int(chunkX, chunkY);
            var expectedLocal = new Vector2Int(localX, localY);

            Assert.That(
                WorldTileCoordinates.GetChunkCoordinate(worldCell),
                Is.EqualTo(expectedChunk));
            Assert.That(
                WorldTileCoordinates.GetLocalCellCoordinate(worldCell),
                Is.EqualTo(expectedLocal));
            Assert.That(
                WorldTileCoordinates.GetWorldCellCoordinate(expectedChunk, expectedLocal),
                Is.EqualTo(worldCell));
        }

        [Test]
        public void CoordinateConversion_RoundTripsRepresentativeCells()
        {
            Vector2Int[] cells =
            {
                new(-97, 96),
                new(-33, -65),
                new(-1, -1),
                Vector2Int.zero,
                new(31, 32),
                new(95, -64)
            };

            foreach (Vector2Int cell in cells)
            {
                Vector2Int chunk = WorldTileCoordinates.GetChunkCoordinate(cell);
                Vector2Int local = WorldTileCoordinates.GetLocalCellCoordinate(cell);
                Assert.That(
                    WorldTileCoordinates.GetWorldCellCoordinate(chunk, local),
                    Is.EqualTo(cell));
                Assert.That(
                    WorldTileCoordinates.WorldPositionToCell(
                        WorldTileCoordinates.CellToWorldPosition(cell)),
                    Is.EqualTo(cell));
            }
        }

        [TestCase(0.499f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(-0.499f, 0)]
        [TestCase(-0.5f, 0)]
        [TestCase(-0.501f, -1)]
        [TestCase(31.499f, 31)]
        [TestCase(31.5f, 32)]
        public void WorldPositionToCell_UsesIntegerCentres(float worldX, int expectedCellX)
        {
            Vector2Int cell = WorldTileCoordinates.WorldPositionToCell(
                new Vector2(worldX, 0f));
            Assert.That(cell, Is.EqualTo(new Vector2Int(expectedCellX, 0)));
        }

        [Test]
        public void ChunkCreation_IsExplicitAndRejectsDuplicates()
        {
            ChunkedWorldData world = CreateWorldData();
            var coordinate = new Vector2Int(2, -3);

            Assert.That(
                world.CreateChunk(coordinate, DefaultBackgroundId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            Assert.That(
                world.CreateChunk(coordinate, AlternateBackgroundId),
                Is.EqualTo(WorldChunkCreationResult.ChunkAlreadyExists));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(1));
            Assert.That(world.ContainsChunk(coordinate), Is.True);
        }

        [Test]
        public void MissingChunkRead_IsUnavailableAndDoesNotGenerateNeighbours()
        {
            ChunkedWorldData world = CreateWorldData();
            world.CreateChunk(Vector2Int.zero, DefaultBackgroundId);

            WorldCellQueryResult result = world.GetCell(
                new Vector2Int(WorldTileCoordinates.ChunkSize, 0),
                out WorldTileCellData cellData);

            Assert.That(result, Is.EqualTo(WorldCellQueryResult.ChunkUnavailable));
            Assert.That(cellData.BackgroundMaterialId, Is.Null);
            Assert.That(world.ResidentChunkCount, Is.EqualTo(1));
            Assert.That(world.ContainsChunk(Vector2Int.right), Is.False);
        }

        [Test]
        public void GeneratedChunk_HasBackgroundInAll1024Cells()
        {
            ChunkedWorldData world = CreateWorldData();
            var chunkCoordinate = new Vector2Int(-2, 3);
            world.CreateChunk(chunkCoordinate, DefaultBackgroundId);

            for (int y = 0; y < WorldTileCoordinates.ChunkSize; y++)
            {
                for (int x = 0; x < WorldTileCoordinates.ChunkSize; x++)
                {
                    Vector2Int worldCell = WorldTileCoordinates.GetWorldCellCoordinate(
                        chunkCoordinate,
                        new Vector2Int(x, y));
                    Assert.That(
                        world.GetCell(worldCell, out WorldTileCellData cellData),
                        Is.EqualTo(WorldCellQueryResult.Available));
                    Assert.That(cellData.BackgroundMaterialId, Is.EqualTo(DefaultBackgroundId));
                    Assert.That(cellData.HasSolidTile, Is.False);
                }
            }
        }

        [Test]
        public void InvalidBackgroundIds_AreRejectedWithoutCreatingOrClearingTiles()
        {
            ChunkedWorldData world = CreateWorldData();

            Assert.That(
                world.CreateChunk(Vector2Int.zero, "background.unknown"),
                Is.EqualTo(WorldChunkCreationResult.InvalidBackgroundMaterial));
            Assert.That(world.ResidentChunkCount, Is.Zero);

            world.CreateChunk(Vector2Int.zero, DefaultBackgroundId);
            Assert.That(
                world.SetBackgroundMaterial(Vector2Int.zero, null),
                Is.EqualTo(WorldCellMutationResult.InvalidMaterial));
            Assert.That(
                world.GetBackgroundMaterialId(Vector2Int.zero, out string backgroundId),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(backgroundId, Is.EqualTo(DefaultBackgroundId));
        }

        [Test]
        public void SolidAssignmentAndRemoval_PreserveBackgroundTile()
        {
            ChunkedWorldData world = CreateWorldData();
            var cell = new Vector2Int(-1, 0);
            world.CreateChunk(new Vector2Int(-1, 0), DefaultBackgroundId);

            Assert.That(
                world.GetCell(cell, out WorldTileCellData exposedCell),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(exposedCell.HasSolidTile, Is.False);

            Assert.That(
                world.SetSolidMaterial(cell, SoilSolidId),
                Is.EqualTo(WorldCellMutationResult.Success));
            world.GetCell(cell, out WorldTileCellData coveredCell);
            Assert.That(coveredCell.BackgroundMaterialId, Is.EqualTo(DefaultBackgroundId));
            Assert.That(coveredCell.SolidMaterialId, Is.EqualTo(SoilSolidId));

            Assert.That(
                world.RemoveSolidMaterial(cell),
                Is.EqualTo(WorldCellMutationResult.Success));
            world.GetCell(cell, out WorldTileCellData removedCell);
            Assert.That(removedCell.BackgroundMaterialId, Is.EqualTo(DefaultBackgroundId));
            Assert.That(removedCell.HasSolidTile, Is.False);
            Assert.That(
                world.RemoveSolidMaterial(cell),
                Is.EqualTo(WorldCellMutationResult.SolidTileNotPresent));
        }

        [Test]
        public void InvalidSolidIdsAndMissingChunks_AreRejectedExplicitly()
        {
            ChunkedWorldData world = CreateWorldData();
            world.CreateChunk(Vector2Int.zero, DefaultBackgroundId);

            Assert.That(
                world.SetSolidMaterial(Vector2Int.zero, "solid.unknown"),
                Is.EqualTo(WorldCellMutationResult.InvalidMaterial));
            Assert.That(
                world.SetSolidMaterial(new Vector2Int(32, 0), SoilSolidId),
                Is.EqualTo(WorldCellMutationResult.ChunkUnavailable));
            Assert.That(
                world.RemoveSolidMaterial(new Vector2Int(32, 0)),
                Is.EqualTo(WorldCellMutationResult.ChunkUnavailable));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(1));
        }

        [Test]
        public void CellAndChunkMutations_AreIsolated()
        {
            ChunkedWorldData world = CreateWorldData();
            world.CreateChunk(Vector2Int.zero, DefaultBackgroundId);
            world.CreateChunk(Vector2Int.right, AlternateBackgroundId);

            world.SetSolidMaterial(new Vector2Int(31, 0), SoilSolidId);
            world.SetSolidMaterial(new Vector2Int(32, 0), RockSolidId);
            world.SetBackgroundMaterial(new Vector2Int(0, 0), AlternateBackgroundId);

            world.GetCell(new Vector2Int(31, 0), out WorldTileCellData leftEdge);
            world.GetCell(new Vector2Int(30, 0), out WorldTileCellData leftNeighbour);
            world.GetCell(new Vector2Int(32, 0), out WorldTileCellData rightEdge);

            Assert.That(leftEdge.SolidMaterialId, Is.EqualTo(SoilSolidId));
            Assert.That(leftNeighbour.SolidMaterialId, Is.Null);
            Assert.That(leftNeighbour.BackgroundMaterialId, Is.EqualTo(DefaultBackgroundId));
            Assert.That(rightEdge.SolidMaterialId, Is.EqualTo(RockSolidId));
            Assert.That(rightEdge.BackgroundMaterialId, Is.EqualTo(AlternateBackgroundId));
        }

        [Test]
        public void WorldExpansion_CreatesIndependentChunksInAllDirections()
        {
            ChunkedWorldData world = CreateWorldData(worldSeed: 8675309);
            Vector2Int[] coordinates =
            {
                Vector2Int.zero,
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down,
                new(-4, 7),
                new(5, -6)
            };

            foreach (Vector2Int coordinate in coordinates)
            {
                Assert.That(
                    world.CreateChunk(coordinate, DefaultBackgroundId),
                    Is.EqualTo(WorldChunkCreationResult.Success));
            }

            Assert.That(world.ResidentChunkCount, Is.EqualTo(coordinates.Length));
            Assert.That(world.WorldSeed, Is.EqualTo(8675309));

            world.SetSolidMaterial(new Vector2Int(-1, 0), SoilSolidId);
            world.GetCell(new Vector2Int(-1, 0), out WorldTileCellData negativeCell);
            world.GetCell(new Vector2Int(0, 0), out WorldTileCellData originCell);
            Assert.That(negativeCell.SolidMaterialId, Is.EqualTo(SoilSolidId));
            Assert.That(originCell.SolidMaterialId, Is.Null);
        }

        [Test]
        public void WorldData_IsNotAComponentAndCreatesNoGameplayObjects()
        {
            int gameObjectCountBefore = Resources.FindObjectsOfTypeAll<GameObject>().Length;
            ChunkedWorldData world = CreateWorldData();
            world.CreateChunk(Vector2Int.zero, DefaultBackgroundId);
            int gameObjectCountAfter = Resources.FindObjectsOfTypeAll<GameObject>().Length;

            Assert.That(typeof(ChunkedWorldData).IsSubclassOf(typeof(Component)), Is.False);
            Assert.That(typeof(WorldChunk).IsSubclassOf(typeof(Component)), Is.False);
            Assert.That(gameObjectCountAfter, Is.EqualTo(gameObjectCountBefore));
        }

        private ChunkedWorldData CreateWorldData(int worldSeed = 0)
        {
            BackgroundTileMaterialDefinition defaultBackground =
                CreateDefinition<BackgroundTileMaterialDefinition>(
                    DefaultBackgroundId,
                    "Default Background");
            BackgroundTileMaterialDefinition alternateBackground =
                CreateDefinition<BackgroundTileMaterialDefinition>(
                    AlternateBackgroundId,
                    "Alternate Background");
            SolidTileMaterialDefinition soil =
                CreateDefinition<SolidTileMaterialDefinition>(SoilSolidId, "Soil");
            SolidTileMaterialDefinition rock =
                CreateDefinition<SolidTileMaterialDefinition>(RockSolidId, "Rock");

            var registry = new WorldMaterialRegistry(
                new[] { defaultBackground, alternateBackground },
                new[] { soil, rock });
            return new ChunkedWorldData(registry, worldSeed);
        }

        private TDefinition CreateDefinition<TDefinition>(string materialId, string displayName)
            where TDefinition : WorldTileMaterialDefinition
        {
            TDefinition definition = ScriptableObject.CreateInstance<TDefinition>();
            definition.name = displayName;

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stableMaterialId").stringValue = materialId;
            serializedDefinition.FindProperty("displayName").stringValue = displayName;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            temporaryDefinitions.Add(definition);
            return definition;
        }
    }
}
