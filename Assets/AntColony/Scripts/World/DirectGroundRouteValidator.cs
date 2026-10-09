using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>Explains the outcome of one bounded, straight ground-route query.</summary>
    public enum DirectGroundRouteStatus
    {
        Success,
        InvalidRequest,
        RouteTooLong,
        HeightChangeUnsupported,
        StartBlocked,
        StartUnsupported,
        DestinationBlocked,
        DestinationUnsupported,
        RouteBlocked,
        RouteUnsupported,
        UnavailableWorldData
    }

    /// <summary>Immutable diagnostics for a direct ground-route query.</summary>
    public readonly struct DirectGroundRouteResult
    {
        public DirectGroundRouteResult(
            DirectGroundRouteStatus status,
            Vector2 problemPosition,
            Vector2Int problemCell,
            int sampleCount)
        {
            Status = status;
            ProblemPosition = problemPosition;
            ProblemCell = problemCell;
            SampleCount = sampleCount;
        }

        public DirectGroundRouteStatus Status { get; }
        public Vector2 ProblemPosition { get; }
        public Vector2Int ProblemCell { get; }
        public int SampleCount { get; }
        public bool Succeeded => Status == DirectGroundRouteStatus.Success;
    }

    /// <summary>
    /// Validates a short, approximately horizontal grounded corridor. This is a
    /// deliberately small milestone tool, not general pathfinding or a swept
    /// navigation graph.
    /// </summary>
    public sealed class DirectGroundRouteValidator
    {
        public const float DefaultSampleSpacing = 0.2f;
        public const float DefaultMaximumHeightDifference = 0.05f;
        public const float DefaultMaximumDistance = 24f;

        private readonly ChunkedWorldData worldData;
        private readonly AntCollisionProfile profile;
        private readonly AntMovementCapabilityEvaluator capabilityEvaluator;
        private readonly float sampleSpacing;
        private readonly float maximumHeightDifference;
        private readonly float maximumDistance;

        public DirectGroundRouteValidator(
            ChunkedWorldData worldData,
            AntCollisionProfile profile,
            float sampleSpacing = DefaultSampleSpacing,
            float maximumHeightDifference = DefaultMaximumHeightDifference,
            float maximumDistance = DefaultMaximumDistance)
        {
            this.worldData = worldData ??
                throw new ArgumentNullException(nameof(worldData));
            if (!profile.IsValid || sampleSpacing <= 0f ||
                maximumHeightDifference < 0f || maximumDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(profile),
                    "The profile and route limits must be valid and positive.");
            }

            this.profile = profile;
            this.sampleSpacing = sampleSpacing;
            this.maximumHeightDifference = maximumHeightDifference;
            this.maximumDistance = maximumDistance;
            capabilityEvaluator = new AntMovementCapabilityEvaluator(worldData);
        }

        /// <summary>
        /// Converts a clicked cell into a standing pose. Clicking either exposed
        /// space directly above a floor or the floor Solid itself is supported.
        /// </summary>
        public DirectGroundRouteResult ResolveGroundedPosition(
            Vector2Int selectedCell,
            out Vector2 groundedPosition)
        {
            groundedPosition = default;
            if (worldData.GetCell(selectedCell, out WorldTileCellData selected) !=
                WorldCellQueryResult.Available)
            {
                return Result(
                    DirectGroundRouteStatus.UnavailableWorldData,
                    WorldTileCoordinates.CellToWorldPosition(selectedCell),
                    selectedCell,
                    0);
            }

            Vector2Int openCell = selectedCell;
            if (IsBlockingSupport(selected))
            {
                openCell += Vector2Int.up;
            }

            float halfHeight = profile.Size.y * 0.5f + profile.ClearanceMargin;
            groundedPosition = WorldTileCoordinates.CellToWorldPosition(openCell) +
                Vector2.down * (0.5f - halfHeight);
            return ValidatePose(
                groundedPosition,
                DirectGroundRouteStatus.DestinationBlocked,
                DirectGroundRouteStatus.DestinationUnsupported,
                1);
        }

        /// <summary>
        /// Samples the complete requested segment. Overlapping body-width samples
        /// prevent narrow blockers from being skipped, while explicit centre-line
        /// support rejects gaps that the capsule could otherwise bridge.
        /// </summary>
        public DirectGroundRouteResult Validate(Vector2 start, Vector2 destination)
        {
            if (!IsFinite(start) || !IsFinite(destination))
            {
                return Result(
                    DirectGroundRouteStatus.InvalidRequest,
                    destination,
                    default,
                    0);
            }

            float horizontalDistance = Mathf.Abs(destination.x - start.x);
            if (horizontalDistance > maximumDistance)
            {
                return Result(
                    DirectGroundRouteStatus.RouteTooLong,
                    destination,
                    WorldTileCoordinates.WorldPositionToCell(destination),
                    0);
            }

            if (Mathf.Abs(destination.y - start.y) > maximumHeightDifference)
            {
                return Result(
                    DirectGroundRouteStatus.HeightChangeUnsupported,
                    destination,
                    WorldTileCoordinates.WorldPositionToCell(destination),
                    0);
            }

            DirectGroundRouteResult startResult = ValidatePose(
                start,
                DirectGroundRouteStatus.StartBlocked,
                DirectGroundRouteStatus.StartUnsupported,
                1);
            if (!startResult.Succeeded)
            {
                return startResult;
            }

            int segmentCount = Mathf.Max(
                1,
                Mathf.CeilToInt(horizontalDistance / sampleSpacing));
            for (int index = 1; index <= segmentCount; index++)
            {
                Vector2 sample = Vector2.Lerp(
                    start,
                    destination,
                    index / (float)segmentCount);
                bool isDestination = index == segmentCount;
                DirectGroundRouteResult sampleResult = ValidatePose(
                    sample,
                    isDestination
                        ? DirectGroundRouteStatus.DestinationBlocked
                        : DirectGroundRouteStatus.RouteBlocked,
                    isDestination
                        ? DirectGroundRouteStatus.DestinationUnsupported
                        : DirectGroundRouteStatus.RouteUnsupported,
                    index + 1);
                if (!sampleResult.Succeeded)
                {
                    return sampleResult;
                }
            }

            return Result(
                DirectGroundRouteStatus.Success,
                destination,
                WorldTileCoordinates.WorldPositionToCell(destination),
                segmentCount + 1);
        }

        private DirectGroundRouteResult ValidatePose(
            Vector2 position,
            DirectGroundRouteStatus blockedStatus,
            DirectGroundRouteStatus unsupportedStatus,
            int sampleCount)
        {
            AntMovementCapabilityResult capability = capabilityEvaluator.Evaluate(
                position,
                0f,
                profile);
            if (capability.Status == AntMovementCapabilityStatus.UnavailableWorldData)
            {
                return Result(
                    DirectGroundRouteStatus.UnavailableWorldData,
                    position,
                    capability.ProblemCell,
                    sampleCount);
            }

            if (!capability.Succeeded || !capability.IsPhysicallyClear)
            {
                return Result(
                    blockedStatus,
                    position,
                    capability.ProblemCell,
                    sampleCount);
            }

            Vector2Int supportCell = capability.CentreCell;
            bool hasCentreSupport = HasCentreGroundSupport(
                position,
                out supportCell);
            if (capability.SuggestedState != AntMovementEligibilityState.Grounded ||
                !capability.AllowsHorizontalMovement ||
                !hasCentreSupport)
            {
                return Result(
                    unsupportedStatus,
                    position,
                    supportCell,
                    sampleCount);
            }

            return Result(
                DirectGroundRouteStatus.Success,
                position,
                capability.CentreCell,
                sampleCount);
        }

        private bool HasCentreGroundSupport(
            Vector2 position,
            out Vector2Int supportCell)
        {
            float halfHeight = profile.Size.y * 0.5f + profile.ClearanceMargin;
            float bodyBottom = position.y + profile.Offset.y - halfHeight;
            int supportY = Mathf.RoundToInt(bodyBottom - 0.5f);
            supportCell = new Vector2Int(
                WorldTileCoordinates.WorldPositionToCell(position).x,
                supportY);

            float expectedTop = supportY + 0.5f;
            if (Mathf.Abs(expectedTop - bodyBottom) >
                AntMovementCapabilityEvaluator.GroundContactTolerance)
            {
                return false;
            }

            return worldData.GetCell(supportCell, out WorldTileCellData cell) ==
                    WorldCellQueryResult.Available &&
                IsBlockingSupport(cell);
        }

        private bool IsBlockingSupport(WorldTileCellData cell)
        {
            return cell.HasSolidTile &&
                worldData.TryGetSolidMaterialDefinition(
                    cell.SolidMaterialId,
                    out SolidTileMaterialDefinition solid) &&
                solid.BlocksMovement &&
                solid.SupportsNavigation;
        }

        private static DirectGroundRouteResult Result(
            DirectGroundRouteStatus status,
            Vector2 problemPosition,
            Vector2Int problemCell,
            int sampleCount)
        {
            return new DirectGroundRouteResult(
                status,
                problemPosition,
                problemCell,
                sampleCount);
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }
    }
}
