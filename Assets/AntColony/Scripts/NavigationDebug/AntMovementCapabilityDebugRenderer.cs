using AntColony.World;
using UnityEngine;

namespace AntColony.NavigationDebug
{
    /// <summary>
    /// Developer-only inspector for local movement capability. It never moves or
    /// spawns an ant and does not create physics components.
    /// </summary>
    [ExecuteAlways]
    public sealed class AntMovementCapabilityDebugRenderer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour worldDataProvider;
        [SerializeField] private Vector2 testWorldPosition = new Vector2(20f, 8f);
        [SerializeField] private float orientationDegrees;
        [SerializeField] private Vector2 bodySize = new Vector2(0.96f, 0.3f);
        [SerializeField] private Vector2 bodyOffset;
        [SerializeField] private AntCapsuleAxis capsuleAxis =
            AntCapsuleAxis.Horizontal;
        [SerializeField, Min(0f)] private float clearanceMargin;

        private AntMovementCapabilityResult lastResult;
        private bool hasResult;

        public Vector2 TestWorldPosition => testWorldPosition;
        public Vector2Int TestWorldCell =>
            WorldTileCoordinates.WorldPositionToCell(testWorldPosition);
        public bool HasResult => hasResult;
        public AntMovementCapabilityResult LastResult => lastResult;
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
            Gizmos.color = GetStateColour();
            Gizmos.DrawWireSphere(testWorldPosition, 0.18f);
            Gizmos.DrawLine(
                testWorldPosition + Vector2.left * 0.3f,
                testWorldPosition + Vector2.right * 0.3f);
            Gizmos.DrawLine(
                testWorldPosition + Vector2.down * 0.3f,
                testWorldPosition + Vector2.up * 0.3f);
        }

        /// <summary>Runs one read-only query against current authoritative data.</summary>
        public bool RunQuery()
        {
            if (!(worldDataProvider is IWorldDataProvider provider) ||
                !provider.IsWorldDataAvailable)
            {
                hasResult = false;
                lastResult = default;
                return false;
            }

            var evaluator = new AntMovementCapabilityEvaluator(provider.WorldData);
            lastResult = evaluator.Evaluate(
                testWorldPosition,
                orientationDegrees,
                Profile);
            hasResult = true;
            return true;
        }

        /// <summary>
        /// Toggles only the selected Background Material in the development host.
        /// Presentation refresh remains an explicit Editor-tool action.
        /// </summary>
        public bool TogglePreparedBackground()
        {
            if (!(worldDataProvider is NavigationTestWorldHost host) ||
                !host.TogglePreparedBackground(TestWorldCell))
            {
                return false;
            }

            return RunQuery();
        }

        /// <summary>Used by deterministic scene authoring and focused tests.</summary>
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

        public void SetTestWorldPosition(Vector2 worldPosition)
        {
            testWorldPosition = worldPosition;
            RunQuery();
        }

        private Color GetStateColour()
        {
            if (!hasResult || !lastResult.Succeeded)
            {
                return Color.grey;
            }

            switch (lastResult.SuggestedState)
            {
                case AntMovementEligibilityState.Grounded:
                    return Color.green;
                case AntMovementEligibilityState.Gripping:
                    return new Color(0.2f, 0.75f, 1f);
                case AntMovementEligibilityState.Falling:
                    return new Color(1f, 0.65f, 0f);
                default:
                    return Color.grey;
            }
        }
    }
}
