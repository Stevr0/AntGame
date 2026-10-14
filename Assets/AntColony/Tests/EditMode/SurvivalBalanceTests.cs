using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Deterministic balance projections for Mara, Pip and Tala. This is not a
    /// second gameplay simulation: it drives the production needs/decision APIs
    /// and uses the authored scene's real path costs to make tuning measurable.
    /// </summary>
    public sealed class SurvivalBalanceTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private const double ProjectionHours = 12d * 24d;
        private const double StepHours = 0.25d;

        [TearDown]
        public void TearDown() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void ProvisionalValues_MatchReviewedBalanceWithoutChangingCriticalRules()
        {
            AntNeedsSimulationSettings needs = AntNeedsSimulationSettings.Provisional;
            AutonomousSurvivalSettings decisions = AutonomousSurvivalSettings.Provisional;

            Assert.That(needs.HungerLossPerHour, Is.EqualTo(1d));
            Assert.That(needs.ThirstLossPerHour, Is.EqualTo(1.4d));
            Assert.That(needs.AwakeFatigueGainPerHour, Is.EqualTo(1.4d));
            Assert.That(needs.RestingFatigueRecoveryPerHour, Is.EqualTo(16d));
            Assert.That(decisions.SeekHungerAtOrBelow, Is.EqualTo(40d));
            Assert.That(decisions.SeekThirstAtOrBelow, Is.EqualTo(40d));
            Assert.That(decisions.SeekFatigueAtOrAbove, Is.EqualTo(70d));
            Assert.That(SurvivalInteractionOrder.ConsumptionDurationHours,
                Is.EqualTo(0.5d));

            Assert.That(needs.CriticalHungerThreshold, Is.EqualTo(10d));
            Assert.That(needs.CriticalThirstThreshold, Is.EqualTo(10d));
            Assert.That(needs.StarvationHealthLossPerHour, Is.EqualTo(2d));
            Assert.That(decisions.CriticalHungerAtOrBelow, Is.EqualTo(10d));
            Assert.That(decisions.CriticalThirstAtOrBelow, Is.EqualTo(10d));
            Assert.That(decisions.CriticalFatigueAtOrAbove, Is.EqualTo(90d));
            Assert.That(SurvivalInteractionOrder.HungerRestoredPerUnit, Is.EqualTo(40d));
            Assert.That(SurvivalInteractionOrder.ThirstRestoredPerUnit, Is.EqualTo(50d));
        }

        [Test]
        public void TwelveDayProjection_ImprovesProductiveShareAndKeepsNeedsSafe()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            ProjectionMetrics baseline = ProjectColony(
                host, BaselineNeeds(), BaselineDecisions(), 100, 100);
            ProjectionMetrics revised = ProjectColony(
                host, AntNeedsSimulationSettings.Provisional,
                AutonomousSurvivalSettings.Provisional, 8, 10);

            TestContext.WriteLine($"SURVIVAL_BALANCE_BASELINE {baseline}");
            TestContext.WriteLine($"SURVIVAL_BALANCE_REVISED {revised}");

            Assert.That(revised.ProductiveShare, Is.InRange(0.70d, 0.80d));
            Assert.That(revised.ProductiveShare,
                Is.GreaterThan(baseline.ProductiveShare + 0.20d));
            Assert.That(revised.BlockedRejectedHours, Is.Zero);
            Assert.That(revised.MinimumHunger, Is.GreaterThan(10d));
            Assert.That(revised.MinimumThirst, Is.GreaterThan(10d));
            Assert.That(revised.MaximumFatigue, Is.LessThan(90d));
            Assert.That(revised.FoodConsumed, Is.LessThanOrEqualTo(24));
            Assert.That(revised.WaterConsumed, Is.LessThanOrEqualTo(30));
        }

        [Test]
        public void OriginalFixtureQuantities_ExhaustDuringTwelveDayProjection()
        {
            AntGameDevelopmentWorldHost host = OpenHost();
            // The old 12 food / 16 water totals divide to 4 food and at most
            // 6 water per ant. The revised finite 24 / 32 totals remain bounded.
            ProjectionMetrics oldSupply = ProjectColony(
                host, AntNeedsSimulationSettings.Provisional,
                AutonomousSurvivalSettings.Provisional, 4, 5);

            Assert.That(oldSupply.BlockedRejectedHours, Is.GreaterThan(0d));
            Assert.That(2 * AntGameDevelopmentWorldHost.ReachableFoodQuantityPerFixture,
                Is.EqualTo(24));
            Assert.That(2 * AntGameDevelopmentWorldHost.ReachableWaterQuantityPerFixture,
                Is.EqualTo(32));
        }

        private static ProjectionMetrics ProjectColony(
            AntGameDevelopmentWorldHost host,
            AntNeedsSimulationSettings needs,
            AutonomousSurvivalSettings decisions,
            int foodPerAnt,
            int waterPerAnt)
        {
            var total = new ProjectionMetrics();
            Vector2Int[] representativeWorkCells =
            {
                new(-4, SampleWorldGenerator.MainPassageY + 1),
                new(4, SampleWorldGenerator.MainPassageY + 1),
                new(6, SampleWorldGenerator.MainPassageY + 1)
            };
            for (int index = 0; index < host.SelectableGridAnts.Count; index++)
            {
                // These are valid work anchors beside the two authored small
                // excavation areas, rather than the one-time spawn positions.
                Vector2Int home = representativeWorkCells[index];
                Assert.That(host.GridNavigationMap.TryGetAnchor(home, out _), Is.True);
                SelectableGridAnt selectable =
                    host.SelectionController.SelectableAnts[index];
                var travel = new Dictionary<SurvivalInteractionKind, double>();
                var service = new AutonomousSurvivalDecisionService(decisions);
                foreach (SurvivalInteractionKind kind in Enum.GetValues(
                    typeof(SurvivalInteractionKind)))
                {
                    Assert.That(service.TryChooseTarget(
                        host.SurvivalInteractionWorld, host.GridNavigationMap,
                        home, kind, out Vector2Int target, out string chooseError),
                        Is.True, chooseError);
                    Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                        host.GridNavigationMap, home, target, out _,
                        out float cost, out string pathError), Is.True, pathError);
                    travel[kind] = cost;
                }

                AntRecord record = new(
                    new AntId($"balance-{index}"), selectable.Record.DisplayName,
                    new ColonyId("balance-colony"), AntCaste.Worker,
                    new AntAttributes(5, 5, 5),
                    AntSkillProgressionSettings.Provisional, needs, 0d);
                total.Add(ProjectAnt(record, service, travel, foodPerAnt, waterPerAnt));
            }
            return total;
        }

        private static ProjectionMetrics ProjectAnt(
            AntRecord ant,
            AutonomousSurvivalDecisionService service,
            IReadOnlyDictionary<SurvivalInteractionKind, double> travel,
            int food,
            int water)
        {
            var metrics = new ProjectionMetrics();
            ProjectionPhase phase = ProjectionPhase.ProductiveWork;
            SurvivalInteractionKind intent = default;
            double remaining = 0d;

            for (double elapsed = 0d; elapsed < ProjectionHours; elapsed += StepHours)
            {
                metrics.Observe(ant.Needs);
                switch (phase)
                {
                    case ProjectionPhase.ProductiveWork:
                        AutonomousSurvivalDecision decision = service.Evaluate(ant.Needs);
                        if (!decision.HasDecision)
                        {
                            metrics.ProductiveWorkHours += StepHours;
                            ant.TryAdvanceNeeds(StepHours, out _);
                            break;
                        }
                        intent = decision.Kind;
                        remaining = travel[intent];
                        phase = ProjectionPhase.SurvivalTravel;
                        goto case ProjectionPhase.SurvivalTravel;

                    case ProjectionPhase.SurvivalTravel:
                        metrics.SurvivalTravelHours += StepHours;
                        ant.TryAdvanceNeeds(StepHours, out _);
                        remaining -= StepHours;
                        if (remaining <= 0d)
                        {
                            remaining = intent == SurvivalInteractionKind.Rest
                                ? double.PositiveInfinity
                                : SurvivalInteractionOrder.ConsumptionDurationHours;
                            phase = ProjectionPhase.Interaction;
                        }
                        break;

                    case ProjectionPhase.Interaction:
                        if (intent == SurvivalInteractionKind.Rest)
                        {
                            metrics.RestingHours += StepHours;
                            ant.TryRest(StepHours, out _);
                            if (ant.Needs.Fatigue <=
                                ant.Needs.Settings.RestedQualificationFatigue)
                                BeginReturnTravel();
                            break;
                        }

                        if (intent == SurvivalInteractionKind.Eat)
                            metrics.EatingHours += StepHours;
                        else
                            metrics.DrinkingHours += StepHours;
                        ant.TryAdvanceNeeds(StepHours, out _);
                        remaining -= StepHours;
                        if (remaining > 0d) break;

                        bool available = intent == SurvivalInteractionKind.Eat
                            ? food > 0 : water > 0;
                        if (!available)
                        {
                            metrics.BlockedRejectedHours += StepHours;
                            remaining = service.Settings.RetryBackoffSeconds;
                            phase = ProjectionPhase.Waiting;
                            break;
                        }
                        if (intent == SurvivalInteractionKind.Eat)
                        {
                            food--;
                            metrics.FoodConsumed++;
                            ant.TryEat(SurvivalInteractionOrder.HungerRestoredPerUnit, out _);
                        }
                        else
                        {
                            water--;
                            metrics.WaterConsumed++;
                            ant.TryDrink(SurvivalInteractionOrder.ThirstRestoredPerUnit, out _);
                        }
                        BeginReturnTravel();
                        break;

                    case ProjectionPhase.WorkTravel:
                        metrics.WorkTravelHours += StepHours;
                        ant.TryAdvanceNeeds(StepHours, out _);
                        remaining -= StepHours;
                        if (remaining <= 0d) phase = ProjectionPhase.ProductiveWork;
                        break;

                    case ProjectionPhase.Waiting:
                        metrics.IdleWaitingHours += StepHours;
                        ant.TryAdvanceNeeds(StepHours, out _);
                        remaining -= StepHours;
                        if (remaining <= 0d) phase = ProjectionPhase.ProductiveWork;
                        break;
                }
            }

            metrics.Observe(ant.Needs);
            return metrics;

            void BeginReturnTravel()
            {
                remaining = travel[intent];
                phase = ProjectionPhase.WorkTravel;
            }
        }

        private static AntNeedsSimulationSettings BaselineNeeds() => new(
            2d, 3d, 4d, 12d, 10d, 2d, 10d, 20d, 80d, 0.15d, 0.25d, 20d);

        private static AutonomousSurvivalSettings BaselineDecisions() => new(
            seekHungerAtOrBelow: 50d,
            seekThirstAtOrBelow: 50d,
            seekFatigueAtOrAbove: 60d);

        private static AntGameDevelopmentWorldHost OpenHost()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AntGameDevelopmentWorldHost>(true))
                .Single();
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);
            return host;
        }

        private enum ProjectionPhase
        {
            ProductiveWork,
            SurvivalTravel,
            Interaction,
            WorkTravel,
            Waiting
        }

        private sealed class ProjectionMetrics
        {
            public double ProductiveWorkHours;
            public double WorkTravelHours;
            public double EatingHours;
            public double DrinkingHours;
            public double RestingHours;
            public double SurvivalTravelHours;
            public double IdleWaitingHours;
            public double BlockedRejectedHours;
            public int FoodConsumed;
            public int WaterConsumed;
            public double MinimumHunger = 100d;
            public double MinimumThirst = 100d;
            public double MaximumFatigue;

            public double TotalHours => ProductiveWorkHours + WorkTravelHours +
                EatingHours + DrinkingHours + RestingHours + SurvivalTravelHours +
                IdleWaitingHours;
            public double ProductiveShare =>
                (ProductiveWorkHours + WorkTravelHours) / TotalHours;

            public void Observe(AntNeedsState needs)
            {
                MinimumHunger = Math.Min(MinimumHunger, needs.Hunger);
                MinimumThirst = Math.Min(MinimumThirst, needs.Thirst);
                MaximumFatigue = Math.Max(MaximumFatigue, needs.Fatigue);
            }

            public void Add(ProjectionMetrics other)
            {
                ProductiveWorkHours += other.ProductiveWorkHours;
                WorkTravelHours += other.WorkTravelHours;
                EatingHours += other.EatingHours;
                DrinkingHours += other.DrinkingHours;
                RestingHours += other.RestingHours;
                SurvivalTravelHours += other.SurvivalTravelHours;
                IdleWaitingHours += other.IdleWaitingHours;
                BlockedRejectedHours += other.BlockedRejectedHours;
                FoodConsumed += other.FoodConsumed;
                WaterConsumed += other.WaterConsumed;
                MinimumHunger = Math.Min(MinimumHunger, other.MinimumHunger);
                MinimumThirst = Math.Min(MinimumThirst, other.MinimumThirst);
                MaximumFatigue = Math.Max(MaximumFatigue, other.MaximumFatigue);
            }

            public override string ToString() =>
                $"productive={ProductiveWorkHours:F2}h workTravel={WorkTravelHours:F2}h " +
                $"eat={EatingHours:F2}h drink={DrinkingHours:F2}h rest={RestingHours:F2}h " +
                $"survivalTravel={SurvivalTravelHours:F2}h idle={IdleWaitingHours:F2}h " +
                $"blocked={BlockedRejectedHours:F2}h share={ProductiveShare:P1} " +
                $"food={FoodConsumed} water={WaterConsumed} minH={MinimumHunger:F1} " +
                $"minT={MinimumThirst:F1} maxF={MaximumFatigue:F1}";
        }
    }
}
