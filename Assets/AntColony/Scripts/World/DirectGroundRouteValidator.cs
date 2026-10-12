using System;
using UnityEngine;

namespace AntColony.World
{
    /// <summary>Explains the outcome of one bounded, straight controlled-route query.</summary>
    public enum DirectGroundRouteStatus
    {
        Success,
        InvalidRequest,
        RouteTooLong,
        DirectionNotPermitted,
        DiagonalCornerBlocked,
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
    /// Validates a short direct controlled segment across grounded or grippable
    /// Background space. This is deliberately not pathfinding or a navigation graph.
    /// </summary>
    public sealed class DirectGroundRouteValidator
    {
        public const float DefaultSampleSpacing = 0.2f;
        public const float DefaultMaximumDistance = 24f;

        private readonly ChunkedWorldData worldData;
        private readonly AntCollisionProfile profile;
        private readonly AntMovementCapabilityEvaluator capabilityEvaluator;
        private readonly float sampleSpacing;
        private readonly float maximumDistance;

        public DirectGroundRouteValidator(
            ChunkedWorldData worldData,
            AntCollisionProfile profile,
            float sampleSpacing = DefaultSampleSpacing,
            float maximumDistance = DefaultMaximumDistance)
        {
            this.worldData = worldData ??
                throw new ArgumentNullException(nameof(worldData));
            if (!profile.IsValid || sampleSpacing <= 0f || maximumDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(profile),
                    "The profile and route limits must be valid and positive.");
            }

            this.profile = profile;
            this.sampleSpacing = sampleSpacing;
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
            DirectGroundRouteResult poseResult = ValidateControlledPose(
                groundedPosition,
                DirectGroundRouteStatus.DestinationBlocked,
                DirectGroundRouteStatus.DestinationUnsupported,
                1);
            if (!poseResult.Succeeded)
            {
                return poseResult;
            }

            AntMovementCapabilityResult capability = capabilityEvaluator.Evaluate(
                groundedPosition,
                0f,
                profile);
            return capability.SuggestedState == AntMovementEligibilityState.Grounded &&
                HasCentreGroundSupport(groundedPosition, out _)
                    ? poseResult
                    : Result(
                        DirectGroundRouteStatus.DestinationUnsupported,
                        groundedPosition,
                        capability.CentreCell,
                        1);
        }

        /// <summary>
        /// Resolves a clicked cell to a standing pose when valid ground is present,
        /// otherwise to its grippable centre. Ground first keeps ordinary floor
        /// clicks stable now that natural Backgrounds also provide grip.
        /// </summary>
        public DirectGroundRouteResult ResolveControlledPosition(
            Vector2Int selectedCell,
            out Vector2 controlledPosition)
        {
            DirectGroundRouteResult grounded = ResolveGroundedPosition(
                selectedCell,
                out controlledPosition);
            if (grounded.Succeeded)
            {
                return grounded;
            }

            controlledPosition = WorldTileCoordinates.CellToWorldPosition(selectedCell);
            AntMovementCapabilityResult capability = capabilityEvaluator.Evaluate(
                controlledPosition,
                0f,
                profile);
            if (capability.Succeeded && capability.IsPhysicallyClear &&
                capability.HasBackgroundGrip)
            {
                return Result(
                    DirectGroundRouteStatus.Success,
                    controlledPosition,
                    selectedCell,
                    1);
            }

            return grounded;
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

            float routeDistance = Vector2.Distance(start, destination);
            if (routeDistance > maximumDistance)
            {
                return Result(
                    DirectGroundRouteStatus.RouteTooLong,
                    destination,
                    WorldTileCoordinates.WorldPositionToCell(destination),
                    0);
            }

            DirectGroundRouteResult startResult = ValidateControlledPose(
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
                Mathf.CeilToInt(routeDistance / sampleSpacing));
            Vector2 previous = start;
            AntMovementCapabilityResult previousCapability =
                capabilityEvaluator.Evaluate(start, 0f, profile);
            for (int index = 1; index <= segmentCount; index++)
            {
                Vector2 sample = Vector2.Lerp(
                    start,
                    destination,
                    index / (float)segmentCount);
                bool isDestination = index == segmentCount;
                DirectGroundRouteResult directionResult = ValidateDirection(
                    previous,
                    sample,
                    previousCapability,
                    index + 1);
                if (!directionResult.Succeeded)
                {
                    return directionResult;
                }

                DirectGroundRouteResult cornerResult = ValidateDiagonalCorner(
                    previous,
                    sample,
                    index + 1);
                if (!cornerResult.Succeeded)
                {
                    return cornerResult;
                }

                DirectGroundRouteResult sampleResult = ValidateControlledPose(
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

                previous = sample;
                previousCapability = capabilityEvaluator.Evaluate(
                    sample,
                    0f,
                    profile);
            }

            return Result(
                DirectGroundRouteStatus.Success,
                destination,
                WorldTileCoordinates.WorldPositionToCell(destination),
                segmentCount + 1);
        }

        private DirectGroundRouteResult ValidateControlledPose(
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

            if (capability.SuggestedState != AntMovementEligibilityState.Grounded &&
                capability.SuggestedState != AntMovementEligibilityState.Gripping)
            {
                return Result(
                    unsupportedStatus,
                    position,
                    capability.CentreCell,
                    sampleCount);
            }

            if (capability.SuggestedState == AntMovementEligibilityState.Grounded &&
                !HasCentreGroundSupport(position, out Vector2Int supportCell))
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

        private DirectGroundRouteResult ValidateDirection(
            Vector2 start,
            Vector2 destination,
            AntMovementCapabilityResult capability,
            int sampleCount)
        {
            Vector2 delta = destination - start;
            bool horizontal = Mathf.Abs(delta.x) >
                AntClearanceValidator.GeometryTolerance;
            bool upward = delta.y > AntClearanceValidator.GeometryTolerance;
            bool downward = delta.y < -AntClearanceValidator.GeometryTolerance;
            bool permitted = (!horizontal || capability.AllowsHorizontalMovement) &&
                (!upward || capability.AllowsUpwardMovement) &&
                (!downward || capability.AllowsDownwardMovement);
            return permitted
                ? Result(
                    DirectGroundRouteStatus.Success,
                    destination,
                    capability.CentreCell,
                    sampleCount)
                : Result(
                    DirectGroundRouteStatus.DirectionNotPermitted,
                    start,
                    capability.CentreCell,
                    sampleCount);
        }

        private DirectGroundRouteResult ValidateDiagonalCorner(
            Vector2 start,
            Vector2 destination,
            int sampleCount)
        {
            Vector2Int startCell = WorldTileCoordinates.WorldPositionToCell(start);
            Vector2Int destinationCell =
                WorldTileCoordinates.WorldPositionToCell(destination);
            if (startCell.x == destinationCell.x || startCell.y == destinationCell.y)
            {
                return Result(
                    DirectGroundRouteStatus.Success,
                    destination,
                    destinationCell,
                    sampleCount);
            }

            var horizontalNeighbour = new Vector2Int(
                destinationCell.x,
                startCell.y);
            var verticalNeighbour = new Vector2Int(
                startCell.x,
                destinationCell.y);
            bool horizontalExposed =
                IsExposedAvailable(horizontalNeighbour, out bool horizontalAvailable);
            bool verticalExposed =
                IsExposedAvailable(verticalNeighbour, out bool verticalAvailable);
            if (!horizontalExposed || !verticalExposed)
            {
                DirectGroundRouteStatus status =
                    !horizontalAvailable || !verticalAvailable
                        ? DirectGroundRouteStatus.UnavailableWorldData
                        : DirectGroundRouteStatus.DiagonalCornerBlocked;
                return Result(status, destination, destinationCell, sampleCount);
            }

            return Result(
                DirectGroundRouteStatus.Success,
                destination,
                destinationCell,
                sampleCount);
        }

        private bool IsExposedAvailable(Vector2Int cell, out bool available)
        {
            available = worldData.GetCell(cell, out WorldTileCellData data) ==
                WorldCellQueryResult.Available;
            if (!available)
            {
                return false;
            }

            return !data.HasSolidTile ||
                !worldData.TryGetSolidMaterialDefinition(
                    data.SolidMaterialId,
                    out SolidTileMaterialDefinition solid) ||
                !solid.BlocksMovement;
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
