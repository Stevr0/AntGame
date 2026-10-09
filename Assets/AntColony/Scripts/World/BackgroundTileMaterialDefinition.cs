using UnityEngine;

namespace AntColony.World
{
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
    }
}
