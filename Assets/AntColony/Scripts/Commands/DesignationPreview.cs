using UnityEngine;

namespace AntColony.Commands
{
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
