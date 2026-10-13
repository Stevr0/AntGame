using AntColony.Ants;
using AntColony.Navigation;
using UnityEngine;

namespace AntColony.Commands
{
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

        public string DisplayName => Record?.DisplayName ?? "Unbound Ant";
        public AntRecord Record => entityBinding?.Record;
        public GridNavigationMotor Motor => motor;
        public bool IsSelected { get; private set; }
        public bool HasActiveOrder => hasActiveOrder;
        public bool HasDestination => hasDestination;
        public Vector2Int Destination => destination;
        public string FailureReason => failureReason;
        public string OrderOutcome => orderOutcome;
        public LineRenderer SelectionIndicator => selectionIndicator;
        public string ControlState => hasActiveOrder
            ? "Ordered"
            : "Autonomous / Idle";

        public string OrderStatus
        {
            get
            {
                if (!hasActiveOrder)
                {
                    return orderOutcome;
                }

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

        public bool IssueMoveOrder(Vector2Int requestedDestination)
        {
            CacheComponents();
            if (Record == null)
            {
                failureReason = "Movement order rejected: ant identity is not bound.";
                orderOutcome = "Order rejected";
                return false;
            }

            if (!Record.CanAcceptMovementOrders)
            {
                failureReason =
                    $"Movement order rejected: {DisplayName} is {Record.LifecycleStatus}.";
                orderOutcome = "Order rejected";
                return false;
            }

            if (!motor.MoveTo(requestedDestination))
            {
                failureReason = motor.StatusMessage;
                orderOutcome = "Order rejected";
                return false;
            }

            destination = requestedDestination;
            hasDestination = true;
            hasActiveOrder = true;
            failureReason = string.Empty;
            orderOutcome = "Order active";
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
            motor.CancelMovement(reason);
            hasActiveOrder = false;
            orderOutcome = "Cancelled; returned to Autonomous / Idle";
            failureReason = reason;
        }

        /// <summary>
        /// Converts terminal motor states into the temporary player-order
        /// lifecycle without changing or duplicating pathfinding state.
        /// </summary>
        public void RefreshOrderStatus()
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
                hasActiveOrder = false;
                orderOutcome = "Completed; returned to Autonomous / Idle";
                failureReason = string.Empty;
            }
            else if (motor.HasFailed)
            {
                hasActiveOrder = false;
                orderOutcome = "Failed; returned to Autonomous / Idle";
                failureReason = motor.FailureReason;
            }
            else if (motor.State == GridNavigationMotorState.Idle)
            {
                hasActiveOrder = false;
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
