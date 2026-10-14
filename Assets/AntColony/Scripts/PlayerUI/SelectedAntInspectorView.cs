using System.Text;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>Read-only shared ant data plus a separate Colony View section.</summary>
    public sealed class SelectedAntInspectorView : MonoBehaviour
    {
        [SerializeField] private Text contentText;
        [SerializeField] private Button cancelOrderButton;

        private readonly StringBuilder builder = new(768);
        private AntSelectionCommandController commandController;
        private float nextRefreshTime;

        public string DisplayedText => contentText != null ? contentText.text : string.Empty;

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.2f;
            Refresh();
        }

        public void Bind(AntSelectionCommandController controller)
        {
            commandController = controller;
            cancelOrderButton.onClick.RemoveAllListeners();
            cancelOrderButton.onClick.AddListener(CancelCurrentOrder);
            Refresh();
        }

        public void Refresh()
        {
            SelectableGridAnt ant = commandController?.SelectedAnt;
            AntRecord record = ant?.Record;
            if (record == null)
            {
                contentText.text = "Select an ant to inspect its colony record.";
                cancelOrderButton.interactable = false;
                return;
            }

            builder.Clear();
            builder.AppendLine($"{record.DisplayName} · {record.Caste}");
            builder.AppendLine($"Lifecycle: {record.LifecycleStatus}");
            builder.AppendLine($"Health {record.Needs.Health:0}  Hunger {record.Needs.Hunger:0}  Thirst {record.Needs.Thirst:0}");
            builder.AppendLine($"Fatigue {record.Needs.Fatigue:0}  Rested: {(record.Needs.RestedStrength > 0d ? "Yes" : "No")}");
            builder.AppendLine($"Attributes: STR {record.Attributes.Strength}  AGI {record.Attributes.Agility}  END {record.Attributes.Endurance}");
            builder.AppendLine();
            builder.AppendLine("Skills");
            foreach (AntSkillState skill in record.Skills.All)
                builder.AppendLine($"  {skill.Definition.DisplayName}: {skill.Level}");
            builder.AppendLine();
            builder.AppendLine("Inventory");
            if (record.Inventory.OccupiedSlots.Count == 0) builder.AppendLine("  Empty");
            foreach (AntInventorySlot slot in record.Inventory.OccupiedSlots)
                builder.AppendLine($"  {ReadableResource(slot.ResourceTypeId)} {slot.Quantity}/{slot.MaximumStack}");
            builder.AppendLine();
            builder.AppendLine("Activity");
            builder.AppendLine($"  {ant.ControlState}: {ant.OrderStatus}");
            ColonyJob job = ant.ActiveColonyJob;
            if (job != null)
                builder.AppendLine($"  {job.Category} at {job.TargetCell} · {job.Status}" +
                    (job.Progress > 0d ? $" · {job.Progress:P0}" : string.Empty));
            string reason = !string.IsNullOrWhiteSpace(ant.FailureReason)
                ? ant.FailureReason
                : commandController.GetAutonomousWorkState(ant)?.FailureReason;
            if (!string.IsNullOrWhiteSpace(reason)) builder.AppendLine($"  Waiting: {reason}");
            builder.AppendLine();
            builder.AppendLine("Colony View");
            builder.AppendLine($"  Position: {ant.Motor.Position}");
            builder.AppendLine($"  Movement: {ant.Motor.State}");
            builder.AppendLine(ant.HasDestination
                ? $"  Local destination: {ant.Destination}"
                : "  Local destination: None");

            contentText.text = builder.ToString();
            cancelOrderButton.interactable = ant.HasActiveOrder;
        }

        public void Configure(Text content, Button cancelButton)
        {
            contentText = content;
            cancelOrderButton = cancelButton;
        }

        private void CancelCurrentOrder()
        {
            commandController?.CancelSelectedOrder();
            Refresh();
        }

        private static string ReadableResource(string stableId)
        {
            return MaterialResourceCatalog.TryGetMaterial(stableId, out ExcavatedMaterialType material)
                ? material.ToString() : stableId;
        }
    }
}
