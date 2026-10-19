using AntColony.World;
using UnityEngine;

namespace AntColony.WorldPresentation
{
    /// <summary>
    /// Converts logical Surface cells to 2D isometric presentation positions.
    /// It owns no terrain, occupancy, navigation, or gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceIsometricProjection : MonoBehaviour
    {
        public const int DefaultTileWidthPixels = 32;
        public const int DefaultTileHeightPixels = 16;
        public const int DefaultPixelsPerUnit = 32;

        [SerializeField] private Vector2 origin;
        [SerializeField, Min(1f)] private float tileWidthPixels =
            DefaultTileWidthPixels;
        [SerializeField, Min(1f)] private float tileHeightPixels =
            DefaultTileHeightPixels;
        [SerializeField, Min(1f)] private float pixelsPerUnit =
            DefaultPixelsPerUnit;

        public Vector2 Origin => origin;
        public float TileWidthPixels => tileWidthPixels;
        public float TileHeightPixels => tileHeightPixels;
        public float PixelsPerUnit => pixelsPerUnit;
        public float HalfTileWidthWorld => tileWidthPixels / pixelsPerUnit * 0.5f;
        public float HalfTileHeightWorld => tileHeightPixels / pixelsPerUnit * 0.5f;

        /// <summary>
        /// Applies calibration values without changing any logical Surface data.
        /// Positive logical X points down-right and positive logical Y points
        /// down-left on screen. Unity world Y therefore decreases for both axes.
        /// </summary>
        public void Configure(Vector2 presentationOrigin, float widthPixels,
            float heightPixels, float configuredPixelsPerUnit)
        {
            origin = presentationOrigin;
            tileWidthPixels = Mathf.Max(1f, widthPixels);
            tileHeightPixels = Mathf.Max(1f, heightPixels);
            pixelsPerUnit = Mathf.Max(1f, configuredPixelsPerUnit);
        }

        /// <summary>Returns the presentation centre of one logical Surface cell.</summary>
        public Vector2 CellToWorld(SurfaceCell cell)
        {
            return origin + new Vector2(
                (cell.X - cell.Y) * HalfTileWidthWorld,
                -(cell.X + cell.Y) * HalfTileHeightWorld);
        }

        /// <summary>
        /// Resolves the diamond containing a presentation point. Exact shared
        /// edges use a stable half-open rule: the larger logical coordinate wins.
        /// This keeps negative and positive coordinates deterministic.
        /// </summary>
        public SurfaceCell WorldToCell(Vector2 worldPosition)
        {
            Vector2 local = worldPosition - origin;
            float difference = local.x / HalfTileWidthWorld;
            float sum = -local.y / HalfTileHeightWorld;
            float logicalX = (difference + sum) * 0.5f;
            float logicalY = (sum - difference) * 0.5f;

            return new SurfaceCell(
                RoundHalfUp(logicalX),
                RoundHalfUp(logicalY));
        }

        private static int RoundHalfUp(float value)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }

        private void OnValidate()
        {
            tileWidthPixels = Mathf.Max(1f, tileWidthPixels);
            tileHeightPixels = Mathf.Max(1f, tileHeightPixels);
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        }
    }
}
