using System;
using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.NavigationDebug
{
    /// <summary>
    /// One cell prepared for debug display. The classification remains the
    /// authority; the colour is only its developer-facing presentation.
    /// </summary>
    public readonly struct NavigationOverlayCell
    {
        public NavigationOverlayCell(
            Vector2Int worldCell,
            Vector2 worldPosition,
            WorldNavigationClassification classification,
            Color32 colour)
        {
            WorldCell = worldCell;
            WorldPosition = worldPosition;
            Classification = classification;
            Colour = colour;
        }

        public Vector2Int WorldCell { get; }
        public Vector2 WorldPosition { get; }
        public WorldNavigationClassification Classification { get; }
        public Color32 Colour { get; }
    }

    /// <summary>
    /// Central colour mapping for the navigation debug overlay. Green means
    /// Supported only; it never claims body clearance or route reachability.
    /// </summary>
    public static class NavigationOverlayPalette
    {
        public static readonly Color32 Covered = new Color32(128, 76, 38, 190);
        public static readonly Color32 Supported = new Color32(45, 190, 90, 175);
        public static readonly Color32 Unsupported = new Color32(220, 55, 55, 185);
        public static readonly Color32 Unknown = new Color32(255, 178, 36, 195);
        public static readonly Color32 Unavailable = new Color32(105, 108, 118, 135);
        public static readonly Color32 ChunkBoundary = new Color32(55, 180, 235, 225);

        /// <summary>Returns the required colour for one classifier result.</summary>
        public static Color32 GetColour(WorldNavigationClassification classification)
        {
            if (classification.Occupancy == NavigationCellOccupancy.Unavailable)
            {
                return Unavailable;
            }

            if (classification.Occupancy == NavigationCellOccupancy.Covered)
            {
                return Covered;
            }

            return classification.Support switch
            {
                NavigationSupportStatus.Supported => Supported,
                NavigationSupportStatus.Unsupported => Unsupported,
                NavigationSupportStatus.Unknown => Unknown,
                _ => Unsupported
            };
        }
    }

    /// <summary>
    /// Builds bounded display data directly from the shared classifier. It does
    /// not own world state, generate chunks, or retain a navigation cache.
    /// </summary>
    public sealed class NavigationOverlayModel
    {
        private readonly WorldNavigationClassifier classifier;

        public NavigationOverlayModel(WorldNavigationClassifier classifier)
        {
            this.classifier = classifier ??
                throw new ArgumentNullException(nameof(classifier));
        }

        /// <summary>
        /// Rebuilds a caller-owned list for the selected rectangular region.
        /// Reusing the list avoids routine garbage after the first refresh.
        /// </summary>
        public void BuildCells(
            Vector2Int minimumCell,
            Vector2Int size,
            List<NavigationOverlayCell> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (size.x <= 0 || size.y <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    size,
                    "Overlay size must be positive on both axes.");
            }

            destination.Clear();
            int requiredCapacity = checked(size.x * size.y);
            if (destination.Capacity < requiredCapacity)
            {
                destination.Capacity = requiredCapacity;
            }

            for (int offsetY = 0; offsetY < size.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < size.x; offsetX++)
                {
                    var worldCell = new Vector2Int(
                        checked(minimumCell.x + offsetX),
                        checked(minimumCell.y + offsetY));
                    WorldNavigationClassification classification =
                        classifier.Classify(worldCell);
                    destination.Add(new NavigationOverlayCell(
                        worldCell,
                        WorldTileCoordinates.CellToWorldPosition(worldCell),
                        classification,
                        NavigationOverlayPalette.GetColour(classification)));
                }
            }
        }

        /// <summary>
        /// Returns the world-space location of a chunk's left or bottom edge.
        /// Cell centres are integers, so chunk edges sit on half units.
        /// </summary>
        public static float GetChunkBoundaryPosition(int chunkCoordinate)
        {
            return checked(chunkCoordinate * WorldTileCoordinates.ChunkSize) - 0.5f;
        }
    }
}
