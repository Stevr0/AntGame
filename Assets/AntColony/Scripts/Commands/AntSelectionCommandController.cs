using System.Collections.Generic;
using AntColony.Ants;
using AntColony.Navigation;
using AntColony.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Commands
{
    /// <summary>
    /// Player-facing single-ant selection and temporary move-order input. The
    /// controller owns neither terrain navigation nor ant movement execution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AntSelectionCommandController : MonoBehaviour
    {
        private const float PlayerPanelWidth = 330f;
        private const float MinimumClickRadiusPixels = 16f;
        private const double DebugExperienceAmount = 100d;
        private const double DebugMaximumExperienceAmount = 50000d;
        private const double DebugInactivityStepHours = 24d;
        private const double DebugNeedsTimeStepHours = 1d;
        private const double DebugRestStepHours = 8d;
        private const double DebugFatigueStep = 25d;

        private static readonly string[] SkillDisplayNames =
            BuildSkillDisplayNames();

        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridNavigationWorldService navigationWorld;

        private readonly List<SelectableGridAnt> selectableAnts = new();
        private readonly Dictionary<SelectableGridAnt, AutonomousSurvivalAgentState>
            autonomousStates = new();
        private readonly AutonomousSurvivalDecisionService autonomousDecisionService = new();
        private readonly Dictionary<SelectableGridAnt, AutonomousWorkAgentState>
            autonomousWorkStates = new();
        private readonly ColonyJobSelectionService colonyJobSelectionService = new();
        private Rect additionalBlockedGuiRect;
        private bool hasAdditionalBlockedGuiRect;
        private string interactionMessage = "Left-click an ant to select it.";
        private bool skillDebugExpanded;
        private bool needsDebugExpanded;
        private int selectedDebugSkillIndex;
        private double simulationHours;
        private SurvivalInteractionWorld interactionWorld;
        private Vector2Int selectedInteractionTarget;
        private bool hasSelectedInteractionTarget;
        private Vector2 playerPanelScrollPosition;
        private double nextAutonomousEvaluationTime;
        private double autonomousClockAccumulator;
        private int autonomousEvaluationIndex;
        private ColonyJobRegistry colonyJobs;
        private double nextWorkEvaluationTime;
        private int workEvaluationIndex;
        private bool colonyJobsDebugExpanded;
        private int selectedWorkCategoryIndex;
        private int nextDevelopmentJobNumber = 1;

        public IReadOnlyList<SelectableGridAnt> SelectableAnts => selectableAnts;
        public SelectableGridAnt SelectedAnt { get; private set; }
        public string InteractionMessage => interactionMessage;
        public double SimulationHours => simulationHours;
        public double SkillSimulationHours => simulationHours;
        public bool AutonomousSurvivalEnabled { get; private set; }
        public bool AutonomousColonyWorkEnabled { get; private set; }
        public ColonyJobRegistry ColonyJobs => colonyJobs;
        public Rect PlayerPanelGuiRect => new(
            Mathf.Max(12f, Screen.width - PlayerPanelWidth - 12f),
            12f,
            PlayerPanelWidth,
            Mathf.Min(
                Mathf.Max(100f, Screen.height - 24f),
                SelectedAnt == null
                    ? 112f
                    : skillDebugExpanded || needsDebugExpanded || colonyJobsDebugExpanded
                        ? 920f : 700f));

        public void Configure(
            Camera commandCamera,
            GridNavigationWorldService sharedNavigationWorld,
            SurvivalInteractionWorld sharedInteractionWorld = null,
            ColonyJobRegistry sharedColonyJobs = null)
        {
            worldCamera = commandCamera;
            navigationWorld = sharedNavigationWorld;
            interactionWorld = sharedInteractionWorld;
            colonyJobs = sharedColonyJobs;
        }

        public void BindInteractionWorld(SurvivalInteractionWorld sharedInteractionWorld)
        {
            interactionWorld = sharedInteractionWorld;
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                ant?.BindInteractionWorld(sharedInteractionWorld);
            }
        }

        public void BindColonyJobs(ColonyJobRegistry sharedColonyJobs)
        {
            colonyJobs = sharedColonyJobs;
            foreach (SelectableGridAnt ant in selectableAnts)
                ant?.BindColonyJobs(sharedColonyJobs);
        }

        public void ClearSelectableAnts()
        {
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant != null &&
                    (ant.OrderSource == AntOrderSource.AutonomousSurvival ||
                     ant.OrderSource == AntOrderSource.AutonomousWork))
                    ant.CancelOrder("Autonomous activity cleared with the world.");
            }
            Deselect();
            selectableAnts.Clear();
            autonomousStates.Clear();
            autonomousWorkStates.Clear();
            autonomousEvaluationIndex = 0;
            workEvaluationIndex = 0;
        }

        public void RegisterSelectableAnt(SelectableGridAnt ant)
        {
            if (ant != null && !selectableAnts.Contains(ant))
            {
                selectableAnts.Add(ant);
                ant.BindInteractionWorld(interactionWorld);
                ant.BindColonyJobs(colonyJobs);
                autonomousStates[ant] = new AutonomousSurvivalAgentState();
                autonomousWorkStates[ant] = new AutonomousWorkAgentState();
            }
        }

        public AutonomousSurvivalAgentState GetAutonomousSurvivalState(
            SelectableGridAnt ant)
        {
            return ant != null && autonomousStates.TryGetValue(
                ant, out AutonomousSurvivalAgentState state) ? state : null;
        }

        public AutonomousWorkAgentState GetAutonomousWorkState(SelectableGridAnt ant)
        {
            return ant != null && autonomousWorkStates.TryGetValue(
                ant, out AutonomousWorkAgentState state) ? state : null;
        }

        public void SetAutonomousColonyWorkEnabled(bool enabled)
        {
            if (AutonomousColonyWorkEnabled == enabled) return;
            AutonomousColonyWorkEnabled = enabled;
            nextWorkEvaluationTime = 0d;
            foreach (KeyValuePair<SelectableGridAnt, AutonomousWorkAgentState> entry
                in autonomousWorkStates)
            {
                if (!enabled && entry.Key != null &&
                    entry.Key.OrderSource == AntOrderSource.AutonomousWork)
                    entry.Key.CancelOrder("Autonomous colony work was disabled.");
                entry.Value.Reset(enabled
                    ? "Waiting for colony work evaluation."
                    : "Autonomous colony work disabled.");
            }
            interactionMessage = enabled
                ? "Developer Only: autonomous colony work enabled."
                : "Developer Only: autonomous colony work disabled.";
        }

        public void SetAutonomousSurvivalEnabled(bool enabled)
        {
            if (AutonomousSurvivalEnabled == enabled) return;
            AutonomousSurvivalEnabled = enabled;
            nextAutonomousEvaluationTime = 0d;
            autonomousClockAccumulator = 0d;
            foreach (KeyValuePair<SelectableGridAnt, AutonomousSurvivalAgentState> entry
                in autonomousStates)
            {
                if (!enabled && entry.Key != null &&
                    entry.Key.OrderSource == AntOrderSource.AutonomousSurvival)
                {
                    entry.Key.CancelOrder("Autonomous survival was disabled.");
                }
                entry.Value.Reset();
                entry.Value.Status = enabled
                    ? "Waiting for staggered survival evaluation."
                    : "Autonomous survival disabled.";
            }
            interactionMessage = enabled
                ? "Developer Only: autonomous survival enabled."
                : "Developer Only: autonomous survival disabled.";
        }

        public void SetAdditionalUiBlocker(Rect guiRect)
        {
            additionalBlockedGuiRect = guiRect;
            hasAdditionalBlockedGuiRect = guiRect.width > 0f && guiRect.height > 0f;
        }

        public bool IsScreenPointBlocked(Vector2 screenPoint)
        {
            Vector2 guiPoint = new(screenPoint.x, Screen.height - screenPoint.y);
            return PlayerPanelGuiRect.Contains(guiPoint) ||
                (hasAdditionalBlockedGuiRect &&
                    additionalBlockedGuiRect.Contains(guiPoint));
        }

        public bool TrySelectAtWorldPoint(Vector2 worldPoint)
        {
            SelectableGridAnt closest = null;
            float closestDistance = float.PositiveInfinity;
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant == null || !ant.gameObject.activeInHierarchy ||
                    !ant.ContainsWorldPoint(worldPoint))
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude(
                    (Vector2)ant.transform.position - worldPoint);
                if (distance < closestDistance)
                {
                    closest = ant;
                    closestDistance = distance;
                }
            }

            Select(closest);
            return closest != null;
        }

        public bool TryHandleLeftClick(Vector2 screenPoint)
        {
            if (IsScreenPointBlocked(screenPoint) || worldCamera == null)
            {
                return false;
            }

            SelectableGridAnt closest = FindClosestScreenAnt(screenPoint);
            if (closest != null)
            {
                Select(closest);
                return true;
            }

            Vector3 worldPoint = worldCamera.ScreenToWorldPoint(screenPoint);
            TrySelectAtWorldPoint(worldPoint);
            return false;
        }

        public void Select(SelectableGridAnt ant)
        {
            if (ant != null && !selectableAnts.Contains(ant))
            {
                return;
            }

            if (SelectedAnt == ant)
            {
                return;
            }

            SelectedAnt?.SetSelected(false);
            SelectedAnt = ant;
            SelectedAnt?.SetSelected(true);
            interactionMessage = SelectedAnt != null
                ? $"Selected {SelectedAnt.DisplayName}."
                : "Selection cleared.";
        }

        public void Deselect()
        {
            Select(null);
        }

        public bool TryIssueMoveOrder(Vector2Int destination)
        {
            if (SelectedAnt == null)
            {
                interactionMessage = "Move ignored: select an ant first.";
                return false;
            }

            if (navigationWorld == null || navigationWorld.Map == null ||
                !navigationWorld.Map.TryGetAnchor(
                    destination,
                    out GridNavigationAnchor anchor))
            {
                string reason = $"No traversable grid anchor at {destination}.";
                SelectedAnt.RecordRejectedOrder(destination, reason);
                interactionMessage = reason;
                return false;
            }

            if (!SelectedAnt.IssueMoveOrder(destination))
            {
                interactionMessage = SelectedAnt.FailureReason;
                return false;
            }

            MarkPlayerCommand(SelectedAnt);

            interactionMessage =
                $"{SelectedAnt.DisplayName} ordered to {destination} " +
                $"({anchor.Position}).";
            return true;
        }

        public bool TryIssueInteractionOrder(
            SurvivalInteractionKind kind,
            Vector2Int targetCell)
        {
            hasSelectedInteractionTarget = true;
            selectedInteractionTarget = targetCell;
            if (SelectedAnt == null)
            {
                interactionMessage = "Interaction ignored: select an ant first.";
                return false;
            }
            if (!SelectedAnt.IssueInteractionOrder(kind, targetCell))
            {
                interactionMessage = SelectedAnt.FailureReason;
                return false;
            }

            MarkPlayerCommand(SelectedAnt);

            interactionMessage =
                $"{SelectedAnt.DisplayName} ordered to {kind} at {targetCell}.";
            return true;
        }

        public bool TryIssueContextOrder(Vector2Int cell)
        {
            if (interactionWorld != null &&
                interactionWorld.TryGetFunction(cell, out BackgroundFunction function))
            {
                SurvivalInteractionKind kind = function switch
                {
                    BackgroundFunction.FoodStorage => SurvivalInteractionKind.Eat,
                    BackgroundFunction.WaterStorage => SurvivalInteractionKind.Drink,
                    _ => SurvivalInteractionKind.Rest
                };
                return TryIssueInteractionOrder(kind, cell);
            }

            return TryIssueMoveOrder(cell);
        }

        public bool CancelSelectedOrder()
        {
            if (SelectedAnt == null || !SelectedAnt.HasActiveOrder)
            {
                interactionMessage = "No selected active order to cancel.";
                return false;
            }

            SelectedAnt.CancelOrder("Player cancelled the movement order.");
            interactionMessage = $"Cancelled {SelectedAnt.DisplayName}'s order.";
            return true;
        }

        /// <summary>
        /// Development boundary for a successful skill-use event. Future gameplay
        /// systems should call the AntRecord API from their own completion logic.
        /// </summary>
        public bool TryAwardSelectedSkillExperience(
            AntSkillId skill,
            double baseExperience,
            double multiplier,
            out AntSkillExperienceResult result,
            out string message)
        {
            result = default;
            if (SelectedAnt?.Record == null)
            {
                message = "Select a bound ant before awarding skill experience.";
                interactionMessage = message;
                return false;
            }

            bool succeeded = SelectedAnt.Record.TryAwardSkillExperience(
                skill,
                baseExperience,
                multiplier,
                simulationHours,
                out result,
                out message);
            interactionMessage = succeeded
                ? $"Developer Only: awarded {result.AwardedExperience:0.##} " +
                    $"{AntSkillCatalog.Get(skill).DisplayName} XP to " +
                    $"{SelectedAnt.DisplayName}; level {result.PreviousLevel} -> " +
                    $"{result.CurrentLevel}."
                : message;
            return succeeded;
        }

        /// <summary>
        /// Advances the one development clock and batches decay across records.
        /// No skill owns an Update method or depends on selection for simulation.
        /// </summary>
        public bool TryAdvanceSkillInactivity(
            double elapsedHours,
            out string message)
        {
            return TryAdvanceSimulationTime(elapsedHours, out message);
        }

        /// <summary>
        /// Advances the shared development clock. Needs use elapsed time while
        /// skill decay receives the resulting absolute time, matching each API's
        /// deterministic contract without adding a frame-driven global scan.
        /// </summary>
        public bool TryAdvanceSimulationTime(
            double elapsedHours,
            out string message)
        {
            return TryAdvanceSimulationTimeInternal(
                elapsedHours, updateInteractionMessage: true, out message);
        }

        private bool TryAdvanceSimulationTimeInternal(
            double elapsedHours,
            bool updateInteractionMessage,
            out string message)
        {
            if (!AntSkillProgressionSettings.IsFinite(elapsedHours) ||
                elapsedHours < 0d ||
                !AntSkillProgressionSettings.IsFinite(
                    simulationHours + elapsedHours))
            {
                message = "Simulation time must be finite and non-negative.";
                if (updateInteractionMessage) interactionMessage = message;
                return false;
            }

            double newTime = simulationHours + elapsedHours;
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.Record != null &&
                    ant.Record.LifecycleStatus == AntLifecycleStatus.Active &&
                    !ant.Record.Needs.HasDepletedHealth &&
                    !(ant.InteractionOrder?.Kind == SurvivalInteractionKind.Rest &&
                      ant.InteractionOrder.Phase == SurvivalInteractionPhase.Interacting) &&
                    !ant.Record.TryAdvanceNeeds(elapsedHours, out message))
                {
                    if (updateInteractionMessage) interactionMessage = message;
                    return false;
                }
            }

            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.Record != null &&
                    ant.Record.LifecycleStatus != AntLifecycleStatus.Deceased &&
                    !ant.Record.TryAdvanceSkillDecay(newTime, out message))
                {
                    if (updateInteractionMessage) interactionMessage = message;
                    return false;
                }
            }

            simulationHours = newTime;
            message =
                $"Developer Only: simulation time is {simulationHours:0.##}h.";
            if (updateInteractionMessage) interactionMessage = message;
            return true;
        }

        /// <summary>Runs a controlled rest interval on the selected record.</summary>
        public bool TryRestSelectedAnt(double elapsedHours, out string message)
        {
            if (SelectedAnt?.Record == null)
            {
                message = "Select a bound ant before simulating rest.";
                interactionMessage = message;
                return false;
            }

            bool succeeded = SelectedAnt.Record.TryRest(elapsedHours, out message);
            interactionMessage = succeeded
                ? $"Developer Only: {SelectedAnt.DisplayName} rested for " +
                    $"{elapsedHours:0.##}h."
                : message;
            message = interactionMessage;
            return succeeded;
        }

        /// <summary>Sets bounded authoritative needs for deterministic testing.</summary>
        public bool TrySetSelectedNeedsForDevelopment(
            double health,
            double hunger,
            double thirst,
            double fatigue,
            out string message)
        {
            if (SelectedAnt?.Record == null)
            {
                message = "Select a bound ant before changing needs.";
                interactionMessage = message;
                return false;
            }

            bool succeeded = SelectedAnt.Record.TrySetNeedsForDevelopment(
                health,
                hunger,
                thirst,
                fatigue,
                out message);
            interactionMessage = succeeded
                ? $"Developer Only: updated {SelectedAnt.DisplayName}'s needs."
                : message;
            message = interactionMessage;
            return succeeded;
        }

        /// <summary>Restores the selected record's deterministic starting needs.</summary>
        public bool ResetSelectedNeeds(out string message)
        {
            if (SelectedAnt?.Record == null)
            {
                message = "Select a bound ant before resetting needs.";
                interactionMessage = message;
                return false;
            }

            SelectedAnt.Record.ResetNeeds();
            message = $"Developer Only: reset {SelectedAnt.DisplayName}'s needs.";
            interactionMessage = message;
            return true;
        }

        public bool ResetSelectedSkillData(out string message)
        {
            if (SelectedAnt?.Record == null)
            {
                message = "Select a bound ant before resetting skill data.";
                interactionMessage = message;
                return false;
            }

            SelectedAnt.Record.ResetSkills(simulationHours);
            message = $"Developer Only: reset {SelectedAnt.DisplayName}'s skills.";
            interactionMessage = message;
            return true;
        }

        /// <summary>Called when the development world creates fresh records.</summary>
        public void ResetSimulationDebugClock()
        {
            simulationHours = 0d;
            nextAutonomousEvaluationTime = 0d;
            nextWorkEvaluationTime = 0d;
            autonomousClockAccumulator = 0d;
            foreach (AutonomousSurvivalAgentState state in autonomousStates.Values)
                state.Reset();
            foreach (AutonomousWorkAgentState state in autonomousWorkStates.Values)
                state.Reset(AutonomousColonyWorkEnabled
                    ? "Waiting for colony work evaluation."
                    : "Autonomous colony work disabled.");
        }

        /// <summary>Compatibility name retained for the accepted 3.1C API.</summary>
        public void ResetSkillDebugClock()
        {
            ResetSimulationDebugClock();
        }

        private void Update()
        {
            double interactionHours = Time.deltaTime *
                autonomousDecisionService.Settings.SimulationHoursPerRealSecond;
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                ant?.RefreshOrderStatus(interactionHours, simulationHours);
            }

            UpdateAutonomousSimulationClock();
            UpdateAutonomousSurvival();
            UpdateAutonomousColonyWork();

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screenPoint = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryHandleLeftClick(screenPoint);
            }

            if (mouse.rightButton.wasPressedThisFrame &&
                !IsScreenPointBlocked(screenPoint) && worldCamera != null)
            {
                Vector3 worldPoint = worldCamera.ScreenToWorldPoint(screenPoint);
                Vector2Int cell =
                    WorldTileCoordinates.WorldPositionToCell(worldPoint);
                TryIssueContextOrder(cell);
            }
        }

        public bool EvaluateAutonomousSurvivalNow(
            SelectableGridAnt ant,
            double currentTimeSeconds)
        {
            if (!AutonomousSurvivalEnabled || ant?.Record == null ||
                !autonomousStates.TryGetValue(ant, out AutonomousSurvivalAgentState state))
                return false;

            if (ant.Record.LifecycleStatus != AntLifecycleStatus.Active ||
                ant.Record.Needs.HasDepletedHealth)
            {
                if (ant.OrderSource == AntOrderSource.AutonomousSurvival)
                    ant.CancelOrder("Autonomous survival stopped because the ant is inactive.");
                state.Intent = null;
                state.IntentIsCritical = false;
                state.Target = null;
                state.Status = "Inactive; autonomous survival stopped.";
                state.FailureReason = string.Empty;
                return false;
            }

            AutonomousSurvivalDecision decision =
                autonomousDecisionService.Evaluate(ant.Record.Needs);

            if (ant.HasActiveOrder)
            {
                if (ant.OrderSource == AntOrderSource.AutonomousSurvival)
                {
                    if (decision.HasDecision && decision.IsCritical &&
                        state.Intent != decision.Kind &&
                        TryChooseAutonomousTarget(
                            ant, decision.Kind, out Vector2Int emergencyTarget,
                            out _))
                    {
                        ant.CancelOrder(
                            $"Higher-priority critical {decision.Kind} replaced autonomous {state.Intent}.");
                        return TryStartAutonomousOrder(
                            ant, state, decision, emergencyTarget,
                            currentTimeSeconds);
                    }
                    state.Status = ant.OrderStatus;
                    return false; // Hysteresis: finish the current survival action.
                }

                state.Intent = null;
                state.IntentIsCritical = false;
                state.Target = null;
                if (!decision.HasDecision || !decision.IsCritical ||
                    IsMatchingInteraction(ant, decision.Kind))
                {
                    state.Status = decision.IsCritical
                        ? "Player order already addresses the critical need."
                        : ant.OrderSource == AntOrderSource.AutonomousWork
                            ? "Ordinary needs wait for the current work boundary."
                            : "Player order has precedence over ordinary needs.";
                    return false;
                }

                // Find a viable emergency destination before cancelling the
                // player order. This avoids discarding an order when no relief
                // is physically available.
                if (!TryChooseAutonomousTarget(
                        ant, decision.Kind, out Vector2Int criticalTarget,
                        out string criticalError))
                {
                    RecordAutonomousFailure(state, currentTimeSeconds, criticalError);
                    return false;
                }

                ant.CancelOrder(
                    $"Critical {decision.Kind} need interrupted the player order at a safe cancellation boundary.");
                return TryStartAutonomousOrder(
                    ant, state, decision, criticalTarget, currentTimeSeconds);
            }

            if (state.Intent.HasValue)
            {
                bool failed = ant.InteractionOrder != null &&
                    (ant.InteractionOrder.Phase == SurvivalInteractionPhase.Failed ||
                     ant.InteractionOrder.Phase == SurvivalInteractionPhase.Cancelled);
                string previousFailure = ant.InteractionOrder?.FailureReason;
                state.Intent = null;
                state.IntentIsCritical = false;
                state.Target = null;
                if (failed)
                {
                    RecordAutonomousFailure(
                        state, currentTimeSeconds,
                        string.IsNullOrEmpty(previousFailure)
                            ? "Autonomous survival interaction ended before completion."
                            : previousFailure);
                    return false;
                }
                state.ConsecutiveFailures = 0;
            }

            if (currentTimeSeconds < state.NextEvaluationTime)
                return false;
            if (!decision.HasDecision)
            {
                state.Status = "Healthy; no survival action needed.";
                state.FailureReason = string.Empty;
                state.NextEvaluationTime = currentTimeSeconds +
                    autonomousDecisionService.Settings.EvaluationIntervalSeconds;
                return false;
            }

            if (!TryChooseAutonomousTarget(
                    ant, decision.Kind, out Vector2Int target, out string error))
            {
                RecordAutonomousFailure(state, currentTimeSeconds, error);
                return false;
            }

            return TryStartAutonomousOrder(
                ant, state, decision, target, currentTimeSeconds);
        }

        private void UpdateAutonomousSurvival()
        {
            if (!AutonomousSurvivalEnabled || selectableAnts.Count == 0) return;

            double now = Time.unscaledTimeAsDouble;
            if (now < nextAutonomousEvaluationTime) return;
            autonomousEvaluationIndex %= selectableAnts.Count;
            EvaluateAutonomousSurvivalNow(
                selectableAnts[autonomousEvaluationIndex], now);
            autonomousEvaluationIndex =
                (autonomousEvaluationIndex + 1) % selectableAnts.Count;
            nextAutonomousEvaluationTime = now +
                autonomousDecisionService.Settings.EvaluationIntervalSeconds /
                selectableAnts.Count;
        }

        private void UpdateAutonomousSimulationClock()
        {
            if (!AutonomousSurvivalEnabled && !AutonomousColonyWorkEnabled) return;
            autonomousClockAccumulator += Time.deltaTime;
            double tickSeconds = autonomousDecisionService.Settings.SimulationTickSeconds;
            if (autonomousClockAccumulator < tickSeconds) return;
            double elapsedHours = autonomousClockAccumulator *
                autonomousDecisionService.Settings.SimulationHoursPerRealSecond;
            autonomousClockAccumulator = 0d;
            TryAdvanceSimulationTimeInternal(
                elapsedHours, updateInteractionMessage: false, out _);
        }

        public bool EvaluateAutonomousColonyWorkNow(
            SelectableGridAnt ant,
            double currentTimeSeconds)
        {
            if (!AutonomousColonyWorkEnabled || ant?.Record == null ||
                colonyJobs == null || navigationWorld?.Map == null ||
                !autonomousWorkStates.TryGetValue(
                    ant, out AutonomousWorkAgentState state))
                return false;

            if (ant.Record.LifecycleStatus != AntLifecycleStatus.Active ||
                ant.Record.Needs.HasDepletedHealth)
            {
                if (ant.OrderSource == AntOrderSource.AutonomousWork)
                    ant.CancelOrder("Autonomous work stopped because the ant is inactive.");
                colonyJobs.ReleaseOwnedJobs(
                    ant.Record.Id, "Reservation released because the ant is inactive.");
                state.Reset("Inactive; autonomous colony work stopped.");
                return false;
            }

            if (ant.HasActiveOrder)
            {
                state.Status = ant.OrderSource switch
                {
                    AntOrderSource.AutonomousWork => ant.OrderStatus,
                    AntOrderSource.AutonomousSurvival =>
                        "Autonomous survival has precedence over colony work.",
                    _ => "Player order has precedence over colony work."
                };
                return false;
            }

            if (state.JobId.HasValue)
            {
                SyntheticColonyJobOrder previous = ant.JobOrder;
                bool failed = previous != null &&
                    (previous.Phase == SyntheticJobOrderPhase.Failed ||
                     previous.Phase == SyntheticJobOrderPhase.Cancelled);
                string failure = previous?.FailureReason;
                state.JobId = null;
                if (failed)
                {
                    RecordWorkFailure(state, currentTimeSeconds,
                        string.IsNullOrEmpty(failure)
                            ? "Synthetic work ended before completion."
                            : failure);
                    return false;
                }
                state.ConsecutiveFailures = 0;
            }

            if (currentTimeSeconds < state.NextEvaluationTime) return false;
            Vector2Int start =
                WorldTileCoordinates.WorldPositionToCell(ant.Motor.Position);
            if (!colonyJobSelectionService.TryChoose(
                    ant.Record, colonyJobs, navigationWorld.Map, start,
                    out ColonyJob job, out Vector2Int approach, out string error))
            {
                RecordWorkFailure(state, currentTimeSeconds, error);
                return false;
            }
            if (!ant.IssueSyntheticJobOrder(job, approach))
            {
                RecordWorkFailure(state, currentTimeSeconds, ant.FailureReason);
                return false;
            }

            state.JobId = job.Id;
            state.Status = $"Claimed {job.Category} job {job.Id} at {job.TargetCell}.";
            state.FailureReason = string.Empty;
            state.NextEvaluationTime = currentTimeSeconds +
                autonomousDecisionService.Settings.EvaluationIntervalSeconds;
            return true;
        }

        private void UpdateAutonomousColonyWork()
        {
            if (!AutonomousColonyWorkEnabled || selectableAnts.Count == 0) return;
            double now = Time.unscaledTimeAsDouble;
            if (now < nextWorkEvaluationTime) return;
            workEvaluationIndex %= selectableAnts.Count;
            EvaluateAutonomousColonyWorkNow(
                selectableAnts[workEvaluationIndex], now);
            workEvaluationIndex = (workEvaluationIndex + 1) % selectableAnts.Count;
            nextWorkEvaluationTime = now +
                autonomousDecisionService.Settings.EvaluationIntervalSeconds /
                selectableAnts.Count;
        }

        private static void RecordWorkFailure(
            AutonomousWorkAgentState state,
            double currentTimeSeconds,
            string reason)
        {
            state.JobId = null;
            state.ConsecutiveFailures++;
            double delay = System.Math.Min(
                8d, 2d * System.Math.Pow(
                    2d, System.Math.Min(2, state.ConsecutiveFailures - 1)));
            state.NextEvaluationTime = currentTimeSeconds + delay;
            state.FailureReason = reason;
            state.Status = $"No eligible reachable work; retry in {delay:0.##}s.";
        }

        public bool TrySetSelectedWorkPriority(
            AntWorkCategory category,
            AntWorkPriority priority,
            out string message)
        {
            if (SelectedAnt?.Record == null)
            {
                message = "Select a bound ant before changing work priorities.";
                return false;
            }
            bool changed = SelectedAnt.Record.WorkPriorities.TrySet(
                category, priority, out message);
            if (changed)
                message = $"{SelectedAnt.DisplayName}: {category} priority is {priority}.";
            interactionMessage = message;
            nextWorkEvaluationTime = 0d;
            return changed;
        }

        public bool TryAddSyntheticJob(
            AntWorkCategory category,
            Vector2Int targetCell,
            out ColonyJob job,
            out string message)
        {
            if (colonyJobs == null)
            {
                job = null;
                message = "The development colony job registry is unavailable.";
                return false;
            }
            ColonyJobId id = new($"development.job.{nextDevelopmentJobNumber++:D3}");
            bool added = colonyJobs.TryAdd(id, category, targetCell, out job, out message);
            interactionMessage = added
                ? $"Added synthetic {category} job {id} at {targetCell}."
                : message;
            nextWorkEvaluationTime = 0d;
            return added;
        }

        public bool TryCancelSyntheticJob(ColonyJobId id, out string message)
        {
            if (colonyJobs == null)
            {
                message = "The development colony job registry is unavailable.";
                interactionMessage = message;
                return false;
            }
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.JobOrder?.Job.Id.Equals(id) == true && ant.HasActiveOrder)
                    ant.CancelOrder("Synthetic job was cancelled by the developer.");
            }
            bool cancelled = colonyJobs.TryCancel(
                id, "Developer cancelled the synthetic job.", out message);
            interactionMessage = message;
            return cancelled;
        }

        public bool ResetSyntheticJobFixtures(out string message)
        {
            if (colonyJobs == null)
            {
                message = "The development colony job registry is unavailable.";
                return false;
            }
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant != null && ant.OrderSource == AntOrderSource.AutonomousWork)
                    ant.CancelOrder("Synthetic job fixtures were reset.");
            }
            colonyJobs.Clear();
            nextDevelopmentJobNumber = 1;
            (AntWorkCategory category, Vector2Int cell)[] fixtures =
            {
                (AntWorkCategory.Excavation, new Vector2Int(-10, SampleWorldGenerator.MainPassageY)),
                (AntWorkCategory.Construction, new Vector2Int(2, SampleWorldGenerator.MainPassageY)),
                (AntWorkCategory.Hauling, new Vector2Int(8, SampleWorldGenerator.MainPassageY)),
                (AntWorkCategory.Foraging, new Vector2Int(11, SampleWorldGenerator.MainPassageY)),
                (AntWorkCategory.Excavation, SampleWorldGenerator.GridUnreachableDestination)
            };
            for (int index = 0; index < fixtures.Length; index++)
            {
                ColonyJobId id = new($"development.fixture.{index + 1:D2}");
                if (!colonyJobs.TryAdd(
                        id, fixtures[index].category, fixtures[index].cell,
                        out _, out message)) return false;
            }
            foreach (AutonomousWorkAgentState state in autonomousWorkStates.Values)
                state.Reset(AutonomousColonyWorkEnabled
                    ? "Synthetic fixtures reset; waiting for evaluation."
                    : "Autonomous colony work disabled.");
            nextWorkEvaluationTime = 0d;
            message = "Reset five deterministic synthetic colony job fixtures.";
            interactionMessage = message;
            return true;
        }

        private bool TryChooseAutonomousTarget(
            SelectableGridAnt ant,
            SurvivalInteractionKind kind,
            out Vector2Int target,
            out string error)
        {
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(ant.Motor.Position);
            return autonomousDecisionService.TryChooseTarget(
                interactionWorld, navigationWorld?.Map, start, kind, out target, out error);
        }

        private bool TryStartAutonomousOrder(
            SelectableGridAnt ant,
            AutonomousSurvivalAgentState state,
            AutonomousSurvivalDecision decision,
            Vector2Int target,
            double currentTimeSeconds)
        {
            if (!ant.IssueInteractionOrder(
                    decision.Kind, target, AntOrderSource.AutonomousSurvival))
            {
                RecordAutonomousFailure(
                    state, currentTimeSeconds, ant.FailureReason);
                return false;
            }

            state.Intent = decision.Kind;
            state.IntentIsCritical = decision.IsCritical;
            state.Target = target;
            state.Status = decision.IsCritical
                ? $"Critical autonomous {decision.Kind} targeting {target}."
                : $"Autonomous {decision.Kind} targeting {target}.";
            state.FailureReason = string.Empty;
            state.NextEvaluationTime = currentTimeSeconds +
                autonomousDecisionService.Settings.EvaluationIntervalSeconds;
            return true;
        }

        private void RecordAutonomousFailure(
            AutonomousSurvivalAgentState state,
            double currentTimeSeconds,
            string reason)
        {
            state.Intent = null;
            state.IntentIsCritical = false;
            state.Target = null;
            state.ConsecutiveFailures++;
            double multiplier = System.Math.Pow(
                2d, System.Math.Min(2, state.ConsecutiveFailures - 1));
            double delay = System.Math.Min(
                8d,
                autonomousDecisionService.Settings.RetryBackoffSeconds * multiplier);
            state.NextEvaluationTime = currentTimeSeconds + delay;
            state.FailureReason = reason;
            state.Status = $"Unmet survival need; retry in {delay:0.##}s.";
        }

        private void MarkPlayerCommand(SelectableGridAnt ant)
        {
            AutonomousSurvivalAgentState state = GetAutonomousSurvivalState(ant);
            if (state == null) return;
            state.Intent = null;
            state.IntentIsCritical = false;
            state.Target = null;
            state.Status = "Player order has precedence.";
            state.FailureReason = string.Empty;
            AutonomousWorkAgentState workState = GetAutonomousWorkState(ant);
            if (workState != null)
            {
                workState.JobId = null;
                workState.Status = "Player order has precedence over colony work.";
                workState.FailureReason = string.Empty;
                workState.NextEvaluationTime = 0d;
            }
        }

        private static bool IsMatchingInteraction(
            SelectableGridAnt ant,
            SurvivalInteractionKind kind)
        {
            return ant.InteractionOrder != null &&
                ant.InteractionOrder.IsActive &&
                ant.InteractionOrder.Kind == kind;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(PlayerPanelGuiRect, GUI.skin.box);
            playerPanelScrollPosition = GUILayout.BeginScrollView(
                playerPanelScrollPosition);
            GUILayout.Label("ANT SELECTION, SURVIVAL & COLONY WORK");
            if (GUILayout.Button(AutonomousSurvivalEnabled
                    ? "Disable Developer Only Autonomous Survival"
                    : "Enable Developer Only Autonomous Survival"))
            {
                SetAutonomousSurvivalEnabled(!AutonomousSurvivalEnabled);
            }
            GUILayout.Label(AutonomousSurvivalEnabled
                ? "Autonomous clock: 1 simulation hour / real second"
                : "Autonomous survival is disabled.");
            if (GUILayout.Button(AutonomousColonyWorkEnabled
                    ? "Disable Developer Only Autonomous Colony Work"
                    : "Enable Developer Only Autonomous Colony Work"))
            {
                SetAutonomousColonyWorkEnabled(!AutonomousColonyWorkEnabled);
            }
            if (SelectedAnt == null)
            {
                GUILayout.Label("Selected ant: None");
                GUILayout.Label("Left-click an ant. Right-click sends it to terrain.");
            }
            else
            {
                AntRecord record = SelectedAnt.Record;
                GUILayout.Label($"Selected ant: {SelectedAnt.DisplayName}");
                GUILayout.Label($"Ant ID: {record?.Id.ToString() ?? "Unbound"}");
                GUILayout.Label($"Colony: {record?.ColonyId.ToString() ?? "Unbound"}");
                GUILayout.Label($"Caste: {record?.Caste.ToString() ?? "Unbound"}");
                GUILayout.Label(
                    $"Attributes: STR {record?.Attributes.Strength.ToString() ?? "-"}  " +
                    $"AGI {record?.Attributes.Agility.ToString() ?? "-"}  " +
                    $"END {record?.Attributes.Endurance.ToString() ?? "-"}");
                GUILayout.Label(
                    $"Lifecycle: {record?.LifecycleStatus.ToString() ?? "Unbound"}");
                DrawNeedsInspection(record);
                DrawSkillInspection(record);
                GUILayout.Label($"Control: {SelectedAnt.ControlState}");
                GUILayout.Label($"Order: {SelectedAnt.OrderStatus}");
                GUILayout.Label(
                    $"Interaction: {SelectedAnt.InteractionOrder?.Kind.ToString() ?? "None"}");
                GUILayout.Label(
                    $"Interaction target: {SelectedAnt.InteractionOrder?.TargetCell.ToString() ?? "None"}");
                GUILayout.Label(
                    $"Colony job: {SelectedAnt.JobOrder?.Job.Id.ToString() ?? "None"}");
                GUILayout.Label(
                    $"Destination: {(SelectedAnt.HasDestination ? SelectedAnt.Destination.ToString() : "None")}");
                GUILayout.Label(
                    $"Failure: {(string.IsNullOrEmpty(SelectedAnt.FailureReason) ? "None" : SelectedAnt.FailureReason)}");
                DrawAutonomousSurvivalInspection(SelectedAnt);
                DrawAutonomousWorkInspection(SelectedAnt);
                GUI.enabled = SelectedAnt.HasActiveOrder;
                if (GUILayout.Button("Cancel Selected Order"))
                {
                    CancelSelectedOrder();
                }
                GUI.enabled = true;
                DrawNeedsDebugControls();
                DrawSkillDebugControls();
                DrawColonyJobsDebugControls();
            }

            DrawResourceDiagnostics();
            DrawColonyJobDiagnostics();

            GUILayout.Label(interactionMessage);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawAutonomousSurvivalInspection(SelectableGridAnt ant)
        {
            GUILayout.Space(4f);
            GUILayout.Label("AUTONOMOUS SURVIVAL");
            AutonomousSurvivalAgentState state = GetAutonomousSurvivalState(ant);
            if (state == null)
            {
                GUILayout.Label("State unavailable.");
                return;
            }

            GUILayout.Label($"Intent: {state.Intent?.ToString() ?? "None"}");
            GUILayout.Label($"Target: {state.Target?.ToString() ?? "None"}");
            GUILayout.Label($"Status: {state.Status}");
            GUILayout.Label(
                $"Unmet/failure: {(string.IsNullOrEmpty(state.FailureReason) ? "None" : state.FailureReason)}");
            double retryIn = System.Math.Max(
                0d, state.NextEvaluationTime - Time.unscaledTimeAsDouble);
            GUILayout.Label($"Next evaluation/retry: {retryIn:0.##}s");
        }

        private void DrawAutonomousWorkInspection(SelectableGridAnt ant)
        {
            GUILayout.Space(4f);
            GUILayout.Label("AUTONOMOUS COLONY WORK");
            AutonomousWorkAgentState state = GetAutonomousWorkState(ant);
            if (state == null)
            {
                GUILayout.Label("State unavailable.");
                return;
            }
            GUILayout.Label($"Job: {state.JobId?.ToString() ?? "None"}");
            GUILayout.Label($"Status: {state.Status}");
            GUILayout.Label($"Failure: {(string.IsNullOrEmpty(state.FailureReason) ? "None" : state.FailureReason)}");
            double retryIn = System.Math.Max(
                0d, state.NextEvaluationTime - Time.unscaledTimeAsDouble);
            GUILayout.Label($"Next evaluation/retry: {retryIn:0.##}s");
        }

        private void DrawColonyJobsDebugControls()
        {
            if (GUILayout.Button(colonyJobsDebugExpanded
                    ? "Hide Developer Only Colony Jobs"
                    : "Show Developer Only Colony Jobs"))
                colonyJobsDebugExpanded = !colonyJobsDebugExpanded;
            if (!colonyJobsDebugExpanded || SelectedAnt?.Record == null) return;

            GUILayout.Label("DEVELOPER ONLY — COLONY JOBS");
            foreach (AntWorkCategoryDefinition definition in AntWorkCategoryCatalog.All)
            {
                AntWorkPriority current =
                    SelectedAnt.Record.WorkPriorities.Get(definition.Id);
                if (GUILayout.Button($"{definition.DisplayName}: {current}"))
                {
                    AntWorkPriority next = current == AntWorkPriority.Low
                        ? AntWorkPriority.Disabled
                        : current == AntWorkPriority.Disabled
                            ? AntWorkPriority.Highest
                            : (AntWorkPriority)((int)current + 1);
                    TrySetSelectedWorkPriority(definition.Id, next, out _);
                }
            }
            string[] categoryNames = new string[AntWorkCategoryCatalog.All.Count];
            for (int index = 0; index < categoryNames.Length; index++)
                categoryNames[index] = AntWorkCategoryCatalog.All[index].DisplayName;
            selectedWorkCategoryIndex = GUILayout.SelectionGrid(
                selectedWorkCategoryIndex, categoryNames, 2);
            AntWorkCategory selectedCategory =
                AntWorkCategoryCatalog.All[selectedWorkCategoryIndex].Id;
            if (GUILayout.Button("Add Reachable Synthetic Job"))
            {
                Vector2Int target = new(
                    -10 + selectedWorkCategoryIndex * 3,
                    SampleWorldGenerator.MainPassageY);
                TryAddSyntheticJob(selectedCategory, target, out _, out _);
            }
            if (GUILayout.Button("Add Unreachable Synthetic Job"))
                TryAddSyntheticJob(
                    selectedCategory, SampleWorldGenerator.GridUnreachableDestination,
                    out _, out _);
            if (GUILayout.Button("Reset Synthetic Job Fixtures"))
                ResetSyntheticJobFixtures(out _);
        }

        private void DrawColonyJobDiagnostics()
        {
            GUILayout.Space(4f);
            GUILayout.Label("DEVELOPER ONLY — COLONY JOB REGISTRY");
            if (colonyJobs == null)
            {
                GUILayout.Label("Registry unavailable.");
                return;
            }
            foreach (ColonyJob job in colonyJobs.Jobs)
            {
                GUILayout.Label(
                    $"{job.Id} | {job.Category} | {job.Status} | {job.Progress:P0} | " +
                    $"owner {(job.ReservationOwner.IsValid ? job.ReservationOwner.ToString() : "None")}");
                if (!string.IsNullOrEmpty(job.FailureReason))
                    GUILayout.Label($"Reason: {job.FailureReason}");
                if (job.Status != ColonyJobStatus.Completed &&
                    job.Status != ColonyJobStatus.Cancelled &&
                    GUILayout.Button($"Cancel {job.Id}"))
                    TryCancelSyntheticJob(job.Id, out _);
            }
        }

        private void DrawResourceDiagnostics()
        {
            GUILayout.Space(4f);
            GUILayout.Label("DEVELOPER ONLY — PHYSICAL STORAGE");
            if (!hasSelectedInteractionTarget || interactionWorld == null)
            {
                GUILayout.Label("Selected storage tile: None");
                if (interactionWorld != null)
                {
                    foreach (SurvivalStorageSlot fixture in interactionWorld.StorageSlots)
                    {
                        GUILayout.Label(
                            $"{fixture.Kind} {fixture.Cell}: {fixture.Quantity} " +
                            $"({fixture.AvailableQuantity} available)");
                    }
                }
                return;
            }

            GUILayout.Label($"Selected storage tile: {selectedInteractionTarget}");
            if (interactionWorld.TryGetStorage(
                    selectedInteractionTarget, out SurvivalStorageSlot slot))
            {
                GUILayout.Label($"Resource: {slot.ResourceTypeId} ({slot.Kind})");
                GUILayout.Label(
                    $"Quantity: {slot.Quantity}  Reserved: {slot.ReservedQuantity}  " +
                    $"Available: {slot.AvailableQuantity}");
                return;
            }

            string sleeper = interactionWorld.GetSleepingReservation(
                selectedInteractionTarget);
            GUILayout.Label(string.IsNullOrEmpty(sleeper)
                ? "Sleeping slot: Available"
                : $"Sleeping slot reserved by: {sleeper}");
        }

        private void DrawNeedsInspection(AntRecord record)
        {
            GUILayout.Space(4f);
            GUILayout.Label("NEEDS & SURVIVAL");
            if (record == null)
            {
                GUILayout.Label("Unbound");
                return;
            }

            AntNeedsState needs = record.Needs;
            GUILayout.Label($"Health: {needs.Health:0.##} / 100");
            GUILayout.Label($"Hunger satisfaction: {needs.Hunger:0.##} / 100");
            GUILayout.Label($"Thirst satisfaction: {needs.Thirst:0.##} / 100");
            GUILayout.Label($"Fatigue: {needs.Fatigue:0.##} / 100");
            GUILayout.Label(
                $"Rested work speed: {needs.WorkSpeedMultiplier:0.###}x");
            GUILayout.Label(
                $"Rested skill XP: {needs.SkillExperienceMultiplier:0.###}x");
            if (needs.HasDepletedHealth)
            {
                GUILayout.Label("WARNING: Health depleted; survival is paused.");
            }
            else if (needs.IsStarving)
            {
                GUILayout.Label("WARNING: Critical Hunger; Health is decreasing.");
            }

            if (needs.IsDehydrated)
            {
                GUILayout.Label("WARNING: Critical Thirst; skill XP is blocked.");
            }

            GUILayout.Space(4f);
        }

        private void DrawNeedsDebugControls()
        {
            if (GUILayout.Button(needsDebugExpanded
                    ? "Hide Developer Only Needs Tools"
                    : "Show Developer Only Needs Tools"))
            {
                needsDebugExpanded = !needsDebugExpanded;
            }

            if (!needsDebugExpanded || SelectedAnt?.Record == null)
            {
                return;
            }

            AntNeedsState needs = SelectedAnt.Record.Needs;
            GUILayout.Label("DEVELOPER ONLY — NEEDS TESTING");
            GUILayout.Label($"Simulation time: {simulationHours:0.##}h");
            if (GUILayout.Button("Advance 1h Simulation Time"))
            {
                TryAdvanceSimulationTime(DebugNeedsTimeStepHours, out _);
            }
            if (GUILayout.Button("Refill Hunger"))
            {
                SetSelectedNeeds(needs.Health, 100d, needs.Thirst, needs.Fatigue);
            }
            if (GUILayout.Button("Refill Thirst"))
            {
                SetSelectedNeeds(needs.Health, needs.Hunger, 100d, needs.Fatigue);
            }
            if (GUILayout.Button("Restore Health"))
            {
                SetSelectedNeeds(100d, needs.Hunger, needs.Thirst, needs.Fatigue);
            }
            if (GUILayout.Button("Add 25 Fatigue"))
            {
                SetSelectedNeeds(
                    needs.Health,
                    needs.Hunger,
                    needs.Thirst,
                    System.Math.Min(100d, needs.Fatigue + DebugFatigueStep));
            }
            if (GUILayout.Button("Simulate 8h Rest"))
            {
                TryRestSelectedAnt(DebugRestStepHours, out _);
            }
            if (GUILayout.Button("Set Critical Hunger (0)"))
            {
                SetSelectedNeeds(needs.Health, 0d, needs.Thirst, needs.Fatigue);
            }
            if (GUILayout.Button("Set Critical Thirst (0)"))
            {
                SetSelectedNeeds(needs.Health, needs.Hunger, 0d, needs.Fatigue);
            }
            if (GUILayout.Button("Reset Selected Ant Needs"))
            {
                ResetSelectedNeeds(out _);
            }
        }

        private void SetSelectedNeeds(
            double health,
            double hunger,
            double thirst,
            double fatigue)
        {
            TrySetSelectedNeedsForDevelopment(
                health,
                hunger,
                thirst,
                fatigue,
                out _);
        }

        private void DrawSkillInspection(AntRecord record)
        {
            GUILayout.Space(4f);
            GUILayout.Label("SKILLS");
            if (record == null)
            {
                GUILayout.Label("Unbound");
                return;
            }

            foreach (AntSkillState skill in record.Skills.All)
            {
                string progress = skill.IsMaximumLevel
                    ? "MAX"
                    : $"{skill.ExperienceIntoLevel:0.##} / " +
                        $"{skill.ExperienceForNextLevel:0.##} XP";
                GUILayout.Label(
                    $"{skill.Definition.DisplayName}: Lv {skill.Level} ({progress})");
            }

            GUILayout.Space(4f);
        }

        private void DrawSkillDebugControls()
        {
            if (GUILayout.Button(skillDebugExpanded
                    ? "Hide Developer Only Skill Tools"
                    : "Show Developer Only Skill Tools"))
            {
                skillDebugExpanded = !skillDebugExpanded;
            }

            if (!skillDebugExpanded)
            {
                return;
            }

            GUILayout.Label("DEVELOPER ONLY — SKILL TESTING");
            GUILayout.Label($"Simulation time: {simulationHours:0.##}h");
            selectedDebugSkillIndex = GUILayout.SelectionGrid(
                selectedDebugSkillIndex,
                SkillDisplayNames,
                2);
            AntSkillId skill = AntSkillCatalog.All[selectedDebugSkillIndex].Id;
            if (GUILayout.Button("Award 100 XP (1.0x)"))
            {
                TryAwardSelectedSkillExperience(
                    skill,
                    DebugExperienceAmount,
                    1d,
                    out _,
                    out _);
            }
            if (GUILayout.Button("Award 100 XP (1.5x)"))
            {
                TryAwardSelectedSkillExperience(
                    skill,
                    DebugExperienceAmount,
                    1.5d,
                    out _,
                    out _);
            }
            if (GUILayout.Button("Test 100 XP (0x — No Gain)"))
            {
                TryAwardSelectedSkillExperience(
                    skill,
                    DebugExperienceAmount,
                    0d,
                    out _,
                    out _);
            }
            if (GUILayout.Button("Award 50,000 XP (Maximum Test)"))
            {
                TryAwardSelectedSkillExperience(
                    skill,
                    DebugMaximumExperienceAmount,
                    1d,
                    out _,
                    out _);
            }
            if (GUILayout.Button("Advance 24h Inactivity"))
            {
                TryAdvanceSkillInactivity(DebugInactivityStepHours, out _);
            }
            if (GUILayout.Button("Reset Selected Ant Skills"))
            {
                ResetSelectedSkillData(out _);
            }
        }

        private static string[] BuildSkillDisplayNames()
        {
            var names = new string[AntSkillCatalog.All.Count];
            for (int index = 0; index < names.Length; index++)
            {
                names[index] = AntSkillCatalog.All[index].DisplayName;
            }

            return names;
        }

        private SelectableGridAnt FindClosestScreenAnt(Vector2 screenPoint)
        {
            SelectableGridAnt closest = null;
            float closestDistance = MinimumClickRadiusPixels *
                MinimumClickRadiusPixels;
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant == null || !ant.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 antScreen = worldCamera.WorldToScreenPoint(
                    ant.transform.position);
                if (antScreen.z < 0f)
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude(
                    (Vector2)antScreen - screenPoint);
                if (distance <= closestDistance)
                {
                    closest = ant;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
