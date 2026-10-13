using UnityEngine;

namespace AntColony.World
{
    public enum ExcavatedMaterialType
    {
        None,
        Soil,
        Stone
    }

    /// <summary>
    /// Defines foreground collision plus the narrow 3.3B excavation output.
    /// Material hardness, tools and richer yield tables remain future work.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SolidMaterial",
        menuName = "Ant Colony/World/Solid Tile Material")]
    public sealed class SolidTileMaterialDefinition : WorldTileMaterialDefinition
    {
        [SerializeField] private bool blocksMovement = true;
        [SerializeField] private bool supportsNavigation = true;
        [SerializeField] private bool isExcavatable = true;
        [SerializeField] private ExcavatedMaterialType excavatedMaterial;
        [SerializeField, Min(0)] private int excavationYield = 1;

        public bool BlocksMovement => blocksMovement;
        public bool SupportsNavigation => supportsNavigation;
        public bool IsExcavatable => isExcavatable;
        public ExcavatedMaterialType ExcavatedMaterial => excavatedMaterial;
        public int ExcavationYield => excavationYield;
    }
}
