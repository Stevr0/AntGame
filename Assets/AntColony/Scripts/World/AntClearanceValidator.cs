using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>Identifies the capsule's long axis before body rotation is applied.</summary>
    public enum AntCapsuleAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>Explains the outcome of one static ant-body occupancy query.</summary>
    public enum AntClearanceStatus
    {
        Clear,
        BlockedByTerrain,
        [Obsolete("Physical clearance no longer reports navigation support.")]
        UnsupportedLocation,
        UnavailableWorldData,
        InvalidQueryOrProfile
    }

    /// <summary>
    /// Reusable, scene-independent capsule dimensions. Size and offset use the
    /// ant's local axes; orientation rotates both into world space.
    /// </summary>
    public readonly struct AntCollisionProfile
    {
        public AntCollisionProfile(
            Vector2 size,
            Vector2 offset,
            AntCapsuleAxis capsuleAxis,
            float clearanceMargin = 0f)
        {
            Size = size;
            Offset = offset;
            CapsuleAxis = capsuleAxis;
            ClearanceMargin = clearanceMargin;
        }

        public Vector2 Size { get; }
        public Vector2 Offset { get; }
        public AntCapsuleAxis CapsuleAxis { get; }
        public float ClearanceMargin { get; }

        /// <summary>
        /// Matches the current movement-scene PlayerAnt instance without modifying
        /// it: the prefab's 1.2x0.6 capsule is scaled by 0.8x0.5 there.
        /// </summary>
        public static AntCollisionProfile OrdinaryAnt => new AntCollisionProfile(
            new Vector2(0.96f, 0.3f),
            Vector2.zero,
            AntCapsuleAxis.Horizontal);

        public bool IsValid
        {
            get
            {
                if (!IsFinite(Size) || !IsFinite(Offset) ||
                    !IsFinite(ClearanceMargin) ||
                    Size.x <= 0f || Size.y <= 0f || ClearanceMargin < 0f)
                {
                    return false;
                }

                if (CapsuleAxis != AntCapsuleAxis.Horizontal &&
                    CapsuleAxis != AntCapsuleAxis.Vertical)
                {
                    return false;
                }

                // Unity capsules require the selected axis to be at least as
                // long as the perpendicular diameter.
                return CapsuleAxis == AntCapsuleAxis.Horizontal
                    ? Size.x >= Size.y
                    : Size.y >= Size.x;
            }
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>Immutable diagnostics from one clearance evaluation.</summary>
    public readonly struct AntClearanceResult
    {
        internal AntClearanceResult(
            AntClearanceStatus status,
            Vector2 requestedPosition,
            Vector2 bodyCentre,
            Vector2Int centreCell,
            Vector2Int problemCell,
            int touchedCellCount)
        {
            Status = status;
            RequestedPosition = requestedPosition;
            BodyCentre = bodyCentre;
            CentreCell = centreCell;
            ProblemCell = problemCell;
            TouchedCellCount = touchedCellCount;
        }

        public AntClearanceStatus Status { get; }
        public Vector2 RequestedPosition { get; }
        public Vector2 BodyCentre { get; }
        public Vector2Int CentreCell { get; }
        public Vector2Int ProblemCell { get; }
        public int TouchedCellCount { get; }
        public bool IsClear => Status == AntClearanceStatus.Clear;
    }

    /// <summary>
    /// Performs allocation-free static capsule checks against authoritative
    /// blocking world cells. It does not read Tilemaps, colliders, or scene state.
    /// </summary>
    public sealed class AntClearanceValidator
    {
        // Tangency is permitted because it is contact rather than overlap. This
        // small tolerance absorbs float noise; use ClearanceMargin when route
        // planning needs a deliberately conservative gap from physics contacts.
        public const float GeometryTolerance = 0.00001f;

        private readonly ChunkedWorldData worldData;

        public AntClearanceValidator(ChunkedWorldData worldData)
        {
            this.worldData = worldData ??
                throw new ArgumentNullException(nameof(worldData));
        }

        /// <summary>
        /// Checks whether the complete rotated capsule can occupy a transform
        /// position. Profile offset is rotated about that position before testing.
        /// This is an occupancy query only; it does not validate a swept path,
        /// route connectivity, reachability, or movement execution.
        /// </summary>
        public AntClearanceResult Validate(
            Vector2 worldPosition,
            float orientationDegrees,
            AntCollisionProfile profile)
        {
            if (!profile.IsValid || !IsFinite(worldPosition) ||
                !IsFinite(orientationDegrees))
            {
                return InvalidResult(worldPosition);
            }

            BuildCapsuleGeometry(
                worldPosition,
                orientationDegrees,
                profile,
                out Vector2 bodyCentre,
                out Vector2 segmentStart,
                out Vector2 segmentEnd,
                out float radius);
            if (!IsFinite(bodyCentre) || !IsFinite(segmentStart) ||
                !IsFinite(segmentEnd) || !IsFinite(radius) || radius <= 0f)
            {
                return InvalidResult(worldPosition);
            }

            Vector2Int centreCell =
                WorldTileCoordinates.WorldPositionToCell(bodyCentre);

            GetCandidateCellBounds(
                segmentStart,
                segmentEnd,
                radius,
                out Vector2Int minimumCell,
                out Vector2Int maximumCell);

            int touchedCellCount = 0;
            bool foundUnavailable = false;
            bool foundBlockingOverlap = false;
            Vector2Int unavailableCell = centreCell;
            Vector2Int blockingCell = centreCell;
            float touchRadius = radius + GeometryTolerance;
            float overlapRadius = Mathf.Max(0f, radius - GeometryTolerance);
            float touchRadiusSquared = touchRadius * touchRadius;
            float overlapRadiusSquared = overlapRadius * overlapRadius;

            for (int y = minimumCell.y; y <= maximumCell.y; y++)
            {
                for (int x = minimumCell.x; x <= maximumCell.x; x++)
                {
                    var worldCell = new Vector2Int(x, y);
                    Vector2 cellCentre =
                        WorldTileCoordinates.CellToWorldPosition(worldCell);
                    Vector2 cellMinimum = cellCentre - Vector2.one * 0.5f;
                    Vector2 cellMaximum = cellCentre + Vector2.one * 0.5f;
                    float distanceSquared = SquaredDistanceSegmentToBox(
                        segmentStart,
                        segmentEnd,
                        cellMinimum,
                        cellMaximum);

                    // Broad-phase bounds include corner cells that the rounded
                    // capsule does not touch. Ignore those false positives.
                    if (distanceSquared > touchRadiusSquared)
                    {
                        continue;
                    }

                    touchedCellCount++;
                    if (worldData.GetCell(worldCell, out WorldTileCellData cell) !=
                        WorldCellQueryResult.Available)
                    {
                        if (!foundUnavailable)
                        {
                            unavailableCell = worldCell;
                        }

                        foundUnavailable = true;
                        continue;
                    }

                    if (!cell.HasSolidTile)
                    {
                        continue;
                    }

                    if (!worldData.TryGetSolidMaterialDefinition(
                            cell.SolidMaterialId,
                            out SolidTileMaterialDefinition material))
                    {
                        if (!foundUnavailable)
                        {
                            unavailableCell = worldCell;
                        }

                        foundUnavailable = true;
                        continue;
                    }

                    // Exact tangency is clear. Any overlap greater than the
                    // documented tolerance is blocked when the material says so.
                    if (material.BlocksMovement &&
                        distanceSquared < overlapRadiusSquared)
                    {
                        if (!foundBlockingOverlap)
                        {
                            blockingCell = worldCell;
                        }

                        foundBlockingOverlap = true;
                    }
                }
            }

            // A known blocking overlap already proves that the pose is invalid,
            // even when another part of the footprint has not loaded yet.
            if (foundBlockingOverlap)
            {
                return Result(
                    AntClearanceStatus.BlockedByTerrain,
                    worldPosition,
                    bodyCentre,
                    centreCell,
                    blockingCell,
                    touchedCellCount);
            }

            // A physically clear result requires data for every touched cell.
            if (foundUnavailable)
            {
                return Result(
                    AntClearanceStatus.UnavailableWorldData,
                    worldPosition,
                    bodyCentre,
                    centreCell,
                    unavailableCell,
                    touchedCellCount);
            }

            return Result(
                AntClearanceStatus.Clear,
                worldPosition,
                bodyCentre,
                centreCell,
                centreCell,
                touchedCellCount);
        }

        /// <summary>
        /// Returns endpoints and radius for debug drawing using exactly the same
        /// profile interpretation as validation.
        /// </summary>
        public static bool TryGetCapsuleGeometry(
            Vector2 worldPosition,
            float orientationDegrees,
            AntCollisionProfile profile,
            out Vector2 bodyCentre,
            out Vector2 segmentStart,
            out Vector2 segmentEnd,
            out float radius)
        {
            if (!profile.IsValid || !IsFinite(worldPosition) ||
                !IsFinite(orientationDegrees))
            {
                bodyCentre = worldPosition;
                segmentStart = worldPosition;
                segmentEnd = worldPosition;
                radius = 0f;
                return false;
            }

            BuildCapsuleGeometry(
                worldPosition,
                orientationDegrees,
                profile,
                out bodyCentre,
                out segmentStart,
                out segmentEnd,
                out radius);
            return IsFinite(bodyCentre) && IsFinite(segmentStart) &&
                IsFinite(segmentEnd) && IsFinite(radius) && radius > 0f;
        }

        private static void BuildCapsuleGeometry(
            Vector2 worldPosition,
            float orientationDegrees,
            AntCollisionProfile profile,
            out Vector2 bodyCentre,
            out Vector2 segmentStart,
            out Vector2 segmentEnd,
            out float radius)
        {
            float radians = orientationDegrees * Mathf.Deg2Rad;
            float sine = Mathf.Sin(radians);
            float cosine = Mathf.Cos(radians);
            Vector2 rotatedOffset = Rotate(profile.Offset, sine, cosine);
            bodyCentre = worldPosition + rotatedOffset;

            Vector2 localAxis = profile.CapsuleAxis == AntCapsuleAxis.Horizontal
                ? Vector2.right
                : Vector2.up;
            Vector2 worldAxis = Rotate(localAxis, sine, cosine);
            float axialSize = profile.CapsuleAxis == AntCapsuleAxis.Horizontal
                ? profile.Size.x
                : profile.Size.y;
            float diameter = profile.CapsuleAxis == AntCapsuleAxis.Horizontal
                ? profile.Size.y
                : profile.Size.x;
            float halfSegmentLength = (axialSize - diameter) * 0.5f;
            radius = diameter * 0.5f + profile.ClearanceMargin;
            Vector2 halfSegment = worldAxis * halfSegmentLength;
            segmentStart = bodyCentre - halfSegment;
            segmentEnd = bodyCentre + halfSegment;
        }

        private static void GetCandidateCellBounds(
            Vector2 segmentStart,
            Vector2 segmentEnd,
            float radius,
            out Vector2Int minimumCell,
            out Vector2Int maximumCell)
        {
            float minimumX = Mathf.Min(segmentStart.x, segmentEnd.x) - radius;
            float minimumY = Mathf.Min(segmentStart.y, segmentEnd.y) - radius;
            float maximumX = Mathf.Max(segmentStart.x, segmentEnd.x) + radius;
            float maximumY = Mathf.Max(segmentStart.y, segmentEnd.y) + radius;

            // Cell n owns [n-0.5,n+0.5]. Include exact contact candidates;
            // the narrow-phase distance test decides whether they are touched.
            minimumCell = new Vector2Int(
                Mathf.CeilToInt(minimumX - 0.5f - GeometryTolerance),
                Mathf.CeilToInt(minimumY - 0.5f - GeometryTolerance));
            maximumCell = new Vector2Int(
                Mathf.FloorToInt(maximumX + 0.5f + GeometryTolerance),
                Mathf.FloorToInt(maximumY + 0.5f + GeometryTolerance));
        }

        private static float SquaredDistanceSegmentToBox(
            Vector2 segmentStart,
            Vector2 segmentEnd,
            Vector2 boxMinimum,
            Vector2 boxMaximum)
        {
            if (SegmentIntersectsBox(
                    segmentStart,
                    segmentEnd,
                    boxMinimum,
                    boxMaximum))
            {
                return 0f;
            }

            float best = Mathf.Min(
                SquaredDistancePointToBox(segmentStart, boxMinimum, boxMaximum),
                SquaredDistancePointToBox(segmentEnd, boxMinimum, boxMaximum));
            best = Mathf.Min(best, SquaredDistancePointToSegment(
                new Vector2(boxMinimum.x, boxMinimum.y),
                segmentStart,
                segmentEnd));
            best = Mathf.Min(best, SquaredDistancePointToSegment(
                new Vector2(boxMinimum.x, boxMaximum.y),
                segmentStart,
                segmentEnd));
            best = Mathf.Min(best, SquaredDistancePointToSegment(
                new Vector2(boxMaximum.x, boxMinimum.y),
                segmentStart,
                segmentEnd));
            return Mathf.Min(best, SquaredDistancePointToSegment(
                new Vector2(boxMaximum.x, boxMaximum.y),
                segmentStart,
                segmentEnd));
        }

        private static bool SegmentIntersectsBox(
            Vector2 start,
            Vector2 end,
            Vector2 minimum,
            Vector2 maximum)
        {
            Vector2 direction = end - start;
            float entry = 0f;
            float exit = 1f;
            return ClipAxis(start.x, direction.x, minimum.x, maximum.x, ref entry, ref exit) &&
                ClipAxis(start.y, direction.y, minimum.y, maximum.y, ref entry, ref exit);
        }

        private static bool ClipAxis(
            float origin,
            float direction,
            float minimum,
            float maximum,
            ref float entry,
            ref float exit)
        {
            if (Mathf.Abs(direction) <= GeometryTolerance)
            {
                return origin >= minimum && origin <= maximum;
            }

            float inverse = 1f / direction;
            float first = (minimum - origin) * inverse;
            float second = (maximum - origin) * inverse;
            if (first > second)
            {
                (first, second) = (second, first);
            }

            entry = Mathf.Max(entry, first);
            exit = Mathf.Min(exit, second);
            return entry <= exit;
        }

        private static float SquaredDistancePointToBox(
            Vector2 point,
            Vector2 minimum,
            Vector2 maximum)
        {
            float deltaX = Mathf.Max(
                Mathf.Max(minimum.x - point.x, 0f),
                point.x - maximum.x);
            float deltaY = Mathf.Max(
                Mathf.Max(minimum.y - point.y, 0f),
                point.y - maximum.y);
            return deltaX * deltaX + deltaY * deltaY;
        }

        private static float SquaredDistancePointToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= GeometryTolerance * GeometryTolerance)
            {
                return (point - start).sqrMagnitude;
            }

            float progress = Mathf.Clamp01(Vector2.Dot(point - start, segment) /
                lengthSquared);
            Vector2 nearest = start + segment * progress;
            return (point - nearest).sqrMagnitude;
        }

        private static Vector2 Rotate(Vector2 value, float sine, float cosine)
        {
            return new Vector2(
                value.x * cosine - value.y * sine,
                value.x * sine + value.y * cosine);
        }

        private static AntClearanceResult InvalidResult(Vector2 requestedPosition)
        {
            return Result(
                AntClearanceStatus.InvalidQueryOrProfile,
                requestedPosition,
                requestedPosition,
                default,
                default,
                0);
        }

        private static AntClearanceResult Result(
            AntClearanceStatus status,
            Vector2 requestedPosition,
            Vector2 bodyCentre,
            Vector2Int centreCell,
            Vector2Int problemCell,
            int touchedCellCount)
        {
            return new AntClearanceResult(
                status,
                requestedPosition,
                bodyCentre,
                centreCell,
                problemCell,
                touchedCellCount);
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
