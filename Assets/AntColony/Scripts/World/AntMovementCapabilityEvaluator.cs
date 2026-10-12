using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>Explains whether a movement-capability query was fully evaluated.</summary>
    public enum AntMovementCapabilityStatus
    {
        Success,
        BlockedByTerrain,
        UnavailableWorldData,
        UnknownBackgroundMaterial,
        InvalidQueryOrProfile
    }

    /// <summary>
    /// Describes the locally suggested movement condition. It does not change a
    /// Rigidbody2D or perform a runtime state transition.
    /// </summary>
    public enum AntMovementEligibilityState
    {
        None,
        Grounded,
        Gripping,
        Falling
    }

    /// <summary>Immutable output from one read-only movement-capability query.</summary>
    public readonly struct AntMovementCapabilityResult
    {
        internal AntMovementCapabilityResult(
            AntMovementCapabilityStatus status,
            AntMovementEligibilityState suggestedState,
            AntClearanceResult clearance,
            Vector2Int centreCell,
            Vector2Int problemCell,
            string backgroundMaterialId,
            bool hasGroundSupport,
            bool hasBackgroundGrip,
            bool allowsHorizontalMovement,
            bool allowsUpwardMovement,
            bool allowsDownwardMovement,
            float movementSpeedMultiplier,
            bool shouldApplyGravity)
        {
            Status = status;
            SuggestedState = suggestedState;
            Clearance = clearance;
            CentreCell = centreCell;
            ProblemCell = problemCell;
            BackgroundMaterialId = backgroundMaterialId;
            HasGroundSupport = hasGroundSupport;
            HasBackgroundGrip = hasBackgroundGrip;
            AllowsHorizontalMovement = allowsHorizontalMovement;
            AllowsUpwardMovement = allowsUpwardMovement;
            AllowsDownwardMovement = allowsDownwardMovement;
            MovementSpeedMultiplier = movementSpeedMultiplier;
            ShouldApplyGravity = shouldApplyGravity;
        }

        public AntMovementCapabilityStatus Status { get; }
        public AntMovementEligibilityState SuggestedState { get; }
        public AntClearanceResult Clearance { get; }
        public Vector2Int CentreCell { get; }
        public Vector2Int ProblemCell { get; }
        public string BackgroundMaterialId { get; }
        public bool IsPhysicallyClear => Clearance.IsClear;
        public bool HasGroundSupport { get; }
        public bool HasBackgroundGrip { get; }
        public bool AllowsHorizontalMovement { get; }
        public bool AllowsUpwardMovement { get; }
        public bool AllowsDownwardMovement { get; }
        public float MovementSpeedMultiplier { get; }
        public bool ShouldApplyGravity { get; }
        public bool Succeeded => Status == AntMovementCapabilityStatus.Success;
    }

    /// <summary>
    /// Combines physical clearance, the centre Background Material, and a small
    /// downward ground-contact check. It owns no scene state or physics objects.
    /// </summary>
    public sealed class AntMovementCapabilityEvaluator
    {
        /// <summary>
        /// Maximum world-space gap accepted between the capsule bottom and the
        /// top face of suitable terrain. This absorbs small pose and float error
        /// without treating a nearby wall or ceiling as ground.
        /// </summary>
        public const float GroundContactTolerance = 0.02f;

        private readonly ChunkedWorldData worldData;
        private readonly AntClearanceValidator clearanceValidator;

        public AntMovementCapabilityEvaluator(ChunkedWorldData worldData)
        {
            this.worldData = worldData ??
                throw new ArgumentNullException(nameof(worldData));
            clearanceValidator = new AntClearanceValidator(worldData);
        }

        /// <summary>
        /// Evaluates local capability without moving an ant, creating chunks, or
        /// consulting Tilemaps, colliders, navigation connectivity, or reachability.
        /// </summary>
        public AntMovementCapabilityResult Evaluate(
            Vector2 worldPosition,
            float orientationDegrees,
            AntCollisionProfile profile)
        {
            AntClearanceResult clearance = clearanceValidator.Validate(
                worldPosition,
                orientationDegrees,
                profile);

            if (!clearance.IsClear)
            {
                return FailedResult(clearance);
            }

            Vector2Int centreCell = clearance.CentreCell;
            if (worldData.GetCell(centreCell, out WorldTileCellData centreData) !=
                WorldCellQueryResult.Available)
            {
                return Result(
                    AntMovementCapabilityStatus.UnavailableWorldData,
                    clearance,
                    centreCell,
                    centreCell,
                    null);
            }

            if (!worldData.TryGetBackgroundMaterialDefinition(
                    centreData.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition background))
            {
                return Result(
                    AntMovementCapabilityStatus.UnknownBackgroundMaterial,
                    clearance,
                    centreCell,
                    centreCell,
                    centreData.BackgroundMaterialId);
            }

            GroundSupportResult ground = EvaluateGroundSupport(
                worldPosition,
                orientationDegrees,
                profile);
            if (ground.Status == GroundSupportStatus.Unavailable)
            {
                return Result(
                    AntMovementCapabilityStatus.UnavailableWorldData,
                    clearance,
                    centreCell,
                    ground.ProblemCell,
                    centreData.BackgroundMaterialId);
            }

            bool grounded = ground.Status == GroundSupportStatus.Grounded;
            bool gripping = background.ProvidesClimbingGrip;
            bool canControlMovement = grounded || gripping;
            bool allowsHorizontal =
                canControlMovement && background.AllowsHorizontalMovement;
            bool allowsUpward = gripping && background.AllowsUpwardMovement;
            bool allowsDownward = gripping && background.AllowsDownwardMovement;
            bool shouldApplyGravity = !grounded && !gripping &&
                background.GravityAppliesWhenUnsupported;

            AntMovementEligibilityState state = grounded
                ? AntMovementEligibilityState.Grounded
                : gripping
                    ? AntMovementEligibilityState.Gripping
                    : shouldApplyGravity
                        ? AntMovementEligibilityState.Falling
                        : AntMovementEligibilityState.None;

            return new AntMovementCapabilityResult(
                AntMovementCapabilityStatus.Success,
                state,
                clearance,
                centreCell,
                centreCell,
                centreData.BackgroundMaterialId,
                grounded,
                gripping,
                allowsHorizontal,
                allowsUpward,
                allowsDownward,
                background.MovementSpeedMultiplier,
                shouldApplyGravity);
        }

        private GroundSupportResult EvaluateGroundSupport(
            Vector2 worldPosition,
            float orientationDegrees,
            AntCollisionProfile profile)
        {
            if (!AntClearanceValidator.TryGetCapsuleGeometry(
                    worldPosition,
                    orientationDegrees,
                    profile,
                    out _,
                    out Vector2 segmentStart,
                    out Vector2 segmentEnd,
                    out float radius))
            {
                return new GroundSupportResult(
                    GroundSupportStatus.Unavailable,
                    default);
            }

            float capsuleBottom =
                Mathf.Min(segmentStart.y, segmentEnd.y) - radius;
            float minimumX = Mathf.Min(segmentStart.x, segmentEnd.x) - radius;
            float maximumX = Mathf.Max(segmentStart.x, segmentEnd.x) + radius;
            int minimumCellX = Mathf.CeilToInt(
                minimumX - 0.5f - GroundContactTolerance);
            int maximumCellX = Mathf.FloorToInt(
                maximumX + 0.5f + GroundContactTolerance);
            int minimumCellY = Mathf.CeilToInt(
                capsuleBottom - GroundContactTolerance - 0.5f);
            int maximumCellY = Mathf.FloorToInt(
                capsuleBottom + GroundContactTolerance - 0.5f);

            bool foundUnavailable = false;
            Vector2Int unavailableCell = default;
            float contactRadius = radius + GroundContactTolerance;
            float contactRadiusSquared = contactRadius * contactRadius;

            for (int y = minimumCellY; y <= maximumCellY; y++)
            {
                float surfaceY = y + 0.5f;
                for (int x = minimumCellX; x <= maximumCellX; x++)
                {
                    var worldCell = new Vector2Int(x, y);
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

                    if (!cell.HasSolidTile ||
                        !worldData.TryGetSolidMaterialDefinition(
                            cell.SolidMaterialId,
                            out SolidTileMaterialDefinition solid) ||
                        !solid.BlocksMovement ||
                        !solid.SupportsNavigation)
                    {
                        continue;
                    }

                    var surfaceStart = new Vector2(x - 0.5f, surfaceY);
                    var surfaceEnd = new Vector2(x + 0.5f, surfaceY);
                    if (SquaredDistanceBetweenSegments(
                            segmentStart,
                            segmentEnd,
                            surfaceStart,
                            surfaceEnd) <= contactRadiusSquared)
                    {
                        return new GroundSupportResult(
                            GroundSupportStatus.Grounded,
                            worldCell);
                    }
                }
            }

            return foundUnavailable
                ? new GroundSupportResult(
                    GroundSupportStatus.Unavailable,
                    unavailableCell)
                : new GroundSupportResult(
                    GroundSupportStatus.NotGrounded,
                    default);
        }

        private static float SquaredDistanceBetweenSegments(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            if (SegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd))
            {
                return 0f;
            }

            return Mathf.Min(
                Mathf.Min(
                    SquaredDistancePointToSegment(
                        firstStart,
                        secondStart,
                        secondEnd),
                    SquaredDistancePointToSegment(
                        firstEnd,
                        secondStart,
                        secondEnd)),
                Mathf.Min(
                    SquaredDistancePointToSegment(
                        secondStart,
                        firstStart,
                        firstEnd),
                    SquaredDistancePointToSegment(
                        secondEnd,
                        firstStart,
                        firstEnd)));
        }

        private static bool SegmentsIntersect(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            Vector2 firstDirection = firstEnd - firstStart;
            Vector2 secondDirection = secondEnd - secondStart;
            float denominator = Cross(firstDirection, secondDirection);
            Vector2 offset = secondStart - firstStart;

            if (Mathf.Abs(denominator) <= AntClearanceValidator.GeometryTolerance)
            {
                return false;
            }

            float firstProgress = Cross(offset, secondDirection) / denominator;
            float secondProgress = Cross(offset, firstDirection) / denominator;
            return firstProgress >= 0f && firstProgress <= 1f &&
                secondProgress >= 0f && secondProgress <= 1f;
        }

        private static float SquaredDistancePointToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= AntClearanceValidator.GeometryTolerance *
                AntClearanceValidator.GeometryTolerance)
            {
                return (point - start).sqrMagnitude;
            }

            float progress = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + segment * progress)).sqrMagnitude;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static AntMovementCapabilityResult FailedResult(
            AntClearanceResult clearance)
        {
            AntMovementCapabilityStatus status;
            switch (clearance.Status)
            {
                case AntClearanceStatus.BlockedByTerrain:
                    status = AntMovementCapabilityStatus.BlockedByTerrain;
                    break;
                case AntClearanceStatus.UnavailableWorldData:
                    status = AntMovementCapabilityStatus.UnavailableWorldData;
                    break;
                default:
                    status = AntMovementCapabilityStatus.InvalidQueryOrProfile;
                    break;
            }

            return Result(
                status,
                clearance,
                clearance.CentreCell,
                clearance.ProblemCell,
                null);
        }

        private static AntMovementCapabilityResult Result(
            AntMovementCapabilityStatus status,
            AntClearanceResult clearance,
            Vector2Int centreCell,
            Vector2Int problemCell,
            string backgroundMaterialId)
        {
            return new AntMovementCapabilityResult(
                status,
                AntMovementEligibilityState.None,
                clearance,
                centreCell,
                problemCell,
                backgroundMaterialId,
                false,
                false,
                false,
                false,
                false,
                1f,
                false);
        }

        private enum GroundSupportStatus
        {
            NotGrounded,
            Grounded,
            Unavailable
        }

        private readonly struct GroundSupportResult
        {
            public GroundSupportResult(
                GroundSupportStatus status,
                Vector2Int problemCell)
            {
                Status = status;
                ProblemCell = problemCell;
            }

            public GroundSupportStatus Status { get; }
            public Vector2Int ProblemCell { get; }
        }
    }
}
