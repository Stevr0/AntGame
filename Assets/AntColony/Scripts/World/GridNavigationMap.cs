using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    public enum GridNavigationAnchorKind
    {
        Grounded,
        BackgroundGrip
    }

    /// <summary>A stable pose derived only from authoritative terrain data.</summary>
    public readonly struct GridNavigationAnchor
    {
        public GridNavigationAnchor(
            Vector2Int cell,
            Vector2 position,
            GridNavigationAnchorKind kind,
            float speedMultiplier)
        {
            Cell = cell;
            Position = position;
            Kind = kind;
            SpeedMultiplier = speedMultiplier;
        }

        public Vector2Int Cell { get; }
        public Vector2 Position { get; }
        public GridNavigationAnchorKind Kind { get; }
        public float SpeedMultiplier { get; }
    }

    public readonly struct GridNavigationConnection
    {
        public GridNavigationConnection(GridNavigationAnchor destination, float travelTime)
        {
            Destination = destination;
            TravelTime = travelTime;
        }

        public GridNavigationAnchor Destination { get; }
        public float TravelTime { get; }
    }

    /// <summary>
    /// Lazily classifies ordinary-ant anchors and terrain-only connections. The
    /// cache is shared by every experimental ant; moving agents never enter it.
    /// </summary>
    public sealed class GridNavigationMap
    {
        private static readonly Vector2Int[] Directions =
        {
            new(-1, -1), new(0, -1), new(1, -1), new(-1, 0),
            new(1, 0), new(-1, 1), new(0, 1), new(1, 1)
        };

        private readonly ChunkedWorldData worldData;
        private readonly AntCollisionProfile profile;
        private readonly AntMovementCapabilityEvaluator capability;
        private readonly Dictionary<Vector2Int, AnchorCacheEntry> anchors = new();
        private readonly Dictionary<ConnectionKey, ConnectionCacheEntry> connections = new();

        public GridNavigationMap(
            ChunkedWorldData worldData,
            AntCollisionProfile profile)
        {
            this.worldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
            this.profile = profile;
            capability = new AntMovementCapabilityEvaluator(worldData);
        }

        public int Revision { get; private set; }
        public int CachedAnchorCount => anchors.Count;
        public int CachedConnectionCount => connections.Count;
        public float MaximumSpeedMultiplier =>
            worldData.MaximumBackgroundMovementSpeedMultiplier;

        public bool TryGetAnchor(Vector2Int cell, out GridNavigationAnchor anchor)
        {
            if (!anchors.TryGetValue(cell, out AnchorCacheEntry cached))
            {
                cached = Classify(cell);
                anchors.Add(cell, cached);
            }

            anchor = cached.Anchor;
            return cached.IsValid;
        }

        public void GetConnections(
            Vector2Int sourceCell,
            List<GridNavigationConnection> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (!TryGetAnchor(sourceCell, out GridNavigationAnchor source))
            {
                return;
            }

            foreach (Vector2Int direction in Directions)
            {
                var key = new ConnectionKey(sourceCell, direction);
                if (!connections.TryGetValue(key, out ConnectionCacheEntry cached))
                {
                    cached = ClassifyConnection(source, direction);
                    connections.Add(key, cached);
                }

                if (cached.IsValid)
                {
                    results.Add(cached.Connection);
                }
            }
        }

        /// <summary>
        /// Drops only entries whose one-cell edge or ant footprint may touch the
        /// changed tile. The world remains authoritative and no chunk is created.
        /// </summary>
        public void InvalidateCell(Vector2Int changedCell)
        {
            const int radius = 2;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int cell = changedCell + new Vector2Int(x, y);
                    anchors.Remove(cell);
                    foreach (Vector2Int direction in Directions)
                    {
                        connections.Remove(new ConnectionKey(cell, direction));
                        connections.Remove(new ConnectionKey(cell - direction, direction));
                    }
                }
            }

            Revision++;
        }

        private AnchorCacheEntry Classify(Vector2Int cell)
        {
            if (worldData.GetCell(cell, out WorldTileCellData data) !=
                    WorldCellQueryResult.Available ||
                data.HasSolidTile ||
                !worldData.TryGetBackgroundMaterialDefinition(
                    data.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition background))
            {
                return default;
            }

            float halfHeight = profile.Size.y * 0.5f;
            Vector2 centre = WorldTileCoordinates.CellToWorldPosition(cell);
            Vector2 groundedPosition = centre +
                Vector2.down * (0.5f - halfHeight);
            AntMovementCapabilityResult grounded = capability.Evaluate(
                groundedPosition, 0f, profile);
            if (grounded.Succeeded && grounded.SuggestedState ==
                    AntMovementEligibilityState.Grounded &&
                grounded.AllowsHorizontalMovement)
            {
                return new AnchorCacheEntry(new GridNavigationAnchor(
                    cell,
                    groundedPosition,
                    GridNavigationAnchorKind.Grounded,
                    grounded.MovementSpeedMultiplier));
            }

            AntMovementCapabilityResult gripping = capability.Evaluate(
                centre, 0f, profile);
            if (gripping.Succeeded && gripping.HasBackgroundGrip)
            {
                return new AnchorCacheEntry(new GridNavigationAnchor(
                    cell,
                    centre,
                    GridNavigationAnchorKind.BackgroundGrip,
                    gripping.MovementSpeedMultiplier));
            }

            return default;
        }

        private ConnectionCacheEntry ClassifyConnection(
            GridNavigationAnchor source,
            Vector2Int direction)
        {
            Vector2Int destinationCell = source.Cell + direction;
            if (!TryGetAnchor(destinationCell, out GridNavigationAnchor destination))
            {
                return default;
            }

            bool diagonal = direction.x != 0 && direction.y != 0;
            if (diagonal &&
                (!TryGetAnchor(source.Cell + new Vector2Int(direction.x, 0), out _) ||
                 !TryGetAnchor(source.Cell + new Vector2Int(0, direction.y), out _)))
            {
                return default;
            }

            Vector2 delta = destination.Position - source.Position;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.2f));
            for (int sample = 0; sample <= sampleCount; sample++)
            {
                Vector2 position = Vector2.Lerp(
                    source.Position,
                    destination.Position,
                    sample / (float)sampleCount);
                AntMovementCapabilityResult result = capability.Evaluate(
                    position, 0f, profile);
                if (!result.Succeeded ||
                    (result.SuggestedState != AntMovementEligibilityState.Grounded &&
                     result.SuggestedState != AntMovementEligibilityState.Gripping))
                {
                    return default;
                }

                Vector2 movement = delta.normalized;
                if (Mathf.Abs(movement.x) > 0.01f && !result.AllowsHorizontalMovement ||
                    movement.y > 0.01f && !result.AllowsUpwardMovement ||
                    movement.y < -0.01f && !result.AllowsDownwardMovement &&
                    !result.HasGroundSupport)
                {
                    return default;
                }
            }

            float averageSpeed = Mathf.Max(
                0.01f,
                (source.SpeedMultiplier + destination.SpeedMultiplier) * 0.5f);
            return new ConnectionCacheEntry(new GridNavigationConnection(
                destination,
                delta.magnitude / averageSpeed));
        }

        private readonly struct AnchorCacheEntry
        {
            public AnchorCacheEntry(GridNavigationAnchor anchor)
            {
                Anchor = anchor;
                IsValid = true;
            }

            public GridNavigationAnchor Anchor { get; }
            public bool IsValid { get; }
        }

        private readonly struct ConnectionCacheEntry
        {
            public ConnectionCacheEntry(GridNavigationConnection connection)
            {
                Connection = connection;
                IsValid = true;
            }

            public GridNavigationConnection Connection { get; }
            public bool IsValid { get; }
        }

        private readonly struct ConnectionKey : IEquatable<ConnectionKey>
        {
            public ConnectionKey(Vector2Int cell, Vector2Int direction)
            {
                Cell = cell;
                Direction = direction;
            }

            public Vector2Int Cell { get; }
            public Vector2Int Direction { get; }
            public bool Equals(ConnectionKey other) =>
                Cell == other.Cell && Direction == other.Direction;
            public override bool Equals(object obj) =>
                obj is ConnectionKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Cell, Direction);
        }
    }
}
