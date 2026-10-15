using System;
using System.Linq;
using AntColony.Ants;
using NUnit.Framework;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused plain-data tests for Milestone 3.1C.</summary>
    public sealed class AntSkillTests
    {
        private static readonly AntSkillProgressionSettings TestSettings =
            new(
                baseExperiencePerLevel: 100d,
                additionalExperiencePerLevel: 10d,
                inactivityGraceHours: 10d,
                decayLevelsPerHour: 1d);

        [Test]
        public void NewAnts_StartWithEveryApprovedSkillAtZero()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(ant.Skills.All.Count, Is.EqualTo(7));
            Assert.That(ant.Skills.All.All(skill => skill.Level == 0), Is.True);
            Assert.That(ant.Skills.All.All(
                skill => skill.TotalExperience == 0d), Is.True);
        }

        [Test]
        public void ApprovedSkills_HaveUniqueStableIdentifiers()
        {
            string[] identifiers = AntSkillCatalog.All
                .Select(skill => skill.StableId)
                .ToArray();

            Assert.That(identifiers, Is.EquivalentTo(new[]
            {
                "extraction",
                "surface_preparation",
                "construction",
                "hauling",
                "foraging",
                "combat",
                "brood_care"
            }));
            Assert.That(identifiers.Distinct().Count(), Is.EqualTo(7));
        }

        [Test]
        public void QueenAndWorker_UseTheSameSkillSystem()
        {
            AntRecord queen = CreateAnt("queen", AntCaste.Queen);
            AntRecord worker = CreateAnt("worker", AntCaste.Worker);

            Assert.That(queen.Skills, Is.TypeOf<AntSkillSet>());
            Assert.That(worker.Skills, Is.TypeOf<AntSkillSet>());
            Assert.That(queen.Skills.All.Select(skill => skill.Definition.Id),
                Is.EqualTo(worker.Skills.All.Select(skill => skill.Definition.Id)));
        }

        [Test]
        public void Experience_ProgressesOnlyTheRequestedSkill()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(Award(ant, AntSkillId.Excavation, 150d, 1d, 0d,
                out AntSkillExperienceResult result), Is.True);
            Assert.That(result.AwardedExperience, Is.EqualTo(150d));
            Assert.That(result.PreviousLevel, Is.Zero);
            Assert.That(result.CurrentLevel, Is.EqualTo(1));
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).Level, Is.EqualTo(1));
            Assert.That(ant.Skills.Get(AntSkillId.Construction).Level, Is.Zero);
        }

        [Test]
        public void LegacyExcavationKeySharesTheAuthoritativeExtractionState()
        {
            AntRecord ant = CreateAnt("legacy-worker", AntCaste.Worker);
#pragma warning disable CS0618
            Assert.That(Award(ant, AntSkillId.Excavation, 75d, 1d, 0d,
                out _), Is.True);
            Assert.That(ant.Skills.Get(AntSkillId.Excavation),
                Is.SameAs(ant.Skills.Get(AntSkillId.Extraction)));
#pragma warning restore CS0618
            Assert.That(ant.Skills.Get(AntSkillId.Extraction).TotalExperience,
                Is.EqualTo(75d));
            Assert.That(ant.Skills.All.Count, Is.EqualTo(7),
                "The legacy name must not create a second skill or duplicate XP.");
        }

        [Test]
        public void ZeroExperience_ProducesNoProgressionButIsSafe()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(Award(ant, AntSkillId.Hauling, 0d, 1d, 5d,
                out AntSkillExperienceResult result), Is.True);
            Assert.That(result.AwardedExperience, Is.Zero);
            Assert.That(ant.Skills.Get(AntSkillId.Hauling).Level, Is.Zero);
            Assert.That(ant.Skills.Get(AntSkillId.Hauling).LastPractisedHours,
                Is.EqualTo(5d));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidBaseExperience_IsRejected(double experience)
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(Award(ant, AntSkillId.Combat, experience, 1d, 0d, out _),
                Is.False);
            Assert.That(ant.Skills.Get(AntSkillId.Combat).TotalExperience, Is.Zero);
        }

        [Test]
        public void ProvisionalCurve_ProducesDeterministicLevelsAndProgress()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(Award(ant, AntSkillId.Foraging, 155d, 1d, 0d, out _),
                Is.True);
            AntSkillState skill = ant.Skills.Get(AntSkillId.Foraging);
            Assert.That(skill.Level, Is.EqualTo(1));
            Assert.That(skill.ExperienceIntoLevel, Is.EqualTo(55d));
            Assert.That(skill.ExperienceForNextLevel, Is.EqualTo(110d));
            Assert.That(skill.ProgressToNextLevel, Is.EqualTo(0.5d).Within(0.000001d));
        }

        [Test]
        public void Experience_IsCappedSafelyAtLevelOneHundred()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(Award(
                ant,
                AntSkillId.Construction,
                double.MaxValue,
                2d,
                0d,
                out AntSkillExperienceResult first), Is.True);
            Assert.That(first.ReachedMaximumLevel, Is.True);
            Assert.That(first.AwardedExperience, Is.EqualTo(
                TestSettings.TotalExperienceForLevel(100)));
            Assert.That(Award(ant, AntSkillId.Construction, 100d, 1d, 0d,
                out AntSkillExperienceResult second), Is.True);
            Assert.That(second.AwardedExperience, Is.Zero);
            Assert.That(second.CurrentLevel, Is.EqualTo(100));
        }

        [Test]
        public void ExperienceModifiers_ApplyAndZeroPreventsGain()
        {
            AntRecord normal = CreateAnt("normal", AntCaste.Worker);
            AntRecord increased = CreateAnt("increased", AntCaste.Worker);
            AntRecord zero = CreateAnt("zero", AntCaste.Worker);

            Assert.That(Award(normal, AntSkillId.BroodCare, 100d, 1d, 0d,
                out AntSkillExperienceResult normalResult), Is.True);
            Assert.That(Award(increased, AntSkillId.BroodCare, 100d, 1.5d, 0d,
                out AntSkillExperienceResult increasedResult), Is.True);
            Assert.That(Award(zero, AntSkillId.BroodCare, 100d, 0d, 0d,
                out AntSkillExperienceResult zeroResult), Is.True);

            Assert.That(normalResult.AwardedExperience, Is.EqualTo(100d));
            Assert.That(increasedResult.AwardedExperience, Is.EqualTo(150d));
            Assert.That(zeroResult.AwardedExperience, Is.Zero);
        }

        [TestCase(-0.01d)]
        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        public void InvalidExperienceModifiers_AreRejected(double multiplier)
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);

            Assert.That(Award(ant, AntSkillId.Excavation, 100d, multiplier, 0d,
                out _), Is.False);
            Assert.That(ant.Skills.Get(AntSkillId.Excavation).TotalExperience,
                Is.Zero);
        }

        [Test]
        public void InactivityDecay_AffectsOnlyValuesAboveFifty()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);
            SetExactLevel(ant, AntSkillId.Excavation, 80);
            SetExactLevel(ant, AntSkillId.Hauling, 50);
            SetExactLevel(ant, AntSkillId.Foraging, 25);

            Assert.That(ant.TryAdvanceSkillDecay(20d, out _), Is.True);

            AssertExactLevel(ant, AntSkillId.Excavation, 70d);
            AssertExactLevel(ant, AntSkillId.Hauling, 50d);
            AssertExactLevel(ant, AntSkillId.Foraging, 25d);
        }

        [Test]
        public void InactivityDecay_NeverCrossesBelowFifty()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);
            SetExactLevel(ant, AntSkillId.SurfacePreparation, 51);

            Assert.That(ant.TryAdvanceSkillDecay(1000d, out _), Is.True);

            AssertExactLevel(ant, AntSkillId.SurfacePreparation, 50d);
        }

        [Test]
        public void Practice_ResetsOnlyThatSkillsInactivityClock()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);
            SetExactLevel(ant, AntSkillId.Excavation, 80);
            SetExactLevel(ant, AntSkillId.Construction, 80);
            Assert.That(ant.TryAdvanceSkillDecay(20d, out _), Is.True);
            Assert.That(Award(ant, AntSkillId.Excavation, 0d, 1d, 20d, out _),
                Is.True);

            Assert.That(ant.TryAdvanceSkillDecay(25d, out _), Is.True);

            AssertExactLevel(ant, AntSkillId.Excavation, 70d);
            AssertExactLevel(ant, AntSkillId.Construction, 65d);
        }

        [Test]
        public void EquivalentElapsedTime_ProducesConsistentDecay()
        {
            AntRecord oneStep = CreateAnt("one-step", AntCaste.Worker);
            AntRecord manySteps = CreateAnt("many-steps", AntCaste.Worker);
            SetExactLevel(oneStep, AntSkillId.Excavation, 90);
            SetExactLevel(manySteps, AntSkillId.Excavation, 90);

            Assert.That(oneStep.TryAdvanceSkillDecay(30d, out _), Is.True);
            for (int hour = 1; hour <= 30; hour++)
            {
                Assert.That(manySteps.TryAdvanceSkillDecay(hour, out _), Is.True);
            }

            Assert.That(
                oneStep.Skills.Get(AntSkillId.Excavation).TotalExperience,
                Is.EqualTo(manySteps.Skills.Get(AntSkillId.Excavation)
                    .TotalExperience).Within(0.000001d));
        }

        [Test]
        public void DifferentAnts_MaintainIndependentSkillState()
        {
            AntRecord first = CreateAnt("first", AntCaste.Worker);
            AntRecord second = CreateAnt("second", AntCaste.Worker);

            Assert.That(Award(first, AntSkillId.Hauling, 500d, 1d, 0d, out _),
                Is.True);

            Assert.That(first.Skills.Get(AntSkillId.Hauling).Level,
                Is.GreaterThan(0));
            Assert.That(second.Skills.Get(AntSkillId.Hauling).Level, Is.Zero);
        }

        [Test]
        public void DeceasedAnts_RejectExperienceAndDecay()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);
            SetExactLevel(ant, AntSkillId.Combat, 80);
            double experience = ant.Skills.Get(AntSkillId.Combat).TotalExperience;
            ant.MarkDeceased();

            Assert.That(Award(ant, AntSkillId.Combat, 100d, 1d, 20d, out _),
                Is.False);
            Assert.That(ant.TryAdvanceSkillDecay(20d, out _), Is.False);
            Assert.That(ant.Skills.Get(AntSkillId.Combat).TotalExperience,
                Is.EqualTo(experience));
        }

        [Test]
        public void ResetSkills_RestoresDeterministicZeroState()
        {
            AntRecord ant = CreateAnt("worker", AntCaste.Worker);
            Assert.That(Award(ant, AntSkillId.Excavation, 1000d, 1d, 0d, out _),
                Is.True);

            ant.ResetSkills(5d);

            Assert.That(ant.Skills.All.All(skill => skill.Level == 0), Is.True);
            Assert.That(ant.Skills.All.All(
                skill => skill.LastPractisedHours == 5d), Is.True);
        }

        private static AntRecord CreateAnt(string id, AntCaste caste)
        {
            return new AntRecord(
                new AntId(id),
                id,
                new ColonyId("test-colony"),
                caste,
                new AntAttributes(5, 5, 5),
                TestSettings,
                startingSimulationHours: 0d);
        }

        private static bool Award(
            AntRecord ant,
            AntSkillId skill,
            double experience,
            double multiplier,
            double simulationHours,
            out AntSkillExperienceResult result)
        {
            return ant.TryAwardSkillExperience(
                skill,
                experience,
                multiplier,
                simulationHours,
                out result,
                out _);
        }

        private static void SetExactLevel(
            AntRecord ant,
            AntSkillId skill,
            int level)
        {
            double experience = TestSettings.TotalExperienceForLevel(level);
            Assert.That(Award(ant, skill, experience, 1d, 0d, out _), Is.True);
        }

        private static void AssertExactLevel(
            AntRecord ant,
            AntSkillId skill,
            double expected)
        {
            double exact = TestSettings.GetExactLevel(
                ant.Skills.Get(skill).TotalExperience);
            Assert.That(exact, Is.EqualTo(expected).Within(0.000001d));
        }
    }
}
