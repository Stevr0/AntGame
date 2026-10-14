using System.Collections.Generic;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using UnityEngine;

namespace AntColony.PlayerUI
{
    /// <summary>
    /// Presents authoritative records and adapts selection to the currently loaded
    /// Colony View visual only when one exists.
    /// </summary>
    public sealed class AntRosterView : MonoBehaviour
    {
        [SerializeField] private AntRosterEntryView[] entries;

        private readonly List<AntRecord> displayedRecords = new();
        private AntRegistry registry;
        private AntSelectionCommandController commandController;
        private float nextRefreshTime;

        public IReadOnlyList<AntRecord> DisplayedRecords => displayedRecords;
        public int EntryCount => entries?.Length ?? 0;

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.2f;
            Refresh();
        }

        public void Bind(AntRegistry antRegistry,
            AntSelectionCommandController selectionController)
        {
            registry = antRegistry;
            commandController = selectionController;
            displayedRecords.Clear();
            if (registry != null)
                displayedRecords.AddRange(registry.Records.OrderBy(record => record.Id.Value));

            for (int index = 0; index < entries.Length; index++)
            {
                AntRecord record = index < displayedRecords.Count
                    ? displayedRecords[index] : null;
                entries[index].Bind(record, antRecord => SelectRecord(antRecord));
            }
            Refresh();
        }

        public bool SelectRecord(AntRecord record)
        {
            if (record == null || commandController == null) return false;
            SelectableGridAnt visual = FindLoadedVisual(record);
            if (visual == null) return false;
            commandController.Select(visual);
            Refresh();
            return true;
        }

        public void Refresh()
        {
            if (entries == null) return;
            for (int index = 0; index < entries.Length; index++)
            {
                AntRecord record = index < displayedRecords.Count
                    ? displayedRecords[index] : null;
                SelectableGridAnt visual = FindLoadedVisual(record);
                bool selected = visual != null && commandController?.SelectedAnt == visual;
                entries[index].Refresh(selected, visual);
            }
        }

        public void Configure(AntRosterEntryView[] rosterEntries)
        {
            entries = rosterEntries ?? System.Array.Empty<AntRosterEntryView>();
        }

        private SelectableGridAnt FindLoadedVisual(AntRecord record)
        {
            if (record == null || commandController == null) return null;
            foreach (SelectableGridAnt ant in commandController.SelectableAnts)
                if (ant != null && ant.Record == record) return ant;
            return null;
        }
    }
}
