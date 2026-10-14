using System;
using AntColony.Ants;
using NUnit.Framework;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused deterministic checks for Milestone 3.2A.</summary>
    public sealed class AntNeedsTests
    {
        [TestCase(AntCaste.Queen)]
        [TestCase(AntCaste.Worker)]
        public void NewAnts_StartWithValidDeterministicNeeds(AntCaste caste)
        {
            AntRecord ant = CreateAnt(caste.ToString(), caste);

            Assert.That(ant.Needs.Health, Is.EqualTo(100d));
            Assert.That(ant.Needs.Hunger, Is.EqualTo(100d));
            Assert.That(ant.Needs.Thirst, Is.EqualTo(100d));
            Assert.That(ant.Needs.Fatigue, Is.EqualTo(20d));
            Assert.That(ant.Needs.HasCompletedRest, Is.False);
            Assert.That(ant.Needs.WorkSpeedMultiplier, Is.EqualTo(1d));
            Assert.That(ant.Needs.SkillExperienceMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void Needs_AreIndependentlyOwned()
        {
            AntRecord first = CreateAnt("first");
            AntRecord second = CreateAnt("second");

            Assert.That(first.TryAdvanceNeeds(5d, out _), Is.True);

            Assert.That(first.Needs.Hunger, Is.EqualTo(95d));
            Assert.That(second.Needs.Hunger, Is.EqualTo(100d));
            Assert.That(first.Needs, Is.Not.SameAs(second.Needs));
        }

        [Test]
        public void AwakeSimulation_DepletesHungerAndThirstAndAddsFatigue()
        {
            AntRecord ant = CreateAnt("worker");

            Assert.That(ant.TryAdvanceNeeds(5d, out _), Is.True);

            Assert.That(ant.Needs.Hunger, Is.EqualTo(95d));
            Assert.That(ant.Needs.Thirst, Is.EqualTo(93d));
            Assert.That(ant.Needs.Fatigue, Is.EqualTo(27d));
        }

        [Test]
        public void Resting_ReducesFatigueAndUnlocksRestedState()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(SetNeeds(ant, fatigue: 100d), Is.True);

            Assert.That(ant.TryRest(8d, out _), Is.True);

            Assert.That(ant.Needs.Fatigue, Is.Zero);
            Assert.That(ant.Needs.HasCompletedRest, Is.True);
            Assert.That(ant.Needs.WorkSpeedMultiplier, Is.EqualTo(1.15d)
                .Within(0.000001d));
        }

        [Test]
        public void Values_ClampAtTheirAllowedBounds()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(SetNeeds(ant, health: 5d, hunger: 5d, thirst: 5d,
                fatigue: 95d), Is.True);

            Assert.That(ant.TryAdvanceNeeds(1000d, out _), Is.True);

            Assert.That(ant.Needs.Health, Is.Zero);
            Assert.That(ant.Needs.Hunger, Is.Zero);
            Assert.That(ant.Needs.Thirst, Is.Zero);
            Assert.That(ant.Needs.Fatigue, Is.EqualTo(100d));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedTime_IsRejected(double elapsedHours)
        {
            AntRecord ant = CreateAnt("worker");

            Assert.That(ant.TryAdvanceNeeds(elapsedHours, out _), Is.False);
            Assert.That(ant.TryRest(elapsedHours, out _), Is.False);
            Assert.That(ant.Needs.Hunger, Is.EqualTo(100d));
        }

        [Test]
        public void EquivalentElapsedTime_ProducesConsistentNeedsAndDamage()
        {
            AntRecord oneStep = CreateAnt("one-step");
            AntRecord manySteps = CreateAnt("many-steps");

            Assert.That(oneStep.TryAdvanceNeeds(50d, out _), Is.True);
            for (int hour = 0; hour < 50; hour++)
            {
                Assert.That(manySteps.TryAdvanceNeeds(1d, out _), Is.True);
            }

            AssertNeedsEqual(oneStep, manySteps);
        }

        [Test]
        public void Starvation_CausesGradualHealthLossOnlyAfterThreshold()
        {
            AntRecord ant = CreateAnt("worker");

            Assert.That(ant.TryAdvanceNeeds(90d, out _), Is.True);
            Assert.That(ant.Needs.Hunger, Is.EqualTo(10d));
            Assert.That(ant.Needs.Health, Is.EqualTo(100d));
            Assert.That(ant.TryAdvanceNeeds(2d, out _), Is.True);
            Assert.That(ant.Needs.Health, Is.EqualTo(96d));
        }

        [Test]
        public void ReplenishingHunger_StopsFurtherStarvationDamage()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(SetNeeds(ant, hunger: 0d), Is.True);
            Assert.That(ant.TryAdvanceNeeds(2d, out _), Is.True);
            double damagedHealth = ant.Needs.Health;
            Assert.That(SetNeeds(ant, health: damagedHealth, hunger: 100d,
                thirst: ant.Needs.Thirst, fatigue: ant.Needs.Fatigue), Is.True);

            Assert.That(ant.TryAdvanceNeeds(2d, out _), Is.True);

            Assert.That(ant.Needs.Health, Is.EqualTo(damagedHealth));
        }

        [Test]
        public void Dehydration_BlocksSkillExperienceWithoutErasingExistingXp()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(Award(ant, 100d, 1d, out _), Is.True);
            double earned = ant.Skills.Get(AntSkillId.Excavation).TotalExperience;
            Assert.That(SetNeeds(ant, thirst: 0d), Is.True);

            Assert.That(Award(ant, 100d, 1d, out AntSkillExperienceResult result),
                Is.True);

            Assert.That(result.AwardedExperience, Is.Zero);
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).TotalExperience,
                Is.EqualTo(earned));
        }

        [Test]
        public void RestoringThirst_AllowsSkillExperienceAgain()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(SetNeeds(ant, thirst: 0d), Is.True);
            Assert.That(Award(ant, 100d, 1d, out _), Is.True);
            Assert.That(SetNeeds(ant, thirst: 100d), Is.True);

            Assert.That(Award(ant, 100d, 1d, out AntSkillExperienceResult result),
                Is.True);

            Assert.That(result.AwardedExperience, Is.EqualTo(100d));
        }

        [Test]
        public void Dehydration_DoesNotBypassInvalidMultiplierValidation()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(SetNeeds(ant, thirst: 0d), Is.True);

            Assert.That(Award(ant, 100d, -1d, out _), Is.False);
            Assert.That(Award(ant, 100d, double.NaN, out _), Is.False);
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).TotalExperience,
                Is.Zero);
        }

        [Test]
        public void RestedExperienceAndWorkMultipliers_ApplyAndFade()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(ant.TryRest(1d, out _), Is.True);
            Assert.That(ant.Needs.SkillExperienceMultiplier, Is.EqualTo(1.25d)
                .Within(0.000001d));
            Assert.That(ant.Needs.WorkSpeedMultiplier, Is.EqualTo(1.15d)
                .Within(0.000001d));
            Assert.That(Award(ant, 100d, 1d, out AntSkillExperienceResult result),
                Is.True);
            Assert.That(result.AwardedExperience, Is.EqualTo(125d));

            Assert.That(ant.TryAdvanceNeeds(20d, out _), Is.True);

            Assert.That(ant.Needs.Fatigue, Is.EqualTo(32d));
            Assert.That(ant.Needs.RestedStrength, Is.GreaterThan(0d)
                .And.LessThan(1d));
            Assert.That(ant.Needs.SkillExperienceMultiplier, Is.GreaterThan(1d)
                .And.LessThan(1.25d));
        }

        [Test]
        public void HighFatigue_RemovesBonusesAndRestCanRestoreThem()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(ant.TryRest(1d, out _), Is.True);
            Assert.That(ant.TryAdvanceNeeds(55d, out _), Is.True);
            Assert.That(ant.Needs.Fatigue, Is.GreaterThanOrEqualTo(80d));
            Assert.That(ant.Needs.WorkSpeedMultiplier, Is.EqualTo(1d));
            Assert.That(ant.Needs.SkillExperienceMultiplier, Is.EqualTo(1d));

            Assert.That(ant.TryRest(8d, out _), Is.True);

            Assert.That(ant.Needs.WorkSpeedMultiplier, Is.EqualTo(1.15d)
                .Within(0.000001d));
            Assert.That(ant.Needs.SkillExperienceMultiplier, Is.EqualTo(1.25d)
                .Within(0.000001d));
        }

        [Test]
        public void DifferentAnts_HaveIndependentRestedEffects()
        {
            AntRecord rested = CreateAnt("rested");
            AntRecord awake = CreateAnt("awake");

            Assert.That(rested.TryRest(1d, out _), Is.True);

            Assert.That(rested.Needs.WorkSpeedMultiplier, Is.EqualTo(1.15d)
                .Within(0.000001d));
            Assert.That(awake.Needs.WorkSpeedMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void InactiveAndDeceasedAnts_DoNotSimulateNeeds()
        {
            AntRecord inactive = CreateAnt("inactive");
            AntRecord deceased = CreateAnt("deceased");
            inactive.MarkInactive();
            deceased.MarkDeceased();

            Assert.That(inactive.TryAdvanceNeeds(1d, out _), Is.False);
            Assert.That(deceased.TryAdvanceNeeds(1d, out _), Is.False);
            Assert.That(inactive.TryRest(1d, out _), Is.False);
            Assert.That(deceased.TryRest(1d, out _), Is.False);
            Assert.That(inactive.Needs.Hunger, Is.EqualTo(100d));
            Assert.That(deceased.Needs.Hunger, Is.EqualTo(100d));
        }

        [Test]
        public void DepletedHealth_PausesWithoutInventingDeathLifecycle()
        {
            AntRecord ant = CreateAnt("worker");
            Assert.That(SetNeeds(ant, health: 0d), Is.True);

            Assert.That(ant.TryAdvanceNeeds(1d, out string error), Is.False);

            Assert.That(error, Does.Contain("Health is depleted"));
            Assert.That(ant.LifecycleStatus, Is.EqualTo(AntLifecycleStatus.Active));
            Assert.That(ant.Needs.Health, Is.Zero);
        }

        [Test]
        public void InvalidDevelopmentValues_AreRejectedWithoutMutation()
        {
            AntRecord ant = CreateAnt("worker");

            Assert.That(ant.TrySetNeedsForDevelopment(
                100d, -1d, double.NaN, 20d, out _), Is.False);

            Assert.That(ant.Needs.Health, Is.EqualTo(100d));
            Assert.That(ant.Needs.Hunger, Is.EqualTo(100d));
            Assert.That(ant.Needs.Thirst, Is.EqualTo(100d));
            Assert.That(ant.Needs.Fatigue, Is.EqualTo(20d));
        }

        private static AntRecord CreateAnt(
            string id,
            AntCaste caste = AntCaste.Worker)
        {
            return new AntRecord(
                new AntId(id),
                id,
                new ColonyId("test-colony"),
                caste,
                new AntAttributes(5, 5, 5));
        }

        private static bool SetNeeds(
            AntRecord ant,
            double health = 100d,
            double hunger = 100d,
            double thirst = 100d,
            double fatigue = 20d)
        {
            return ant.TrySetNeedsForDevelopment(
                health,
                hunger,
                thirst,
                fatigue,
                out _);
        }

        private static bool Award(
            AntRecord ant,
            double baseExperience,
            double multiplier,
            out AntSkillExperienceResult result)
        {
            return ant.TryAwardSkillExperience(
                AntSkillId.Excavation,
                baseExperience,
                multiplier,
                0d,
                out result,
                out _);
        }

        private static void AssertNeedsEqual(AntRecord first, AntRecord second)
        {
            Assert.That(first.Needs.Health,
                Is.EqualTo(second.Needs.Health).Within(0.000001d));
            Assert.That(first.Needs.Hunger,
                Is.EqualTo(second.Needs.Hunger).Within(0.000001d));
            Assert.That(first.Needs.Thirst,
                Is.EqualTo(second.Needs.Thirst).Within(0.000001d));
            Assert.That(first.Needs.Fatigue,
                Is.EqualTo(second.Needs.Fatigue).Within(0.000001d));
        }
    }
}
