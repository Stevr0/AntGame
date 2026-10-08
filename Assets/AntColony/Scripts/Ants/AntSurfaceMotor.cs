using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Ants
{
    /// <summary>
    /// Moves the directly controlled ant along a flat floor.
    ///
    /// This first milestone slice deliberately supports horizontal floors only.
    /// Wall, ceiling, and corner traversal will be added after floor movement is
    /// verified in the Unity Editor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class AntSurfaceMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float movementSpeed = 4f;

        [Header("Floor Detection")]
        [SerializeField] private LayerMask climbableSurfaces;
        [SerializeField, Min(0.01f)] private float floorProbeDistance = 0.12f;

        [Header("Input Actions")]
        [SerializeField] private string movementActionName = "Move";

        private Rigidbody2D antBody;
        private Collider2D antCollider;
        private PlayerInput playerInput;
        private InputAction movementAction;

        /// <summary>
        /// Gets whether the ant's floor probe currently touches a climbable surface.
        /// This is public so later animation and tests can read the result without
        /// being allowed to change it.
        /// </summary>
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            antBody = GetComponent<Rigidbody2D>();
            antCollider = GetComponent<Collider2D>();
            playerInput = GetComponent<PlayerInput>();

            // PlayerInput owns the shared Input Action asset. Looking up the action
            // by name keeps this component small and avoids a second input system.
            movementAction = playerInput.actions?.FindAction(movementActionName);

            if (movementAction == null)
            {
                Debug.LogError(
                    $"{nameof(AntSurfaceMotor)} on '{name}' could not find the " +
                    $"'{movementActionName}' input action.",
                    this);
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();

            float horizontalInput = movementAction?.ReadValue<Vector2>().x ?? 0f;

            // The ant may only steer while it is standing on the test floor.
            // Gravity remains responsible for landing and vertical movement.
            float horizontalSpeed = IsGrounded ? horizontalInput * movementSpeed : 0f;
            antBody.linearVelocity = new Vector2(horizontalSpeed, antBody.linearVelocity.y);
        }

        private void UpdateGroundedState()
        {
            Bounds colliderBounds = antCollider.bounds;
            Vector2 probeOrigin = new Vector2(colliderBounds.center.x, colliderBounds.min.y + 0.01f);

            IsGrounded = Physics2D.Raycast(
                probeOrigin,
                Vector2.down,
                floorProbeDistance,
                climbableSurfaces);
        }

        private void OnValidate()
        {
            movementSpeed = Mathf.Max(0f, movementSpeed);
            floorProbeDistance = Mathf.Max(0.01f, floorProbeDistance);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Collider2D currentCollider = GetComponent<Collider2D>();
            if (currentCollider == null)
            {
                return;
            }

            Bounds colliderBounds = currentCollider.bounds;
            Vector3 probeOrigin = new Vector3(colliderBounds.center.x, colliderBounds.min.y + 0.01f);

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawLine(probeOrigin, probeOrigin + Vector3.down * floorProbeDistance);
        }
#endif
    }
}
