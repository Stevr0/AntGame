using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>
    /// Global colony work matrix. It operates on records, so an ant does not need
    /// a currently loaded Colony View GameObject to appear or be edited.
    /// </summary>
    public sealed class ColonyManagementView : MonoBehaviour
    {
        private static readonly AntWorkCategory[] supportedCategories =
        {
            AntWorkCategory.Excavation,
            AntWorkCategory.Construction,
            AntWorkCategory.Hauling
        };

        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform rowContent;
        [SerializeField] private WorkPriorityRowView rowPrefab;
        [SerializeField] private Text feedbackText;

        private readonly List<WorkPriorityRowView> rows = new();
        private readonly List<AntRecord> displayedRecords = new();
        private AntRegistry registry;
        private AntSelectionCommandController commandController;
        private float nextRefreshTime;

        public static IReadOnlyList<AntWorkCategory> SupportedCategories =>
            supportedCategories;
        public IReadOnlyList<AntRecord> DisplayedRecords => displayedRecords;
        public IReadOnlyList<WorkPriorityRowView> Rows => rows;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int EscapeConsumedFrame { get; private set; } = -1;

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                EscapeConsumedFrame = Time.frameCount;
                Close();
                return;
            }

            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.25f;
            EnsureRowsMatchRegistry();
            foreach (WorkPriorityRowView row in rows) row.Refresh();
        }

        public void Bind(AntRegistry antRegistry,
            AntSelectionCommandController controller)
        {
            registry = antRegistry;
            commandController = controller;
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(Open);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
            RebuildRows();
            Close();
        }

        public void Open()
        {
            if (panelRoot == null) return;
            EnsureRowsMatchRegistry();
            panelRoot.SetActive(true);
            feedbackText.text =
                "1 is highest, 4 is lowest. Disabled work is skipped. " +
                "Priority changes affect subsequent autonomous job choices.";
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void RebuildRows()
        {
            foreach (WorkPriorityRowView row in rows)
            {
                if (row == null) continue;
                // Scene validators rebind the HUD outside Play Mode. Unity requires
                // immediate cleanup there, while runtime rows use normal Destroy.
                if (Application.isPlaying) Destroy(row.gameObject);
                else DestroyImmediate(row.gameObject);
            }
            rows.Clear();
            displayedRecords.Clear();

            if (registry == null || rowPrefab == null || rowContent == null) return;
            displayedRecords.AddRange(registry.Records
                .Where(record => record.LifecycleStatus == AntLifecycleStatus.Active)
                .OrderBy(record => record.Id.Value));
            foreach (AntRecord record in displayedRecords)
            {
                WorkPriorityRowView row = Instantiate(rowPrefab, rowContent);
                row.name = $"{record.DisplayName} Work Priorities";
                row.gameObject.SetActive(true);
                row.Bind(record, commandController, supportedCategories);
                rows.Add(row);
            }
        }

        public void Configure(Button workButton, Button close,
            GameObject root, RectTransform content,
            WorkPriorityRowView priorityRowPrefab, Text feedback)
        {
            openButton = workButton;
            closeButton = close;
            panelRoot = root;
            rowContent = content;
            rowPrefab = priorityRowPrefab;
            feedbackText = feedback;
        }

        private void EnsureRowsMatchRegistry()
        {
            if (registry == null) return;
            int activeCount = registry.Records.Count(
                record => record.LifecycleStatus == AntLifecycleStatus.Active);
            if (activeCount != displayedRecords.Count ||
                displayedRecords.Any(record =>
                    record.LifecycleStatus != AntLifecycleStatus.Active))
                RebuildRows();
        }
    }
}
