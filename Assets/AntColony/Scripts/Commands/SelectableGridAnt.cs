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
        public ColonyJob ActiveColonyJob => excavationOrder?.Job ?? jobOrder?.Job;
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
            colonyJobs = jobs;
            jobOrder = null;
            if (interactionOrder == null || !interactionOrder.IsActive)
            {
                hasActiveOrder = false;
                OrderSource = AntOrderSource.None;
            }
        }

        public void BindExcavation(ExcavationDesignationRegistry designations,
            ExcavationWorldTransaction transaction)
        {
            excavationOrder?.Cancel("Excavation world was replaced.");
            excavationDesignations = designations;
            excavationTransaction = transaction;
            excavationOrder = null;
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
                if (excavationOrder != null && excavationOrder.IsActive)
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
