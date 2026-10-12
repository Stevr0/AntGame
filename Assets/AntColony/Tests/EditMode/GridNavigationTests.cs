using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.Tests.EditMode
{
    public sealed class GridNavigationTests
    {
        private const string NaturalId = "background.grid-natural";
        private const string PreparedId = "background.grid-prepared";
        private const string SkyId = "background.grid-sky";
        private const string SolidId = "solid.grid-blocking";
        private readonly List<Object> temporaryObjects = new();
        private ChunkedWorldData world;
        private GridNavigationMap map;

        [SetUp]
        public void SetUp()
        {
            BackgroundTileMaterialDefinition natural = Background(
                NaturalId, true, 1f, false);
            BackgroundTileMaterialDefinition prepared = Background(
                PreparedId, true, 1.25f, false);
            BackgroundTileMaterialDefinition sky = Background(
                SkyId, false, 1f, true);
            SolidTileMaterialDefinition solid =
                Track(ScriptableObject.CreateInstance<SolidTileMaterialDefinition>());
            SetField(solid, "stableMaterialId", SolidId);
            SetField(solid, "blocksMovement", true);
            SetField(solid, "supportsNavigation", true);

            world = new ChunkedWorldData(
                new WorldMaterialRegistry(
                    new[] { natural, prepared, sky },
                    new[] { solid }),
                42);
            Assert.That(world.CreateChunk(Vector2Int.zero, NaturalId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            Assert.That(world.CreateChunk(Vector2Int.right, NaturalId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            map = new GridNavigationMap(world, AntCollisionProfile.OrdinaryAnt);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in temporaryObjects)
            {
                Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void Anchors_ClassifyGripAndGroundedSurfacePositions()
        {
            SetFloor(0, 4, 0);

            Assert.That(map.TryGetAnchor(new Vector2Int(2, 3), out GridNavigationAnchor grip), Is.True);
            Assert.That(grip.Kind, Is.EqualTo(GridNavigationAnchorKind.BackgroundGrip));
            Assert.That(map.TryGetAnchor(new Vector2Int(2, 1), out GridNavigationAnchor ground), Is.True);
            Assert.That(ground.Kind, Is.EqualTo(GridNavigationAnchorKind.Grounded));
            Assert.That(ground.Position.y, Is.EqualTo(0.65f).Within(0.001f));
        }

        [Test]
        public void EightDirectionRoute_DetoursAndCannotCutBlockedCorner()
        {
            SetSolid(new Vector2Int(1, 1));
            var pathfinder = new GridPathfinder(map);

            GridPathResult result = pathfinder.FindPath(
                Vector2Int.zero,
                new Vector2Int(2, 2));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Anchors.Count, Is.GreaterThan(3));
            Assert.That(result.Anchors[1].Cell,
                Is.Not.EqualTo(new Vector2Int(1, 1)));
        }

        [Test]
        public void WholeBodyClearance_RejectsBlockingDestination()
        {
            SetSolid(new Vector2Int(3, 3));

            Assert.That(map.TryGetAnchor(new Vector2Int(3, 3), out _), Is.False);
            Assert.That(new GridPathfinder(map).FindPath(
                Vector2Int.zero,
                new Vector2Int(3, 3)).Status,
                Is.EqualTo(GridPathStatus.DestinationInvalid));
        }

        [Test]
        public void PreparedRoute_HasLowerMovementTimeCost()
        {
            var pathfinder = new GridPathfinder(map);
            GridPathResult natural = pathfinder.FindPath(
                new Vector2Int(1, 4), new Vector2Int(8, 4));
            for (int x = 1; x <= 8; x++)
            {
                Assert.That(world.SetBackgroundMaterial(
                    new Vector2Int(x, 4), PreparedId),
                    Is.EqualTo(WorldCellMutationResult.Success));
                map.InvalidateCell(new Vector2Int(x, 4));
            }

            GridPathResult prepared = pathfinder.FindPath(
                new Vector2Int(1, 4), new Vector2Int(8, 4));

            Assert.That(natural.Succeeded, Is.True);
            Assert.That(prepared.Succeeded, Is.True);
            Assert.That(prepared.TravelTime, Is.LessThan(natural.TravelTime));
        }

        [Test]
        public void RouteCrossesChunkBoundaryWithoutGeneratingMissingChunks()
        {
            int chunksBefore = world.ResidentChunkCount;
            GridPathResult result = new GridPathfinder(map).FindPath(
                new Vector2Int(30, 6), new Vector2Int(34, 6));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunksBefore));
        }

        [Test]
        public void LocalInvalidation_ReclassifiesMutatedTerrainAndChangesRoute()
        {
            var pathfinder = new GridPathfinder(map);
            GridPathResult direct = pathfinder.FindPath(
                new Vector2Int(1, 8), new Vector2Int(5, 8));
            SetSolid(new Vector2Int(3, 8));
            int revisionBefore = map.Revision;
            map.InvalidateCell(new Vector2Int(3, 8));
            GridPathResult detour = pathfinder.FindPath(
                new Vector2Int(1, 8), new Vector2Int(5, 8));

            Assert.That(direct.Succeeded, Is.True);
            Assert.That(detour.Succeeded, Is.True);
            Assert.That(map.Revision, Is.EqualTo(revisionBefore + 1));
            Assert.That(detour.TravelTime, Is.GreaterThan(direct.TravelTime));
        }

        [Test]
        public void EnclosedExposedCell_IsCorrectlyUnreachable()
        {
            Vector2Int destination = new(12, 12);
            foreach (Vector2Int direction in new[]
            {
                Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down,
                new Vector2Int(-1, -1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(1, 1)
            })
            {
                SetSolid(destination + direction);
            }

            GridPathResult result = new GridPathfinder(map).FindPath(
                Vector2Int.zero, destination);

            Assert.That(result.Status, Is.EqualTo(GridPathStatus.Unreachable));
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(300)]
        [TestCase(500)]
        public void BenchmarkRepresentativeGridAntWorkloads(int antCount)
        {
            var pathfinder = new GridPathfinder(map);
            Vector2Int start = new(1, 16);
            Vector2Int destination = new(40, 16);
            GridPathResult warmup = pathfinder.FindPath(start, destination);
            Assert.That(warmup.Succeeded, Is.True);

            // Unity's Mono backend reports zero from
            // GetAllocatedBytesForCurrentThread on some editor versions. Keep
            // the path results alive and use the process-managed heap delta so
            // the benchmark always reports an observed allocation value.
            long allocatedBefore = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();
            var paths = new GridPathResult[antCount];
            int examined = 0;
            for (int index = 0; index < antCount; index++)
            {
                paths[index] = pathfinder.FindPath(start, destination);
                examined += paths[index].ExaminedNodeCount;
            }
            stopwatch.Stop();
            double pathMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            int arrived = SimulateSmoothTraversal(paths, 2f, 0.02f);
            stopwatch.Stop();
            double navigationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            // Results remain referenced, so a full collection leaves a stable
            // lower-bound measurement of managed memory retained by the batch.
            long allocated = Math.Max(
                0L,
                GC.GetTotalMemory(true) - allocatedBefore);

            var bodies = new List<GameObject>(antCount);
            SimulationMode2D previousMode = Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                for (int index = 0; index < antCount; index++)
                {
                    var item = new GameObject($"Benchmark Grid Ant {index}");
                    item.AddComponent<Rigidbody2D>().simulated = false;
                    bodies.Add(item);
                }

                stopwatch.Restart();
                for (int step = 0; step < 100; step++)
                {
                    Physics2D.Simulate(0.02f);
                }
                stopwatch.Stop();
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                foreach (GameObject item in bodies)
                {
                    Object.DestroyImmediate(item);
                }
            }

            bool failureCorrect = new GridPathfinder(map).FindPath(
                start, new Vector2Int(70, 16)).Status ==
                GridPathStatus.DestinationInvalid;
            UnityEngine.Debug.Log(
                $"GRID_BENCHMARK count={antCount} pathMs={pathMilliseconds:0.###} " +
                $"navMs={navigationMilliseconds:0.###} physicsMs={stopwatch.Elapsed.TotalMilliseconds:0.###} " +
                $"allocatedBytes={allocated} examined={examined} arrived={arrived} " +
                $"failedCorrectly={failureCorrect}");

            Assert.That(arrived, Is.EqualTo(antCount));
            Assert.That(failureCorrect, Is.True);
        }

        private static int SimulateSmoothTraversal(
            IReadOnlyList<GridPathResult> paths,
            float speed,
            float fixedDeltaTime)
        {
            int arrived = 0;
            foreach (GridPathResult path in paths)
            {
                Vector2 position = path.Anchors[0].Position;
                int anchor = 1;
                int guard = 0;
                while (anchor < path.Anchors.Count && guard++ < 100000)
                {
                    position = Vector2.MoveTowards(
                        position,
                        path.Anchors[anchor].Position,
                        speed * fixedDeltaTime);
                    if (position == path.Anchors[anchor].Position)
                    {
                        anchor++;
                    }
                }

                if (anchor == path.Anchors.Count)
                {
                    arrived++;
                }
            }

            return arrived;
        }

        private BackgroundTileMaterialDefinition Background(
            string id,
            bool grip,
            float speed,
            bool gravity)
        {
            BackgroundTileMaterialDefinition result =
                Track(ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>());
            SetField(result, "stableMaterialId", id);
            SetField(result, "allowsHorizontalMovement", true);
            SetField(result, "allowsUpwardMovement", grip);
            SetField(result, "allowsDownwardMovement", grip);
            SetField(result, "providesClimbingGrip", grip);
            SetField(result, "gravityAppliesWhenUnsupported", gravity);
            SetField(result, "movementSpeedMultiplier", speed);
            return result;
        }

        private void SetFloor(int minimumX, int maximumX, int y)
        {
            for (int x = minimumX; x <= maximumX; x++)
            {
                SetSolid(new Vector2Int(x, y));
            }
        }

        private void SetSolid(Vector2Int cell)
        {
            Assert.That(world.SetSolidMaterial(cell, SolidId),
                Is.EqualTo(WorldCellMutationResult.Success));
        }

        private T Track<T>(T item) where T : Object
        {
            temporaryObjects.Add(item);
            return item;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
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
