using System.Collections.Generic;
using System.Linq;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    public sealed class ProceduralWorldGenerationTests
    {
        private static readonly long[] SeedCorpus = { 0L, 1L, 2203L, -918273645L };

        [TestCaseSource(nameof(SeedCorpus))]
        public void SeedCorpusProducesValidDeterministicWorlds(long seed)
        {
            Assert.That(ProceduralWorldGenerator.TryGenerate(seed,
                new WorldGenerationConfiguration(), out GeneratedWorldSnapshot first,
                out GeneratedWorldValidationReport firstReport), Is.True,
                firstReport.Summary);
            Assert.That(ProceduralWorldGenerator.TryGenerate(seed,
                new WorldGenerationConfiguration(), out GeneratedWorldSnapshot second,
                out GeneratedWorldValidationReport secondReport), Is.True,
                secondReport.Summary);
            Assert.That(second.CreateFingerprint(), Is.EqualTo(first.CreateFingerprint()));
            Assert.That(first.Metadata.Seed, Is.EqualTo(seed));
            Assert.That(first.Metadata.Version,
                Is.EqualTo(WorldGenerationConfiguration.CurrentVersion));
            Assert.That(first.Metadata.ConfigurationId,
                Is.EqualTo(WorldGenerationConfiguration.CurrentConfigurationId));
        }

        [Test]
        public void DifferentSeedsProduceMeaningfullyDifferentLayouts()
        {
            ProceduralWorldGenerator.TryGenerate(2203, new WorldGenerationConfiguration(),
                out GeneratedWorldSnapshot first, out _);
            ProceduralWorldGenerator.TryGenerate(2204, new WorldGenerationConfiguration(),
                out GeneratedWorldSnapshot second, out _);
            Assert.That(second.CreateFingerprint(), Is.Not.EqualTo(first.CreateFingerprint()));
        }

        [Test]
        public void GeneratedColonyNodesUseApprovedVariableQuantityRanges()
        {
            var config = new WorldGenerationConfiguration();
            Assert.That(ProceduralWorldGenerator.TryGenerate(2203, config,
                out GeneratedWorldSnapshot world, out GeneratedWorldValidationReport report),
                Is.True, report.Summary);

            GeneratedResourceNodeRecord[] soil = world.Nodes.Where(node =>
                node.Location.Kind == RegionLocationKind.Colony &&
                node.DefinitionId == ProceduralWorldGenerator.SoilNodeId).ToArray();
            GeneratedResourceNodeRecord[] stone = world.Nodes.Where(node =>
                node.Location.Kind == RegionLocationKind.Colony &&
                node.DefinitionId == ProceduralWorldGenerator.StoneNodeId).ToArray();

            Assert.That(soil, Is.Not.Empty);
            Assert.That(stone, Is.Not.Empty);
            Assert.That(soil.All(node => node.InitialQuantity >= 8 &&
                node.InitialQuantity <= 12), Is.True);
            Assert.That(stone.All(node => node.InitialQuantity >= 5 &&
                node.InitialQuantity <= 15), Is.True);
            Assert.That(soil.Select(node => node.InitialQuantity).Distinct().Count(),
                Is.GreaterThan(1));
            Assert.That(stone.Select(node => node.InitialQuantity).Distinct().Count(),
                Is.GreaterThan(1));
        }

        [Test]
        public void ColonyNodeQuantityIsStablePerNodeAndVariesAcrossSeeds()
        {
            var config = new WorldGenerationConfiguration();
            ProceduralWorldGenerator.TryGenerate(2203, config,
                out GeneratedWorldSnapshot first, out _);
            ProceduralWorldGenerator.TryGenerate(2203, config,
                out GeneratedWorldSnapshot repeat, out _);
            Dictionary<string, int> firstQuantities = first.Nodes
                .Where(node => node.Location.Kind == RegionLocationKind.Colony)
                .ToDictionary(node => node.Id, node => node.InitialQuantity);
            Dictionary<string, int> repeatQuantities = repeat.Nodes
                .Where(node => node.Location.Kind == RegionLocationKind.Colony)
                .ToDictionary(node => node.Id, node => node.InitialQuantity);
            Assert.That(repeatQuantities, Is.EquivalentTo(firstQuantities));

            int different = 0;
            for (int column = 0; column < 32; column++)
            {
                int firstQuantity = ProceduralWorldGenerator.DetermineColonyNodeQuantity(
                    2203, config, column, 12, ProceduralWorldGenerator.SoilNodeId);
                int secondQuantity = ProceduralWorldGenerator.DetermineColonyNodeQuantity(
                    2204, config, column, 12, ProceduralWorldGenerator.SoilNodeId);
                if (firstQuantity != secondQuantity) different++;
            }
            Assert.That(different, Is.GreaterThan(0));
        }

        [Test]
        public void PerNodeQuantityDerivationIsIndependentOfEvaluationOrder()
        {
            var config = new WorldGenerationConfiguration();
            (int column, int depth, string definition)[] nodes =
            {
                (3, 4, ProceduralWorldGenerator.SoilNodeId),
                (41, 17, ProceduralWorldGenerator.StoneNodeId),
                (92, 33, ProceduralWorldGenerator.SoilNodeId),
                (15, 80, ProceduralWorldGenerator.StoneNodeId)
            };
            int[] forward = nodes.Select(node =>
                ProceduralWorldGenerator.DetermineColonyNodeQuantity(2203, config,
                    node.column, node.depth, node.definition)).ToArray();
            System.Array.Reverse(nodes);
            int[] reverse = nodes.Select(node =>
                ProceduralWorldGenerator.DetermineColonyNodeQuantity(2203, config,
                    node.column, node.depth, node.definition)).Reverse().ToArray();
            Assert.That(reverse, Is.EqualTo(forward));
        }

        [Test]
        public void GenerationDoesNotReadOrMutateUnityGlobalRandom()
        {
            Random.InitState(7319);
            int expected = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(7319);
            ProceduralWorldGenerator.TryGenerate(44, new WorldGenerationConfiguration(),
                out _, out _);
            Assert.That(Random.Range(int.MinValue, int.MaxValue), Is.EqualTo(expected));
        }

        [Test]
        public void UnifiedCellsHaveBackgroundAndEmptyPlayerOwnedSlots()
        {
            ProceduralWorldGenerator.TryGenerate(2203, new WorldGenerationConfiguration(),
                out GeneratedWorldSnapshot world, out _);
            IEnumerable<GeneratedCellRecord> cells = world.SurfaceCells.Cast<GeneratedCellRecord>()
                .Concat(world.ColonyCells.Cast<GeneratedCellRecord>());
            Assert.That(cells.All(cell => !string.IsNullOrEmpty(
                cell.BackgroundDefinitionId)), Is.True);
            Assert.That(cells.All(cell => cell.PlacementId == null &&
                cell.LooseResourceStackId == null), Is.True);
            Assert.That(world.Nodes.Select(node => node.Id).Distinct().Count(),
                Is.EqualTo(world.Nodes.Count));
            Assert.That(world.Nodes.All(node => node.InitialQuantity >= 0 &&
                node.RemainingQuantity >= 0), Is.True);
        }

        [Test]
        public void SoilStoneSeedAndTwigNodesShareExtractionContract()
        {
            ProceduralWorldGenerator.TryGenerate(2203,
                new WorldGenerationConfiguration(), out GeneratedWorldSnapshot world,
                out _);
            string[] extractionDefinitions =
            {
                ProceduralWorldGenerator.SoilNodeId,
                ProceduralWorldGenerator.StoneNodeId,
                ProceduralWorldGenerator.SeedNodeId,
                ProceduralWorldGenerator.TwigNodeId
            };
            foreach (string definition in extractionDefinitions)
            {
                GeneratedResourceNodeRecord[] nodes = world.Nodes.Where(node =>
                    node.DefinitionId == definition).ToArray();
                Assert.That(nodes, Is.Not.Empty, definition);
                Assert.That(nodes.All(node => node.ActivityId == "extraction" &&
                    node.SkillId == "extraction"), Is.True, definition);
            }
        }

        [Test]
        public void SurfaceHasExpectedExtentClearingAndReachableSources()
        {
            ProceduralWorldGenerator.TryGenerate(2203, new WorldGenerationConfiguration(),
                out GeneratedWorldSnapshot world, out _);
            Assert.That(world.SurfaceCells.GetLength(0), Is.EqualTo(64));
            Assert.That(world.SurfaceCells.GetLength(1), Is.EqualTo(64));
            Assert.That(world.SurfaceCells.Cast<GeneratedCellRecord>().Count(cell =>
                cell.BackgroundDefinitionId == ProceduralWorldGenerator.GrassBackgroundId),
                Is.GreaterThan(2048));
            for (int y = 30; y <= 34; y++) for (int x = 30; x <= 34; x++)
            {
                Assert.That(world.SurfaceCells[x, y].BackgroundDefinitionId,
                    Is.Not.EqualTo(ProceduralWorldGenerator.WaterBackgroundId));
                Assert.That(world.SurfaceCells[x, y].Node?.ObstructsMovement ?? false,
                    Is.False);
            }
            Assert.That(world.Nodes.Any(node =>
                node.DefinitionId == ProceduralWorldGenerator.SeedNodeId), Is.True);
            Assert.That(world.Nodes.Any(node =>
                node.DefinitionId == ProceduralWorldGenerator.FreshwaterNodeId), Is.True);
            Assert.That(new SurfacePathfinder().FindPath(
                world.CreateSurfaceNavigationWorld(),
                ProceduralWorldGenerator.SurfaceArrival, new SurfaceCell(28, 32)).Succeeded,
                Is.True);
            Assert.That(CountCardinallyClusteredSurfaceFeatures(world,
                cell => cell.BackgroundDefinitionId ==
                    ProceduralWorldGenerator.WaterBackgroundId),
                Is.GreaterThan(300), "Water should form connected local patches.");
            Assert.That(CountCardinallyClusteredSurfaceFeatures(world,
                cell => cell.Node?.DefinitionId == ProceduralWorldGenerator.StoneNodeId),
                Is.GreaterThan(200), "Most Stone cells should belong to a cluster.");
        }

        [Test]
        public void ColonyUsesSignedChunksAndExactStartingClearance()
        {
            ProceduralWorldGenerator.TryGenerate(2203, new WorldGenerationConfiguration(),
                out GeneratedWorldSnapshot world, out _);
            Assert.That(world.ColonyCells.GetLength(0), Is.EqualTo(128));
            Assert.That(world.ColonyCells.GetLength(1), Is.EqualTo(128));
            Assert.That(ProceduralWorldGenerator.ToColonyWorldCell(0, 0),
                Is.EqualTo(new Vector2Int(-64, -1)));
            Assert.That(WorldTileCoordinates.GetChunkCoordinate(
                ProceduralWorldGenerator.ToColonyWorldCell(0, 127)),
                Is.EqualTo(new Vector2Int(-2, -4)));
            for (int depth = 5; depth <= 9; depth++)
            for (int column = 61; column <= 67; column++)
                Assert.That(world.ColonyCells[column, depth].Node, Is.Null,
                    $"Chamber cell {column},{depth} was not excavated.");
            for (int depth = 0; depth <= 4; depth++)
            for (int column = 63; column <= 65; column++)
                Assert.That(world.ColonyCells[column, depth].Node, Is.Null,
                    $"Passage cell {column},{depth} was not excavated.");
            Assert.That(world.ColonyCells.Cast<GeneratedCellRecord>().Count(cell =>
                cell.Node?.DefinitionId == ProceduralWorldGenerator.SoilNodeId),
                Is.GreaterThan(8192));
            Assert.That(world.QueenSpawn, Is.EqualTo(ProceduralWorldGenerator.QueenSpawn));

            ColonyCell stone = ProceduralWorldGenerator.FoundingStoneFace;
            GeneratedCellRecord stoneFace = world.ColonyCells[stone.X + 64, -1 - stone.Y];
            Assert.That(stoneFace.Node?.DefinitionId,
                Is.EqualTo(ProceduralWorldGenerator.StoneNodeId));
            Assert.That(stoneFace.BackgroundDefinitionId,
                Is.EqualTo(ProceduralWorldGenerator.StoneBackgroundId));
            Assert.That(world.ColonyCells[stone.X + 65, -1 - stone.Y].Node,
                Is.Null, "The guaranteed Stone face must border the open chamber.");
        }

        [Test]
        public void EntranceUsesGeneratedScenarioEndpointsAndStableIdentity()
        {
            ProceduralWorldGenerator.TryGenerate(2203, new WorldGenerationConfiguration(),
                out GeneratedWorldSnapshot world, out _);
            Assert.That(world.Entrance.Id, Is.EqualTo("entrance.founding-nest.001"));
            Assert.That(world.Entrance.SurfaceArrival,
                Is.EqualTo(ProceduralWorldGenerator.SurfaceArrival));
            Assert.That(world.Entrance.ColonyArrival,
                Is.EqualTo(ProceduralWorldGenerator.ColonyArrival));
        }

        private static int CountCardinallyClusteredSurfaceFeatures(
            GeneratedWorldSnapshot world,
            System.Func<GeneratedCellRecord, bool> predicate)
        {
            int clustered = 0;
            (int x, int y)[] directions =
            {
                (0, 1), (1, 0), (0, -1), (-1, 0)
            };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                if (!predicate(world.SurfaceCells[x, y])) continue;
                foreach ((int dx, int dy) in directions)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < 64 && ny < 64 &&
                        predicate(world.SurfaceCells[nx, ny]))
                    { clustered++; break; }
                }
            }
            return clustered;
        }
    }
}
