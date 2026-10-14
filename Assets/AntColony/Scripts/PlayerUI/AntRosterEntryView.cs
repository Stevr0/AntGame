using System;
using AntColony.Ants;
using AntColony.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>One reusable authored row in the colony ant roster.</summary>
    public sealed class AntRosterEntryView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Text nameText;
        [SerializeField] private Text detailText;

        public AntRecord Record { get; private set; }
        public string DisplayedName => nameText != null ? nameText.text : string.Empty;

        public void Bind(AntRecord record, Action<AntRecord> selected)
        {
            Record = record;
            selectButton.onClick.RemoveAllListeners();
            if (record != null)
                selectButton.onClick.AddListener(() => selected?.Invoke(record));
            selectButton.interactable = record != null;
        }

        public void Refresh(bool isSelected, SelectableGridAnt visual)
        {
            if (Record == null)
            {
                nameText.text = "No ant";
                detailText.text = string.Empty;
                selectionHighlight.enabled = false;
                return;
            }

            nameText.text = Record.DisplayName;
            string health = Record.Needs.Health <= 25d ? "Critical" :
                Record.Needs.IsStarving || Record.Needs.IsDehydrated
                    ? "Needs attention" : $"Health {Record.Needs.Health:0}";
            string activity = visual != null
                ? visual.OrderStatus : "Not visible in Colony View";
            detailText.text = $"{Record.Caste} · {health}\n{activity}";
            detailText.color = Record.Needs.Health <= 25d ||
                Record.Needs.IsStarving || Record.Needs.IsDehydrated
                    ? new Color32(238, 138, 103, 255)
                    : new Color32(208, 198, 177, 255);
            selectionHighlight.enabled = isSelected;
        }

        public void Configure(Button button, Image highlight, Text antName, Text details)
        {
            selectButton = button;
            selectionHighlight = highlight;
            nameText = antName;
            detailText = details;
        }
    }
}
