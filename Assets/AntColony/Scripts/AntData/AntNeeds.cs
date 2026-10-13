using System;

namespace AntColony.Ants
{
    /// <summary>
    /// Provisional Milestone 3.2A balance. Keeping every rate in one immutable
    /// object makes later tuning possible without changing the needs model.
    /// </summary>
    public sealed class AntNeedsSimulationSettings
    {
        public static AntNeedsSimulationSettings Provisional { get; } = new(
            hungerLossPerHour: 2d,
            thirstLossPerHour: 3d,
            awakeFatigueGainPerHour: 4d,
            restingFatigueRecoveryPerHour: 12d,
            criticalHungerThreshold: 10d,
            starvationHealthLossPerHour: 2d,
            criticalThirstThreshold: 10d,
            restedQualificationFatigue: 20d,
            restedBonusEndsAtFatigue: 80d,
            maximumRestedWorkSpeedBonus: 0.15d,
            maximumRestedExperienceBonus: 0.25d,
            startingFatigue: 20d);

        public AntNeedsSimulationSettings(
            double hungerLossPerHour,
            double thirstLossPerHour,
            double awakeFatigueGainPerHour,
            double restingFatigueRecoveryPerHour,
            double criticalHungerThreshold,
            double starvationHealthLossPerHour,
            double criticalThirstThreshold,
            double restedQualificationFatigue,
            double restedBonusEndsAtFatigue,
            double maximumRestedWorkSpeedBonus,
            double maximumRestedExperienceBonus,
            double startingFatigue)
        {
            ValidateNonNegative(nameof(hungerLossPerHour), hungerLossPerHour);
            ValidateNonNegative(nameof(thirstLossPerHour), thirstLossPerHour);
            ValidateNonNegative(
                nameof(awakeFatigueGainPerHour),
                awakeFatigueGainPerHour);
            ValidateNonNegative(
                nameof(restingFatigueRecoveryPerHour),
                restingFatigueRecoveryPerHour);
            ValidateRange(
                nameof(criticalHungerThreshold),
                criticalHungerThreshold);
            ValidateNonNegative(
                nameof(starvationHealthLossPerHour),
                starvationHealthLossPerHour);
            ValidateRange(
                nameof(criticalThirstThreshold),
                criticalThirstThreshold);
            ValidateRange(
                nameof(restedQualificationFatigue),
                restedQualificationFatigue);
            ValidateRange(
                nameof(restedBonusEndsAtFatigue),
                restedBonusEndsAtFatigue);
            ValidateNonNegative(
                nameof(maximumRestedWorkSpeedBonus),
                maximumRestedWorkSpeedBonus);
            ValidateNonNegative(
                nameof(maximumRestedExperienceBonus),
                maximumRestedExperienceBonus);
            ValidateRange(nameof(startingFatigue), startingFatigue);

            if (restedBonusEndsAtFatigue <= restedQualificationFatigue)
            {
                throw new ArgumentException(
                    "The Rested fade range must end above its qualification value.");
            }

            HungerLossPerHour = hungerLossPerHour;
            ThirstLossPerHour = thirstLossPerHour;
            AwakeFatigueGainPerHour = awakeFatigueGainPerHour;
            RestingFatigueRecoveryPerHour = restingFatigueRecoveryPerHour;
            CriticalHungerThreshold = criticalHungerThreshold;
            StarvationHealthLossPerHour = starvationHealthLossPerHour;
            CriticalThirstThreshold = criticalThirstThreshold;
            RestedQualificationFatigue = restedQualificationFatigue;
            RestedBonusEndsAtFatigue = restedBonusEndsAtFatigue;
            MaximumRestedWorkSpeedBonus = maximumRestedWorkSpeedBonus;
            MaximumRestedExperienceBonus = maximumRestedExperienceBonus;
            StartingFatigue = startingFatigue;
        }

        public const double MinimumValue = 0d;
        public const double MaximumValue = 100d;

        public double HungerLossPerHour { get; }
        public double ThirstLossPerHour { get; }
        public double AwakeFatigueGainPerHour { get; }
        public double RestingFatigueRecoveryPerHour { get; }
        public double CriticalHungerThreshold { get; }
        public double StarvationHealthLossPerHour { get; }
        public double CriticalThirstThreshold { get; }
        public double RestedQualificationFatigue { get; }
        public double RestedBonusEndsAtFatigue { get; }
        public double MaximumRestedWorkSpeedBonus { get; }
        public double MaximumRestedExperienceBonus { get; }
        public double StartingFatigue { get; }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void ValidateRange(string name, double value)
        {
            if (!IsFinite(value) || value < MinimumValue || value > MaximumValue)
            {
                throw new ArgumentOutOfRangeException(name, value, null);
            }
        }

        private static void ValidateNonNegative(string name, double value)
        {
            if (!IsFinite(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(name, value, null);
            }
        }
    }

    /// <summary>
    /// Authoritative survival state for one ant. It has no Unity dependency and
    /// changes only when an owning AntRecord receives an explicit simulation call.
    /// </summary>
    public sealed class AntNeedsState
    {
        internal AntNeedsState(AntNeedsSimulationSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Reset();
        }

        public AntNeedsSimulationSettings Settings { get; }
        public double Health { get; private set; }
        public double Hunger { get; private set; }
        public double Thirst { get; private set; }
        public double Fatigue { get; private set; }
        public bool HasCompletedRest { get; private set; }
        public bool HasDepletedHealth => Health <= AntNeedsSimulationSettings.MinimumValue;
        public bool IsStarving => Hunger <= Settings.CriticalHungerThreshold;
        public bool IsDehydrated => Thirst <= Settings.CriticalThirstThreshold;

        /// <summary>
        /// A rest event first qualifies the ant. Its benefit then fades smoothly
        /// from full strength at low fatigue to zero at the configured high point.
        /// </summary>
        public double RestedStrength
        {
            get
            {
                if (!HasCompletedRest ||
                    Fatigue >= Settings.RestedBonusEndsAtFatigue)
                {
                    return 0d;
                }

                double fadeRange = Settings.RestedBonusEndsAtFatigue -
                    Settings.RestedQualificationFatigue;
                double fatigueIntoFade = Math.Max(
                    0d,
                    Fatigue - Settings.RestedQualificationFatigue);
                return Math.Max(0d, 1d - fatigueIntoFade / fadeRange);
            }
        }

        public double WorkSpeedMultiplier =>
            1d + RestedStrength * Settings.MaximumRestedWorkSpeedBonus;

        public double SkillExperienceMultiplier =>
            1d + RestedStrength * Settings.MaximumRestedExperienceBonus;

        internal bool TryAdvanceAwake(double elapsedHours, out string error)
        {
            if (!TryValidateElapsedTime(elapsedHours, out error))
            {
                return false;
            }

            // Work out how much of this interval was spent at or below the
            // starvation threshold. This analytic calculation makes one large
            // step match many smaller steps, including when Hunger crosses 10.
            double hoursBeforeStarvation;
            if (Hunger <= Settings.CriticalHungerThreshold)
            {
                hoursBeforeStarvation = 0d;
            }
            else if (Settings.HungerLossPerHour <= 0d)
            {
                // A zero depletion rate can never cross the threshold.
                hoursBeforeStarvation = double.PositiveInfinity;
            }
            else
            {
                hoursBeforeStarvation =
                    (Hunger - Settings.CriticalHungerThreshold) /
                    Settings.HungerLossPerHour;
            }
            double starvingHours = Math.Max(
                0d,
                elapsedHours - hoursBeforeStarvation);

            Hunger = Clamp(Hunger - Settings.HungerLossPerHour * elapsedHours);
            Thirst = Clamp(Thirst - Settings.ThirstLossPerHour * elapsedHours);
            Fatigue = Clamp(
                Fatigue + Settings.AwakeFatigueGainPerHour * elapsedHours);
            Health = Clamp(
                Health - Settings.StarvationHealthLossPerHour * starvingHours);
            error = string.Empty;
            return true;
        }

        internal bool TryRest(double elapsedHours, out string error)
        {
            if (!TryValidateElapsedTime(elapsedHours, out error))
            {
                return false;
            }

            Fatigue = Clamp(
                Fatigue - Settings.RestingFatigueRecoveryPerHour * elapsedHours);
            if (elapsedHours > 0d &&
                Fatigue <= Settings.RestedQualificationFatigue)
            {
                HasCompletedRest = true;
            }

            error = string.Empty;
            return true;
        }

        internal bool TrySetForDevelopment(
            double health,
            double hunger,
            double thirst,
            double fatigue,
            out string error)
        {
            if (!IsValidValue(health) || !IsValidValue(hunger) ||
                !IsValidValue(thirst) || !IsValidValue(fatigue))
            {
                error = "Need values must be finite and between 0 and 100.";
                return false;
            }

            Health = health;
            Hunger = hunger;
            Thirst = thirst;
            Fatigue = fatigue;
            if (Fatigue >= Settings.RestedBonusEndsAtFatigue)
            {
                HasCompletedRest = false;
            }

            error = string.Empty;
            return true;
        }

        internal void Reset()
        {
            Health = AntNeedsSimulationSettings.MaximumValue;
            Hunger = AntNeedsSimulationSettings.MaximumValue;
            Thirst = AntNeedsSimulationSettings.MaximumValue;
            Fatigue = Settings.StartingFatigue;
            HasCompletedRest = false;
        }

        private static bool TryValidateElapsedTime(
            double elapsedHours,
            out string error)
        {
            if (!AntNeedsSimulationSettings.IsFinite(elapsedHours) ||
                elapsedHours < 0d)
            {
                error = "Elapsed needs time must be finite and non-negative.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsValidValue(double value)
        {
            return AntNeedsSimulationSettings.IsFinite(value) &&
                value >= AntNeedsSimulationSettings.MinimumValue &&
                value <= AntNeedsSimulationSettings.MaximumValue;
        }

        private static double Clamp(double value)
        {
            return Math.Max(
                AntNeedsSimulationSettings.MinimumValue,
                Math.Min(AntNeedsSimulationSettings.MaximumValue, value));
        }
    }
}
