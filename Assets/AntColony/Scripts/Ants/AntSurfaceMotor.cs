using UnityEngine;
using UnityEngine.Serialization;

namespace AntColony.Ants
{
    /// <summary>
    /// Identifies the simple surfaces supported by the current movement slice.
    /// Ceilings and corner transitions are intentionally not represented yet.
    /// </summary>
    public enum AntSurfaceType
    {
        None,
        Floor,
        LeftWall,
        RightWall
    }

    /// <summary>
    /// Moves an ant along a detected floor or vertical wall.
    ///
    /// Controllers only provide a world-space movement intent. This component
    /// owns all Rigidbody2D movement and attachment decisions, so a future AI
    /// controller can reuse the same physical traversal rules as the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AntSurfaceMotor : MonoBehaviour
    {
        private const int MaximumProbeHits = 8;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float movementSpeed = 4f;
        [SerializeField, Min(0f)] private float rotationSpeed = 360f;

        [Header("Surface Detection")]
        [SerializeField] private LayerMask climbableSurfaces;
        [FormerlySerializedAs("floorProbeDistance")]
        [SerializeField, Min(0.01f)] private float surfaceProbeDistance = 0.18f;
        [SerializeField, Range(0f, 1f)] private float surfaceNormalTolerance = 0.7f;
        [SerializeField, Range(0f, 1f)] private float wallAttachInputThreshold = 0.5f;

        [Header("Surface Attachment")]
        [SerializeField, Min(0f)] private float surfaceSeparation = 0.02f;
        [SerializeField, Min(0f)] private float attachmentCorrectionStrength = 12f;
        [SerializeField, Min(0f)] private float maximumAttachmentCorrectionSpeed = 2f;

        [Header("Debugging")]
        [SerializeField] private bool showDebugGizmos;

        // Collider2D.Cast fills this reusable array. Reusing it avoids creating
        // garbage every physics step, which becomes important with many ants.
        private readonly RaycastHit2D[] probeHits = new RaycastHit2D[MaximumProbeHits];

        private Rigidbody2D antBody;
        private Collider2D antCollider;
        private ContactFilter2D probeFilter;
        private Vector2 movementIntent;
        private Vector2 surfacePoint;
        private Vector2 surfaceNormal = Vector2.up;
        private Vector2 appliedMovementDirection;
        private float detachedGravityScale;

        /// <summary>Gets whether a supported climbable surface is attached.</summary>
        public bool IsAttached => SurfaceType != AntSurfaceType.None;

        /// <summary>Gets whether the current attached surface is a floor.</summary>
        public bool IsGrounded => SurfaceType == AntSurfaceType.Floor;

        /// <summary>Gets the current floor or wall attachment type.</summary>
        public AntSurfaceType SurfaceType { get; private set; }

        /// <summary>Gets the latest detected contact point for debugging.</summary>
        public Vector2 SurfacePoint => surfacePoint;

        /// <summary>Gets the outward normal of the attached surface.</summary>
        public Vector2 SurfaceNormal => surfaceNormal;

        /// <summary>
        /// Gets the latest normalised world-direction request supplied by the
        /// active controller.
        /// </summary>
        public Vector2 MovementIntent => movementIntent;

        private void Awake()
        {
            antBody = GetComponent<Rigidbody2D>();
            antCollider = GetComponent<Collider2D>();
            detachedGravityScale = antBody.gravityScale;

            // Probe every collision layer first. The nearest obstruction is then
            // checked against climbableSurfaces, preventing attachment through a
            // non-climbable collider that happens to be closer to the ant.
            probeFilter = ContactFilter2D.noFilter;
            probeFilter.useTriggers = false;
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
            movementIntent = Vector2.ClampMagnitude(intent, 1f);
        }

        private void FixedUpdate()
        {
            SurfaceProbe floor = ProbeSurface(Vector2.down, Vector2.up, AntSurfaceType.Floor);
            SurfaceProbe leftWall = ProbeSurface(Vector2.left, Vector2.right, AntSurfaceType.LeftWall);
            SurfaceProbe rightWall = ProbeSurface(Vector2.right, Vector2.left, AntSurfaceType.RightWall);

            SurfaceProbe selectedSurface = SelectSurface(floor, leftWall, rightWall);
            ApplyAttachment(selectedSurface);

            if (IsAttached)
            {
                MoveAlongAttachedSurface(selectedSurface.Distance);
                AlignToSurface();
            }
            else
            {
                MoveWhileDetached();
            }
        }

        private SurfaceProbe ProbeSurface(
            Vector2 direction,
            Vector2 expectedNormal,
            AntSurfaceType surfaceType)
        {
            int hitCount = antCollider.Cast(
                direction,
                probeFilter,
                probeHits,
                surfaceProbeDistance);

            float nearestDistance = float.PositiveInfinity;
            RaycastHit2D nearestHit = default;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = probeHits[index];
                if (hit.collider != null && hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestHit = hit;
                }
            }

            if (nearestHit.collider == null || !IsClimbableLayer(nearestHit.collider.gameObject.layer))
            {
                return SurfaceProbe.Invalid;
            }

            // A floor probe should not accept a steep wall, and a wall probe
            // should not accept the floor. This keeps attachment state explicit.
            if (Vector2.Dot(nearestHit.normal, expectedNormal) < surfaceNormalTolerance)
            {
                return SurfaceProbe.Invalid;
            }

            return new SurfaceProbe(
                surfaceType,
                nearestHit.point,
                nearestHit.normal.normalized,
                nearestHit.distance);
        }

        private SurfaceProbe SelectSurface(
            SurfaceProbe floor,
            SurfaceProbe leftWall,
            SurfaceProbe rightWall)
        {
            // Once attached to a wall, remain there while its probe is valid.
            // This means releasing input stops the ant instead of making it fall.
            if (SurfaceType == AntSurfaceType.LeftWall && leftWall.IsValid)
            {
                return leftWall;
            }

            if (SurfaceType == AntSurfaceType.RightWall && rightWall.IsValid)
            {
                return rightWall;
            }

            // Upward intent is the deliberate action that begins a wall climb.
            // Merely walking into a wall remains collision-safe floor movement.
            if (movementIntent.y > wallAttachInputThreshold)
            {
                if (movementIntent.x < 0f && leftWall.IsValid)
                {
                    return leftWall;
                }

                if (movementIntent.x > 0f && rightWall.IsValid)
                {
                    return rightWall;
                }

                if (leftWall.IsValid && rightWall.IsValid)
                {
                    return leftWall.Distance <= rightWall.Distance ? leftWall : rightWall;
                }

                if (leftWall.IsValid)
                {
                    return leftWall;
                }

                if (rightWall.IsValid)
                {
                    return rightWall;
                }
            }

            return floor.IsValid ? floor : SurfaceProbe.Invalid;
        }

        private void ApplyAttachment(SurfaceProbe selectedSurface)
        {
            if (!selectedSurface.IsValid)
            {
                SurfaceType = AntSurfaceType.None;
                surfacePoint = antBody.position;
                surfaceNormal = Vector2.up;
                antBody.gravityScale = detachedGravityScale;
                return;
            }

            SurfaceType = selectedSurface.Type;
            surfacePoint = selectedSurface.Point;
            surfaceNormal = selectedSurface.Normal;
            antBody.gravityScale = 0f;
            antBody.angularVelocity = 0f;
        }

        private void MoveAlongAttachedSurface(float distanceFromSurface)
        {
            appliedMovementDirection = SurfaceType == AntSurfaceType.Floor
                ? new Vector2(movementIntent.x, 0f)
                : new Vector2(0f, movementIntent.y);

            Vector2 velocity = appliedMovementDirection * movementSpeed;

            // Correct only an actual gap. At zero cast distance the physics
            // solver already has contact and pushing farther inward causes jitter.
            float gap = Mathf.Max(0f, distanceFromSurface - surfaceSeparation);
            float correctionSpeed = Mathf.Min(
                gap * attachmentCorrectionStrength,
                maximumAttachmentCorrectionSpeed);

            antBody.linearVelocity = velocity - surfaceNormal * correctionSpeed;
        }

        private void MoveWhileDetached()
        {
            appliedMovementDirection = Vector2.zero;
            antBody.angularVelocity = 0f;

            // Air steering is outside this slice. Preserve gravity-driven vertical
            // speed, but do not carry the last surface movement into open space.
            antBody.linearVelocity = new Vector2(0f, antBody.linearVelocity.y);
        }

        private void AlignToSurface()
        {
            float targetRotation = Vector2.SignedAngle(Vector2.up, surfaceNormal);
            float nextRotation = Mathf.MoveTowardsAngle(
                antBody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);

            if (Mathf.Abs(Mathf.DeltaAngle(antBody.rotation, nextRotation)) > 0.1f)
            {
                antBody.MoveRotation(nextRotation);
            }
        }

        private bool IsClimbableLayer(int layer)
        {
            return (climbableSurfaces.value & (1 << layer)) != 0;
        }

        private void OnDisable()
        {
            movementIntent = Vector2.zero;
            SurfaceType = AntSurfaceType.None;

            if (antBody != null)
            {
                antBody.gravityScale = detachedGravityScale;
                antBody.angularVelocity = 0f;
                antBody.linearVelocity = new Vector2(0f, antBody.linearVelocity.y);
            }
        }

        private void OnValidate()
        {
            movementSpeed = Mathf.Max(0f, movementSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            surfaceProbeDistance = Mathf.Max(0.01f, surfaceProbeDistance);
            surfaceSeparation = Mathf.Max(0f, surfaceSeparation);
            attachmentCorrectionStrength = Mathf.Max(0f, attachmentCorrectionStrength);
            maximumAttachmentCorrectionSpeed = Mathf.Max(0f, maximumAttachmentCorrectionSpeed);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos)
            {
                return;
            }

            Collider2D currentCollider = GetComponent<Collider2D>();
            if (currentCollider == null)
            {
                return;
            }

            Vector3 centre = currentCollider.bounds.center;
            Gizmos.color = IsAttached ? Color.green : Color.yellow;
            Gizmos.DrawLine(centre, centre + Vector3.down * surfaceProbeDistance);
            Gizmos.DrawLine(centre, centre + Vector3.left * surfaceProbeDistance);
            Gizmos.DrawLine(centre, centre + Vector3.right * surfaceProbeDistance);

            if (IsAttached)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(surfacePoint, 0.05f);
                Gizmos.DrawLine(surfacePoint, surfacePoint + surfaceNormal * 0.5f);

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(centre, centre + (Vector3)appliedMovementDirection);
            }
        }
#endif

        private readonly struct SurfaceProbe
        {
            public static readonly SurfaceProbe Invalid = default;

            public SurfaceProbe(
                AntSurfaceType type,
                Vector2 point,
                Vector2 normal,
                float distance)
            {
                Type = type;
                Point = point;
                Normal = normal;
                Distance = distance;
                IsValid = true;
            }

            public AntSurfaceType Type { get; }
            public Vector2 Point { get; }
            public Vector2 Normal { get; }
            public float Distance { get; }
            public bool IsValid { get; }
        }
    }
}
