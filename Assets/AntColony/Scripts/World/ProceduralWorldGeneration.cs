using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public enum GeneratedWorldScenario { DevelopmentFixture, FoundingQueen }

    /// <summary>Frozen values that participate in deterministic world identity.</summary>
    public sealed class WorldGenerationConfiguration
    {
        public const int CurrentVersion = 2;
        public const string CurrentConfigurationId = "wv-b2b.default.002";
        public const int SurfaceWidth = 64;
        public const int SurfaceHeight = 64;
        public const int ColonyWidth = 128;
        public const int ColonyHeight = 128;

        public int Version => CurrentVersion;
        public string ConfigurationId => CurrentConfigurationId;
        public int SurfaceWaterWeight => 8;
        public int SurfaceStoneWeight => 6;
        public int SurfaceOtherWeight => 4;
        public int ColonyStoneWeight => 12;
        public int SeedQuantity => 8;
        public int TwigQuantity => 5;
        public int FreshwaterQuantity => 12;
        public int SurfaceStoneQuantity => 6;
        public int ColonySoilMinimumQuantity => 8;
        public int ColonySoilMaximumQuantity => 12;
        public int ColonyStoneMinimumQuantity => 5;
        public int ColonyStoneMaximumQuantity => 15;
        public string ColonyNodeQuantityDomain => "colony-node-quantity.v1";
    }

    public sealed class GeneratedResourceNodeRecord
    {
        public GeneratedResourceNodeRecord(string id, RegionDestination location,
            string definitionId, int quantity, bool obstructsMovement,
            string activityId, string skillId, string outputResourceId,
            string provenance)
        {
            Id = id; Location = location; DefinitionId = definitionId;
            InitialQuantity = quantity; RemainingQuantity = quantity;
            ObstructsMovement = obstructsMovement; ActivityId = activityId;
            SkillId = skillId; OutputResourceId = outputResourceId;
            Provenance = provenance;
        }

        public string Id { get; }
        public RegionDestination Location { get; }
        public string DefinitionId { get; }
        public int InitialQuantity { get; }
        public int RemainingQuantity { get; private set; }
        public bool ObstructsMovement { get; }
        public string ActivityId { get; }
        public string SkillId { get; }
        public string OutputResourceId { get; }
        public string Provenance { get; }
        public bool IsDepleted => RemainingQuantity == 0;
    }

    /// <summary>
    /// Generation-time unified cell. Runtime construction and loose-resource
    /// authorities remain separate; generated worlds deliberately start with
    /// those two optional slots empty.
    /// </summary>
    public sealed class GeneratedCellRecord
    {
        public GeneratedCellRecord(string backgroundDefinitionId)
        {
            BackgroundDefinitionId = backgroundDefinitionId;
        }

        public string BackgroundDefinitionId { get; }
        public GeneratedResourceNodeRecord Node { get; internal set; }
        public string PlacementId { get; internal set; }
        public string LooseResourceStackId { get; internal set; }
    }

    public sealed class GeneratedWorldMetadata
    {
        public GeneratedWorldMetadata(long seed, int version, string configurationId)
        { Seed = seed; Version = version; ConfigurationId = configurationId; }
        public long Seed { get; }
        public int Version { get; }
        public string ConfigurationId { get; }
        public string SurfaceStream => "surface";
        public string ColonyStream => "colony";
    }

    /// <summary>Typed generated endpoints adapted to the WV-B1 transaction at composition.</summary>
    public sealed class GeneratedEntranceRecord
    {
        public GeneratedEntranceRecord(string id, ColonyCell colonyApproach,
            ColonyCell colonyArrival, SurfaceCell surfaceApproach,
            SurfaceCell surfaceArrival)
        {
            Id = id; ColonyApproach = colonyApproach; ColonyArrival = colonyArrival;
            SurfaceApproach = surfaceApproach; SurfaceArrival = surfaceArrival;
        }
        public string Id { get; }
        public ColonyCell ColonyApproach { get; }
        public ColonyCell ColonyArrival { get; }
        public SurfaceCell SurfaceApproach { get; }
        public SurfaceCell SurfaceArrival { get; }
    }

    public sealed class GeneratedWorldSnapshot
    {
        internal GeneratedWorldSnapshot(GeneratedWorldMetadata metadata,
            GeneratedCellRecord[,] surface, GeneratedCellRecord[,] colony,
            GeneratedEntranceRecord entrance, ColonyCell queenSpawn,
            IReadOnlyList<GeneratedResourceNodeRecord> nodes)
        {
            Metadata = metadata; SurfaceCells = surface; ColonyCells = colony;
            Entrance = entrance; QueenSpawn = queenSpawn; Nodes = nodes;
        }

        public GeneratedWorldMetadata Metadata { get; }
        public GeneratedCellRecord[,] SurfaceCells { get; }
        public GeneratedCellRecord[,] ColonyCells { get; }
        public GeneratedEntranceRecord Entrance { get; }
        public ColonyCell QueenSpawn { get; }
        public IReadOnlyList<GeneratedResourceNodeRecord> Nodes { get; }

        public SurfaceWorldData CreateSurfaceNavigationWorld()
        {
            var blocked = new List<SurfaceCell>();
            for (int y = 0; y < WorldGenerationConfiguration.SurfaceHeight; y++)
            for (int x = 0; x < WorldGenerationConfiguration.SurfaceWidth; x++)
            {
                GeneratedCellRecord cell = SurfaceCells[x, y];
                if (cell.BackgroundDefinitionId == ProceduralWorldGenerator.WaterBackgroundId ||
                    cell.Node != null && cell.Node.ObstructsMovement)
                    blocked.Add(new SurfaceCell(x, y));
            }
            return new SurfaceWorldData(WorldGenerationConfiguration.SurfaceWidth,
                WorldGenerationConfiguration.SurfaceHeight, blocked);
        }

        public ChunkedWorldData CreateColonyWorld(WorldMaterialRegistry registry,
            SampleWorldMaterialIds materials)
        {
            var world = new ChunkedWorldData(registry, Metadata.Seed);
            for (int chunkY = -4; chunkY <= -1; chunkY++)
            for (int chunkX = -2; chunkX <= 1; chunkX++)
                Require(world.CreateChunk(new Vector2Int(chunkX, chunkY),
                    materials.EarthBackground));

            for (int depth = 0; depth < WorldGenerationConfiguration.ColonyHeight; depth++)
            for (int column = 0; column < WorldGenerationConfiguration.ColonyWidth; column++)
            {
                GeneratedCellRecord generated = ColonyCells[column, depth];
                Vector2Int cell = ProceduralWorldGenerator.ToColonyWorldCell(column, depth);
                string background = generated.BackgroundDefinitionId ==
                    ProceduralWorldGenerator.StoneBackgroundId
                        ? materials.StoneBackground : materials.EarthBackground;
                Require(world.SetBackgroundMaterial(cell, background));
                if (generated.Node != null)
                    Require(world.SetSolidMaterial(cell,
                        generated.Node.DefinitionId == ProceduralWorldGenerator.StoneNodeId
                            ? materials.StoneSolid : materials.SoilSolid));
            }
            return world;
        }

        public string CreateFingerprint()
        {
            ulong hash = 1469598103934665603UL;
            void Add(string value)
            {
                foreach (char character in value ?? string.Empty)
                { hash ^= character; hash *= 1099511628211UL; }
                hash ^= 255; hash *= 1099511628211UL;
            }
            Add(Metadata.Seed.ToString()); Add(Metadata.Version.ToString());
            Add(Metadata.ConfigurationId);
            foreach (GeneratedCellRecord cell in SurfaceCells)
            {
                Add(cell.BackgroundDefinitionId); Add(cell.Node?.DefinitionId);
                Add(cell.Node?.Id); Add(cell.Node?.InitialQuantity.ToString());
            }
            foreach (GeneratedCellRecord cell in ColonyCells)
            {
                Add(cell.BackgroundDefinitionId); Add(cell.Node?.DefinitionId);
                Add(cell.Node?.InitialQuantity.ToString());
            }
            return hash.ToString("X16");
        }

        private static void Require(WorldChunkCreationResult result)
        { if (result != WorldChunkCreationResult.Success) throw new InvalidOperationException(result.ToString()); }
        private static void Require(WorldCellMutationResult result)
        { if (result != WorldCellMutationResult.Success) throw new InvalidOperationException(result.ToString()); }
    }

    public sealed class GeneratedWorldValidationReport
    {
        internal GeneratedWorldValidationReport(List<string> errors)
        { Errors = errors; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
        public string Summary => IsValid ? "Generated world validation passed."
            : string.Join("\n", Errors);
    }

    /// <summary>
    /// Deterministic WV-B2B generator. It uses independent SplitMix64 streams;
    /// adding draws to one region cannot perturb the other region.
    /// </summary>
    public static class ProceduralWorldGenerator
    {
        public const string GrassBackgroundId = "background.surface.grass";
        public const string WaterBackgroundId = "background.surface.water";
        public const string EarthBackgroundId = "background.earth.unprepared";
        public const string StoneBackgroundId = "background.stone.unprepared";
        public const string SoilNodeId = "solid.soil";
        public const string StoneNodeId = "solid.stone";
        public const string SeedNodeId = "node.surface.seed";
        public const string TwigNodeId = "node.surface.twig";
        public const string FreshwaterNodeId = "node.surface.freshwater";
        public const string ObstacleNodeId = "node.surface.natural-obstacle";
        public static readonly SurfaceCell SurfaceEntrance = new(32, 32);
        public static readonly SurfaceCell SurfaceApproach = new(32, 31);
        public static readonly SurfaceCell SurfaceArrival = new(32, 32);
        public static readonly ColonyCell ColonyExit = new(0, -1);
        public static readonly ColonyCell ColonyApproach = new(0, -2);
        public static readonly ColonyCell ColonyArrival = new(0, -1);
        public static readonly ColonyCell QueenSpawn = new(0, -8);
        public static readonly ColonyCell FoundingStoneFace = new(-4, -8);

        public static bool TryGenerate(long seed, WorldGenerationConfiguration config,
            out GeneratedWorldSnapshot snapshot, out GeneratedWorldValidationReport report)
        {
            config ??= new WorldGenerationConfiguration();
            var nodes = new List<GeneratedResourceNodeRecord>();
            GeneratedCellRecord[,] surface = GenerateSurface(seed, config, nodes);
            GeneratedCellRecord[,] colony = GenerateColony(seed, config, nodes);
            var entrance = new GeneratedEntranceRecord(
                "entrance.founding-nest.001", ColonyApproach,
                ColonyArrival, SurfaceApproach, SurfaceArrival);
            var candidate = new GeneratedWorldSnapshot(
                new GeneratedWorldMetadata(seed, config.Version, config.ConfigurationId),
                surface, colony, entrance, QueenSpawn, nodes);
            report = Validate(candidate);
            snapshot = report.IsValid ? candidate : null;
            return report.IsValid;
        }

        public static Vector2Int ToColonyWorldCell(int column, int depth) =>
            new(column - 64, -1 - depth);

        private static GeneratedCellRecord[,] GenerateSurface(long seed,
            WorldGenerationConfiguration config, List<GeneratedResourceNodeRecord> nodes)
        {
            const int width = WorldGenerationConfiguration.SurfaceWidth;
            const int height = WorldGenerationConfiguration.SurfaceHeight;
            var cells = new GeneratedCellRecord[width, height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                cells[x, y] = new GeneratedCellRecord(GrassBackgroundId);

            var random = new StableRandom(DeriveSeed(seed, "surface"));
            var protectedCells = new HashSet<int>();
            for (int y = 30; y <= 34; y++) for (int x = 30; x <= 34; x++)
                protectedCells.Add(y * width + x);
            // Keep one dry interaction anchor beside the guaranteed freshwater
            // source even when the connected water cluster grows toward it.
            protectedCells.Add(32 * width + 35);

            var water = GrowClusters(random, width, height, 328, 5,
                protectedCells, new SurfaceCell(36, 32));
            var orderedWater = new List<int>(water);
            orderedWater.Sort();
            foreach (int index in orderedWater)
                cells[index % width, index / width] = new GeneratedCellRecord(WaterBackgroundId);

            var occupied = new HashSet<int>(protectedCells);
            occupied.UnionWith(water);
            HashSet<int> stones = GrowClusters(random, width, height, 246, 12, occupied, null);
            var orderedStones = new List<int>(stones);
            orderedStones.Sort();
            foreach (int index in orderedStones)
            {
                int x = index % width, y = index / width;
                AddNode(cells[x, y], nodes, seed, "surface", x, y, StoneNodeId,
                    config.SurfaceStoneQuantity, true, "extraction", "extraction",
                    "material.stone");
            }
            occupied.UnionWith(stones);

            int obstacles = 0;
            for (int attempts = 0; attempts < 12000 && obstacles < 164; attempts++)
            {
                int x = random.NextInt(width), y = random.NextInt(height);
                int index = y * width + x;
                if (occupied.Contains(index) || HasBlockingNeighbour(cells, x, y, 2)) continue;
                AddNode(cells[x, y], nodes, seed, "surface", x, y, ObstacleNodeId,
                    1, true, string.Empty, string.Empty, string.Empty);
                occupied.Add(index); obstacles++;
            }

            // Fixed relative guarantee points make validation/repair bounded while
            // the remaining source scatter still varies by the Surface stream.
            AddNode(cells[28, 32], nodes, seed, "surface", 28, 32, SeedNodeId,
                config.SeedQuantity, false, "extraction", "extraction", "resource.food.seed");
            int waterIndex = 32 * width + 36;
            int waterX = waterIndex % width, waterY = waterIndex / width;
            AddNode(cells[waterX, waterY], nodes, seed, "surface", waterX, waterY,
                FreshwaterNodeId, config.FreshwaterQuantity, false, "drinking",
                "foraging", "resource.water.fresh");
            ScatterNonBlocking(random, cells, nodes, seed, SeedNodeId,
                "resource.food.seed", config.SeedQuantity, 24, occupied);
            ScatterNonBlocking(random, cells, nodes, seed, TwigNodeId,
                "resource.material.twig", config.TwigQuantity, 32, occupied);
            return cells;
        }

        private static GeneratedCellRecord[,] GenerateColony(long seed,
            WorldGenerationConfiguration config, List<GeneratedResourceNodeRecord> nodes)
        {
            const int width = WorldGenerationConfiguration.ColonyWidth;
            const int height = WorldGenerationConfiguration.ColonyHeight;
            var cells = new GeneratedCellRecord[width, height];
            var random = new StableRandom(DeriveSeed(seed, "colony"));
            var stone = GrowClusters(random, width, height, 1966, 36,
                new HashSet<int>(), null);
            for (int depth = 0; depth < height; depth++)
            for (int column = 0; column < width; column++)
            {
                bool isStone = stone.Contains(depth * width + column);
                cells[column, depth] = new GeneratedCellRecord(
                    isStone ? StoneBackgroundId : EarthBackgroundId);
                ColonyCell location = ToTypedColonyCell(column, depth);
                string definition = isStone ? StoneNodeId : SoilNodeId;
                int quantity = DetermineColonyNodeQuantity(seed, config, column,
                    depth, definition);
                var node = new GeneratedResourceNodeRecord(
                    NodeId(seed, "colony", column, depth, definition),
                    RegionDestination.Colony(AntRegionIds.Colony, location), definition,
                    quantity, true, "extraction", "extraction",
                    isStone ? "material.stone" : "material.soil", "generated:colony");
                cells[column, depth].Node = node; nodes.Add(node);
            }

            // Three-wide passage at columns 63..65, then the exact 7x5 chamber.
            for (int depth = 0; depth <= 4; depth++)
            for (int column = 63; column <= 65; column++) cells[column, depth].Node = null;
            for (int depth = 5; depth <= 9; depth++)
            for (int column = 61; column <= 67; column++) cells[column, depth].Node = null;
            nodes.RemoveAll(node => node.Location.Kind == RegionLocationKind.Colony &&
                cells[node.Location.ColonyCell.X + 64,
                    -1 - node.Location.ColonyCell.Y].Node == null);

            // Guarantee one real Stone node on the chamber's west face. Without
            // this, a seed could expose only Stone-coloured natural Backgrounds
            // in the chamber and leave every actual Stone node buried.
            const int stoneColumn = 60;
            const int stoneDepth = 7;
            GeneratedCellRecord stoneFace = new(StoneBackgroundId);
            ColonyCell stoneLocation = ToTypedColonyCell(stoneColumn, stoneDepth);
            int stoneQuantity = DetermineColonyNodeQuantity(seed, config,
                stoneColumn, stoneDepth, StoneNodeId);
            var stoneNode = new GeneratedResourceNodeRecord(
                NodeId(seed, "colony", stoneColumn, stoneDepth, StoneNodeId),
                RegionDestination.Colony(AntRegionIds.Colony, stoneLocation),
                StoneNodeId, stoneQuantity, true, "extraction", "extraction",
                "material.stone", "generated:colony");
            nodes.RemoveAll(node => node.Location.Kind == RegionLocationKind.Colony &&
                node.Location.ColonyCell.Equals(stoneLocation));
            stoneFace.Node = stoneNode;
            cells[stoneColumn, stoneDepth] = stoneFace;
            nodes.Add(stoneNode);
            return cells;
        }

        private static GeneratedWorldValidationReport Validate(GeneratedWorldSnapshot world)
        {
            var errors = new List<string>();
            if (world.SurfaceCells.GetLength(0) != 64 || world.SurfaceCells.GetLength(1) != 64)
                errors.Add("Surface dimensions must be 64x64.");
            if (world.ColonyCells.GetLength(0) != 128 || world.ColonyCells.GetLength(1) != 128)
                errors.Add("Colony dimensions must be 128x128.");
            var ids = new HashSet<string>();
            int grass = 0, soil = 0;
            foreach (GeneratedCellRecord cell in world.SurfaceCells)
            {
                if (cell == null || string.IsNullOrEmpty(cell.BackgroundDefinitionId))
                    errors.Add("Every Surface cell requires a Background.");
                if (cell?.BackgroundDefinitionId == GrassBackgroundId) grass++;
                ValidateNode(cell?.Node, ids, errors);
                if (cell?.PlacementId != null || cell?.LooseResourceStackId != null)
                    errors.Add("Generated Surface placements and loose stacks must start empty.");
            }
            foreach (GeneratedCellRecord cell in world.ColonyCells)
            {
                if (cell == null || string.IsNullOrEmpty(cell.BackgroundDefinitionId))
                    errors.Add("Every Colony cell requires a Background.");
                if (cell?.Node?.DefinitionId == SoilNodeId) soil++;
                ValidateNode(cell?.Node, ids, errors);
                if (cell?.PlacementId != null || cell?.LooseResourceStackId != null)
                    errors.Add("Generated Colony placements and loose stacks must start empty.");
            }
            if (grass <= 4096 / 2) errors.Add("Grass must predominate on the Surface.");
            if (soil <= 16384 / 2) errors.Add("Soil Solids must predominate underground.");
            if (world.Entrance == null ||
                world.Entrance.Id != "entrance.founding-nest.001")
                errors.Add("Exactly one stable founding entrance is required.");
            for (int y = 30; y <= 34; y++) for (int x = 30; x <= 34; x++)
                if (world.SurfaceCells[x, y].Node?.ObstructsMovement == true ||
                    world.SurfaceCells[x, y].BackgroundDefinitionId == WaterBackgroundId)
                    errors.Add("The protected 5x5 entrance clearing must remain open.");
            if (world.ColonyCells[64, 7].Node != null)
                errors.Add("Queen spawn must be clear.");
            SurfaceWorldData surface = world.CreateSurfaceNavigationWorld();
            var pathfinder = new SurfacePathfinder();
            if (!surface.IsTraversable(world.Entrance.SurfaceApproach) ||
                !surface.IsTraversable(world.Entrance.SurfaceArrival))
                errors.Add("Surface entrance approach and arrival must be traversable.");
            int clearColonyCells = 0;
            for (int depth = 0; depth < 128; depth++)
            for (int column = 0; column < 128; column++)
                if (world.ColonyCells[column, depth].Node == null) clearColonyCells++;
            if (clearColonyCells != 50)
                errors.Add("The Colony must contain exactly the 15-cell passage and 35-cell chamber clearance.");
            if (world.ColonyCells[64, 0].Node != null ||
                world.ColonyCells[64, 1].Node != null)
                errors.Add("Colony entrance approach and arrival must be clear.");
            ValidateNearbySource(world, surface, pathfinder, SeedNodeId, errors);
            ValidateNearbySource(world, surface, pathfinder, FreshwaterNodeId, errors);
            if (!new GridReachabilityProbe(world.ColonyCells).CanReach(64, 7, 64, 1))
                errors.Add("Queen spawn must reach the Colony entrance passage.");
            return new GeneratedWorldValidationReport(errors);
        }

        private static void ValidateNearbySource(GeneratedWorldSnapshot world,
            SurfaceWorldData surface, SurfacePathfinder pathfinder, string definition,
            List<string> errors)
        {
            foreach (GeneratedResourceNodeRecord node in world.Nodes)
            {
                if (node.DefinitionId != definition ||
                    node.Location.Kind != RegionLocationKind.Surface) continue;
                SurfaceCell source = node.Location.SurfaceCell;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    var approach = new SurfaceCell(source.X + x, source.Y + y);
                    if (pathfinder.FindPath(surface, SurfaceArrival, approach).Succeeded) return;
                }
            }
            errors.Add($"Required reachable source missing: {definition}.");
        }

        private static void ValidateNode(GeneratedResourceNodeRecord node,
            HashSet<string> ids, List<string> errors)
        {
            if (node == null) return;
            if (string.IsNullOrEmpty(node.Id) || !ids.Add(node.Id))
                errors.Add("Generated node IDs must be present and unique.");
            if (!IsKnownNodeDefinition(node.DefinitionId) ||
                node.InitialQuantity < 0 || node.InitialQuantity > 10000 ||
                node.RemainingQuantity < 0 || node.RemainingQuantity > node.InitialQuantity)
                errors.Add($"Generated node {node.Id} has invalid definition or quantity.");
        }

        private static bool IsKnownNodeDefinition(string definition) =>
            definition == SoilNodeId || definition == StoneNodeId ||
            definition == SeedNodeId || definition == TwigNodeId ||
            definition == FreshwaterNodeId || definition == ObstacleNodeId;

        private static void ScatterNonBlocking(StableRandom random,
            GeneratedCellRecord[,] cells, List<GeneratedResourceNodeRecord> nodes,
            long seed, string definition, string output, int quantity, int count,
            HashSet<int> occupied)
        {
            int placed = 0;
            for (int attempts = 0; attempts < 10000 && placed < count; attempts++)
            {
                int x = random.NextInt(64), y = random.NextInt(64), index = y * 64 + x;
                if (occupied.Contains(index) || cells[x, y].Node != null ||
                    cells[x, y].BackgroundDefinitionId == WaterBackgroundId) continue;
                AddNode(cells[x, y], nodes, seed, "surface", x, y, definition,
                    quantity, false, "extraction", "extraction", output);
                occupied.Add(index); placed++;
            }
        }

        private static void AddNode(GeneratedCellRecord cell,
            List<GeneratedResourceNodeRecord> nodes, long seed, string region,
            int x, int y, string definition, int quantity, bool obstructs,
            string activity, string skill, string output)
        {
            RegionDestination location = RegionDestination.Surface(
                AntRegionIds.Surface, new SurfaceCell(x, y));
            var node = new GeneratedResourceNodeRecord(
                NodeId(seed, region, x, y, definition), location, definition,
                quantity, obstructs, activity, skill, output, $"generated:{region}");
            cell.Node = node; nodes.Add(node);
        }

        private static HashSet<int> GrowClusters(StableRandom random, int width,
            int height, int target, int clusterCount, HashSet<int> excluded,
            SurfaceCell? required)
        {
            var result = new HashSet<int>();
            var frontier = new List<int>();
            if (required.HasValue)
            {
                int index = required.Value.Y * width + required.Value.X;
                if (!excluded.Contains(index)) { result.Add(index); frontier.Add(index); }
            }
            for (int i = 0; i < clusterCount; i++)
            {
                int index;
                do index = random.NextInt(width * height); while (excluded.Contains(index));
                if (result.Add(index)) frontier.Add(index);
            }
            int guard = width * height * 20;
            while (result.Count < target && guard-- > 0)
            {
                int source = frontier[random.NextInt(frontier.Count)];
                int x = source % width, y = source / width;
                int direction = random.NextInt(4);
                int nx = x + (direction == 1 ? 1 : direction == 3 ? -1 : 0);
                int ny = y + (direction == 0 ? 1 : direction == 2 ? -1 : 0);
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                int candidate = ny * width + nx;
                if (excluded.Contains(candidate) || !result.Add(candidate)) continue;
                frontier.Add(candidate);
            }
            return result;
        }

        private static bool HasBlockingNeighbour(GeneratedCellRecord[,] cells,
            int x, int y, int radius)
        {
            for (int oy = -radius; oy <= radius; oy++) for (int ox = -radius; ox <= radius; ox++)
            {
                int nx = x + ox, ny = y + oy;
                if (nx >= 0 && ny >= 0 && nx < 64 && ny < 64 &&
                    cells[nx, ny].Node?.ObstructsMovement == true) return true;
            }
            return false;
        }

        private static ColonyCell ToTypedColonyCell(int column, int depth)
        { Vector2Int cell = ToColonyWorldCell(column, depth); return new ColonyCell(cell.x, cell.y); }
        private static string NodeId(long seed, string region, int x, int y, string definition) =>
            $"generated.{region}.{definition}.{x}.{y}.{unchecked((ulong)seed):X16}";

        /// <summary>
        /// Derives quantity from immutable per-Node inputs. No shared random
        /// stream is consumed, so generation order cannot reroll another Node.
        /// </summary>
        public static int DetermineColonyNodeQuantity(long seed,
            WorldGenerationConfiguration config, int column, int depth,
            string definitionId)
        {
            config ??= new WorldGenerationConfiguration();
            int minimum;
            int maximum;
            if (string.Equals(definitionId, SoilNodeId, StringComparison.Ordinal))
            {
                minimum = config.ColonySoilMinimumQuantity;
                maximum = config.ColonySoilMaximumQuantity;
            }
            else if (string.Equals(definitionId, StoneNodeId,
                         StringComparison.Ordinal))
            {
                minimum = config.ColonyStoneMinimumQuantity;
                maximum = config.ColonyStoneMaximumQuantity;
            }
            else
            {
                throw new ArgumentException(
                    $"Unsupported Colony Node definition '{definitionId}'.",
                    nameof(definitionId));
            }

            string domain = $"{config.ColonyNodeQuantityDomain}:" +
                $"{config.ConfigurationId}:v{config.Version}:" +
                $"{definitionId}:{column}:{depth}";
            var random = new StableRandom(DeriveSeed(seed, domain));
            return minimum + random.NextInt(maximum - minimum + 1);
        }

        private static ulong DeriveSeed(long seed, string domain)
        {
            ulong value = unchecked((ulong)seed) ^ 1469598103934665603UL;
            foreach (char character in domain) { value ^= character; value *= 1099511628211UL; }
            value ^= unchecked((ulong)WorldGenerationConfiguration.CurrentVersion *
                0x9E3779B97F4A7C15UL);
            return value;
        }

        private sealed class StableRandom
        {
            private ulong state;
            public StableRandom(ulong seed) { state = seed; }
            public uint NextUInt()
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return (uint)((z ^ (z >> 31)) >> 32);
            }
            public int NextInt(int maximum) => (int)(NextUInt() % (uint)maximum);
        }

        private sealed class GridReachabilityProbe
        {
            private readonly GeneratedCellRecord[,] cells;
            public GridReachabilityProbe(GeneratedCellRecord[,] cells) { this.cells = cells; }
            public bool CanReach(int sx, int sy, int gx, int gy)
            {
                var queue = new Queue<Vector2Int>(); var seen = new HashSet<Vector2Int>();
                queue.Enqueue(new Vector2Int(sx, sy)); seen.Add(new Vector2Int(sx, sy));
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    if (current.x == gx && current.y == gy) return true;
                    foreach (Vector2Int direction in directions)
                    {
                        Vector2Int next = current + direction;
                        if (next.x < 0 || next.y < 0 || next.x >= 128 || next.y >= 128 ||
                            cells[next.x, next.y].Node != null || !seen.Add(next)) continue;
                        queue.Enqueue(next);
                    }
                }
                return false;
            }
        }
    }
}
