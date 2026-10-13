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

        private static readonly string[] SkillDisplayNames =
            BuildSkillDisplayNames();

        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridNavigationWorldService navigationWorld;

        private readonly List<SelectableGridAnt> selectableAnts = new();
        private Rect additionalBlockedGuiRect;
        private bool hasAdditionalBlockedGuiRect;
        private string interactionMessage = "Left-click an ant to select it.";
        private bool skillDebugExpanded;
        private int selectedDebugSkillIndex;
        private double skillSimulationHours;
        private Vector2 playerPanelScrollPosition;

        public IReadOnlyList<SelectableGridAnt> SelectableAnts => selectableAnts;
        public SelectableGridAnt SelectedAnt { get; private set; }
        public string InteractionMessage => interactionMessage;
        public double SkillSimulationHours => skillSimulationHours;
        public Rect PlayerPanelGuiRect => new(
            Mathf.Max(12f, Screen.width - PlayerPanelWidth - 12f),
            12f,
            PlayerPanelWidth,
            Mathf.Min(
                Mathf.Max(100f, Screen.height - 24f),
                SelectedAnt == null
                    ? 112f
                    : skillDebugExpanded ? 720f : 510f));

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
                skillSimulationHours,
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
            if (!AntSkillProgressionSettings.IsFinite(elapsedHours) ||
                elapsedHours < 0d ||
                !AntSkillProgressionSettings.IsFinite(
                    skillSimulationHours + elapsedHours))
            {
                message = "Inactivity time must be finite and non-negative.";
                interactionMessage = message;
                return false;
            }

            double newTime = skillSimulationHours + elapsedHours;
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

            skillSimulationHours = newTime;
            message =
                $"Developer Only: simulation time is {skillSimulationHours:0.##}h.";
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

            SelectedAnt.Record.ResetSkills(skillSimulationHours);
            message = $"Developer Only: reset {SelectedAnt.DisplayName}'s skills.";
            interactionMessage = message;
            return true;
        }

        /// <summary>Called when the development world creates fresh records.</summary>
        public void ResetSkillDebugClock()
        {
            skillSimulationHours = 0d;
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
                DrawSkillDebugControls();
            }

            GUILayout.Label(interactionMessage);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
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
            GUILayout.Label($"Simulation time: {skillSimulationHours:0.##}h");
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
