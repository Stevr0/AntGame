using System.Collections.Generic;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>Player-facing camera/input contexts loaded by WV-B1.</summary>
    public enum PlayerViewContext
    {
        Colony,
        World
    }

    /// <summary>
    /// Binds authored player-HUD presenters to existing authorities. It owns no
    /// ant, clock, job, terrain, inventory, or resource state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHudController : MonoBehaviour, IPlayerUiInputBlocker,
        IPlayerUiKeyboardBlocker
    {
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        [SerializeField] private AntRosterView antRoster;
        [SerializeField] private SelectedAntInspectorView selectedAntInspector;
        [SerializeField] private SimulationControlsView simulationControls;
        [SerializeField] private ColonyViewCommandBar commandBar;
        [SerializeField] private ColonyResourceSummaryView resourceSummary;
        [SerializeField] private ColonyJobStatusView jobStatus;
        [SerializeField] private ColonyManagementView colonyManagement;

        private readonly List<RaycastResult> raycastResults = new();

        public PlayerViewContext CurrentView { get; private set; } =
            PlayerViewContext.Colony;
        public bool IsReady { get; private set; }
        public AntRosterView AntRoster => antRoster;
        public SelectedAntInspectorView SelectedAntInspector => selectedAntInspector;
        public SimulationControlsView SimulationControls => simulationControls;
        public ColonyViewCommandBar CommandBar => commandBar;
        public ColonyResourceSummaryView ResourceSummary => resourceSummary;
        public ColonyJobStatusView JobStatus => jobStatus;
        public ColonyManagementView ColonyManagement => colonyManagement;
        public bool ConsumesEscape => colonyManagement != null &&
            (colonyManagement.IsOpen ||
             colonyManagement.EscapeConsumedFrame == Time.frameCount);

        /// <summary>Called by the existing scene host after its authorities exist.</summary>
        public void Bind(
            AntRegistry antRegistry,
            AntSelectionCommandController commandController,
            SimulationClockController simulationClock,
            IReadOnlyList<ConstructionRecipe> constructionRecipes,
            MaterialStorageWorld materialStorage,
            WorldMaterialDropRegistry materialDrops,
            ConstructionDesignationRegistry constructionDesignations,
            ExcavationDesignationRegistry excavationDesignations)
        {
            if (commandController == null || simulationClock == null)
            {
                IsReady = false;
                return;
            }

            commandController.BindPlayerUiInputBlocker(this);
            antRoster?.Bind(antRegistry, commandController);
            selectedAntInspector?.Bind(commandController);
            simulationControls?.Bind(simulationClock);
            commandBar?.Bind(commandController, constructionRecipes);
            resourceSummary?.Bind(antRegistry, materialStorage, materialDrops,
                constructionDesignations);
            jobStatus?.Bind(antRegistry, constructionDesignations,
                excavationDesignations);
            colonyManagement?.Bind(antRegistry, commandController);
            IsReady = antRegistry != null;
        }

        /// <summary>
        /// Uses the EventSystem and Canvas raycaster at the exact pointer position.
        /// This protects world tools and camera input without hard-coded panel rects.
        /// </summary>
        public bool IsScreenPointBlocked(Vector2 screenPoint)
        {
            EventSystem eventSystem = EventSystem.current;
            if (graphicRaycaster == null || eventSystem == null ||
                !graphicRaycaster.isActiveAndEnabled)
                return false;

            var pointer = new PointerEventData(eventSystem) { position = screenPoint };
            raycastResults.Clear();
            graphicRaycaster.Raycast(pointer, raycastResults);
            return raycastResults.Count > 0;
        }

        /// <summary>Updates presentation only; simulation and orders are untouched.</summary>
        public void SetViewContext(PlayerViewContext view)
        {
            CurrentView = view;
            if (commandBar != null)
                commandBar.gameObject.SetActive(view == PlayerViewContext.Colony);
        }

        /// <summary>Used by the scene authoring utility to wire prefab references.</summary>
        public void Configure(
            GraphicRaycaster raycaster,
            AntRosterView roster,
            SelectedAntInspectorView inspector,
            SimulationControlsView controls,
            ColonyViewCommandBar colonyCommandBar,
            ColonyResourceSummaryView resources,
            ColonyJobStatusView jobs,
            ColonyManagementView management)
        {
            graphicRaycaster = raycaster;
            antRoster = roster;
            selectedAntInspector = inspector;
            simulationControls = controls;
            commandBar = colonyCommandBar;
            resourceSummary = resources;
            jobStatus = jobs;
            colonyManagement = management;
        }
    }
}
