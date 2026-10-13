using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    /// <summary>
    /// One temporary Eat, Drink or Rest order. Path choice is synchronous and
    /// deterministic, while actual travel remains owned by GridNavigationMotor.
    /// </summary>
    public sealed class SurvivalInteractionOrder
    {
        public const double ConsumptionDurationHours = 1d;
        public const double HungerRestoredPerUnit = 40d;
        public const double ThirstRestoredPerUnit = 50d;
        private static readonly Vector2Int[] AdjacentOffsets =
        {
            new(-1, -1), new(0, -1), new(1, -1), new(-1, 0),
            new(1, 0), new(-1, 1), new(0, 1), new(1, 1)
        };

        private readonly SurvivalInteractionWorld interactionWorld;
        private readonly GridNavigationMap navigationMap;
        private readonly AntRecord ant;
        private readonly string ownerId;
        private double elapsedInteractionHours;

        private SurvivalInteractionOrder(
            SurvivalInteractionWorld interactionWorld,
            GridNavigationMap navigationMap,
            AntRecord ant,
            SurvivalInteractionKind kind,
            Vector2Int targetCell,
            Vector2Int approachCell)
        {
            this.interactionWorld = interactionWorld;
            this.navigationMap = navigationMap;
            this.ant = ant;
            ownerId = ant.Id.Value;
            Kind = kind;
            TargetCell = targetCell;
            ApproachCell = approachCell;
            Phase = SurvivalInteractionPhase.Travelling;
            Status = $"Travelling to {kind} at {targetCell}.";
        }

        public SurvivalInteractionKind Kind { get; }
        public SurvivalInteractionPhase Phase { get; private set; }
        public Vector2Int TargetCell { get; }
        public Vector2Int ApproachCell { get; }
        public string Status { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsActive => Phase == SurvivalInteractionPhase.Travelling ||
            Phase == SurvivalInteractionPhase.Interacting;

        public static bool TryCreate(
            SurvivalInteractionWorld interactionWorld,
            GridNavigationMap navigationMap,
            AntRecord ant,
            SurvivalInteractionKind kind,
            Vector2Int startCell,
            Vector2Int targetCell,
            out SurvivalInteractionOrder order,
            out string error)
        {
            order = null;
            if (interactionWorld == null || navigationMap == null || ant == null ||
                !ant.CanAcceptMovementOrders)
            {
                error = "Interaction requires an active bound ant and navigation world.";
                return false;
            }

            BackgroundFunction required = RequiredFunction(kind);
            if (!interactionWorld.TryValidateFunction(targetCell, required, out error))
            {
                return false;
            }

            string ownerId = ant.Id.Value;
            bool reserved = kind switch
            {
                SurvivalInteractionKind.Eat => interactionWorld.TryReserveResource(
                    targetCell, SurvivalResourceKind.Food, ownerId, 1, out error),
                SurvivalInteractionKind.Drink => interactionWorld.TryReserveResource(
                    targetCell, SurvivalResourceKind.Water, ownerId, 1, out error),
                _ => interactionWorld.TryReserveSleeping(targetCell, ownerId, out error)
            };
            if (!reserved)
            {
                return false;
            }

            if (!TryChooseApproach(
                    navigationMap, startCell, targetCell, out Vector2Int approach, out error))
            {
                interactionWorld.ReleaseReservations(ownerId);
                return false;
            }

            order = new SurvivalInteractionOrder(
                interactionWorld, navigationMap, ant, kind, targetCell, approach);
            return true;
        }

        public bool TryBeginInteraction(Vector2Int currentCell)
        {
            if (Phase != SurvivalInteractionPhase.Travelling)
            {
                return false;
            }
            if (!IsValidAdjacentAnchor(navigationMap, currentCell, TargetCell))
            {
                Fail("The ant is not at a valid adjacent interaction position.");
                return false;
            }
            if (!interactionWorld.TryValidateFunction(
                    TargetCell, RequiredFunction(Kind), out string error))
            {
                Fail(error);
                return false;
            }

            Phase = SurvivalInteractionPhase.Interacting;
            Status = Kind == SurvivalInteractionKind.Rest
                ? $"Resting beside {TargetCell}."
                : $"Performing {Kind} interaction at {TargetCell}.";
            return true;
        }

        public void Advance(double elapsedHours)
        {
            if (Phase != SurvivalInteractionPhase.Interacting)
            {
                return;
            }
            if (!ant.CanAcceptMovementOrders || ant.Needs.HasDepletedHealth)
            {
                Fail("Interaction cancelled because the ant is inactive or has no Health.");
                return;
            }
            if (!interactionWorld.TryValidateFunction(
                    TargetCell, RequiredFunction(Kind), out string validationError))
            {
                Fail(validationError);
                return;
            }
            if (elapsedHours < 0d || double.IsNaN(elapsedHours) || double.IsInfinity(elapsedHours))
            {
                Fail("Interaction time must be finite and non-negative.");
                return;
            }

            if (Kind == SurvivalInteractionKind.Rest)
            {
                if (!ant.TryRest(elapsedHours, out string restError))
                {
                    Fail(restError);
                }
                else if (ant.Needs.HasCompletedRest &&
                    ant.Needs.Fatigue <= ant.Needs.Settings.RestedQualificationFatigue)
                {
                    Complete("Rest completed; returned to Autonomous / Idle.");
                }
                return;
            }

            elapsedInteractionHours += elapsedHours;
            if (elapsedInteractionHours < ConsumptionDurationHours)
            {
                return;
            }

            if (!interactionWorld.TryConsumeReserved(
                    TargetCell, ownerId, 1, out string consumeError))
            {
                Fail(consumeError);
                return;
            }

            bool applied = Kind == SurvivalInteractionKind.Eat
                ? ant.TryEat(HungerRestoredPerUnit, out string applyError)
                : ant.TryDrink(ThirstRestoredPerUnit, out applyError);
            if (!applied)
            {
                Fail(applyError);
                return;
            }

            Complete($"{Kind} completed; returned to Autonomous / Idle.");
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            interactionWorld.ReleaseReservations(ownerId);
            Phase = SurvivalInteractionPhase.Cancelled;
            Status = "Cancelled; returned to Autonomous / Idle.";
            FailureReason = reason;
        }

        private void Complete(string status)
        {
            interactionWorld.ReleaseReservations(ownerId);
            Phase = SurvivalInteractionPhase.Completed;
            Status = status;
            FailureReason = string.Empty;
        }

        private void Fail(string reason)
        {
            interactionWorld.ReleaseReservations(ownerId);
            Phase = SurvivalInteractionPhase.Failed;
            Status = "Failed; returned to Autonomous / Idle.";
            FailureReason = reason;
        }

        private static bool TryChooseApproach(
            GridNavigationMap map,
            Vector2Int start,
            Vector2Int target,
            out Vector2Int approach,
            out string error)
        {
            approach = default;
            float bestCost = float.PositiveInfinity;
            var pathfinder = new GridPathfinder(map);
            foreach (Vector2Int offset in AdjacentOffsets)
            {
                Vector2Int candidate = target + offset;
                if (!IsValidAdjacentAnchor(map, candidate, target)) continue;
                GridPathResult path = pathfinder.FindPath(start, candidate);
                if (path.Succeeded && path.TravelTime < bestCost)
                {
                    bestCost = path.TravelTime;
                    approach = candidate;
                }
            }

            if (float.IsPositiveInfinity(bestCost))
            {
                error = $"No reachable adjacent interaction position exists for {target}.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsValidAdjacentAnchor(
            GridNavigationMap map,
            Vector2Int candidate,
            Vector2Int target)
        {
            Vector2Int offset = candidate - target;
            if (Mathf.Abs(offset.x) > 1 || Mathf.Abs(offset.y) > 1 || offset == Vector2Int.zero ||
                !map.TryGetAnchor(candidate, out _)) return false;

            // A diagonal interaction cannot reach through a blocked corner. This
            // is the same two-cardinal-anchor rule used by navigation edges.
            return offset.x == 0 || offset.y == 0 ||
                map.TryGetAnchor(target + new Vector2Int(offset.x, 0), out _) &&
                map.TryGetAnchor(target + new Vector2Int(0, offset.y), out _);
        }

        private static BackgroundFunction RequiredFunction(SurvivalInteractionKind kind) =>
            kind switch
            {
                SurvivalInteractionKind.Eat => BackgroundFunction.FoodStorage,
                SurvivalInteractionKind.Drink => BackgroundFunction.WaterStorage,
                _ => BackgroundFunction.Sleeping
            };
    }
}
