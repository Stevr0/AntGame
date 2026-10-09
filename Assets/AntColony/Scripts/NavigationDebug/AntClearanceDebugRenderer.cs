using AntColony.World;
using UnityEngine;

namespace AntColony.NavigationDebug
{
    /// <summary>
    /// Scene-only inspector for static ant clearance. It draws the exact capsule
    /// passed to AntClearanceValidator and never creates gameplay colliders.
    /// </summary>
    [ExecuteAlways]
    public sealed class AntClearanceDebugRenderer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour worldDataProvider;
        [SerializeField] private Vector2 testWorldPosition = new Vector2(2f, 1f);
        [SerializeField] private float orientationDegrees;
        [SerializeField] private Vector2 bodySize = new Vector2(0.96f, 0.3f);
        [SerializeField] private Vector2 bodyOffset;
        [SerializeField] private AntCapsuleAxis capsuleAxis =
            AntCapsuleAxis.Horizontal;
        [SerializeField, Min(0f)] private float clearanceMargin;
        [SerializeField] private bool showTestCapsule = true;

        private AntClearanceResult lastResult;
        private bool hasResult;

        public MonoBehaviour WorldDataProvider => worldDataProvider;
        public Vector2 TestWorldPosition => testWorldPosition;
        public float OrientationDegrees => orientationDegrees;
        public Vector2 BodySize => bodySize;
        public Vector2 BodyOffset => bodyOffset;
        public AntCapsuleAxis CapsuleAxis => capsuleAxis;
        public float ClearanceMargin => clearanceMargin;
        public bool ShowTestCapsule => showTestCapsule;
        public bool HasResult => hasResult;
        public AntClearanceResult LastResult => lastResult;

        public AntCollisionProfile Profile => new AntCollisionProfile(
            bodySize,
            bodyOffset,
            capsuleAxis,
            clearanceMargin);

        private void OnEnable()
        {
            RunQuery();
        }

        private void OnValidate()
        {
            clearanceMargin = Mathf.Max(0f, clearanceMargin);
            if (isActiveAndEnabled)
            {
                RunQuery();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showTestCapsule ||
                !AntClearanceValidator.TryGetCapsuleGeometry(
                    testWorldPosition,
                    orientationDegrees,
                    Profile,
                    out _,
                    out Vector2 segmentStart,
                    out Vector2 segmentEnd,
                    out float radius))
            {
                return;
            }

            Gizmos.color = GetResultColour();
            Vector2 axis = segmentEnd - segmentStart;
            if (axis.sqrMagnitude <= AntClearanceValidator.GeometryTolerance)
            {
                DrawArc(segmentStart, Vector2.right, Vector2.up, radius, 0f, 360f, 32);
                return;
            }

            Vector2 forward = axis.normalized;
            Vector2 perpendicular = new Vector2(-forward.y, forward.x);
            Gizmos.DrawLine(
                segmentStart + perpendicular * radius,
                segmentEnd + perpendicular * radius);
            Gizmos.DrawLine(
                segmentStart - perpendicular * radius,
                segmentEnd - perpendicular * radius);
            DrawArc(segmentEnd, perpendicular, forward, radius, 0f, 180f, 16);
            DrawArc(segmentStart, -perpendicular, -forward, radius, 0f, 180f, 16);
        }

        /// <summary>Runs one read-only query against the provider's current world.</summary>
        public bool RunQuery()
        {
            if (!(worldDataProvider is IWorldDataProvider provider) ||
                !provider.IsWorldDataAvailable)
            {
                hasResult = false;
                lastResult = default;
                return false;
            }

            var validator = new AntClearanceValidator(provider.WorldData);
            lastResult = validator.Validate(
                testWorldPosition,
                orientationDegrees,
                Profile);
            hasResult = true;
            return true;
        }

        /// <summary>Used by scene authoring and focused tests.</summary>
        public void Configure(
            MonoBehaviour provider,
            Vector2 worldPosition,
            AntCollisionProfile profile,
            float orientation)
        {
            worldDataProvider = provider;
            testWorldPosition = worldPosition;
            bodySize = profile.Size;
            bodyOffset = profile.Offset;
            capsuleAxis = profile.CapsuleAxis;
            clearanceMargin = profile.ClearanceMargin;
            orientationDegrees = orientation;
            RunQuery();
        }

        /// <summary>Updates the Scene-view query position without moving an ant.</summary>
        public void SetTestWorldPosition(Vector2 worldPosition)
        {
            testWorldPosition = worldPosition;
            RunQuery();
        }

        private Color GetResultColour()
        {
            if (!hasResult)
            {
                return Color.grey;
            }

            switch (lastResult.Status)
            {
                case AntClearanceStatus.Clear:
                    return Color.green;
                case AntClearanceStatus.BlockedByTerrain:
                    return Color.red;
                case AntClearanceStatus.UnsupportedLocation:
                    return new Color(0.25f, 0.55f, 1f);
                case AntClearanceStatus.UnavailableWorldData:
                    return new Color(1f, 0.65f, 0f);
                default:
                    return Color.grey;
            }
        }

        private static void DrawArc(
            Vector2 centre,
            Vector2 startDirection,
            Vector2 bulgeDirection,
            float radius,
            float startDegrees,
            float endDegrees,
            int segmentCount)
        {
            float previousAngle = startDegrees * Mathf.Deg2Rad;
            Vector2 previous = centre + radius *
                (startDirection * Mathf.Cos(previousAngle) +
                    bulgeDirection * Mathf.Sin(previousAngle));
            for (int index = 1; index <= segmentCount; index++)
            {
                float progress = index / (float)segmentCount;
                float angle = Mathf.Lerp(startDegrees, endDegrees, progress) *
                    Mathf.Deg2Rad;
                Vector2 current = centre + radius *
                    (startDirection * Mathf.Cos(angle) +
                        bulgeDirection * Mathf.Sin(angle));
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
