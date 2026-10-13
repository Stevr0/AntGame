using System;

namespace AntColony.Ants
{
    /// <summary>Biological type. Caste does not assign work or alter statistics.</summary>
    public enum AntCaste
    {
        Queen,
        Worker
    }

    /// <summary>Small lifecycle foundation; no damage or death mechanics exist yet.</summary>
    public enum AntLifecycleStatus
    {
        Active,
        Inactive,
        Deceased
    }

    /// <summary>
    /// Innate individual variation. Values are provisional and have no gameplay
    /// effects during Milestone 3.1B.
    /// </summary>
    public readonly struct AntAttributes
    {
        public const int MinimumValue = 1;
        public const int MaximumValue = 10;

        public AntAttributes(int strength, int agility, int endurance)
        {
            Validate(nameof(strength), strength);
            Validate(nameof(agility), agility);
            Validate(nameof(endurance), endurance);
            Strength = strength;
            Agility = agility;
            Endurance = endurance;
        }

        public int Strength { get; }
        public int Agility { get; }
        public int Endurance { get; }

        public static bool IsValid(int value)
        {
            return value >= MinimumValue && value <= MaximumValue;
        }

        private static void Validate(string parameterName, int value)
        {
            if (!IsValid(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"Ant attributes must be between {MinimumValue} and {MaximumValue}.");
            }
        }
    }

    /// <summary>
    /// Authoritative data for one ant. This plain C# object owns no GameObject,
    /// renderer, input, navigation or other Unity presentation state.
    /// </summary>
    public sealed class AntRecord
    {
        public AntRecord(
            AntId id,
            string displayName,
            ColonyId colonyId,
            AntCaste caste,
            AntAttributes attributes)
            : this(
                id,
                displayName,
                colonyId,
                caste,
                attributes,
                AntSkillProgressionSettings.Provisional,
                AntNeedsSimulationSettings.Provisional,
                startingSimulationHours: 0d)
        {
        }

        public AntRecord(
            AntId id,
            string displayName,
            ColonyId colonyId,
            AntCaste caste,
            AntAttributes attributes,
            AntSkillProgressionSettings skillSettings,
            double startingSimulationHours)
            : this(
                id,
                displayName,
                colonyId,
                caste,
                attributes,
                skillSettings,
                AntNeedsSimulationSettings.Provisional,
                startingSimulationHours)
        {
        }

        public AntRecord(
            AntId id,
            string displayName,
            ColonyId colonyId,
            AntCaste caste,
            AntAttributes attributes,
            AntSkillProgressionSettings skillSettings,
            AntNeedsSimulationSettings needsSettings,
            double startingSimulationHours)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("The ant must have a valid ID.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("The ant must have a display name.", nameof(displayName));
            }

            if (!colonyId.IsValid)
            {
                throw new ArgumentException("The ant must belong to a valid colony.", nameof(colonyId));
            }

            Id = id;
            DisplayName = displayName.Trim();
            ColonyId = colonyId;
            Caste = caste;
            Attributes = attributes;
            LifecycleStatus = AntLifecycleStatus.Active;
            Skills = new AntSkillSet(skillSettings, startingSimulationHours);
            Needs = new AntNeedsState(needsSettings);
            WorkPriorities = new AntWorkPrioritySet();
        }

        public AntId Id { get; }
        public string DisplayName { get; }
        public ColonyId ColonyId { get; }
        public AntCaste Caste { get; }
        public AntAttributes Attributes { get; }
        public AntSkillSet Skills { get; private set; }
        public AntNeedsState Needs { get; }
        public AntWorkPrioritySet WorkPriorities { get; }
        public AntLifecycleStatus LifecycleStatus { get; private set; }
        public bool CanAcceptMovementOrders => LifecycleStatus == AntLifecycleStatus.Active;

        /// <summary>
        /// Records permanent death without removing identity data. Gameplay causes
        /// of death and corpse presentation are intentionally outside this milestone.
        /// </summary>
        public void MarkDeceased()
        {
            LifecycleStatus = AntLifecycleStatus.Deceased;
        }

        /// <summary>Allows tests and future systems to temporarily deactivate a record.</summary>
        public void MarkInactive()
        {
            if (LifecycleStatus != AntLifecycleStatus.Deceased)
            {
                LifecycleStatus = AntLifecycleStatus.Inactive;
            }
        }

        /// <summary>
        /// Records experience from one successful, approved progression event.
        /// Attempted or cancelled work should not call this method.
        /// </summary>
        public bool TryAwardSkillExperience(
            AntSkillId skill,
            double baseExperience,
            double experienceMultiplier,
            double simulationHours,
            out AntSkillExperienceResult result,
            out string error)
        {
            if (LifecycleStatus == AntLifecycleStatus.Deceased)
            {
                result = default;
                error = "Deceased ants cannot gain skill experience.";
                return false;
            }

            // Validate the caller's modifier before needs change its effective
            // value. This preserves the 3.1C rejection rules even when dry.
            if (!AntSkillProgressionSettings.IsFinite(experienceMultiplier) ||
                experienceMultiplier < 0d)
            {
                result = default;
                error = "The skill experience multiplier must be finite and non-negative.";
                return false;
            }

            double needsMultiplier = Needs.IsDehydrated
                ? 0d
                : Needs.SkillExperienceMultiplier;
            double effectiveMultiplier = experienceMultiplier * needsMultiplier;
            if (!AntSkillProgressionSettings.IsFinite(effectiveMultiplier))
            {
                effectiveMultiplier = double.MaxValue;
            }

            return Skills.TryAwardExperience(
                skill,
                baseExperience,
                effectiveMultiplier,
                simulationHours,
                out result,
                out error);
        }

        /// <summary>
        /// Applies deterministic inactivity decay at an absolute simulation time.
        /// No Update loop is required; a future central clock can call this in batches.
        /// </summary>
        public bool TryAdvanceSkillDecay(double simulationHours, out string error)
        {
            if (LifecycleStatus == AntLifecycleStatus.Deceased)
            {
                error = "Deceased ants do not continue skill decay.";
                return false;
            }

            return Skills.TryAdvanceDecay(simulationHours, out error);
        }

        /// <summary>Development/test reset; it does not replace identity data.</summary>
        public void ResetSkills(double simulationHours = 0d)
        {
            Skills = new AntSkillSet(Skills.Settings, simulationHours);
        }

        /// <summary>
        /// Advances ordinary awake survival simulation. Inactive, deceased, and
        /// zero-health ants retain their data but cannot keep progressing normally.
        /// </summary>
        public bool TryAdvanceNeeds(double elapsedHours, out string error)
        {
            if (LifecycleStatus != AntLifecycleStatus.Active)
            {
                error = "Only active ants continue ordinary needs simulation.";
                return false;
            }

            if (Needs.HasDepletedHealth)
            {
                error = "Health is depleted; normal survival simulation is paused.";
                return false;
            }

            return Needs.TryAdvanceAwake(elapsedHours, out error);
        }

        /// <summary>Development/future-rest boundary; no sleeping job is implied.</summary>
        public bool TryRest(double elapsedHours, out string error)
        {
            if (LifecycleStatus != AntLifecycleStatus.Active ||
                Needs.HasDepletedHealth)
            {
                error = "Only active ants with Health remaining can rest.";
                return false;
            }

            return Needs.TryRest(elapsedHours, out error);
        }

        public bool TryEat(double hungerRestored, out string error)
        {
            if (!CanReceiveSurvivalInteraction(out error))
            {
                return false;
            }

            return Needs.TryRestoreHunger(hungerRestored, out error);
        }

        public bool TryDrink(double thirstRestored, out string error)
        {
            if (!CanReceiveSurvivalInteraction(out error))
            {
                return false;
            }

            return Needs.TryRestoreThirst(thirstRestored, out error);
        }

        private bool CanReceiveSurvivalInteraction(out string error)
        {
            if (LifecycleStatus != AntLifecycleStatus.Active ||
                Needs.HasDepletedHealth)
            {
                error = "Only active ants with Health remaining can use survival interactions.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Development-only controlled state setup. Real consumption, damage, and
        /// recovery systems will use narrower gameplay APIs in later milestones.
        /// </summary>
        public bool TrySetNeedsForDevelopment(
            double health,
            double hunger,
            double thirst,
            double fatigue,
            out string error)
        {
            return Needs.TrySetForDevelopment(
                health,
                hunger,
                thirst,
                fatigue,
                out error);
        }

        /// <summary>Restores deterministic development starting values.</summary>
        public void ResetNeeds()
        {
            Needs.Reset();
        }
    }
}
