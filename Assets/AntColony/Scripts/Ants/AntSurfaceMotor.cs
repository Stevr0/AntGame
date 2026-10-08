using UnityEngine;

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
    public sealed class AntSurfaceMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float movementSpeed = 4f;

        [Header("Floor Detection")]
        [SerializeField] private LayerMask climbableSurfaces;
        [SerializeField, Min(0.01f)] private float floorProbeDistance = 0.12f;

        private Rigidbody2D antBody;
        private Collider2D antCollider;
        private Vector2 _movementIntent;

        /// <summary>
        /// Gets whether the ant's floor probe currently touches a climbable surface.
        /// This is public so later animation and tests can read the result without
        /// being allowed to change it.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// Gets the latest normalised world-direction request supplied by the
        /// active controller. Vertical intent is retained for later traversal
        /// work, but this floor-only motor currently acts on the X value only.
        /// </summary>
        public Vector2 MovementIntent => _movementIntent;

        private void Awake()
        {
            antBody = GetComponent<Rigidbody2D>();
            antCollider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Supplies the world direction requested by the currently active
        /// controller. Player input uses this now; a future autonomous controller
        /// can use the same method after player control has been disabled.
        /// </summary>
        /// <param name="intent">
        /// Desired world direction. Values with a magnitude above one are
        /// normalised so diagonal requests cannot increase movement speed.
        /// </param>
        public void SetMovementIntent(Vector2 intent)
        {
            _movementIntent = Vector2.ClampMagnitude(intent, 1f);
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();

            // The ant may only steer while it is standing on the test floor.
            // Vertical intent is deliberately stored but ignored until wall and
            // ceiling traversal are implemented in later P1 slices.
            float horizontalSpeed = IsGrounded ? _movementIntent.x * movementSpeed : 0f;
            antBody.linearVelocity = new Vector2(horizontalSpeed, antBody.linearVelocity.y);
        }

        private void OnDisable()
        {
            _movementIntent = Vector2.zero;

            // Prevent the Rigidbody2D from retaining the last commanded horizontal
            // velocity if movement execution itself is disabled.
            if (antBody != null)
            {
                antBody.linearVelocity = new Vector2(0f, antBody.linearVelocity.y);
            }
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
