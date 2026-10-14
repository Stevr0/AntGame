using System;
using AntColony.Ants;
using AntColony.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>
    /// One presentation row for an authoritative ant record. The row stores no
    /// priority data; every displayed value is read back from the record.
    /// </summary>
    public sealed class WorkPriorityRowView : MonoBehaviour
    {
        [SerializeField] private Text identityText;
        [SerializeField] private Text activityText;
        [SerializeField] private Button[] priorityButtons;
        [SerializeField] private Text[] priorityLabels;

        private AntRecord record;
        private AntSelectionCommandController commandController;
        private AntWorkCategory[] categories = Array.Empty<AntWorkCategory>();

        public AntRecord Record => record;
        public string DisplayedIdentity => identityText != null ? identityText.text : string.Empty;
        public string DisplayedActivity => activityText != null ? activityText.text : string.Empty;

        public void Bind(AntRecord antRecord,
            AntSelectionCommandController controller,
            AntWorkCategory[] supportedCategories)
        {
            record = antRecord;
            commandController = controller;
            categories = supportedCategories ?? Array.Empty<AntWorkCategory>();

            for (int index = 0; index < priorityButtons.Length; index++)
            {
                int capturedIndex = index;
                priorityButtons[index].onClick.RemoveAllListeners();
                if (record != null && capturedIndex < categories.Length)
                    priorityButtons[index].onClick.AddListener(
                        () => CyclePriority(capturedIndex));
            }

            Refresh();
        }

        public void Refresh()
        {
            if (record == null)
            {
                identityText.text = "No active ant";
                activityText.text = string.Empty;
                SetButtonsAvailable(false);
                return;
            }

            identityText.text = $"{record.DisplayName}\n{record.Caste}";
            activityText.text = BuildActivityText();
            SetButtonsAvailable(true);

            for (int index = 0; index < priorityLabels.Length; index++)
            {
                if (index >= categories.Length)
                {
                    priorityLabels[index].text = "Unavailable";
                    continue;
                }

                AntWorkCategory category = categories[index];
                AntWorkPriority priority = record.WorkPriorities.Get(category);
                AntWorkCategoryDefinition definition = AntWorkCategoryCatalog.Get(category);
                int skillLevel = definition.Skill.HasValue
                    ? record.Skills.Get(definition.Skill.Value).Level : 0;
                string priorityLabel = priority == AntWorkPriority.Disabled
                    ? "Disabled" : $"Priority {(int)priority}";
                priorityLabels[index].text = $"{priorityLabel}\nSkill {skillLevel}";
            }
        }

        /// <summary>Used by the matrix and focused tests to edit one record.</summary>
        public bool TrySetPriority(AntWorkCategory category,
            AntWorkPriority priority, out string message)
        {
            if (record == null || commandController == null)
            {
                message = "The work-priority row is not bound.";
                return false;
            }

            bool changed = commandController.TrySetWorkPriority(
                record, category, priority, out message);
            if (changed) Refresh();
            return changed;
        }

        public void Configure(Text identity, Text activity,
            Button[] buttons, Text[] labels)
        {
            identityText = identity;
            activityText = activity;
            priorityButtons = buttons ?? Array.Empty<Button>();
            priorityLabels = labels ?? Array.Empty<Text>();
        }

        private void CyclePriority(int index)
        {
            AntWorkCategory category = categories[index];
            AntWorkPriority current = record.WorkPriorities.Get(category);
            AntWorkPriority next = current switch
            {
                AntWorkPriority.Highest => AntWorkPriority.High,
                AntWorkPriority.High => AntWorkPriority.Normal,
                AntWorkPriority.Normal => AntWorkPriority.Low,
                AntWorkPriority.Low => AntWorkPriority.Disabled,
                _ => AntWorkPriority.Highest
            };
            TrySetPriority(category, next, out _);
        }

        private string BuildActivityText()
        {
            SelectableGridAnt visual = FindLoadedVisual();
            if (visual == null)
                return "Not loaded in Colony View\nPriorities remain editable";

            ColonyJob activeJob = visual.ActiveColonyJob;
            if (activeJob != null)
            {
                string failure = string.IsNullOrWhiteSpace(activeJob.FailureReason)
                    ? string.Empty : $"\n{activeJob.FailureReason}";
                return $"{activeJob.Category} · {activeJob.Status}" +
                    $" · P{(int)record.WorkPriorities.Get(activeJob.Category)}" + failure;
            }

            AutonomousWorkAgentState state =
                commandController?.GetAutonomousWorkState(visual);
            if (state != null && !string.IsNullOrWhiteSpace(state.FailureReason))
                return $"Blocked: {state.FailureReason}";
            if (state != null && !string.IsNullOrWhiteSpace(state.Status))
                return state.Status;
            return visual.OrderStatus;
        }

        private SelectableGridAnt FindLoadedVisual()
        {
            if (record == null || commandController == null) return null;
            foreach (SelectableGridAnt ant in commandController.SelectableAnts)
                if (ant != null && ant.Record == record) return ant;
            return null;
        }

        private void SetButtonsAvailable(bool available)
        {
            foreach (Button button in priorityButtons)
                if (button != null) button.interactable = available;
        }
    }
}
