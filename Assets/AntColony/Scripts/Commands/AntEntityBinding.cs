using System.Collections.Generic;
using AntColony.Ants;
using UnityEngine;

namespace AntColony.Commands
{
    /// <summary>
    /// Resolves a visual GameObject to an existing authoritative ant record.
    /// Binding never creates or deletes records.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AntEntityBinding : MonoBehaviour
    {
        [SerializeField] private string antId;

        private AntEntityBindingRegistry bindingRegistry;

        public string SerializedAntId => antId;
        public AntRecord Record { get; private set; }
        public bool IsBound => Record != null;

        public void Configure(string stableAntId)
        {
            antId = stableAntId?.Trim() ?? string.Empty;
        }

        public bool Bind(
            AntRegistry records,
            AntEntityBindingRegistry visuals,
            out string error)
        {
            if (records == null || visuals == null)
            {
                error = "Ant binding requires record and visual registries.";
                return false;
            }

            if (!records.TryGet(antId, out AntRecord record))
            {
                error = $"No authoritative ant record exists for ID '{antId}'.";
                return false;
            }

            if (bindingRegistry == visuals && Record != null &&
                Record.Id.Equals(record.Id))
            {
                error = string.Empty;
                return true;
            }

            ReleaseVisualClaim();
            if (!visuals.TryClaim(record.Id, this, out error))
            {
                return false;
            }

            Record = record;
            bindingRegistry = visuals;
            return true;
        }

        private void OnDestroy()
        {
            // The visual claim is transient. The AntRegistry and its record remain.
            ReleaseVisualClaim();
        }

        private void ReleaseVisualClaim()
        {
            if (bindingRegistry != null && Record != null)
            {
                bindingRegistry.Release(Record.Id, this);
            }

            bindingRegistry = null;
            Record = null;
        }
    }

    /// <summary>Prevents two live visuals from claiming the same individual.</summary>
    public sealed class AntEntityBindingRegistry
    {
        private readonly Dictionary<AntId, AntEntityBinding> bindings = new();

        public bool TryClaim(
            AntId antId,
            AntEntityBinding binding,
            out string error)
        {
            if (binding == null)
            {
                error = "Cannot bind a null ant visual.";
                return false;
            }

            if (bindings.TryGetValue(antId, out AntEntityBinding existing) &&
                existing != null && existing != binding)
            {
                error = $"Ant ID {antId} is already bound to another visual.";
                return false;
            }

            bindings[antId] = binding;
            error = string.Empty;
            return true;
        }

        public void Release(AntId antId, AntEntityBinding binding)
        {
            if (bindings.TryGetValue(antId, out AntEntityBinding existing) &&
                existing == binding)
            {
                bindings.Remove(antId);
            }
        }
    }
}
