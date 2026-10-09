using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Defines the properties needed to store a foreground Solid Tile. Digging,
    /// durability, and resource production intentionally remain future work.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SolidMaterial",
        menuName = "Ant Colony/World/Solid Tile Material")]
    public sealed class SolidTileMaterialDefinition : WorldTileMaterialDefinition
    {
        [SerializeField] private bool blocksMovement = true;
        [SerializeField] private bool supportsNavigation = true;
        [SerializeField] private bool isExcavatable = true;

        public bool BlocksMovement => blocksMovement;
        public bool SupportsNavigation => supportsNavigation;
        public bool IsExcavatable => isExcavatable;
    }
}
