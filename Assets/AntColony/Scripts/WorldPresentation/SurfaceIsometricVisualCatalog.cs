using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.WorldPresentation
{
    /// <summary>
    /// Authored presentation mapping for the limited isometric preview. Gameplay
    /// background IDs remain owned by the world model; this asset only selects art.
    /// </summary>
    [CreateAssetMenu(fileName = "SurfaceIsometricVisualCatalog",
        menuName = "Ant Colony/Visuals/Surface Isometric Catalog")]
    public sealed class SurfaceIsometricVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class BackgroundEntry
        {
            [SerializeField] private string gameplayDefinitionId = string.Empty;
            [SerializeField] private Sprite sprite;
            [SerializeField] private Color colour = Color.white;

            public string GameplayDefinitionId => gameplayDefinitionId;
            public Sprite Sprite => sprite;
            public Color Colour => colour;
        }

        [SerializeField] private List<BackgroundEntry> backgrounds = new();
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private Color fallbackColour =
            new Color32(255, 0, 220, 255);

        public IReadOnlyList<BackgroundEntry> Backgrounds => backgrounds;
        public Sprite FallbackSprite => fallbackSprite;
        public Color FallbackColour => fallbackColour;
    }
}
