using System;
using System.Collections.Generic;

namespace AntColony.Ants
{
    /// <summary>Stable ordinary-work categories approved for the per-ant matrix.</summary>
    public enum AntWorkCategory
    {
        // Keep value zero to preserve every existing priority record.
        Extraction = 0,
        Excavation = Extraction,
        SurfacePreparation = 1,
        Construction = 2,
        Hauling = 3,
        Foraging = 4,
        Production = 5,
        Combat = 6,
        BroodCare = 7
    }

    /// <summary>Lower numeric values are selected before higher values.</summary>
    public enum AntWorkPriority
    {
        Disabled = 0,
        Highest = 1,
        High = 2,
        Normal = 3,
        Low = 4
    }

    /// <summary>Stable category metadata shared by priorities and synthetic jobs.</summary>
    public sealed class AntWorkCategoryDefinition
    {
        internal AntWorkCategoryDefinition(
            AntWorkCategory id,
            string stableId,
            string displayName,
            AntSkillId? skill)
        {
            Id = id;
            StableId = stableId;
            DisplayName = displayName;
            Skill = skill;
        }

        public AntWorkCategory Id { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public AntSkillId? Skill { get; }
    }

    public static class AntWorkCategoryCatalog
    {
        private static readonly AntWorkCategoryDefinition[] definitions =
        {
            new(AntWorkCategory.Extraction, "extraction", "Extraction", AntSkillId.Extraction),
            new(AntWorkCategory.SurfacePreparation, "surface_preparation", "Surface Preparation", AntSkillId.SurfacePreparation),
            new(AntWorkCategory.Construction, "construction", "Construction", AntSkillId.Construction),
            new(AntWorkCategory.Hauling, "hauling", "Hauling", AntSkillId.Hauling),
            new(AntWorkCategory.Foraging, "foraging", "Foraging", AntSkillId.Foraging),
            new(AntWorkCategory.Production, "production", "Production", null),
            new(AntWorkCategory.Combat, "combat", "Combat", AntSkillId.Combat),
            new(AntWorkCategory.BroodCare, "brood_care", "Brood Care", AntSkillId.BroodCare)
        };

        public static IReadOnlyList<AntWorkCategoryDefinition> All => definitions;

        public static AntWorkCategoryDefinition Get(AntWorkCategory category)
        {
            int index = (int)category;
            if (index < 0 || index >= definitions.Length || definitions[index].Id != category)
                throw new ArgumentOutOfRangeException(nameof(category));
            return definitions[index];
        }
    }

    /// <summary>
    /// Authoritative, serialisation-friendly ordinary-work preferences for one ant.
    /// Skills deliberately do not participate in this matrix.
    /// </summary>
    public sealed class AntWorkPrioritySet
    {
        private readonly AntWorkPriority[] priorities =
            new AntWorkPriority[AntWorkCategoryCatalog.All.Count];

        /// <summary>
        /// Changes whenever the matrix changes. Resumable job selection uses this
        /// to restart a partial pass instead of finishing with stale priorities.
        /// </summary>
        public int Revision { get; private set; }

        internal AntWorkPrioritySet()
        {
            Reset();
        }

        public AntWorkPriority Get(AntWorkCategory category) =>
            priorities[ValidateCategory(category)];

        public bool TrySet(
            AntWorkCategory category,
            AntWorkPriority priority,
            out string error)
        {
            if (!Enum.IsDefined(typeof(AntWorkPriority), priority))
            {
                error = "Work priority must be 1-4 or Disabled.";
                return false;
            }

            int index = ValidateCategory(category);
            if (priorities[index] != priority)
            {
                priorities[index] = priority;
                Revision++;
            }
            error = string.Empty;
            return true;
        }

        public void Reset(AntWorkPriority defaultPriority = AntWorkPriority.Normal)
        {
            if (!Enum.IsDefined(typeof(AntWorkPriority), defaultPriority))
                throw new ArgumentOutOfRangeException(nameof(defaultPriority));
            for (int index = 0; index < priorities.Length; index++)
                priorities[index] = defaultPriority;
            Revision++;
        }

        private static int ValidateCategory(AntWorkCategory category)
        {
            int index = (int)category;
            if (index < 0 || index >= AntWorkCategoryCatalog.All.Count)
                throw new ArgumentOutOfRangeException(nameof(category));
            return index;
        }
    }
}
