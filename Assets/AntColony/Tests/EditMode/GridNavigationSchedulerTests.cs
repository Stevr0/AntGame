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
    public sealed class GridNavigationSchedulerTests
    {
        private const string BackgroundId = "background.scheduler-natural";
        private const string PreparedId = "background.scheduler-prepared";
        private const string SolidId = "solid.scheduler-blocking";
        private readonly List<Object> temporaryObjects = new();
        private ChunkedWorldData world;
        private GridNavigationMap map;
        private GridNavigationScheduler scheduler;

        [SetUp]
        public void SetUp()
        {
            BackgroundTileMaterialDefinition background = Track(
                ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>());
            SetField(background, "stableMaterialId", BackgroundId);
            SetField(background, "allowsHorizontalMovement", true);
            SetField(background, "allowsUpwardMovement", true);
            SetField(background, "allowsDownwardMovement", true);
            SetField(background, "providesClimbingGrip", true);
            SetField(background, "gravityAppliesWhenUnsupported", false);
            SetField(background, "movementSpeedMultiplier", 1f);

            BackgroundTileMaterialDefinition prepared = Track(
                ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>());
            SetField(prepared, "stableMaterialId", PreparedId);
            SetField(prepared, "allowsHorizontalMovement", true);
            SetField(prepared, "allowsUpwardMovement", true);
            SetField(prepared, "allowsDownwardMovement", true);
            SetField(prepared, "providesClimbingGrip", true);
            SetField(prepared, "gravityAppliesWhenUnsupported", false);
            SetField(prepared, "movementSpeedMultiplier", 1.25f);

            SolidTileMaterialDefinition solid = Track(
                ScriptableObject.CreateInstance<SolidTileMaterialDefinition>());
            SetField(solid, "stableMaterialId", SolidId);
            SetField(solid, "blocksMovement", true);
            SetField(solid, "supportsNavigation", true);

            world = new ChunkedWorldData(
                new WorldMaterialRegistry(
                    new[] { background, prepared },
                    new[] { solid }),
                73);
            Assert.That(world.CreateChunk(Vector2Int.zero, BackgroundId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            Assert.That(world.CreateChunk(Vector2Int.right, BackgroundId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            for (int x = 0; x < 64; x++)
            {
                Assert.That(world.SetBackgroundMaterial(
                    new Vector2Int(x, 17), PreparedId),
                    Is.EqualTo(WorldCellMutationResult.Success));
            }
            map = new GridNavigationMap(world, AntCollisionProfile.OrdinaryAnt);
            scheduler = new GridNavigationScheduler(map);
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
        public void MultipleRequests_RespectTotalAndPerRequestBudgets()
        {
            GridNavigationRequestHandle first = Request(1, 1, 40);
            GridNavigationRequestHandle second = Request(2, 2, 41);
            GridNavigationRequestHandle third = Request(3, 3, 42);

            scheduler.ProcessUpdate(5, 2);

            Assert.That(scheduler.Diagnostics.NodesExpandedLastUpdate,
                Is.LessThanOrEqualTo(5));
            Assert.That(scheduler.Diagnostics.Pending, Is.EqualTo(3));
            Assert.That(scheduler.GetStatus(first.RequestId),
                Is.EqualTo(GridNavigationRequestStatus.Pending));
            Assert.That(scheduler.GetStatus(second.RequestId),
                Is.EqualTo(GridNavigationRequestStatus.Pending));
            Assert.That(scheduler.GetStatus(third.RequestId),
                Is.EqualTo(GridNavigationRequestStatus.Pending));
        }

        [Test]
        public void SingleLongSearch_YieldsAndResumesAcrossUpdates()
        {
            GridNavigationRequestHandle handle = Request(1, 1, 40);

            scheduler.ProcessUpdate(3, 3);
            Assert.That(scheduler.GetStatus(handle.RequestId),
                Is.EqualTo(GridNavigationRequestStatus.Pending));
            long nodesAfterFirstUpdate = scheduler.Diagnostics.TotalNodesExpanded;
            GridScheduledPathResult result = RunUntilComplete(handle);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.LatencyUpdates, Is.GreaterThan(1));
            Assert.That(scheduler.Diagnostics.TotalNodesExpanded,
                Is.GreaterThan(nodesAfterFirstUpdate));
        }

        [Test]
        public void PriorityFavoursHighButLowDoesNotStarve()
        {
            GridNavigationRequestHandle low = scheduler.RequestPath(
                1, new Vector2Int(1, 16), new Vector2Int(1, 16),
                GridNavigationRequestPriority.Low, 100, out _);
            GridNavigationRequestHandle high = scheduler.RequestPath(
                2, new Vector2Int(2, 16), new Vector2Int(2, 16),
                GridNavigationRequestPriority.High, 100, out _);

            scheduler.ProcessUpdate(1, 1);
            Assert.That(scheduler.TryTakeResult(high.RequestId, out var highResult),
                Is.True);
            Assert.That(highResult.Succeeded, Is.True);
            Assert.That(scheduler.TryTakeResult(low.RequestId, out _), Is.False);

            for (int update = 0; update < 6; update++)
            {
                scheduler.ProcessUpdate(1, 1);
            }

            Assert.That(scheduler.TryTakeResult(low.RequestId, out var lowResult),
                Is.True);
            Assert.That(lowResult.Succeeded, Is.True);
        }

        [Test]
        public void IdenticalOwnerRequest_IsReusedAndReplacementCancelsOldWork()
        {
            GridNavigationRequestHandle original = scheduler.RequestPath(
                9, new Vector2Int(1, 16), new Vector2Int(40, 16),
                GridNavigationRequestPriority.Normal, 8192, out bool created);
            GridNavigationRequestHandle duplicate = scheduler.RequestPath(
                9, new Vector2Int(1, 16), new Vector2Int(40, 16),
                GridNavigationRequestPriority.Normal, 8192, out bool duplicateCreated);
            GridNavigationRequestHandle replacement = scheduler.RequestPath(
                9, new Vector2Int(1, 16), new Vector2Int(35, 16),
                GridNavigationRequestPriority.Normal, 8192, out bool replacementCreated);

            Assert.That(created, Is.True);
            Assert.That(duplicateCreated, Is.False);
            Assert.That(duplicate.RequestId, Is.EqualTo(original.RequestId));
            Assert.That(replacementCreated, Is.True);
            Assert.That(replacement.RequestId, Is.Not.EqualTo(original.RequestId));
            Assert.That(scheduler.TryTakeResult(original.RequestId, out _), Is.False,
                "A replaced request is discarded and can never deliver stale data.");
            Assert.That(RunUntilComplete(replacement).Succeeded, Is.True);
        }

        [Test]
        public void CancellingQueuedOrActiveRequestPreventsSuccessDelivery()
        {
            GridNavigationRequestHandle queued = Request(1, 1, 40);
            Assert.That(scheduler.Cancel(queued.RequestId), Is.True);
            Assert.That(scheduler.TryTakeResult(queued.RequestId, out var queuedResult),
                Is.True);
            Assert.That(queuedResult.Status,
                Is.EqualTo(GridNavigationRequestStatus.Cancelled));

            GridNavigationRequestHandle active = Request(2, 2, 41);
            scheduler.ProcessUpdate(4, 4);
            Assert.That(scheduler.Cancel(active.RequestId), Is.True);
            Assert.That(scheduler.TryTakeResult(active.RequestId, out var activeResult),
                Is.True);
            Assert.That(activeResult.Status,
                Is.EqualTo(GridNavigationRequestStatus.Cancelled));
        }

        [Test]
        public void TerrainRevisionCancelsStaleSearchAndFreshRouteUsesMutation()
        {
            GridNavigationRequestHandle stale = Request(1, 1, 40);
            GridNavigationRequestHandle secondStale = Request(2, 2, 41);
            scheduler.ProcessUpdate(8, 8);
            Assert.That(world.SetSolidMaterial(new Vector2Int(20, 16), SolidId),
                Is.EqualTo(WorldCellMutationResult.Success));
            map.InvalidateCell(new Vector2Int(20, 16));
            scheduler.NotifyTerrainInvalidated();

            Assert.That(scheduler.TryTakeResult(stale.RequestId, out var staleResult),
                Is.True);
            Assert.That(staleResult.Status,
                Is.EqualTo(GridNavigationRequestStatus.Cancelled));
            Assert.That(scheduler.TryTakeResult(
                secondStale.RequestId,
                out var secondStaleResult), Is.True);
            Assert.That(secondStaleResult.Status,
                Is.EqualTo(GridNavigationRequestStatus.Cancelled));

            GridScheduledPathResult fresh = RunUntilComplete(Request(3, 1, 40));
            Assert.That(fresh.Succeeded, Is.True);
            foreach (GridNavigationAnchor anchor in fresh.Path.Anchors)
            {
                Assert.That(anchor.Cell, Is.Not.EqualTo(new Vector2Int(20, 16)));
            }
            Assert.That(scheduler.Diagnostics.CacheInvalidations, Is.EqualTo(1));
        }

        [Test]
        public void SearchLimitAndUnreachableReturnStableOutcomes()
        {
            GridNavigationRequestHandle limited = scheduler.RequestPath(
                1, new Vector2Int(1, 16), new Vector2Int(40, 16),
                GridNavigationRequestPriority.Normal, 2, out _);
            GridScheduledPathResult limitedResult = RunUntilComplete(limited);
            Assert.That(limitedResult.Status,
                Is.EqualTo(GridNavigationRequestStatus.SearchLimitReached));

            Vector2Int enclosed = new(12, 12);
            foreach (Vector2Int direction in Neighbours())
            {
                Assert.That(world.SetSolidMaterial(enclosed + direction, SolidId),
                    Is.EqualTo(WorldCellMutationResult.Success));
                map.InvalidateCell(enclosed + direction);
            }
            scheduler.NotifyTerrainInvalidated();
            GridNavigationRequestHandle unreachable = scheduler.RequestPath(
                2, new Vector2Int(1, 16), enclosed,
                GridNavigationRequestPriority.Normal, 8192, out _);
            Assert.That(RunUntilComplete(unreachable).Status,
                Is.EqualTo(GridNavigationRequestStatus.Unreachable));
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(300)]
        [TestCase(500)]
        public void BenchmarkScheduledRequests(int antCount)
        {
            var warmupTimer = Stopwatch.StartNew();
            GridPathResult warmup = new GridPathfinder(map).FindPath(
                new Vector2Int(1, 16),
                new Vector2Int(43, 16));
            warmupTimer.Stop();
            Assert.That(warmup.Succeeded, Is.True);
            var handles = new GridNavigationRequestHandle[antCount];
            long memoryBefore = GC.GetTotalMemory(true);
            var totalTimer = Stopwatch.StartNew();
            for (int index = 0; index < antCount; index++)
            {
                handles[index] = scheduler.RequestPath(
                    index + 1,
                    new Vector2Int(1 + index % 4, 16),
                    new Vector2Int(40 + index % 4, 16),
                    index % 9 == 0
                        ? GridNavigationRequestPriority.High
                        : index % 5 == 0
                            ? GridNavigationRequestPriority.Low
                            : GridNavigationRequestPriority.Normal,
                    8192,
                    out _);
            }

            int completed = 0;
            int successful = 0;
            var completedResults = new GridScheduledPathResult[antCount];
            long latencyTotal = 0;
            long worstLatency = 0;
            double travelTotal = 0d;
            double worstTravel = 0d;
            int updateCount = 0;
            double schedulerProcessingMilliseconds = 0d;
            while (completed < antCount && updateCount++ < 10000)
            {
                scheduler.ProcessUpdate(128, 8);
                schedulerProcessingMilliseconds +=
                    scheduler.Diagnostics.LastUpdateMilliseconds;
                for (int index = 0; index < handles.Length; index++)
                {
                    if (!handles[index].IsValid ||
                        !scheduler.TryTakeResult(
                            handles[index].RequestId,
                            out GridScheduledPathResult result))
                    {
                        continue;
                    }

                    handles[index] = default;
                    completedResults[index] = result;
                    completed++;
                    if (result.Succeeded)
                    {
                        successful++;
                        double travel = result.Path.TravelTime / 2d;
                        travelTotal += travel;
                        worstTravel = Math.Max(worstTravel, travel);
                    }
                    latencyTotal += result.LatencyUpdates;
                    worstLatency = Math.Max(worstLatency, result.LatencyUpdates);
                }
            }
            totalTimer.Stop();
            double physicsMilliseconds = MeasureDisabledBodyPhysics(antCount);
            long retainedBytes = Math.Max(
                0L,
                GC.GetTotalMemory(true) - memoryBefore);
            GridNavigationSchedulerDiagnostics diagnostics = scheduler.Diagnostics;
            UnityEngine.Debug.Log(
                $"GRID_SCHEDULER_BENCHMARK count={antCount} " +
                $"cacheWarmupMs={warmupTimer.Elapsed.TotalMilliseconds:0.###} " +
                $"batchMs={totalTimer.Elapsed.TotalMilliseconds:0.###} " +
                $"schedulerMs={schedulerProcessingMilliseconds:0.###} " +
                $"harnessMs={Math.Max(0d, totalTimer.Elapsed.TotalMilliseconds - schedulerProcessingMilliseconds):0.###} " +
                $"maxUpdateMs={diagnostics.MaximumUpdateMilliseconds:0.###} " +
                $"avgLatencyUpdates={(double)latencyTotal / antCount:0.###} " +
                $"worstLatencyUpdates={worstLatency} " +
                $"nodesPerUpdateBudget=128 totalNodes={diagnostics.TotalNodesExpanded} " +
                $"updates={updateCount} peakQueue={diagnostics.PeakQueueLength} " +
                $"invalidations={diagnostics.CacheInvalidations} " +
                $"avgTravelSeconds={travelTotal / Math.Max(1, successful):0.###} " +
                $"worstTravelSeconds={worstTravel:0.###} " +
                $"physics100StepsMs={physicsMilliseconds:0.###} " +
                $"retainedBytes={retainedBytes} successes={successful} failures={completed - successful}");

            Assert.That(completed, Is.EqualTo(antCount));
            Assert.That(successful, Is.EqualTo(antCount));
            Assert.That(diagnostics.NodesExpandedLastUpdate, Is.LessThanOrEqualTo(128));
            GC.KeepAlive(completedResults);
        }

        private static double MeasureDisabledBodyPhysics(int antCount)
        {
            var bodies = new List<GameObject>(antCount);
            SimulationMode2D previous = Physics2D.simulationMode;
            var timer = new Stopwatch();
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                for (int index = 0; index < antCount; index++)
                {
                    var item = new GameObject($"Scheduled Benchmark Ant {index}");
                    item.AddComponent<Rigidbody2D>().simulated = false;
                    bodies.Add(item);
                }

                timer.Start();
                for (int step = 0; step < 100; step++)
                {
                    Physics2D.Simulate(0.02f);
                }
                timer.Stop();
            }
            finally
            {
                Physics2D.simulationMode = previous;
                foreach (GameObject item in bodies)
                {
                    Object.DestroyImmediate(item);
                }
            }

            return timer.Elapsed.TotalMilliseconds;
        }

        private GridNavigationRequestHandle Request(int owner, int startX, int endX)
        {
            return scheduler.RequestPath(
                owner,
                new Vector2Int(startX, 16),
                new Vector2Int(endX, 16),
                GridNavigationRequestPriority.Normal,
                8192,
                out _);
        }

        private GridScheduledPathResult RunUntilComplete(
            GridNavigationRequestHandle handle)
        {
            for (int update = 0; update < 10000; update++)
            {
                scheduler.ProcessUpdate(32, 4);
                if (scheduler.TryTakeResult(handle.RequestId, out var result))
                {
                    return result;
                }
            }

            Assert.Fail("Scheduled request did not complete within the test bound.");
            return default;
        }

        private static IEnumerable<Vector2Int> Neighbours()
        {
            yield return Vector2Int.left;
            yield return Vector2Int.right;
            yield return Vector2Int.up;
            yield return Vector2Int.down;
            yield return new Vector2Int(-1, -1);
            yield return new Vector2Int(1, -1);
            yield return new Vector2Int(-1, 1);
            yield return new Vector2Int(1, 1);
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
