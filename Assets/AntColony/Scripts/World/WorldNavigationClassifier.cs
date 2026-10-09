using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Describes whether authoritative terrain occupies a queried coordinate.
    /// Exposed only means that no movement-blocking Solid Tile covers the cell.
    /// </summary>
    public enum NavigationCellOccupancy
    {
        Unavailable,
        Covered,
        Exposed
    }

    /// <summary>
    /// Describes cardinal terrain support for an exposed cell. Unknown means at
    /// least one required neighbour is unavailable and no support was confirmed.
    /// </summary>
    public enum NavigationSupportStatus
    {
        NotApplicable,
        Supported,
        Unsupported,
        Unknown
    }

    /// <summary>
    /// Immutable terrain-classification result. Supported is only navigation
    /// eligibility; it does not include body clearance, obstacles, or reachability.
    /// </summary>
    public readonly struct WorldNavigationClassification
    {
        internal WorldNavigationClassification(
            NavigationCellOccupancy occupancy,
            NavigationSupportStatus support)
        {
            Occupancy = occupancy;
            Support = support;
        }

        public NavigationCellOccupancy Occupancy { get; }
        public NavigationSupportStatus Support { get; }
        public bool IsAvailable => Occupancy != NavigationCellOccupancy.Unavailable;
        public bool IsExposed => Occupancy == NavigationCellOccupancy.Exposed;
        public bool HasConfirmedSupport => Support == NavigationSupportStatus.Supported;
    }

    /// <summary>
    /// Derives terrain occupancy and cardinal support directly from authoritative
    /// world data. It owns no cache, scene object, physics query, or Tilemap state.
    /// </summary>
    public sealed class WorldNavigationClassifier
    {
        private readonly ChunkedWorldData worldData;

        public WorldNavigationClassifier(ChunkedWorldData worldData)
        {
            this.worldData = worldData ??
                throw new ArgumentNullException(nameof(worldData));
        }

        /// <summary>
        /// Classifies one world cell without generating chunks. Support is checked
        /// only north, south, east, and west; diagonal neighbours never qualify.
        /// </summary>
        public WorldNavigationClassification Classify(Vector2Int worldCell)
        {
            if (worldData.GetCell(worldCell, out WorldTileCellData cell) ==
                WorldCellQueryResult.ChunkUnavailable)
            {
                return new WorldNavigationClassification(
                    NavigationCellOccupancy.Unavailable,
                    NavigationSupportStatus.NotApplicable);
            }

            if (IsMovementBlocking(cell))
            {
                return new WorldNavigationClassification(
                    NavigationCellOccupancy.Covered,
                    NavigationSupportStatus.NotApplicable);
            }

            bool hasUnavailableNeighbour = false;

            // Check each cardinal neighbour explicitly. This keeps ordinary
            // queries allocation-free and makes the no-diagonal rule visible.
            if (ProvidesSupport(worldCell + Vector2Int.up, ref hasUnavailableNeighbour) ||
                ProvidesSupport(worldCell + Vector2Int.down, ref hasUnavailableNeighbour) ||
                ProvidesSupport(worldCell + Vector2Int.right, ref hasUnavailableNeighbour) ||
                ProvidesSupport(worldCell + Vector2Int.left, ref hasUnavailableNeighbour))
            {
                return new WorldNavigationClassification(
                    NavigationCellOccupancy.Exposed,
                    NavigationSupportStatus.Supported);
            }

            return new WorldNavigationClassification(
                NavigationCellOccupancy.Exposed,
                hasUnavailableNeighbour
                    ? NavigationSupportStatus.Unknown
                    : NavigationSupportStatus.Unsupported);
        }

        private bool IsMovementBlocking(WorldTileCellData cell)
        {
            if (!cell.HasSolidTile)
            {
                return false;
            }

            return worldData.TryGetSolidMaterialDefinition(
                    cell.SolidMaterialId,
                    out SolidTileMaterialDefinition definition) &&
                definition.BlocksMovement;
        }

        private bool ProvidesSupport(
            Vector2Int neighbourCell,
            ref bool hasUnavailableNeighbour)
        {
            if (worldData.GetCell(neighbourCell, out WorldTileCellData neighbour) ==
                WorldCellQueryResult.ChunkUnavailable)
            {
                hasUnavailableNeighbour = true;
                return false;
            }

            if (!neighbour.HasSolidTile)
            {
                return false;
            }

            // Blocking and support are independent material properties. A
            // non-blocking material can still support navigation when authored so.
            return worldData.TryGetSolidMaterialDefinition(
                    neighbour.SolidMaterialId,
                    out SolidTileMaterialDefinition definition) &&
                definition.SupportsNavigation;
        }
    }
}
