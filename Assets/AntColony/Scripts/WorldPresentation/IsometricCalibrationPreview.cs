using AntColony.World;
using UnityEngine;

namespace AntColony.WorldPresentation
{
    /// <summary>
    /// Builds the isolated ISO-WV-0 art preview. This component reads only its
    /// five assigned Sprites and never connects to Surface simulation data.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class IsometricCalibrationPreview : MonoBehaviour
    {
        public const int PreviewSize = 8;
        public static readonly SurfaceCell WaterCell = new(1, 5);
        public static readonly SurfaceCell VegetationCell = new(2, 2);
        public static readonly SurfaceCell CentreMarkerCell = new(3, 3);
        public static readonly SurfaceCell RaisedGroundCell = new(4, 4);
        public static readonly SurfaceCell RockCell = new(5, 3);

        [SerializeField] private SurfaceIsometricProjection projection;
        [SerializeField] private Sprite flatGroundSprite;
        [SerializeField] private Sprite raisedGroundSprite;
        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite rockSprite;
        [SerializeField] private Sprite vegetationSprite;
        [SerializeField] private Transform previewRoot;

        public SurfaceIsometricProjection Projection => projection;
        public Transform PreviewRoot => previewRoot;

        public void Configure(SurfaceIsometricProjection configuredProjection,
            Sprite flatGround, Sprite raisedGround, Sprite water,
            Sprite rock, Sprite vegetation)
        {
            projection = configuredProjection;
            flatGroundSprite = flatGround;
            raisedGroundSprite = raisedGround;
            waterSprite = water;
            rockSprite = rock;
            vegetationSprite = vegetation;
        }

        /// <summary>Recreates only the children owned by this isolated preview.</summary>
        [ContextMenu("Rebuild Calibration Preview")]
        public void RebuildPreview()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "ISO-WV-0 preview requires its projection and five Sprites.",
                    this);
                return;
            }

            ClearPreview();
            var root = new GameObject("Calibration Content");
            root.transform.SetParent(transform, false);
            previewRoot = root.transform;

            for (int y = 0; y < PreviewSize; y++)
            {
                for (int x = 0; x < PreviewSize; x++)
                {
                    var cell = new SurfaceCell(x, y);
                    Sprite sprite = cell.Equals(WaterCell)
                        ? waterSprite
                        : cell.Equals(RaisedGroundCell)
                            ? raisedGroundSprite
                            : flatGroundSprite;
                    Color colour = sprite == flatGroundSprite && (x + y) % 2 != 0
                        ? new Color32(220, 229, 238, 255)
                        : Color.white;
                    CreateSprite(
                        root.transform,
                        $"Terrain ({x},{y})",
                        cell,
                        sprite,
                        colour,
                        0);
                }
            }

            CreateSprite(root.transform, "Rock Resource (5,3)", RockCell,
                rockSprite, Color.white, 5);
            CreateSprite(root.transform, "Vegetation (2,2)", VegetationCell,
                vegetationSprite, Color.white, 5);
            CreateCentreMarker(root.transform);
        }

        private void CreateCentreMarker(Transform parent)
        {
            GameObject marker = CreateSprite(parent, "Logical Centre Marker (3,3)",
                CentreMarkerCell, flatGroundSprite,
                new Color32(255, 70, 210, 255), 50);
            marker.transform.localScale = Vector3.one * 0.16f;

            var labelObject = new GameObject("Logical Centre Label", typeof(TextMesh));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = projection.CellToWorld(CentreMarkerCell) +
                new Vector2(0f, 0.38f);
            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.text = "Cell (3,3) centre";
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 32;
            label.characterSize = 0.035f;
            label.color = new Color32(255, 226, 90, 255);
            labelObject.GetComponent<MeshRenderer>().sortingOrder = 10000;
        }

        private GameObject CreateSprite(Transform parent, string objectName,
            SurfaceCell cell, Sprite sprite, Color colour, int orderOffset)
        {
            var item = new GameObject(objectName, typeof(SpriteRenderer));
            item.transform.SetParent(parent, false);
            item.transform.position = projection.CellToWorld(cell);
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = colour;
            // Cells farther down the screen draw in front. The offset keeps a
            // prop above the terrain occupying the same logical cell.
            renderer.sortingOrder = (cell.X + cell.Y) * 10 + orderOffset;
            return item;
        }

        private bool HasRequiredReferences()
        {
            return projection != null && flatGroundSprite != null &&
                raisedGroundSprite != null && waterSprite != null &&
                rockSprite != null && vegetationSprite != null;
        }

        private void ClearPreview()
        {
            if (previewRoot == null)
            {
                Transform existing = transform.Find("Calibration Content");
                previewRoot = existing;
            }

            if (previewRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(previewRoot.gameObject);
            }
            else
            {
                DestroyImmediate(previewRoot.gameObject);
            }

            previewRoot = null;
        }
    }
}
