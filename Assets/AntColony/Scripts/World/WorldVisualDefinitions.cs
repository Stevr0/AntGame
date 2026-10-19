using UnityEngine;

namespace AntColony.World
{
    public enum WorldVisualCategory
    {
        NaturalBackground,
        ResourceNode,
        ConstructionSite,
        Placement,
        LooseResource,
        ManufacturedObject
    }

    public enum WorldVisualPerspective
    {
        ColonySideOn,
        SurfaceTopDown
    }

    public enum PlacementVisualState
    {
        Construction,
        Completed,
        Damaged,
        Destroyed
    }

    /// <summary>
    /// Shared authoring identity and rendering metadata. Category-specific
    /// subclasses expose only sprite states that make sense for that object.
    /// These assets never contain mutable gameplay state.
    /// </summary>
    public abstract class WorldVisualDefinition : ScriptableObject
    {
        [SerializeField] private string stableVisualId = string.Empty;
        [SerializeField] private string gameplayDefinitionId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder;
        [SerializeField] private Vector2 visualOffset;
        [SerializeField] private Vector2 visualScale = Vector2.one;
        [SerializeField] private Color fallbackColour = Color.white;

        public string StableVisualId => stableVisualId;
        public string GameplayDefinitionId => gameplayDefinitionId;
        public string DisplayName => displayName;
        public abstract WorldVisualCategory Category { get; }
        public string SortingLayerName => sortingLayerName;
        public int SortingOrder => sortingOrder;
        public Vector2 VisualOffset => visualOffset;
        public Vector2 VisualScale => visualScale;
        public Color FallbackColour => fallbackColour;
    }

    [CreateAssetMenu(fileName = "BackgroundVisual",
        menuName = "Ant Colony/Visuals/Natural Background")]
    public sealed class BackgroundVisualDefinition : WorldVisualDefinition
    {
        [SerializeField] private Sprite colonySprite;
        [SerializeField] private Sprite surfaceSprite;

        public override WorldVisualCategory Category =>
            WorldVisualCategory.NaturalBackground;
        public Sprite ColonySprite => colonySprite;
        public Sprite SurfaceSprite => surfaceSprite;
        public Sprite GetSprite(WorldVisualPerspective perspective) =>
            perspective == WorldVisualPerspective.ColonySideOn
                ? colonySprite : surfaceSprite;
    }

    [CreateAssetMenu(fileName = "ResourceNodeVisual",
        menuName = "Ant Colony/Visuals/Resource Node")]
    public sealed class ResourceNodeVisualDefinition : WorldVisualDefinition
    {
        [SerializeField] private Sprite colonyNormalSprite;
        [SerializeField] private Sprite surfaceNormalSprite;
        [SerializeField] private Sprite colonyWorkedSprite;
        [SerializeField] private Sprite surfaceWorkedSprite;
        [SerializeField] private Sprite colonyDepletedSprite;
        [SerializeField] private Sprite surfaceDepletedSprite;

        public override WorldVisualCategory Category => WorldVisualCategory.ResourceNode;
        public Sprite GetNormalSprite(WorldVisualPerspective perspective) =>
            perspective == WorldVisualPerspective.ColonySideOn
                ? colonyNormalSprite : surfaceNormalSprite;
        public Sprite GetWorkedSprite(WorldVisualPerspective perspective) =>
            perspective == WorldVisualPerspective.ColonySideOn
                ? colonyWorkedSprite : surfaceWorkedSprite;
        public Sprite GetDepletedSprite(WorldVisualPerspective perspective) =>
            perspective == WorldVisualPerspective.ColonySideOn
                ? colonyDepletedSprite : surfaceDepletedSprite;
    }

    [CreateAssetMenu(fileName = "PlacementVisual",
        menuName = "Ant Colony/Visuals/Placement")]
    public sealed class PlacementVisualDefinition : WorldVisualDefinition
    {
        [Header("Colony View")]
        [SerializeField] private Sprite colonyConstructionSprite;
        [SerializeField] private Sprite colonyCompletedSprite;
        [SerializeField] private Sprite colonyDamagedSprite;
        [SerializeField] private Sprite colonyDestroyedSprite;
        [Header("World View")]
        [SerializeField] private Sprite surfaceConstructionSprite;
        [SerializeField] private Sprite surfaceCompletedSprite;
        [SerializeField] private Sprite surfaceDamagedSprite;
        [SerializeField] private Sprite surfaceDestroyedSprite;
        [SerializeField, Range(0f, 1f)] private float damagedBelowCondition = 0.5f;

        public override WorldVisualCategory Category => WorldVisualCategory.Placement;
        public float DamagedBelowCondition => Mathf.Clamp01(damagedBelowCondition);

        public Sprite GetSprite(WorldVisualPerspective perspective,
            PlacementVisualState state)
        {
            bool colony = perspective == WorldVisualPerspective.ColonySideOn;
            return state switch
            {
                PlacementVisualState.Construction => colony
                    ? colonyConstructionSprite : surfaceConstructionSprite,
                PlacementVisualState.Damaged => colony
                    ? colonyDamagedSprite : surfaceDamagedSprite,
                PlacementVisualState.Destroyed => colony
                    ? colonyDestroyedSprite : surfaceDestroyedSprite,
                _ => colony ? colonyCompletedSprite : surfaceCompletedSprite
            };
        }

        /// <summary>
        /// Optional state sprites fall back to Completed. Missing artwork never
        /// blocks gameplay and never changes the resolved gameplay state.
        /// </summary>
        public Sprite GetSpriteWithFallback(WorldVisualPerspective perspective,
            PlacementVisualState state)
        {
            Sprite requested = GetSprite(perspective, state);
            return requested != null
                ? requested
                : GetSprite(perspective, PlacementVisualState.Completed);
        }
    }

    [CreateAssetMenu(fileName = "LooseResourceVisual",
        menuName = "Ant Colony/Visuals/Loose Resource")]
    public sealed class LooseResourceVisualDefinition : WorldVisualDefinition
    {
        [SerializeField] private Sprite colonySprite;
        [SerializeField] private Sprite surfaceSprite;

        public override WorldVisualCategory Category => WorldVisualCategory.LooseResource;
        public Sprite GetSprite(WorldVisualPerspective perspective) =>
            perspective == WorldVisualPerspective.ColonySideOn
                ? colonySprite : surfaceSprite;
    }

    /// <summary>Pure presentation selection. It cannot mutate Placement state.</summary>
    public static class PlacementVisualStateResolver
    {
        public static PlacementVisualState Resolve(bool constructionInProgress,
            PlacementLifecycle lifecycle, int? currentHitPoints,
            int? maximumHitPoints, float damagedBelowCondition)
        {
            if (constructionInProgress) return PlacementVisualState.Construction;
            if (lifecycle == PlacementLifecycle.Retired ||
                currentHitPoints.HasValue && currentHitPoints.Value <= 0)
                return PlacementVisualState.Destroyed;
            if (currentHitPoints.HasValue && maximumHitPoints.HasValue &&
                maximumHitPoints.Value > 0 &&
                (float)currentHitPoints.Value / maximumHitPoints.Value <
                    Mathf.Clamp01(damagedBelowCondition))
                return PlacementVisualState.Damaged;
            // Unknown HP is deliberately treated as normal, never damaged.
            return PlacementVisualState.Completed;
        }
    }
}
