using AntColony.Ants;
using AntColony.Navigation;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum AntOrderSource
    {
        None,
        Player,
        AutonomousSurvival,
        AutonomousWork
    }

    /// <summary>
    /// Adds player-facing selection and temporary-order presentation to one grid
    /// ant. Navigation remains entirely owned by <see cref="GridNavigationMotor"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(GridNavigationMotor),
        typeof(CapsuleCollider2D),
        typeof(AntEntityBinding))]
    public sealed class SelectableGridAnt : MonoBehaviour
    {
        private const int IndicatorPointCount = 32;

        private GridNavigationMotor motor;
        private AntEntityBinding entityBinding;
        private CapsuleCollider2D selectionCollider;
        private LineRenderer selectionIndicator;
        private Material indicatorMaterial;
        private bool hasActiveOrder;
        private bool hasDestination;
        private Vector2Int destination;
        private string orderOutcome = "No movement order.";
        private string failureReason = string.Empty;
        private SurvivalInteractionWorld interactionWorld;
        private SurvivalInteractionOrder interactionOrder;
        private ColonyJobRegistry colonyJobs;
        private SyntheticColonyJobOrder jobOrder;
        private ExcavationDesignationRegistry excavationDesignations;
        private ExcavationWorldTransaction excavationTransaction;
        private ExcavationJobOrder excavationOrder;
        private WorldMaterialDropRegistry materialDrops;
        private MaterialPickupOrder pickupOrder;
        private MaterialStorageWorld materialStorage;
        private MaterialHaulingOrder haulingOrder;
        private ConstructionDesignationRegistry constructionDesignations;
        private ConstructionWorldTransaction constructionTransaction;
        private ConstructionJobOrder constructionOrder;

        public string DisplayName => Record?.DisplayName ?? "Unbound Ant";
        public AntRecord Record => entityBinding?.Record;
        public GridNavigationMotor Motor => motor;
        public bool IsSelected { get; private set; }
        public bool HasActiveOrder => hasActiveOrder;
        public bool HasDestination => hasDestination;
        public Vector2Int Destination => destination;
        public string FailureReason => failureReason;
        public string OrderOutcome => orderOutcome;
        public SurvivalInteractionOrder InteractionOrder => interactionOrder;
        public SyntheticColonyJobOrder JobOrder => jobOrder;
        public ExcavationJobOrder ExcavationOrder => excavationOrder;
        public MaterialPickupOrder PickupOrder => pickupOrder;
        public MaterialHaulingOrder HaulingOrder => haulingOrder;
        public ConstructionJobOrder ConstructionOrder => constructionOrder;
        public ColonyJob ActiveColonyJob => constructionOrder?.Job ?? haulingOrder?.Job ??
            excavationOrder?.Job ?? jobOrder?.Job;
        public AntOrderSource OrderSource { get; private set; }
        public LineRenderer SelectionIndicator => selectionIndicator;
        public string ControlState => hasActiveOrder
            ? OrderSource == AntOrderSource.AutonomousSurvival
                ? "Autonomous Survival"
                : OrderSource == AntOrderSource.AutonomousWork
                    ? "Autonomous Colony Work"
                : "Player Ordered"
            : "Autonomous / Idle";

        public string OrderStatus
        {
            get
            {
                if (!hasActiveOrder)
                {
                    return orderOutcome;
                }

                if (interactionOrder != null && interactionOrder.IsActive)
                {
                    return interactionOrder.Phase == SurvivalInteractionPhase.Travelling
                        ? $"Travelling to {interactionOrder.Kind}"
                        : interactionOrder.Status;
                }

                if (excavationOrder != null && excavationOrder.IsActive)
                    return excavationOrder.Status;
                if (pickupOrder != null && pickupOrder.IsActive)
                    return pickupOrder.Status;
                if (haulingOrder != null && haulingOrder.IsActive)
                    return haulingOrder.Status;
                if (constructionOrder != null && constructionOrder.IsActive)
                    return constructionOrder.Status;
                if (jobOrder != null && jobOrder.IsActive) return jobOrder.Status;

                return motor.State switch
                {
                    GridNavigationMotorState.WaitingForPath => "Waiting for path",
                    GridNavigationMotorState.FollowingPath => "Moving",
                    GridNavigationMotorState.Falling => "Falling",
                    GridNavigationMotorState.Recovering => "Recovering",
                    _ => motor.State.ToString()
                };
            }
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnDestroy()
        {
            if (indicatorMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(indicatorMaterial);
                }
                else
                {
                    DestroyImmediate(indicatorMaterial);
                }
            }
        }

        private void OnDisable()
        {
            interactionOrder?.Cancel("Interaction cancelled because the ant was disabled.");
            jobOrder?.Cancel("Synthetic work cancelled because the ant was disabled.");
            excavationOrder?.Cancel("Excavation cancelled because the ant was disabled.");
            pickupOrder?.Cancel("Pickup cancelled because the ant was disabled.");
            haulingOrder?.Cancel("Hauling cancelled because the ant was disabled.");
            constructionOrder?.Cancel("Construction cancelled because the ant was disabled.");
            hasActiveOrder = false;
            OrderSource = AntOrderSource.None;
        }

        /// <summary>Applies the temporary name, colour and selection indicator.</summary>
        public void Configure(Color placeholderColour)
        {
            CacheComponents();

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.color = placeholderColour;
            }

            EnsureSelectionIndicator();
            SetSelected(false);
        }

        public void BindInteractionWorld(SurvivalInteractionWorld world)
        {
            interactionOrder?.Cancel("Interaction world was replaced.");
            interactionWorld = world;
            interactionOrder = null;
            if (jobOrder == null || !jobOrder.IsActive)
            {
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
            }
        }

        public void BindColonyJobs(ColonyJobRegistry jobs)
        {
            jobOrder?.Cancel("Colony job registry was replaced.");
            excavationOrder?.Cancel("Colony job registry was replaced.");
            haulingOrder?.Cancel("Colony job registry was replaced.");
            constructionOrder?.Cancel("Colony job registry was replaced.");
            colonyJobs = jobs;
            jobOrder = null;
            if (interactionOrder == null || !interactionOrder.IsActive)
            {
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
            }
        }

        public void BindExcavation(ExcavationDesignationRegistry designations,
            ExcavationWorldTransaction transaction,
            WorldMaterialDropRegistry drops = null)
        {
            excavationOrder?.Cancel("Excavation world was replaced.");
            pickupOrder?.Cancel("Material world was replaced.");
            haulingOrder?.Cancel("Material world was replaced.");
            constructionOrder?.Cancel("Material world was replaced.");
            excavationDesignations = designations;
            excavationTransaction = transaction;
            materialDrops = drops;
            excavationOrder = null;
            pickupOrder = null;
        }

        public void BindMaterialHauling(MaterialStorageWorld storage)
        {
            haulingOrder?.Cancel("Material Storage world was replaced.");
            materialStorage = storage;
            haulingOrder = null;
        }

        public void BindConstruction(ConstructionDesignationRegistry designations,
            ConstructionWorldTransaction transaction)
        {
            constructionOrder?.Cancel("Construction world was replaced.");
            constructionDesignations = designations;
            constructionTransaction = transaction;
            constructionOrder = null;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            EnsureSelectionIndicator();
            selectionIndicator.enabled = selected;
        }

        /// <summary>
        /// Uses the authored collider as a selection footprint even while the
        /// Rigidbody2D is excluded from physics simulation during grid travel.
        /// </summary>
        public bool ContainsWorldPoint(Vector2 worldPoint, float padding = 0.2f)
        {
            CacheComponents();
            Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector2 offsetPoint = localPoint - selectionCollider.offset;
            Vector2 halfSize = selectionCollider.size * 0.5f;
            return Mathf.Abs(offsetPoint.x) <= halfSize.x + padding &&
                Mathf.Abs(offsetPoint.y) <= halfSize.y + padding;
        }

        public bool IssueMoveOrder(
            Vector2Int requestedDestination,
            AntOrderSource source = AntOrderSource.Player)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by a movement order.");
            jobOrder?.Cancel("Synthetic work interrupted by a movement order.");
            excavationOrder?.Cancel("Excavation interrupted by a movement order.");
            pickupOrder?.Cancel("Pickup interrupted by a movement order.");
            haulingOrder?.Cancel("Hauling interrupted by a movement order.");
            constructionOrder?.Cancel("Construction interrupted by a movement order.");
            if (Record == null)
            {
                failureReason = "Movement order rejected: ant identity is not bound.";
                orderOutcome = "Order rejected";
                OrderSource = AntOrderSource.None;
                return false;
            }

            if (!Record.CanAcceptMovementOrders)
            {
                failureReason =
                    $"Movement order rejected: {DisplayName} is {Record.LifecycleStatus}.";
                orderOutcome = "Order rejected";
                OrderSource = AntOrderSource.None;
                return false;
            }

            if (!motor.MoveTo(requestedDestination))
            {
                failureReason = motor.StatusMessage;
                orderOutcome = "Order rejected";
                OrderSource = AntOrderSource.None;
                return false;
            }

            destination = requestedDestination;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Order active";
            OrderSource = source;
            return true;
        }

        public bool IssueInteractionOrder(
            SurvivalInteractionKind kind,
            Vector2Int targetCell,
            AntOrderSource source = AntOrderSource.Player)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by a new order.");
            jobOrder?.Cancel("Synthetic work interrupted by a survival interaction.");
            excavationOrder?.Cancel("Excavation interrupted by a survival interaction.");
            pickupOrder?.Cancel("Pickup interrupted by a survival interaction.");
            haulingOrder?.Cancel("Hauling interrupted by a survival interaction.");
            constructionOrder?.Cancel("Construction interrupted by a survival interaction.");
            motor.CancelMovement("Interaction order is choosing an approach.");
            Vector2Int startCell = WorldTileCoordinates.WorldPositionToCell(motor.Position);
            if (!SurvivalInteractionOrder.TryCreate(
                    interactionWorld,
                    motor.NavigationWorld?.Map,
                    Record,
                    kind,
                    startCell,
                    targetCell,
                    out interactionOrder,
                    out string error))
            {
                failureReason = error;
                orderOutcome = "Interaction rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }

            if (!motor.MoveTo(interactionOrder.ApproachCell))
            {
                interactionOrder.Cancel(motor.StatusMessage);
                failureReason = motor.StatusMessage;
                orderOutcome = "Interaction rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }

            destination = interactionOrder.ApproachCell;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Interaction active";
            OrderSource = source;
            return true;
        }

        public bool IssueSyntheticJobOrder(
            ColonyJob job,
            Vector2Int approachCell)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by synthetic work.");
            pickupOrder?.Cancel("Pickup replaced by synthetic work.");
            haulingOrder?.Cancel("Hauling replaced by synthetic work.");
            constructionOrder?.Cancel("Construction replaced by synthetic work.");
            jobOrder?.Cancel("Synthetic work replaced by another job.");
            motor.CancelMovement("Synthetic work is choosing an approach.");
            if (!SyntheticColonyJobOrder.TryCreate(
                    colonyJobs,
                    motor.NavigationWorld?.Map,
                    Record,
                    job,
                    approachCell,
                    out jobOrder,
                    out string error))
            {
                failureReason = error;
                orderOutcome = "Synthetic job rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            if (!motor.MoveTo(approachCell))
            {
                jobOrder.Cancel(motor.StatusMessage);
                failureReason = motor.StatusMessage;
                orderOutcome = "Synthetic job rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }

            destination = approachCell;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Synthetic job active";
            OrderSource = AntOrderSource.AutonomousWork;
            return true;
        }

        public bool IssueExcavationJobOrder(ColonyJob job, Vector2Int approachCell)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by excavation.");
            pickupOrder?.Cancel("Pickup replaced by excavation.");
            haulingOrder?.Cancel("Hauling replaced by excavation.");
            jobOrder?.Cancel("Synthetic work replaced by excavation.");
            excavationOrder?.Cancel("Excavation replaced by another job.");
            motor.CancelMovement("Excavation is choosing an approach.");
            string error = string.Empty;
            if (excavationDesignations == null ||
                !excavationDesignations.TryGet(job.Id, out ExcavationDesignation designation) ||
                !ExcavationJobOrder.TryCreate(colonyJobs, motor.NavigationWorld?.Map,
                    Record, designation, approachCell, excavationTransaction,
                    out excavationOrder, out error))
            {
                failureReason = error ?? "Excavation designation is unavailable.";
                orderOutcome = "Excavation rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            if (!motor.MoveTo(approachCell))
            {
                excavationOrder.Cancel(motor.StatusMessage);
                failureReason = motor.StatusMessage;
                orderOutcome = "Excavation rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            destination = approachCell;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Excavation active";
            OrderSource = AntOrderSource.AutonomousWork;
            return true;
        }

        public bool IssueMaterialHaulingOrder(MaterialHaulingPlan plan,
            Vector2Int sourceApproach)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by hauling.");
            pickupOrder?.Cancel("Pickup replaced by hauling.");
            jobOrder?.Cancel("Synthetic work replaced by hauling.");
            excavationOrder?.Cancel("Excavation replaced by hauling.");
            constructionOrder?.Cancel("Construction replaced by hauling.");
            haulingOrder?.Cancel("Hauling replaced by another hauling job.");
            motor.CancelMovement("Hauling is choosing physical transfer positions.");
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(motor.Position);
            if (!MaterialHaulingOrder.TryCreate(colonyJobs,
                    motor.NavigationWorld?.Map, materialDrops, materialStorage,
                    Record, plan, sourceApproach, start,
                    out haulingOrder, out string error))
            {
                failureReason = error; orderOutcome = "Hauling rejected";
                hasActiveOrder = false; OrderSource = AntOrderSource.None;
                return false;
            }
            if (!motor.MoveTo(haulingOrder.InitialApproachCell))
            {
                haulingOrder.Cancel(motor.StatusMessage);
                failureReason = motor.StatusMessage; orderOutcome = "Hauling rejected";
                hasActiveOrder = false; OrderSource = AntOrderSource.None;
                return false;
            }
            destination = haulingOrder.InitialApproachCell;
            hasDestination = true; hasActiveOrder = true;
            failureReason = string.Empty; orderOutcome = "Hauling active";
            OrderSource = AntOrderSource.AutonomousWork;
            return true;
        }

        public bool IssueConstructionJobOrder(ColonyJob job, Vector2Int siteApproach)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by construction.");
            pickupOrder?.Cancel("Pickup replaced by construction.");
            haulingOrder?.Cancel("Hauling replaced by construction.");
            jobOrder?.Cancel("Synthetic work replaced by construction.");
            excavationOrder?.Cancel("Excavation replaced by construction.");
            constructionOrder?.Cancel("Construction replaced by another job.");
            motor.CancelMovement("Construction is choosing physical supply positions.");
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(motor.Position);
            if (constructionDesignations == null ||
                !constructionDesignations.TryGet(job.Id,
                    out ConstructionDesignation designation))
            {
                failureReason = "Construction designation is unavailable.";
                orderOutcome = "Construction rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            if (!ConstructionJobOrder.TryCreate(colonyJobs,
                    motor.NavigationWorld?.Map, materialDrops, materialStorage,
                    Record, designation, siteApproach, start,
                    constructionTransaction, out constructionOrder, out string error))
            {
                failureReason = error;
                orderOutcome = "Construction rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            if (!motor.MoveTo(constructionOrder.InitialApproachCell))
            {
                constructionOrder.Cancel(motor.StatusMessage);
                failureReason = motor.StatusMessage;
                orderOutcome = "Construction rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            destination = constructionOrder.InitialApproachCell;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Construction active";
            OrderSource = AntOrderSource.AutonomousWork;
            return true;
        }

        public bool IssueMaterialPickupOrder(WorldMaterialDrop drop,
            Vector2Int approachCell)
        {
            CacheComponents();
            interactionOrder?.Cancel("Interaction replaced by material pickup.");
            jobOrder?.Cancel("Synthetic work replaced by material pickup.");
            excavationOrder?.Cancel("Excavation replaced by material pickup.");
            haulingOrder?.Cancel("Hauling replaced by material pickup.");
            constructionOrder?.Cancel("Construction replaced by material pickup.");
            pickupOrder?.Cancel("Pickup replaced by another pickup.");
            motor.CancelMovement("Material pickup is choosing an approach.");
            if (!MaterialPickupOrder.TryCreate(materialDrops,
                    motor.NavigationWorld?.Map, Record, drop, approachCell,
                    out pickupOrder, out string error))
            {
                failureReason = error;
                orderOutcome = "Pickup rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            if (!motor.MoveTo(approachCell))
            {
                pickupOrder.Cancel(motor.StatusMessage);
                failureReason = motor.StatusMessage;
                orderOutcome = "Pickup rejected";
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                return false;
            }
            destination = approachCell;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Pickup active";
            OrderSource = AntOrderSource.Player;
            return true;
        }

        public void RecordRejectedOrder(Vector2Int rejectedDestination, string reason)
        {
            failureReason = reason;
            orderOutcome = $"Order to {rejectedDestination} rejected";
        }

        public void CancelOrder(string reason = "Movement order cancelled.")
        {
            CacheComponents();
            interactionOrder?.Cancel(reason);
            jobOrder?.Cancel(reason);
            excavationOrder?.Cancel(reason);
            pickupOrder?.Cancel(reason);
            haulingOrder?.Cancel(reason);
            constructionOrder?.Cancel(reason);
            motor.CancelMovement(reason);
            hasActiveOrder = false;
            OrderSource = AntOrderSource.None;
            orderOutcome = "Cancelled; returned to Autonomous / Idle";
            failureReason = reason;
        }

        /// <summary>
        /// Converts terminal motor states into the temporary player-order
        /// lifecycle without changing or duplicating pathfinding state.
        /// </summary>
        public void RefreshOrderStatus(
            double elapsedSimulationHours = 0d,
            double simulationHours = 0d)
        {
            if (!hasActiveOrder || motor == null)
            {
                return;
            }

            if (Record == null || !Record.CanAcceptMovementOrders)
            {
                CancelOrder("Movement cancelled because the ant is not active.");
                return;
            }

            if (motor.HasArrived)
            {
                if (constructionOrder != null && constructionOrder.IsActive)
                {
                    if (constructionOrder.Phase != ConstructionOrderPhase.Working)
                    {
                        Vector2Int current =
                            WorldTileCoordinates.WorldPositionToCell(motor.Position);
                        bool moveAgain = constructionOrder.TryArrive(
                            current, out Vector2Int nextApproach);
                        if (moveAgain && constructionOrder.IsActive)
                        {
                            if (!motor.MoveTo(nextApproach))
                                constructionOrder.Cancel(motor.StatusMessage);
                            else
                            {
                                destination = nextApproach;
                                hasDestination = true;
                            }
                        }
                    }
                    constructionOrder.Advance(elapsedSimulationHours, simulationHours);
                    if (!constructionOrder.IsActive)
                    {
                        hasActiveOrder = false;
                        OrderSource = AntOrderSource.None;
                        orderOutcome = constructionOrder.Status;
                        failureReason = constructionOrder.FailureReason;
                    }
                }
                else if (haulingOrder != null && haulingOrder.IsActive)
                {
                    Vector2Int current =
                        WorldTileCoordinates.WorldPositionToCell(motor.Position);
                    MaterialHaulingPhase before = haulingOrder.Phase;
                    bool advanced = haulingOrder.TryArrive(
                        current, simulationHours, out Vector2Int nextApproach);
                    if (advanced && haulingOrder.IsActive &&
                        before == MaterialHaulingPhase.TravellingToSource)
                    {
                        if (!motor.MoveTo(nextApproach))
                            haulingOrder.Cancel(motor.StatusMessage);
                        else
                        { destination = nextApproach; hasDestination = true; }
                    }
                    if (!haulingOrder.IsActive)
                    {
                        hasActiveOrder = false; OrderSource = AntOrderSource.None;
                        orderOutcome = haulingOrder.Status;
                        failureReason = haulingOrder.FailureReason;
                    }
                }
                else if (pickupOrder != null && pickupOrder.IsActive)
                {
                    Vector2Int current =
                        WorldTileCoordinates.WorldPositionToCell(motor.Position);
                    pickupOrder.TryComplete(current);
                    hasActiveOrder = false;
                    OrderSource = AntOrderSource.None;
                    orderOutcome = pickupOrder.Status;
                    failureReason = pickupOrder.FailureReason;
                }
                else if (excavationOrder != null && excavationOrder.IsActive)
                {
                    if (excavationOrder.Phase == ExcavationOrderPhase.Travelling)
                    {
                        Vector2Int current =
                            WorldTileCoordinates.WorldPositionToCell(motor.Position);
                        excavationOrder.TryBegin(current);
                    }
                    excavationOrder.Advance(elapsedSimulationHours, simulationHours);
                    if (!excavationOrder.IsActive)
                    {
                        hasActiveOrder = false;
                        OrderSource = AntOrderSource.None;
                        orderOutcome = excavationOrder.Status;
                        failureReason = excavationOrder.FailureReason;
                    }
                }
                else if (jobOrder != null && jobOrder.IsActive)
                {
                    if (jobOrder.Phase == SyntheticJobOrderPhase.Travelling)
                    {
                        Vector2Int current =
                            WorldTileCoordinates.WorldPositionToCell(motor.Position);
                        jobOrder.TryBegin(current);
                    }
                    jobOrder.Advance(elapsedSimulationHours, simulationHours);
                    if (!jobOrder.IsActive)
                    {
                        hasActiveOrder = false;
                        OrderSource = AntOrderSource.None;
                        orderOutcome = jobOrder.Status;
                        failureReason = jobOrder.FailureReason;
                    }
                }
                else if (interactionOrder != null && interactionOrder.IsActive)
                {
                    if (interactionOrder.Phase == SurvivalInteractionPhase.Travelling)
                    {
                        Vector2Int current = WorldTileCoordinates.WorldPositionToCell(motor.Position);
                        interactionOrder.TryBeginInteraction(current);
                    }
                    interactionOrder.Advance(elapsedSimulationHours);
                    if (!interactionOrder.IsActive)
                    {
                        hasActiveOrder = false;
                        OrderSource = AntOrderSource.None;
                        orderOutcome = interactionOrder.Status;
                        failureReason = interactionOrder.FailureReason;
                    }
                }
                else
                {
                    hasActiveOrder = false;
                    OrderSource = AntOrderSource.None;
                    orderOutcome = "Completed; returned to Autonomous / Idle";
                    failureReason = string.Empty;
                }
            }
            else if (motor.HasFailed)
            {
                interactionOrder?.Cancel(motor.FailureReason);
                jobOrder?.Cancel(motor.FailureReason);
                excavationOrder?.Cancel(motor.FailureReason);
                pickupOrder?.Cancel(motor.FailureReason);
                haulingOrder?.Cancel(motor.FailureReason);
                constructionOrder?.Cancel(motor.FailureReason);
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                orderOutcome = "Failed; returned to Autonomous / Idle";
                failureReason = motor.FailureReason;
            }
            else if (motor.State == GridNavigationMotorState.Idle)
            {
                interactionOrder?.Cancel("Navigation stopped before interaction.");
                jobOrder?.Cancel("Navigation stopped before synthetic work.");
                excavationOrder?.Cancel("Navigation stopped before excavation.");
                pickupOrder?.Cancel("Navigation stopped before material pickup.");
                haulingOrder?.Cancel("Navigation stopped before material hauling.");
                constructionOrder?.Cancel("Navigation stopped before construction.");
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
                orderOutcome = "Cancelled; returned to Autonomous / Idle";
            }
        }

        private void CacheComponents()
        {
            motor ??= GetComponent<GridNavigationMotor>();
            entityBinding ??= GetComponent<AntEntityBinding>();
            selectionCollider ??= GetComponent<CapsuleCollider2D>();
        }

        private void EnsureSelectionIndicator()
        {
            if (selectionIndicator != null)
            {
                return;
            }

            Transform existing = transform.Find("Selection Indicator");
            GameObject indicatorObject = existing != null
                ? existing.gameObject
                : new GameObject("Selection Indicator");
            if (existing == null)
            {
                indicatorObject.transform.SetParent(transform, false);
            }

            selectionIndicator = indicatorObject.GetComponent<LineRenderer>();
            if (selectionIndicator == null)
            {
                selectionIndicator = indicatorObject.AddComponent<LineRenderer>();
            }
            selectionIndicator.useWorldSpace = false;
            selectionIndicator.loop = true;
            selectionIndicator.positionCount = IndicatorPointCount;
            selectionIndicator.startWidth = 0.055f;
            selectionIndicator.endWidth = 0.055f;
            selectionIndicator.startColor = new Color32(255, 232, 70, 255);
            selectionIndicator.endColor = new Color32(255, 232, 70, 255);
            selectionIndicator.sortingOrder = 20;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null && selectionIndicator.sharedMaterial == null)
            {
                indicatorMaterial = new Material(spriteShader)
                {
                    name = "Runtime Ant Selection Indicator"
                };
                selectionIndicator.sharedMaterial = indicatorMaterial;
            }

            Vector2 halfSize = selectionCollider != null
                ? selectionCollider.size * 0.75f
                : new Vector2(0.75f, 0.35f);
            for (int index = 0; index < IndicatorPointCount; index++)
            {
                float angle = index * Mathf.PI * 2f / IndicatorPointCount;
                selectionIndicator.SetPosition(
                    index,
                    new Vector3(
                        Mathf.Cos(angle) * halfSize.x,
                        Mathf.Sin(angle) * halfSize.y,
                        -0.05f));
            }
        }
    }
}
