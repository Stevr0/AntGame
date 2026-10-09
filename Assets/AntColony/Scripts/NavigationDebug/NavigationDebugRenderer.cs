using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AntColony.NavigationDebug
{
    /// <summary>
    /// Renders a bounded navigation-classification region as one batched mesh.
    /// It creates no tile GameObjects, SpriteRenderers, or gameplay colliders.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class NavigationDebugRenderer : MonoBehaviour
    {
        private const int MaximumAxisSize = 128;
        private const float CellInset = 0.08f;
        private const float BoundaryWidth = 0.08f;

        [SerializeField] private NavigationTestWorldHost worldHost;
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private bool overlayEnabled = true;
        [SerializeField] private bool showChunkBoundaries = true;
        [SerializeField] private Vector2Int minimumCell = new Vector2Int(-40, -8);
        [SerializeField] private Vector2Int regionSize = new Vector2Int(81, 44);
        [SerializeField] private Vector2Int inspectedCell = Vector2Int.zero;

        private readonly List<NavigationOverlayCell> displayedCells = new();
        private readonly List<Vector3> vertices = new();
        private readonly List<Color32> colours = new();
        private readonly List<int> triangles = new();
        private Mesh overlayMesh;

        public NavigationTestWorldHost WorldHost => worldHost;
        public bool OverlayEnabled => overlayEnabled;
        public bool ShowChunkBoundaries => showChunkBoundaries;
        public Vector2Int MinimumCell => minimumCell;
        public Vector2Int RegionSize => regionSize;
        public Vector2Int InspectedCell => inspectedCell;
        public int DisplayedCellCount => displayedCells.Count;

        private void OnEnable()
        {
            RefreshOverlay();
        }

        private void OnDisable()
        {
            ReleaseMesh();
        }

        private void OnValidate()
        {
            regionSize.x = Mathf.Clamp(regionSize.x, 1, MaximumAxisSize);
            regionSize.y = Mathf.Clamp(regionSize.y, 1, MaximumAxisSize);
            if (isActiveAndEnabled)
            {
                RefreshOverlay();
            }
        }

        /// <summary>
        /// Requeries the classifier and rebuilds one mesh. Refresh is explicit;
        /// nothing scans the world or allocates per cell every frame.
        /// </summary>
        public void RefreshOverlay()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = overlayEnabled;
            if (!overlayEnabled)
            {
                return;
            }

            if (worldHost == null ||
                (!worldHost.IsInitialized && !worldHost.TryRebuildFixture()))
            {
                ClearMesh();
                return;
            }

            if (overlayMaterial != null)
            {
                meshRenderer.sharedMaterial = overlayMaterial;
            }

            var model = new NavigationOverlayModel(worldHost.Classifier);
            model.BuildCells(minimumCell, regionSize, displayedCells);
            BuildMesh();
        }

        /// <summary>Enables or disables rendering without changing world data.</summary>
        public void SetOverlayEnabled(bool enabled)
        {
            overlayEnabled = enabled;
            GetComponent<MeshRenderer>().enabled = enabled;
            if (enabled)
            {
                RefreshOverlay();
            }
        }

        /// <summary>Shows or hides chunk-edge guides and refreshes the mesh.</summary>
        public void SetChunkBoundariesVisible(bool visible)
        {
            showChunkBoundaries = visible;
            RefreshOverlay();
        }

        /// <summary>Updates the cell reported by the custom debug inspector.</summary>
        public void SetInspectedCell(Vector2Int worldCell)
        {
            inspectedCell = worldCell;
        }

        /// <summary>
        /// Converts a Scene-view click to the shared integer-centre convention.
        /// </summary>
        public Vector2Int WorldPositionToCell(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            return AntColony.World.WorldTileCoordinates.WorldPositionToCell(localPosition);
        }

        private void BuildMesh()
        {
            vertices.Clear();
            colours.Clear();
            triangles.Clear();

            int estimatedQuads = displayedCells.Count +
                (showChunkBoundaries ? 16 : 0);
            EnsureListCapacity(estimatedQuads * 4, estimatedQuads * 6);

            float halfSize = (AntColony.World.WorldTileCoordinates.CellWorldSize - CellInset) *
                0.5f;
            foreach (NavigationOverlayCell cell in displayedCells)
            {
                AppendQuad(
                    cell.WorldPosition.x - halfSize,
                    cell.WorldPosition.y - halfSize,
                    cell.WorldPosition.x + halfSize,
                    cell.WorldPosition.y + halfSize,
                    0f,
                    cell.Colour);
            }

            if (showChunkBoundaries)
            {
                AppendChunkBoundaries();
            }

            EnsureMesh();
            overlayMesh.Clear();
            overlayMesh.indexFormat = vertices.Count > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            overlayMesh.SetVertices(vertices);
            overlayMesh.SetColors(colours);
            overlayMesh.SetTriangles(triangles, 0, true);
            GetComponent<MeshFilter>().sharedMesh = overlayMesh;
        }

        private void AppendChunkBoundaries()
        {
            int maximumX = checked(minimumCell.x + regionSize.x - 1);
            int maximumY = checked(minimumCell.y + regionSize.y - 1);
            int minimumChunkX = AntColony.World.WorldTileCoordinates
                .GetChunkCoordinate(new Vector2Int(minimumCell.x, 0)).x;
            int maximumChunkX = AntColony.World.WorldTileCoordinates
                .GetChunkCoordinate(new Vector2Int(maximumX, 0)).x;
            int minimumChunkY = AntColony.World.WorldTileCoordinates
                .GetChunkCoordinate(new Vector2Int(0, minimumCell.y)).y;
            int maximumChunkY = AntColony.World.WorldTileCoordinates
                .GetChunkCoordinate(new Vector2Int(0, maximumY)).y;

            float minimumX = minimumCell.x - 0.5f;
            float maximumXEdge = maximumX + 0.5f;
            float minimumY = minimumCell.y - 0.5f;
            float maximumYEdge = maximumY + 0.5f;
            float halfWidth = BoundaryWidth * 0.5f;

            for (int chunkX = minimumChunkX; chunkX <= maximumChunkX + 1; chunkX++)
            {
                float boundary = NavigationOverlayModel.GetChunkBoundaryPosition(chunkX);
                AppendQuad(
                    boundary - halfWidth,
                    minimumY,
                    boundary + halfWidth,
                    maximumYEdge,
                    -0.05f,
                    NavigationOverlayPalette.ChunkBoundary);
            }

            for (int chunkY = minimumChunkY; chunkY <= maximumChunkY + 1; chunkY++)
            {
                float boundary = NavigationOverlayModel.GetChunkBoundaryPosition(chunkY);
                AppendQuad(
                    minimumX,
                    boundary - halfWidth,
                    maximumXEdge,
                    boundary + halfWidth,
                    -0.05f,
                    NavigationOverlayPalette.ChunkBoundary);
            }
        }

        private void AppendQuad(
            float minimumX,
            float minimumY,
            float maximumX,
            float maximumY,
            float z,
            Color32 colour)
        {
            int firstVertex = vertices.Count;
            vertices.Add(new Vector3(minimumX, minimumY, z));
            vertices.Add(new Vector3(minimumX, maximumY, z));
            vertices.Add(new Vector3(maximumX, maximumY, z));
            vertices.Add(new Vector3(maximumX, minimumY, z));

            colours.Add(colour);
            colours.Add(colour);
            colours.Add(colour);
            colours.Add(colour);

            triangles.Add(firstVertex);
            triangles.Add(firstVertex + 1);
            triangles.Add(firstVertex + 2);
            triangles.Add(firstVertex);
            triangles.Add(firstVertex + 2);
            triangles.Add(firstVertex + 3);
        }

        private void EnsureListCapacity(int vertexCapacity, int triangleCapacity)
        {
            if (vertices.Capacity < vertexCapacity)
            {
                vertices.Capacity = vertexCapacity;
                colours.Capacity = vertexCapacity;
            }

            if (triangles.Capacity < triangleCapacity)
            {
                triangles.Capacity = triangleCapacity;
            }
        }

        private void EnsureMesh()
        {
            if (overlayMesh != null)
            {
                return;
            }

            overlayMesh = new Mesh
            {
                name = "Navigation Classification Overlay",
                hideFlags = HideFlags.DontSave
            };
        }

        private void ClearMesh()
        {
            displayedCells.Clear();
            overlayMesh?.Clear();
        }

        private void ReleaseMesh()
        {
            if (overlayMesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(overlayMesh);
            }
            else
            {
                DestroyImmediate(overlayMesh);
            }

            overlayMesh = null;
        }
    }
}
