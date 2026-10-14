using System;
using AntColony.World;
using NUnit.Framework;

namespace AntColony.Tests.EditMode
{
    public sealed class SimulationClockTests
    {
        [Test]
        public void NewClock_StartsPausedAtDayOneEightAm()
        {
            var clock = new SimulationClockState();

            Assert.That(clock.IsRunning, Is.False);
            Assert.That(clock.SelectedSpeed, Is.EqualTo(SimulationSpeed.Normal));
            Assert.That(clock.DayNumber, Is.EqualTo(1));
            Assert.That(clock.HourOfDay, Is.EqualTo(8));
            Assert.That(clock.MinuteOfHour, Is.EqualTo(0));
        }

        [TestCase(SimulationSpeed.Normal, 1d)]
        [TestCase(SimulationSpeed.Fast, 2d)]
        [TestCase(SimulationSpeed.VeryFast, 3d)]
        public void Advance_UsesSelectedSpeed(
            SimulationSpeed speed,
            double expectedHours)
        {
            var clock = new SimulationClockState();
            clock.SetRunningSpeed(speed);

            clock.Advance(1d, 10d);

            Assert.That(clock.ElapsedHours, Is.EqualTo(expectedHours));
            Assert.That(clock.DeltaHours, Is.EqualTo(expectedHours));
        }

        [Test]
        public void Pause_RetainsSelectedSpeedAndClearsDelta()
        {
            var clock = new SimulationClockState();
            clock.SetRunningSpeed(SimulationSpeed.VeryFast);
            clock.Advance(1d, 10d);

            clock.Pause();
            clock.Advance(5d, 10d);

            Assert.That(clock.SelectedSpeed, Is.EqualTo(SimulationSpeed.VeryFast));
            Assert.That(clock.ElapsedHours, Is.EqualTo(3d));
            Assert.That(clock.DeltaHours, Is.Zero);
        }

        [Test]
        public void Resume_UsesRetainedSpeed()
        {
            var clock = new SimulationClockState();
            clock.SetRunningSpeed(SimulationSpeed.Fast);
            clock.Pause();

            clock.Resume();
            clock.Advance(1d, 10d);

            Assert.That(clock.ElapsedHours, Is.EqualTo(2d));
        }

        [Test]
        public void Advance_IsBoundedByMaximumSimulationDelta()
        {
            var clock = new SimulationClockState();
            clock.SetRunningSpeed(SimulationSpeed.VeryFast);

            clock.Advance(2d, 0.5d);

            Assert.That(clock.DeltaSeconds, Is.EqualTo(0.5d));
            Assert.That(clock.ElapsedHours, Is.EqualTo(0.5d));
        }

        [Test]
        public void ElapsedTime_RollsIntoNextDay()
        {
            var clock = new SimulationClockState();

            Assert.That(clock.TryAdvanceDevelopmentHours(16.5d), Is.True);

            Assert.That(clock.DayNumber, Is.EqualTo(2));
            Assert.That(clock.HourOfDay, Is.EqualTo(0));
            Assert.That(clock.MinuteOfHour, Is.EqualTo(30));
        }

        [Test]
        public void DevelopmentAdvance_RejectsInvalidValues()
        {
            var clock = new SimulationClockState();

            Assert.That(clock.TryAdvanceDevelopmentHours(-1d), Is.False);
            Assert.That(clock.TryAdvanceDevelopmentHours(double.NaN), Is.False);
            Assert.That(clock.TryAdvanceDevelopmentHours(double.PositiveInfinity), Is.False);
            Assert.That(clock.ElapsedHours, Is.Zero);
        }

        [Test]
        public void InvalidSpeed_IsRejected()
        {
            var clock = new SimulationClockState();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                clock.SetRunningSpeed((SimulationSpeed)99));
        }

        [Test]
        public void Reset_RestoresInitialPausedState()
        {
            var clock = new SimulationClockState();
            clock.SetRunningSpeed(SimulationSpeed.VeryFast);
            clock.Advance(3d, 10d);

            clock.Reset();

            Assert.That(clock.IsRunning, Is.False);
            Assert.That(clock.SelectedSpeed, Is.EqualTo(SimulationSpeed.Normal));
            Assert.That(clock.ElapsedHours, Is.Zero);
            Assert.That(clock.DayNumber, Is.EqualTo(1));
            Assert.That(clock.HourOfDay, Is.EqualTo(8));
        }
    }
}
