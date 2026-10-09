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
    }
}
