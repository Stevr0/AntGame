using System;
using System.Collections.Generic;

namespace AntColony.World
{
    /// <summary>
    /// Resolves stable material IDs used by authoritative cell data. The
    /// registry is deliberately independent of scenes and rendering.
    /// </summary>
    public sealed class WorldMaterialRegistry
    {
        private readonly Dictionary<string, BackgroundTileMaterialDefinition> backgroundMaterials;
        private readonly Dictionary<string, SolidTileMaterialDefinition> solidMaterials;

        public WorldMaterialRegistry(
            IEnumerable<BackgroundTileMaterialDefinition> backgroundDefinitions,
            IEnumerable<SolidTileMaterialDefinition> solidDefinitions)
        {
            backgroundMaterials = BuildLookup(backgroundDefinitions, "background");
            solidMaterials = BuildLookup(solidDefinitions, "solid");
        }

        public bool ContainsBackgroundMaterial(string materialId)
        {
            return IsWellFormedMaterialId(materialId) &&
                backgroundMaterials.ContainsKey(materialId);
        }

        public bool ContainsSolidMaterial(string materialId)
        {
            return IsWellFormedMaterialId(materialId) &&
                solidMaterials.ContainsKey(materialId);
        }

        public bool TryGetBackgroundMaterial(
            string materialId,
            out BackgroundTileMaterialDefinition definition)
        {
            if (!IsWellFormedMaterialId(materialId))
            {
                definition = null;
                return false;
            }

            return backgroundMaterials.TryGetValue(materialId, out definition);
        }

        public bool TryGetSolidMaterial(
            string materialId,
            out SolidTileMaterialDefinition definition)
        {
            if (!IsWellFormedMaterialId(materialId))
            {
                definition = null;
                return false;
            }

            return solidMaterials.TryGetValue(materialId, out definition);
        }

        private static Dictionary<string, TDefinition> BuildLookup<TDefinition>(
            IEnumerable<TDefinition> definitions,
            string materialKind)
            where TDefinition : WorldTileMaterialDefinition
        {
            var lookup = new Dictionary<string, TDefinition>(StringComparer.Ordinal);
            if (definitions == null)
            {
                return lookup;
            }

            foreach (TDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        $"The {materialKind} material list contains a missing definition.",
                        nameof(definitions));
                }

                string materialId = definition.StableMaterialId;
                if (!IsWellFormedMaterialId(materialId))
                {
                    throw new ArgumentException(
                        $"The {materialKind} material '{definition.name}' has an invalid stable ID. " +
                        "IDs cannot be empty or contain leading/trailing whitespace.",
                        nameof(definitions));
                }

                if (!lookup.TryAdd(materialId, definition))
                {
                    throw new ArgumentException(
                        $"The stable {materialKind} material ID '{materialId}' is duplicated.",
                        nameof(definitions));
                }
            }

            return lookup;
        }

        private static bool IsWellFormedMaterialId(string materialId)
        {
            return !string.IsNullOrWhiteSpace(materialId) &&
                string.Equals(materialId, materialId.Trim(), StringComparison.Ordinal);
        }
    }
}
