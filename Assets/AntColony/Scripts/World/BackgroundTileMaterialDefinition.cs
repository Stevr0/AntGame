using UnityEngine;

namespace AntColony.World
{
    public enum BackgroundFunction
    {
        None,
        FoodStorage,
        WaterStorage,
        Sleeping,
        MaterialStorage
    }

    /// <summary>
    /// Defines a permanent Background Tile material. Background Tiles are data
    /// entries, not GameObjects, and do not inherently block or support movement.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BackgroundMaterial",
        menuName = "Ant Colony/World/Background Tile Material")]
    public sealed class BackgroundTileMaterialDefinition : WorldTileMaterialDefinition
    {
        [SerializeField] private bool allowsHorizontalMovement = true;
        [SerializeField] private bool allowsUpwardMovement;
        [SerializeField] private bool allowsDownwardMovement;
        [SerializeField] private bool providesClimbingGrip;
        [SerializeField] private bool gravityAppliesWhenUnsupported = true;
        [SerializeField, Min(0.01f)] private float movementSpeedMultiplier = 1f;
        [SerializeField] private BackgroundFunction function;

        /// <summary>
        /// Horizontal permission still needs either physical ground or active
        /// Background grip. The flag alone never grants free movement in mid-air.
        /// </summary>
        public bool AllowsHorizontalMovement => allowsHorizontalMovement;

        public bool AllowsUpwardMovement => allowsUpwardMovement;
        public bool AllowsDownwardMovement => allowsDownwardMovement;
        public bool ProvidesClimbingGrip => providesClimbingGrip;
        public bool GravityAppliesWhenUnsupported =>
            gravityAppliesWhenUnsupported;

        /// <summary>
        /// Multiplies an ant motor's base speed while its centre is over this
        /// Background. Natural terrain uses 1; prepared terrain may be faster.
        /// </summary>
        public float MovementSpeedMultiplier =>
            Mathf.Max(0.01f, movementSpeedMultiplier);
        public BackgroundFunction Function => function;

        /// <summary>
        /// Milestone 3.2B functions deliberately have exactly one physical slot.
        /// Ordinary backgrounds have no functional capacity.
        /// </summary>
        public int FunctionalCapacity => function == BackgroundFunction.None ? 0 : 1;
    }
}
