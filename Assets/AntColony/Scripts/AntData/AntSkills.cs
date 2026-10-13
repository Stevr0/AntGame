using System;
using System.Collections.Generic;

namespace AntColony.Ants
{
    /// <summary>
    /// Stable ordinary-skill keys. Explicit numeric values and string IDs keep
    /// future saved data independent of display names and enum ordering changes.
    /// </summary>
    public enum AntSkillId
    {
        Excavation = 0,
        SurfacePreparation = 1,
        Construction = 2,
        Hauling = 3,
        Foraging = 4,
        Combat = 5,
        BroodCare = 6
    }

    /// <summary>Immutable name and serialization ID shared by every ant.</summary>
    public sealed class AntSkillDefinition
    {
        internal AntSkillDefinition(
            AntSkillId id,
            string stableId,
            string displayName)
        {
            Id = id;
            StableId = stableId;
            DisplayName = displayName;
        }

        public AntSkillId Id { get; }
        public string StableId { get; }
        public string DisplayName { get; }
    }

    /// <summary>Shared skill definitions; they are constructed once, not per ant.</summary>
    public static class AntSkillCatalog
    {
        private static readonly AntSkillDefinition[] definitions =
        {
            new(AntSkillId.Excavation, "excavation", "Excavation"),
            new(
                AntSkillId.SurfacePreparation,
                "surface_preparation",
                "Surface Preparation"),
            new(AntSkillId.Construction, "construction", "Construction"),
            new(AntSkillId.Hauling, "hauling", "Hauling"),
            new(AntSkillId.Foraging, "foraging", "Foraging"),
            new(AntSkillId.Combat, "combat", "Combat"),
            new(AntSkillId.BroodCare, "brood_care", "Brood Care")
        };

        public static IReadOnlyList<AntSkillDefinition> All => definitions;

        public static AntSkillDefinition Get(AntSkillId id)
        {
            int index = (int)id;
            if (index < 0 || index >= definitions.Length ||
                definitions[index].Id != id)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown ant skill.");
            }

            return definitions[index];
        }
    }

    /// <summary>
    /// Provisional deterministic balance values for Milestone 3.1C. They are
    /// supplied to the skill set so later balancing does not require a new model.
    /// Simulation time is measured in abstract hours until a central clock exists.
    /// </summary>
    public sealed class AntSkillProgressionSettings
    {
        public static AntSkillProgressionSettings Provisional { get; } =
            new(
                baseExperiencePerLevel: 100d,
                additionalExperiencePerLevel: 5d,
                inactivityGraceHours: 24d,
                decayLevelsPerHour: 0.25d);

        public AntSkillProgressionSettings(
            double baseExperiencePerLevel,
            double additionalExperiencePerLevel,
            double inactivityGraceHours,
            double decayLevelsPerHour)
        {
            RequireFinitePositive(
                nameof(baseExperiencePerLevel),
                baseExperiencePerLevel,
                allowZero: false);
            RequireFinitePositive(
                nameof(additionalExperiencePerLevel),
                additionalExperiencePerLevel,
                allowZero: true);
            RequireFinitePositive(
                nameof(inactivityGraceHours),
                inactivityGraceHours,
                allowZero: true);
            RequireFinitePositive(
                nameof(decayLevelsPerHour),
                decayLevelsPerHour,
                allowZero: true);

            BaseExperiencePerLevel = baseExperiencePerLevel;
            AdditionalExperiencePerLevel = additionalExperiencePerLevel;
            InactivityGraceHours = inactivityGraceHours;
            DecayLevelsPerHour = decayLevelsPerHour;
        }

        public const int MaximumLevel = 100;
        public const int DecayFloorLevel = 50;

        public double BaseExperiencePerLevel { get; }
        public double AdditionalExperiencePerLevel { get; }
        public double InactivityGraceHours { get; }
        public double DecayLevelsPerHour { get; }

        public double ExperienceForNextLevel(int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= MaximumLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel));
            }

            return BaseExperiencePerLevel +
                AdditionalExperiencePerLevel * currentLevel;
        }

        public double TotalExperienceForLevel(int level)
        {
            if (level < 0 || level > MaximumLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            // Sum of the arithmetic progression for levels [0, level).
            return level * BaseExperiencePerLevel +
                AdditionalExperiencePerLevel * level * (level - 1) * 0.5d;
        }

        public int GetLevel(double totalExperience)
        {
            if (!IsFinite(totalExperience) || totalExperience <= 0d)
            {
                return 0;
            }

            for (int level = 1; level <= MaximumLevel; level++)
            {
                if (totalExperience < TotalExperienceForLevel(level))
                {
                    return level - 1;
                }
            }

            return MaximumLevel;
        }

        public double GetExactLevel(double totalExperience)
        {
            int level = GetLevel(totalExperience);
            if (level >= MaximumLevel)
            {
                return MaximumLevel;
            }

            double levelStart = TotalExperienceForLevel(level);
            return level +
                (totalExperience - levelStart) / ExperienceForNextLevel(level);
        }

        public double TotalExperienceForExactLevel(double exactLevel)
        {
            if (!IsFinite(exactLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(exactLevel));
            }

            exactLevel = Math.Max(0d, Math.Min(MaximumLevel, exactLevel));
            int wholeLevel = Math.Min(
                (int)Math.Floor(exactLevel),
                MaximumLevel);
            if (wholeLevel >= MaximumLevel)
            {
                return TotalExperienceForLevel(MaximumLevel);
            }

            return TotalExperienceForLevel(wholeLevel) +
                (exactLevel - wholeLevel) *
                ExperienceForNextLevel(wholeLevel);
        }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void RequireFinitePositive(
            string parameterName,
            double value,
            bool allowZero)
        {
            if (!IsFinite(value) || value < 0d || (!allowZero && value == 0d))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, null);
            }
        }
    }

    /// <summary>Result returned to the gameplay system that supplied an XP event.</summary>
    public readonly struct AntSkillExperienceResult
    {
        public AntSkillExperienceResult(
            double requestedExperience,
            double awardedExperience,
            int previousLevel,
            int currentLevel)
        {
            RequestedExperience = requestedExperience;
            AwardedExperience = awardedExperience;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
        }

        public double RequestedExperience { get; }
        public double AwardedExperience { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public int LevelsGained => Math.Max(0, CurrentLevel - PreviousLevel);
        public bool ReachedMaximumLevel =>
            CurrentLevel == AntSkillProgressionSettings.MaximumLevel;
    }

    /// <summary>Read-only public state for one skill owned by one ant.</summary>
    public sealed class AntSkillState
    {
        private readonly AntSkillProgressionSettings settings;
        private double totalExperience;
        private double lastDecayEvaluationHours;

        internal AntSkillState(
            AntSkillDefinition definition,
            AntSkillProgressionSettings settings,
            double startingSimulationHours)
        {
            Definition = definition;
            this.settings = settings;
            LastPractisedHours = startingSimulationHours;
            lastDecayEvaluationHours = startingSimulationHours;
        }

        public AntSkillDefinition Definition { get; }
        public double TotalExperience => totalExperience;
        public int Level => settings.GetLevel(totalExperience);
        public bool IsMaximumLevel =>
            Level == AntSkillProgressionSettings.MaximumLevel;
        public double LastPractisedHours { get; private set; }
        public double ExperienceIntoLevel => IsMaximumLevel
            ? 0d
            : totalExperience - settings.TotalExperienceForLevel(Level);
        public double ExperienceForNextLevel => IsMaximumLevel
            ? 0d
            : settings.ExperienceForNextLevel(Level);
        public double ProgressToNextLevel => IsMaximumLevel
            ? 1d
            : ExperienceIntoLevel / ExperienceForNextLevel;

        internal bool TryAwardExperience(
            double baseExperience,
            double multiplier,
            double simulationHours,
            out AntSkillExperienceResult result,
            out string error)
        {
            result = default;
            if (!AntSkillProgressionSettings.IsFinite(baseExperience) ||
                baseExperience < 0d)
            {
                error = "Base skill experience must be finite and non-negative.";
                return false;
            }

            if (!AntSkillProgressionSettings.IsFinite(multiplier) || multiplier < 0d)
            {
                error = "The skill experience multiplier must be finite and non-negative.";
                return false;
            }

            if (!TryValidateSimulationTime(simulationHours, out error))
            {
                return false;
            }

            AdvanceDecay(simulationHours);
            int previousLevel = Level;
            double maximumExperience = settings.TotalExperienceForLevel(
                AntSkillProgressionSettings.MaximumLevel);
            double requested = baseExperience * multiplier;
            if (!AntSkillProgressionSettings.IsFinite(requested))
            {
                requested = maximumExperience;
            }

            double available = Math.Max(0d, maximumExperience - totalExperience);
            double awarded = Math.Min(requested, available);
            totalExperience += awarded;
            LastPractisedHours = simulationHours;
            lastDecayEvaluationHours = simulationHours;
            result = new AntSkillExperienceResult(
                requested,
                awarded,
                previousLevel,
                Level);
            error = string.Empty;
            return true;
        }

        internal bool TryAdvanceDecay(double simulationHours, out string error)
        {
            if (!TryValidateSimulationTime(simulationHours, out error))
            {
                return false;
            }

            AdvanceDecay(simulationHours);
            error = string.Empty;
            return true;
        }

        private void AdvanceDecay(double simulationHours)
        {
            double decayStart = Math.Max(
                lastDecayEvaluationHours,
                LastPractisedHours + settings.InactivityGraceHours);
            double elapsedDecayHours = Math.Max(0d, simulationHours - decayStart);
            double exactLevel = settings.GetExactLevel(totalExperience);
            if (elapsedDecayHours > 0d &&
                exactLevel > AntSkillProgressionSettings.DecayFloorLevel)
            {
                double decayedLevel = Math.Max(
                    AntSkillProgressionSettings.DecayFloorLevel,
                    exactLevel - settings.DecayLevelsPerHour * elapsedDecayHours);
                totalExperience = settings.TotalExperienceForExactLevel(decayedLevel);
            }

            lastDecayEvaluationHours = simulationHours;
        }

        private bool TryValidateSimulationTime(
            double simulationHours,
            out string error)
        {
            if (!AntSkillProgressionSettings.IsFinite(simulationHours) ||
                simulationHours < lastDecayEvaluationHours)
            {
                error = "Skill simulation time must be finite and monotonic.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// The seven independent skills for one ant. Mutations are internal so all
    /// callers must pass through AntRecord lifecycle validation.
    /// </summary>
    public sealed class AntSkillSet
    {
        private readonly AntSkillState[] states;
        private double latestSimulationHours;

        internal AntSkillSet(
            AntSkillProgressionSettings settings,
            double startingSimulationHours)
        {
            Settings = settings ??
                throw new ArgumentNullException(nameof(settings));
            if (!AntSkillProgressionSettings.IsFinite(startingSimulationHours) ||
                startingSimulationHours < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingSimulationHours));
            }

            states = new AntSkillState[AntSkillCatalog.All.Count];
            latestSimulationHours = startingSimulationHours;
            for (int index = 0; index < states.Length; index++)
            {
                states[index] = new AntSkillState(
                    AntSkillCatalog.All[index],
                    settings,
                    startingSimulationHours);
            }
        }

        public AntSkillProgressionSettings Settings { get; }
        public IReadOnlyList<AntSkillState> All => states;

        public AntSkillState Get(AntSkillId id)
        {
            int index = (int)id;
            if (index < 0 || index >= states.Length ||
                states[index].Definition.Id != id)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown ant skill.");
            }

            return states[index];
        }

        internal bool TryAwardExperience(
            AntSkillId id,
            double baseExperience,
            double multiplier,
            double simulationHours,
            out AntSkillExperienceResult result,
            out string error)
        {
            if (!TryValidateSimulationTime(simulationHours, out error))
            {
                result = default;
                return false;
            }

            bool succeeded = Get(id).TryAwardExperience(
                baseExperience,
                multiplier,
                simulationHours,
                out result,
                out error);
            if (succeeded)
            {
                latestSimulationHours = simulationHours;
            }

            return succeeded;
        }

        internal bool TryAdvanceDecay(double simulationHours, out string error)
        {
            if (!TryValidateSimulationTime(simulationHours, out error))
            {
                return false;
            }

            foreach (AntSkillState state in states)
            {
                if (!state.TryAdvanceDecay(simulationHours, out error))
                {
                    return false;
                }
            }

            latestSimulationHours = simulationHours;
            error = string.Empty;
            return true;
        }

        private bool TryValidateSimulationTime(
            double simulationHours,
            out string error)
        {
            if (!AntSkillProgressionSettings.IsFinite(simulationHours) ||
                simulationHours < latestSimulationHours)
            {
                error = "Skill simulation time must be finite and monotonic.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
