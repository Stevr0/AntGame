using UnityEngine;
using UnityEngine.Tilemaps;

namespace AntColony.World
{
    /// <summary>
    /// Shared authored identity and presentation data for a tile material.
    /// Runtime cells store the stable ID rather than depending on scene objects.
    /// </summary>
    public abstract class WorldTileMaterialDefinition : ScriptableObject
    {
        [SerializeField] private string stableMaterialId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private TileBase visualTile;

        public string StableMaterialId => stableMaterialId;
        public string DisplayName => displayName;
        public TileBase VisualTile => visualTile;
    }
}
