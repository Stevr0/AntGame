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
        private Rect additionalBlockedGuiRect;
        private bool hasAdditionalBlockedGuiRect;
        private string interactionMessage = "Left-click an ant to select it.";
        private bool skillDebugExpanded;
        private bool needsDebugExpanded;
        private int selectedDebugSkillIndex;
        private double simulationHours;
        private Vector2 playerPanelScrollPosition;

        public IReadOnlyList<SelectableGridAnt> SelectableAnts => selectableAnts;
        public SelectableGridAnt SelectedAnt { get; private set; }
        public string InteractionMessage => interactionMessage;
        public double SimulationHours => simulationHours;
        public double SkillSimulationHours => simulationHours;
        public Rect PlayerPanelGuiRect => new(
            Mathf.Max(12f, Screen.width - PlayerPanelWidth - 12f),
            12f,
            PlayerPanelWidth,
            Mathf.Min(
                Mathf.Max(100f, Screen.height - 24f),
                SelectedAnt == null
                    ? 112f
                    : skillDebugExpanded || needsDebugExpanded ? 920f : 650f));

        public void Configure(
            Camera commandCamera,
            GridNavigationWorldService sharedNavigationWorld)
        {
            worldCamera = commandCamera;
            navigationWorld = sharedNavigationWorld;
        }

        public void ClearSelectableAnts()
        {
            Deselect();
            selectableAnts.Clear();
        }

        public void RegisterSelectableAnt(SelectableGridAnt ant)
        {
            if (ant != null && !selectableAnts.Contains(ant))
            {
                selectableAnts.Add(ant);
            }
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

            interactionMessage =
                $"{SelectedAnt.DisplayName} ordered to {destination} " +
                $"({anchor.Position}).";
            return true;
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
            if (!AntSkillProgressionSettings.IsFinite(elapsedHours) ||
                elapsedHours < 0d ||
                !AntSkillProgressionSettings.IsFinite(
                    simulationHours + elapsedHours))
            {
                message = "Simulation time must be finite and non-negative.";
                interactionMessage = message;
                return false;
            }

            double newTime = simulationHours + elapsedHours;
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.Record != null &&
                    ant.Record.LifecycleStatus == AntLifecycleStatus.Active &&
                    !ant.Record.Needs.HasDepletedHealth &&
                    !ant.Record.TryAdvanceNeeds(elapsedHours, out message))
                {
                    interactionMessage = message;
                    return false;
                }
            }

            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.Record != null &&
                    ant.Record.LifecycleStatus != AntLifecycleStatus.Deceased &&
                    !ant.Record.TryAdvanceSkillDecay(newTime, out message))
                {
                    interactionMessage = message;
                    return false;
                }
            }

            simulationHours = newTime;
            message =
                $"Developer Only: simulation time is {simulationHours:0.##}h.";
            interactionMessage = message;
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
        }

        /// <summary>Compatibility name retained for the accepted 3.1C API.</summary>
        public void ResetSkillDebugClock()
        {
            ResetSimulationDebugClock();
        }

        private void Update()
        {
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                ant?.RefreshOrderStatus();
            }

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
                TryIssueMoveOrder(cell);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(PlayerPanelGuiRect, GUI.skin.box);
            playerPanelScrollPosition = GUILayout.BeginScrollView(
                playerPanelScrollPosition);
            GUILayout.Label("ANT SELECTION & MOVE ORDER");
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
                    $"Destination: {(SelectedAnt.HasDestination ? SelectedAnt.Destination.ToString() : "None")}");
                GUILayout.Label(
                    $"Failure: {(string.IsNullOrEmpty(SelectedAnt.FailureReason) ? "None" : SelectedAnt.FailureReason)}");
                GUI.enabled = SelectedAnt.HasActiveOrder;
                if (GUILayout.Button("Cancel Selected Move Order"))
                {
                    CancelSelectedOrder();
                }
                GUI.enabled = true;
                DrawNeedsDebugControls();
                DrawSkillDebugControls();
            }

            GUILayout.Label(interactionMessage);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
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
