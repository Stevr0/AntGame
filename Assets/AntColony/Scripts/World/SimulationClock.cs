using System;
using UnityEngine;

namespace AntColony.World
{
    public enum SimulationSpeed
    {
        Normal = 1,
        Fast = 2,
        VeryFast = 3
    }

    /// <summary>Read-only simulation time shared by gameplay systems.</summary>
    public interface ISimulationClock
    {
        bool IsRunning { get; }
        SimulationSpeed SelectedSpeed { get; }
        double SpeedMultiplier { get; }
        double DeltaSeconds { get; }
        double DeltaHours { get; }
        double ElapsedSeconds { get; }
        double ElapsedHours { get; }
        int DayNumber { get; }
        int HourOfDay { get; }
        int MinuteOfHour { get; }
    }

    /// <summary>
    /// Deterministic clock state independent of Unity lifecycle callbacks. The
    /// controller below is the only component that advances this state in play.
    /// </summary>
    public sealed class SimulationClockState : ISimulationClock
    {
        public const double HoursPerDay = 24d;
        public const double StartingHourOfDay = 8d;

        public SimulationClockState(double simulationHoursPerRealSecond = 1d)
        {
            if (!IsFinitePositive(simulationHoursPerRealSecond))
                throw new ArgumentOutOfRangeException(nameof(simulationHoursPerRealSecond));
            SimulationHoursPerRealSecond = simulationHoursPerRealSecond;
        }

        public double SimulationHoursPerRealSecond { get; }
        public bool IsRunning { get; private set; }
        public SimulationSpeed SelectedSpeed { get; private set; } =
            SimulationSpeed.Normal;
        public double SpeedMultiplier => IsRunning ? (int)SelectedSpeed : 0d;
        public double DeltaSeconds { get; private set; }
        public double DeltaHours { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public double ElapsedHours { get; private set; }
        public int DayNumber => (int)Math.Floor(
            (StartingHourOfDay + ElapsedHours) / HoursPerDay) + 1;
        public int HourOfDay => (int)Math.Floor(
            (StartingHourOfDay + ElapsedHours) % HoursPerDay);
        public int MinuteOfHour => (int)Math.Floor(
            ((StartingHourOfDay + ElapsedHours) % 1d) * 60d);

        public void Pause()
        {
            IsRunning = false;
            ClearDelta();
        }

        public void Resume()
        {
            IsRunning = true;
            ClearDelta();
        }

        public void SetRunningSpeed(SimulationSpeed speed)
        {
            if (!Enum.IsDefined(typeof(SimulationSpeed), speed))
                throw new ArgumentOutOfRangeException(nameof(speed));
            SelectedSpeed = speed;
            IsRunning = true;
            ClearDelta();
        }

        /// <summary>
        /// Advances from real elapsed time. The cap mirrors Unity's bounded
        /// maximumDeltaTime policy and prevents an unbounded catch-up frame.
        /// </summary>
        public void Advance(double unscaledDeltaSeconds, double maximumSimulationDelta)
        {
            ClearDelta();
            if (!IsRunning || !IsFiniteNonNegative(unscaledDeltaSeconds) ||
                !IsFinitePositive(maximumSimulationDelta)) return;

            DeltaSeconds = Math.Min(
                unscaledDeltaSeconds * (int)SelectedSpeed,
                maximumSimulationDelta);
            DeltaHours = DeltaSeconds * SimulationHoursPerRealSecond;
            ElapsedSeconds += DeltaSeconds;
            ElapsedHours += DeltaHours;
        }

        /// <summary>Explicit Developer Mode time jump used by existing test tools.</summary>
        public bool TryAdvanceDevelopmentHours(double hours)
        {
            if (!IsFiniteNonNegative(hours)) return false;
            ElapsedHours += hours;
            ElapsedSeconds += hours / SimulationHoursPerRealSecond;
            ClearDelta();
            return true;
        }

        public void Reset()
        {
            IsRunning = false;
            SelectedSpeed = SimulationSpeed.Normal;
            ElapsedSeconds = 0d;
            ElapsedHours = 0d;
            ClearDelta();
        }

        private void ClearDelta()
        {
            DeltaSeconds = 0d;
            DeltaHours = 0d;
        }

        private static bool IsFinitePositive(double value) =>
            value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool IsFiniteNonNegative(double value) =>
            value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value);
    }

}
