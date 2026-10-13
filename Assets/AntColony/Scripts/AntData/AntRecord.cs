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
        }

        public AntId Id { get; }
        public string DisplayName { get; }
        public ColonyId ColonyId { get; }
        public AntCaste Caste { get; }
        public AntAttributes Attributes { get; }
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
    }
}
