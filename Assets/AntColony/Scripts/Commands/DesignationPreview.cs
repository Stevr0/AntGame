using UnityEngine;

namespace AntColony.Commands
{
    /// <summary>
    /// Shared screen-space geometry for transient previews and persistent
    /// designation borders. Keeping this calculation in one place prevents a
    /// hover outline from drifting away from its committed marker.
    /// </summary>
    public static class DesignationOverlayGeometry
    {
        public const float BorderThickness = 3f;
        public const float MarkerSize = 18f;

        public static bool TryGetCellGuiRect(Camera camera, Vector2Int cell,
            float guiHeight, out Rect rectangle)
        {
            rectangle = default;
            if (camera == null) return false;

            Vector3 minimum = camera.WorldToScreenPoint(
                new Vector3(cell.x, cell.y, 0f));
            Vector3 maximum = camera.WorldToScreenPoint(
                new Vector3(cell.x + 1f, cell.y + 1f, 0f));
            if (minimum.z < 0f || maximum.z < 0f) return false;

            float left = Mathf.Min(minimum.x, maximum.x);
            float top = guiHeight - Mathf.Max(minimum.y, maximum.y);
            rectangle = new Rect(left, top,
                Mathf.Abs(maximum.x - minimum.x),
                Mathf.Abs(maximum.y - minimum.y));
            return rectangle.width > 0f && rectangle.height > 0f;
        }
    }

    /// <summary>Terminal designations never contribute persistent world UI.</summary>
    public static class DesignationOverlayRules
    {
        public static bool IsVisible(ExcavationDesignation designation) =>
            designation != null && designation.State is
                ExcavationDesignationState.Pending or ExcavationDesignationState.Active;

        public static bool IsVisible(ConstructionDesignation designation) =>
            designation != null && designation.State is
                ConstructionDesignationState.Pending or ConstructionDesignationState.Active;
    }

    /// <summary>Presentation result returned by an authoritative designation registry.</summary>
    public enum DesignationPreviewState
    {
        Valid,
        Invalid,
        ExistingDesignation
    }

    /// <summary>
    /// Read-only description of one preview cell. It contains no gameplay state and
    /// is rebuilt only for the bounded hover or drag rectangle.
    /// </summary>
    public readonly struct DesignationPreviewCell
    {
        public DesignationPreviewCell(Vector2Int cell,
            DesignationPreviewState state, string reason)
        {
            Cell = cell;
            State = state;
            Reason = reason ?? string.Empty;
        }

        public Vector2Int Cell { get; }
        public DesignationPreviewState State { get; }
        public string Reason { get; }
    }
}
