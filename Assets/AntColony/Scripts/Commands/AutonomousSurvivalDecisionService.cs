using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public readonly struct AutonomousSurvivalDecision
    {
        public AutonomousSurvivalDecision(
            SurvivalInteractionKind kind,
            bool isCritical,
            double urgency)
        {
            Kind = kind;
            IsCritical = isCritical;
            Urgency = urgency;
            HasDecision = true;
        }

        public bool HasDecision { get; }
        public SurvivalInteractionKind Kind { get; }
        public bool IsCritical { get; }
        public double Urgency { get; }
    }

    /// <summary>
    /// Provisional 3.2C balancing values. They live outside AntNeedsState so the
    /// biological data remains authoritative and AI policy can change separately.
    /// </summary>
    public sealed class AutonomousSurvivalSettings
    {
        public static AutonomousSurvivalSettings Provisional { get; } = new();

        public AutonomousSurvivalSettings(
            double seekHungerAtOrBelow = 40d,
            double criticalHungerAtOrBelow = 10d,
            double seekThirstAtOrBelow = 40d,
            double criticalThirstAtOrBelow = 10d,
            double seekFatigueAtOrAbove = 70d,
            double criticalFatigueAtOrAbove = 90d,
            double evaluationIntervalSeconds = 0.5d,
            double retryBackoffSeconds = 2d,
            double simulationHoursPerRealSecond = 1d,
            double simulationTickSeconds = 0.25d)
        {
            SeekHungerAtOrBelow = seekHungerAtOrBelow;
            CriticalHungerAtOrBelow = criticalHungerAtOrBelow;
            SeekThirstAtOrBelow = seekThirstAtOrBelow;
            CriticalThirstAtOrBelow = criticalThirstAtOrBelow;
            SeekFatigueAtOrAbove = seekFatigueAtOrAbove;
            CriticalFatigueAtOrAbove = criticalFatigueAtOrAbove;
            EvaluationIntervalSeconds = evaluationIntervalSeconds;
            RetryBackoffSeconds = retryBackoffSeconds;
            SimulationHoursPerRealSecond = simulationHoursPerRealSecond;
            SimulationTickSeconds = simulationTickSeconds;
        }

        public double SeekHungerAtOrBelow { get; }
        public double CriticalHungerAtOrBelow { get; }
        public double SeekThirstAtOrBelow { get; }
        public double CriticalThirstAtOrBelow { get; }
        public double SeekFatigueAtOrAbove { get; }
        public double CriticalFatigueAtOrAbove { get; }
        public double EvaluationIntervalSeconds { get; }
        public double RetryBackoffSeconds { get; }
        public double SimulationHoursPerRealSecond { get; }
        public double SimulationTickSeconds { get; }
    }

    /// <summary>
    /// Stateless need policy plus a bounded candidate search. Interaction
    /// execution and reservations remain in the accepted 3.2B order system.
    /// </summary>
    public sealed class AutonomousSurvivalDecisionService
    {
        private readonly AutonomousSurvivalSettings settings;
        private readonly List<Vector2Int> candidates = new(8);

        public AutonomousSurvivalDecisionService(
            AutonomousSurvivalSettings settings = null)
        {
            this.settings = settings ?? AutonomousSurvivalSettings.Provisional;
        }

        public AutonomousSurvivalSettings Settings => settings;

        public AutonomousSurvivalDecision Evaluate(AntNeedsState needs)
        {
            if (needs == null) return default;

            // Critical ties deliberately follow Thirst, Hunger, Fatigue.
            if (needs.Thirst <= settings.CriticalThirstAtOrBelow)
                return new AutonomousSurvivalDecision(
                    SurvivalInteractionKind.Drink, true, 1d);
            if (needs.Hunger <= settings.CriticalHungerAtOrBelow)
                return new AutonomousSurvivalDecision(
                    SurvivalInteractionKind.Eat, true, 1d);
            if (needs.Fatigue >= settings.CriticalFatigueAtOrAbove)
                return new AutonomousSurvivalDecision(
                    SurvivalInteractionKind.Rest, true, 1d);

            AutonomousSurvivalDecision best = default;
            Consider(
                SurvivalInteractionKind.Drink,
                needs.Thirst <= settings.SeekThirstAtOrBelow,
                (settings.SeekThirstAtOrBelow - needs.Thirst) /
                    settings.SeekThirstAtOrBelow,
                ref best);
            Consider(
                SurvivalInteractionKind.Eat,
                needs.Hunger <= settings.SeekHungerAtOrBelow,
                (settings.SeekHungerAtOrBelow - needs.Hunger) /
                    settings.SeekHungerAtOrBelow,
                ref best);
            Consider(
                SurvivalInteractionKind.Rest,
                needs.Fatigue >= settings.SeekFatigueAtOrAbove,
                (needs.Fatigue - settings.SeekFatigueAtOrAbove) /
                    (100d - settings.SeekFatigueAtOrAbove),
                ref best);
            return best;
        }

        public bool TryChooseTarget(
            SurvivalInteractionWorld world,
            GridNavigationMap map,
            Vector2Int start,
            SurvivalInteractionKind kind,
            out Vector2Int target,
            out string error)
        {
            target = default;
            if (world == null || map == null)
            {
                error = "Autonomous survival requires world and navigation data.";
                return false;
            }

            world.CollectAvailableTargets(kind, candidates);
            float bestCost = float.PositiveInfinity;
            foreach (Vector2Int candidate in candidates)
            {
                if (!SurvivalInteractionOrder.TryMeasureApproach(
                        map, start, candidate, out _, out float cost, out _))
                {
                    continue;
                }

                if (cost < bestCost ||
                    Mathf.Approximately(cost, bestCost) && CompareCells(candidate, target) < 0)
                {
                    bestCost = cost;
                    target = candidate;
                }
            }

            if (float.IsPositiveInfinity(bestCost))
            {
                error = $"No reachable available target can satisfy {kind}.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void Consider(
            SurvivalInteractionKind kind,
            bool eligible,
            double urgency,
            ref AutonomousSurvivalDecision best)
        {
            if (!eligible) return;
            if (!best.HasDecision || urgency > best.Urgency)
            {
                best = new AutonomousSurvivalDecision(kind, false, urgency);
            }
            // Equal urgency keeps the earlier deterministic Drink/Eat/Rest order.
        }

        private static int CompareCells(Vector2Int first, Vector2Int second)
        {
            int y = first.y.CompareTo(second.y);
            return y != 0 ? y : first.x.CompareTo(second.x);
        }
    }

    /// <summary>Inspectable retry and intent state owned per authoritative ant.</summary>
    public sealed class AutonomousSurvivalAgentState
    {
        public SurvivalInteractionKind? Intent { get; internal set; }
        public bool IntentIsCritical { get; internal set; }
        public Vector2Int? Target { get; internal set; }
        public string Status { get; internal set; } = "Healthy; no survival action needed.";
        public string FailureReason { get; internal set; } = string.Empty;
        public double NextEvaluationTime { get; internal set; }
        public int ConsecutiveFailures { get; internal set; }

        internal void Reset()
        {
            Intent = null;
            IntentIsCritical = false;
            Target = null;
            Status = "Autonomous survival reset.";
            FailureReason = string.Empty;
            NextEvaluationTime = 0d;
            ConsecutiveFailures = 0;
        }
    }
}
