using System;
using System.Collections.Generic;
using AntColony.Ants;
using AntColony.Navigation;
using AntColony.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Commands
{
    public enum WorldDesignationMode
    {
        None,
        Excavate,
        BackgroundConversion,
        CancelDesignation
    }

    /// <summary>
    /// Read-only boundary used by player presentation to stop pointer input before
    /// it reaches Colony View tools. Commands do not depend on a particular UI kit.
    /// </summary>
    public interface IPlayerUiInputBlocker
    {
        bool IsScreenPointBlocked(Vector2 screenPoint);
    }

    /// <summary>Optional keyboard-focus boundary for modal player UI.</summary>
    public interface IPlayerUiKeyboardBlocker
    {
        bool ConsumesEscape { get; }
    }

    /// <summary>Mutually exclusive sections in the development toolbar.</summary>
    public enum DevelopmentUiCategory
    {
        None,
        Ant,
        Needs,
        Skills,
        Jobs,
        Resources,
        World,
        Debug
    }

    /// <summary>
    /// Player-facing single-ant selection and temporary move-order input. The
    /// controller owns neither terrain navigation nor ant movement execution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AntSelectionCommandController : MonoBehaviour
    {
        private const float DevelopmentUiMargin = 8f;
        private const float DevelopmentToolbarHeight = 32f;
        private const float DevelopmentSummaryHeight = 26f;
        private const float DevelopmentPanelMaximumWidth = 760f;
        private const float MinimumClickRadiusPixels = 16f;
        private const double DebugExperienceAmount = 100d;
        private const double DebugMaximumExperienceAmount = 50000d;
        private const double DebugInactivityStepHours = 24d;
        private const double DebugNeedsTimeStepHours = 1d;
        private const double DebugRestStepHours = 8d;
        private const double DebugFatigueStep = 25d;
        private static readonly Vector2Int GeneralMaterialStorageFixture = new(-12, -8);
        private static readonly Vector2Int SoilMaterialStorageFixture = new(9, -8);
        private static readonly Vector2Int StoneMaterialStorageFixture = new(22, -8);
        private static readonly Vector2Int FullMaterialStorageFixture = new(30, -8);
        private static readonly Vector2Int UnreachableMaterialStorageFixture = new(40, -20);
        private static readonly Vector2Int SoilDropFixture = new(-2, -8);
        private static readonly Vector2Int StoneDropFixture = new(8, -8);

        private static readonly string[] SkillDisplayNames =
            BuildSkillDisplayNames();
        private static readonly DevelopmentUiCategory[] DevelopmentToolbarCategories =
        {
            DevelopmentUiCategory.Ant,
            DevelopmentUiCategory.Needs,
            DevelopmentUiCategory.Skills,
            DevelopmentUiCategory.Jobs,
            DevelopmentUiCategory.Resources,
            DevelopmentUiCategory.World,
            DevelopmentUiCategory.Debug
        };

        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridNavigationWorldService navigationWorld;
        [SerializeField] private SimulationClockController simulationClock;
        [SerializeField] private GameplayModeCoordinator gameplayModeCoordinator;

        private readonly List<SelectableGridAnt> selectableAnts = new();
        private readonly Dictionary<SelectableGridAnt, AutonomousSurvivalAgentState>
            autonomousStates = new();
        private readonly AutonomousSurvivalDecisionService autonomousDecisionService = new();
        private readonly Dictionary<SelectableGridAnt, AutonomousWorkAgentState>
            autonomousWorkStates = new();
        private readonly ColonyJobSelectionService colonyJobSelectionService = new();
        private Rect additionalBlockedGuiRect;
        private bool hasAdditionalBlockedGuiRect;
        private Rect timeControlBlockedGuiRect;
        private bool hasTimeControlBlockedGuiRect;
        private string interactionMessage = "Left-click an ant to select it.";
        private int selectedDebugSkillIndex;
        private double simulationHours;
        private SurvivalInteractionWorld interactionWorld;
        private Vector2Int selectedInteractionTarget;
        private bool hasSelectedInteractionTarget;
        private double nextAutonomousEvaluationTime;
        private double autonomousClockAccumulator;
        private int autonomousEvaluationIndex;
        private ColonyJobRegistry colonyJobs;
        private double nextWorkEvaluationTime;
        private int workEvaluationIndex;
        private int selectedWorkCategoryIndex;
        private int nextDevelopmentJobNumber = 1;
        private int nextDevelopmentDropNumber = 1;
        private int selectedInventorySlotIndex;
        private ExcavationDesignationRegistry excavationDesignations;
        private ExcavationWorldTransaction excavationTransaction;
        private WorldMaterialDropRegistry materialDrops;
        private MaterialStorageWorld materialStorage;
        private readonly Dictionary<ColonyJobId, MaterialHaulingPlan> haulingPlans = new();
        private ConstructionDesignationRegistry constructionDesignations;
        private ConstructionWorldTransaction constructionTransaction;
        private IReadOnlyList<ConstructionRecipe> constructionRecipes;
        private int selectedConstructionRecipeIndex;
        private Vector2Int dragStartCell;
        private bool excavationDragActive;
        private readonly List<DesignationPreviewCell> designationPreview = new();
        private bool hasHoveredDesignationCell;
        private Vector2Int hoveredDesignationCell;
        private bool developmentUiVisible;
        private DevelopmentUiCategory expandedDevelopmentCategory;
        private Vector2 developmentPanelScrollPosition;
        private IPlayerUiInputBlocker playerUiInputBlocker;

        public IReadOnlyList<SelectableGridAnt> SelectableAnts => selectableAnts;
        public SelectableGridAnt SelectedAnt { get; private set; }
        public string InteractionMessage => interactionMessage;
        public double SimulationHours => simulationClock != null
            ? simulationClock.ElapsedHours : simulationHours;
        public double SkillSimulationHours => SimulationHours;
        public SimulationClockController SimulationClock => simulationClock;
        public Camera WorldCamera => worldCamera;
        private double CurrentSimulationSeconds => simulationClock != null
            ? simulationClock.ElapsedSeconds : Time.unscaledTimeAsDouble;
        public bool AutonomousSurvivalEnabled { get; private set; }
        public bool AutonomousColonyWorkEnabled { get; private set; }
        public bool AutonomousHaulingEnabled { get; private set; }
        public ColonyJobRegistry ColonyJobs => colonyJobs;
        public ExcavationDesignationRegistry ExcavationDesignations => excavationDesignations;
        public WorldMaterialDropRegistry MaterialDrops => materialDrops;
        public MaterialStorageWorld MaterialStorage => materialStorage;
        public ConstructionDesignationRegistry ConstructionDesignations =>
            constructionDesignations;
        public ConstructionRecipe SelectedConstructionRecipe =>
            constructionRecipes != null && constructionRecipes.Count > 0
                ? constructionRecipes[Mathf.Clamp(selectedConstructionRecipeIndex,
                    0, constructionRecipes.Count - 1)] : null;
        public WorldDesignationMode DesignationMode { get; private set; }
        public IReadOnlyList<DesignationPreviewCell> DesignationPreview =>
            designationPreview;
        public bool HasHoveredDesignationCell => hasHoveredDesignationCell;
        public bool IsDesignationDragActive => excavationDragActive;
        public Vector2Int HoveredDesignationCell => hoveredDesignationCell;
        public string PreviewDiagnostic { get; private set; } = string.Empty;
        public event Action<SelectableGridAnt> SelectionChanged;
        public bool DevelopmentUiVisible => developmentUiVisible;
        public DevelopmentUiCategory ExpandedDevelopmentCategory =>
            expandedDevelopmentCategory;
        public Rect DevelopmentToolbarGuiRect => CalculateToolbarRect(
            Screen.width, Screen.height);
        public Rect DevelopmentSummaryGuiRect => CalculateSummaryRect(
            Screen.width, Screen.height);
        public Rect DevelopmentPanelGuiRect => CalculatePanelRect(
            Screen.width, Screen.height);
        public Rect DevelopmentReopenGuiRect => new(
            Mathf.Max(DevelopmentUiMargin, (Screen.width - 160f) * 0.5f),
            DevelopmentUiMargin, 160f, 30f);
        // Compatibility aliases retained for older tests and callers.
        public Rect DesignationToolbarGuiRect => DevelopmentToolbarGuiRect;
        public Rect PlayerPanelGuiRect => DevelopmentSummaryGuiRect;

        public static Rect CalculateToolbarRect(int screenWidth, int screenHeight) =>
            new(DevelopmentUiMargin, DevelopmentUiMargin,
                Mathf.Max(1f, screenWidth - DevelopmentUiMargin * 2f),
                DevelopmentToolbarHeight);

        public static Rect CalculateSummaryRect(int screenWidth, int screenHeight) =>
            new(DevelopmentUiMargin,
                DevelopmentUiMargin + DevelopmentToolbarHeight + 2f,
                Mathf.Max(1f, screenWidth - DevelopmentUiMargin * 2f),
                DevelopmentSummaryHeight);

        public static Rect CalculatePanelRect(int screenWidth, int screenHeight)
        {
            float top = DevelopmentUiMargin + DevelopmentToolbarHeight +
                DevelopmentSummaryHeight + 6f;
            float width = Mathf.Min(DevelopmentPanelMaximumWidth,
                Mathf.Max(1f, screenWidth - DevelopmentUiMargin * 2f));
            return new Rect(Mathf.Max(0f, (screenWidth - width) * 0.5f), top,
                width, Mathf.Max(1f, screenHeight - top - DevelopmentUiMargin));
        }

        public void SetDevelopmentUiVisible(bool visible)
        {
            developmentUiVisible = visible;
            if (!visible) expandedDevelopmentCategory = DevelopmentUiCategory.None;
        }

        /// <summary>Connects the authored player HUD without making commands own UI.</summary>
        public void BindPlayerUiInputBlocker(IPlayerUiInputBlocker blocker)
        {
            playerUiInputBlocker = blocker;
        }

        public void ToggleDevelopmentCategory(DevelopmentUiCategory category)
        {
            if (category == DevelopmentUiCategory.None) return;
            developmentUiVisible = true;
            expandedDevelopmentCategory = expandedDevelopmentCategory == category
                ? DevelopmentUiCategory.None : category;
            developmentPanelScrollPosition = Vector2.zero;
        }

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

        public void BindSimulationClock(SimulationClockController clock)
        {
            simulationClock = clock;
            simulationHours = clock != null ? clock.ElapsedHours : simulationHours;
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

        public void BindExcavation(ExcavationDesignationRegistry designations,
            ExcavationWorldTransaction transaction, WorldMaterialDropRegistry drops)
        {
            ClearDesignationPreview();
            excavationDesignations = designations;
            excavationTransaction = transaction;
            materialDrops = drops;
            foreach (SelectableGridAnt ant in selectableAnts)
                ant?.BindExcavation(designations, transaction, drops);
        }

        public void BindMaterialStorage(MaterialStorageWorld storage)
        {
            materialStorage = storage;
            haulingPlans.Clear();
            foreach (SelectableGridAnt ant in selectableAnts)
                ant?.BindMaterialHauling(storage);
        }

        public void BindConstruction(ConstructionDesignationRegistry designations,
            ConstructionWorldTransaction transaction,
            IReadOnlyList<ConstructionRecipe> recipes)
        {
            constructionDesignations = designations;
            constructionTransaction = transaction;
            constructionRecipes = recipes;
            selectedConstructionRecipeIndex = 0;
            foreach (SelectableGridAnt ant in selectableAnts)
                ant?.BindConstruction(designations, transaction);
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
                ant.BindExcavation(
                    excavationDesignations, excavationTransaction, materialDrops);
                ant.BindMaterialHauling(materialStorage);
                ant.BindConstruction(constructionDesignations,
                    constructionTransaction);
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

        public void SetAutonomousHaulingEnabled(bool enabled)
        {
            if (AutonomousHaulingEnabled == enabled) return;
            AutonomousHaulingEnabled = enabled;
            if (!enabled)
            {
                foreach (SelectableGridAnt ant in selectableAnts)
                    if (ant?.HaulingOrder?.IsActive == true)
                        ant.CancelOrder("Autonomous hauling was disabled.");
                RemoveDynamicHaulingJobs();
            }
            nextWorkEvaluationTime = 0d;
            interactionMessage = enabled
                ? "Developer Only: autonomous hauling enabled."
                : "Developer Only: autonomous hauling disabled.";
        }

        /// <summary>
        /// Restores the authored storage, loose-drop and carrier-inventory baseline.
        /// This explicit development control makes repeated hauling checks comparable.
        /// </summary>
        public bool ResetMaterialHaulingFixtures(out string message)
        {
            if (materialStorage == null || materialDrops == null)
            {
                message = "Material hauling fixtures are unavailable.";
                interactionMessage = message;
                return false;
            }
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.HaulingOrder?.IsActive == true)
                    ant.CancelOrder("Material hauling fixtures were reset.");
                ant?.Record?.Inventory.Clear();
            }
            RemoveDynamicHaulingJobs();
            materialStorage.Clear();
            materialDrops.Clear();
            string soil = MaterialResourceCatalog.GetStableId(
                ExcavatedMaterialType.Soil);
            string stone = MaterialResourceCatalog.GetStableId(
                ExcavatedMaterialType.Stone);
            (Vector2Int cell, MaterialStorageFilter filter, string resource,
                int quantity)[] slots =
            {
                (GeneralMaterialStorageFixture,
                    MaterialStorageFilter.AllBuildingMaterials, soil, 5),
                (SoilMaterialStorageFixture,
                    MaterialStorageFilter.Soil, string.Empty, 0),
                (StoneMaterialStorageFixture,
                    MaterialStorageFilter.Stone, string.Empty, 0),
                (FullMaterialStorageFixture,
                    MaterialStorageFilter.AllBuildingMaterials, stone, 10),
                (UnreachableMaterialStorageFixture,
                    MaterialStorageFilter.AllBuildingMaterials, string.Empty, 0)
            };
            foreach (var slot in slots)
                if (!materialStorage.TryAdd(slot.cell, slot.filter, slot.resource,
                        slot.quantity, MaterialResourceCatalog.ProvisionalMaximumStack,
                        out message))
                {
                    interactionMessage = message;
                    return false;
                }
            if (!materialDrops.TryAdd("development.drop.soil",
                    ExcavatedMaterialType.Soil, 4,
                    SoilDropFixture,
                    out _, out message) ||
                !materialDrops.TryAdd("development.drop.stone",
                    ExcavatedMaterialType.Stone, 4,
                    StoneDropFixture,
                    out _, out message))
            {
                interactionMessage = message;
                return false;
            }
            nextWorkEvaluationTime = 0d;
            message = "Reset Material Storage, loose drops and ant inventories.";
            interactionMessage = message;
            return true;
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

        public void SetTimeControlUiBlocker(Rect guiRect)
        {
            timeControlBlockedGuiRect = guiRect;
            hasTimeControlBlockedGuiRect = guiRect.width > 0f && guiRect.height > 0f;
        }

        public bool IsScreenPointBlocked(Vector2 screenPoint)
        {
            if (playerUiInputBlocker?.IsScreenPointBlocked(screenPoint) == true)
                return true;
            Vector2 guiPoint = new(screenPoint.x, Screen.height - screenPoint.y);
            return IsGuiPointBlocked(guiPoint);
        }

        /// <summary>
        /// Central UI hit test shared by commands, excavation, inspection and
        /// camera mouse input. Hidden UI blocks only its small reopen control.
        /// </summary>
        public bool IsGuiPointBlocked(Vector2 guiPoint)
        {
            if (hasTimeControlBlockedGuiRect &&
                timeControlBlockedGuiRect.Contains(guiPoint)) return true;
            if (!developmentUiVisible)
                return DevelopmentReopenGuiRect.Contains(guiPoint);
            return DevelopmentToolbarGuiRect.Contains(guiPoint) ||
                DevelopmentSummaryGuiRect.Contains(guiPoint) ||
                expandedDevelopmentCategory != DevelopmentUiCategory.None &&
                    DevelopmentPanelGuiRect.Contains(guiPoint) ||
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
            if (DesignationMode != WorldDesignationMode.None) return false;
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

        public void SetDesignationMode(WorldDesignationMode mode)
        {
            DesignationMode = mode;
            excavationDragActive = false;
            ClearDesignationPreview();
            interactionMessage = mode == WorldDesignationMode.Excavate
                ? "Excavate mode: left-click or drag over foreground Solids."
                : mode == WorldDesignationMode.BackgroundConversion
                    ? $"Background Conversion: {SelectedConstructionRecipe?.Result.DisplayName ?? "No recipe"}."
                    : mode == WorldDesignationMode.CancelDesignation
                        ? "Cancel designation: click or drag over Colony View jobs."
                        : "Designation mode closed; selection controls restored.";
        }

        /// <summary>Selects an authored recipe by stable ID for the player build menu.</summary>
        public bool TrySelectConstructionRecipe(string stableId)
        {
            if (constructionRecipes == null || string.IsNullOrWhiteSpace(stableId))
                return false;
            for (int index = 0; index < constructionRecipes.Count; index++)
            {
                if (!string.Equals(constructionRecipes[index].StableId, stableId,
                        StringComparison.Ordinal)) continue;
                selectedConstructionRecipeIndex = index;
                SetDesignationMode(WorldDesignationMode.BackgroundConversion);
                return true;
            }
            return false;
        }

        public void CycleConstructionRecipe(int direction)
        {
            if (constructionRecipes == null || constructionRecipes.Count == 0) return;
            selectedConstructionRecipeIndex =
                (selectedConstructionRecipeIndex + direction + constructionRecipes.Count) %
                constructionRecipes.Count;
            interactionMessage =
                $"Background Conversion target: {SelectedConstructionRecipe.Result.DisplayName}.";
        }

        public bool TryDesignateConstruction(Vector2Int cell, out string message)
        {
            ConstructionRecipe recipe = SelectedConstructionRecipe;
            if (constructionDesignations == null || recipe == null)
            {
                message = "Construction services or recipes are unavailable.";
                return false;
            }
            bool result = constructionDesignations.TryDesignate(cell,
                recipe.Result.StableMaterialId, out _, out message);
            if (result)
                message = $"Designated {cell} for {recipe.Result.DisplayName}.";
            interactionMessage = message;
            nextWorkEvaluationTime = 0d;
            return result;
        }

        public int DesignateConstructionArea(Vector2Int first, Vector2Int second,
            out string message)
        {
            ConstructionRecipe recipe = SelectedConstructionRecipe;
            if (constructionDesignations == null || recipe == null)
            {
                message = "Construction services or recipes are unavailable.";
                return 0;
            }
            int count = constructionDesignations.DesignateArea(first, second,
                recipe.Result.StableMaterialId, out message);
            interactionMessage = message;
            nextWorkEvaluationTime = 0d;
            return count;
        }

        public bool TryCancelConstruction(Vector2Int cell, out string message)
        {
            if (constructionDesignations == null)
            {
                message = "Construction services are unavailable.";
                return false;
            }
            foreach (SelectableGridAnt ant in selectableAnts)
                if (ant?.ConstructionOrder?.Job.TargetCell == cell && ant.HasActiveOrder)
                    ant.CancelOrder("Background conversion was cancelled.");
            bool result = constructionDesignations.TryCancel(cell, out message);
            interactionMessage = result
                ? $"Cancelled Background conversion at {cell}." : message;
            return result;
        }

        public bool TryDesignateExcavation(Vector2Int cell, out string message)
        {
            if (excavationDesignations == null)
            {
                message = "Excavation services are unavailable.";
                return false;
            }
            bool result = excavationDesignations.TryDesignate(cell, out _, out message);
            if (result) message = $"Designated foreground Solid at {cell}.";
            interactionMessage = message;
            nextWorkEvaluationTime = 0d;
            return result;
        }

        public int DesignateExcavationArea(Vector2Int first, Vector2Int second,
            out string message)
        {
            if (excavationDesignations == null)
            {
                message = "Excavation services are unavailable.";
                return 0;
            }
            int count = excavationDesignations.DesignateArea(first, second, out message);
            interactionMessage = message;
            nextWorkEvaluationTime = 0d;
            return count;
        }

        public bool TryCancelExcavation(Vector2Int cell, out string message)
        {
            if (excavationDesignations == null)
            {
                message = "Excavation services are unavailable.";
                return false;
            }
            foreach (SelectableGridAnt ant in selectableAnts)
                if (ant?.ExcavationOrder?.Job.TargetCell == cell && ant.HasActiveOrder)
                    ant.CancelOrder("Excavation designation was cancelled.");
            bool result = excavationDesignations.TryCancel(cell, out message);
            interactionMessage = result ? $"Cancelled excavation at {cell}." : message;
            return result;
        }

        /// <summary>
        /// Cancels only the designation compatible with the active tool. The
        /// pointer event is still consumed when no designation exists so a move
        /// order cannot leak through the same right-click.
        /// </summary>
        public bool TryHandleDesignationRightClick(Vector2Int cell)
        {
            if (DesignationMode == WorldDesignationMode.Excavate)
                return TryCancelExcavation(cell, out _);
            if (DesignationMode == WorldDesignationMode.BackgroundConversion)
                return TryCancelConstruction(cell, out _);
            return false;
        }

        /// <summary>Rebuilds the bounded visual preview without changing the world.</summary>
        public void UpdateDesignationPreview(Vector2Int hoveredCell, bool dragging)
        {
            designationPreview.Clear();
            hasHoveredDesignationCell = true;
            hoveredDesignationCell = hoveredCell;
            Vector2Int first = dragging ? dragStartCell : hoveredCell;
            int minimumX = Mathf.Min(first.x, hoveredCell.x);
            int maximumX = Mathf.Max(first.x, hoveredCell.x);
            int minimumY = Mathf.Min(first.y, hoveredCell.y);
            int maximumY = Mathf.Max(first.y, hoveredCell.y);
            int considered = 0;
            int maximum = DesignationMode == WorldDesignationMode.BackgroundConversion
                ? ConstructionDesignationRegistry.MaximumAreaCells
                : ExcavationDesignationRegistry.MaximumAreaCells;

            for (int y = minimumY; y <= maximumY && considered < maximum; y++)
            for (int x = minimumX; x <= maximumX && considered < maximum; x++)
            {
                considered++;
                Vector2Int cell = new(x, y);
                DesignationPreviewState state;
                string reason;
                if (DesignationMode == WorldDesignationMode.Excavate &&
                    excavationDesignations != null)
                    state = excavationDesignations.EvaluateCell(cell, out reason);
                else if (DesignationMode == WorldDesignationMode.BackgroundConversion &&
                    constructionDesignations != null && SelectedConstructionRecipe != null)
                    state = constructionDesignations.EvaluateCell(cell,
                        SelectedConstructionRecipe.Result.StableMaterialId, out reason);
                else
                {
                    state = DesignationPreviewState.Invalid;
                    reason = "The active designation tool is unavailable.";
                }
                designationPreview.Add(new DesignationPreviewCell(cell, state, reason));
            }

            PreviewDiagnostic = designationPreview.Count == 1
                ? designationPreview[0].Reason
                : $"Previewing {designationPreview.Count} cells.";
        }

        /// <summary>Testable pointer-independent entry point for drag presentation.</summary>
        public void BeginDesignationDragPreview(Vector2Int startCell)
        {
            dragStartCell = startCell;
            excavationDragActive = true;
            UpdateDesignationPreview(startCell, true);
        }

        public void ClearDesignationPreview()
        {
            designationPreview.Clear();
            hasHoveredDesignationCell = false;
            PreviewDiagnostic = string.Empty;
        }

        public void CancelDesignationDragPreview()
        {
            excavationDragActive = false;
            ClearDesignationPreview();
        }

        /// <summary>
        /// Cancels accepted excavation and construction designations in a bounded
        /// area. Each registry retains responsibility for material preservation.
        /// </summary>
        public int CancelDesignationArea(Vector2Int first, Vector2Int second,
            out string summary)
        {
            int minX = Mathf.Min(first.x, second.x);
            int maxX = Mathf.Max(first.x, second.x);
            int minY = Mathf.Min(first.y, second.y);
            int maxY = Mathf.Max(first.y, second.y);
            int considered = 0;
            int cancelled = 0;
            int maximum = Mathf.Max(ExcavationDesignationRegistry.MaximumAreaCells,
                ConstructionDesignationRegistry.MaximumAreaCells);
            for (int y = minY; y <= maxY && considered < maximum; y++)
            for (int x = minX; x <= maxX && considered < maximum; x++)
            {
                considered++;
                Vector2Int cell = new(x, y);
                bool constructionCancelled = TryCancelConstruction(cell, out _);
                bool excavationCancelled = TryCancelExcavation(cell, out _);
                if (constructionCancelled || excavationCancelled) cancelled++;
            }
            summary = $"Cancelled {cancelled} designation(s) from {considered} cell(s).";
            interactionMessage = summary;
            return cancelled;
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
            SelectionChanged?.Invoke(SelectedAnt);
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
            if (materialDrops != null &&
                materialDrops.TryFindAtCell(cell, out WorldMaterialDrop drop))
                return TryIssueMaterialPickupOrder(drop);

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

        public bool TryIssueMaterialPickupOrder(WorldMaterialDrop drop)
        {
            if (SelectedAnt?.Record == null || navigationWorld?.Map == null)
            {
                interactionMessage = "Pickup ignored: select an active ant first.";
                return false;
            }
            Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                SelectedAnt.Motor.Position);
            if (!SurvivalInteractionOrder.TryMeasureApproach(
                    navigationWorld.Map, start, drop.WorldCell,
                    out Vector2Int approach, out _, out string error))
            {
                interactionMessage = error;
                return false;
            }
            if (!SelectedAnt.IssueMaterialPickupOrder(drop, approach))
            {
                interactionMessage = SelectedAnt.FailureReason;
                return false;
            }
            MarkPlayerCommand(SelectedAnt);
            interactionMessage =
                $"{SelectedAnt.DisplayName} ordered to pick up {drop.Material}.";
            return true;
        }

        public bool TryDropSelectedInventory(int slotIndex, int quantity,
            out string message)
        {
            if (SelectedAnt?.Record == null || materialDrops == null ||
                navigationWorld?.Map == null)
            {
                message = "Select a bound ant before dropping material.";
                return false;
            }
            Vector2Int cell = WorldTileCoordinates.WorldPositionToCell(
                SelectedAnt.Motor.Position);
            string key = $"manual-drop.{SelectedAnt.Record.Id.Value}." +
                $"{nextDevelopmentDropNumber++:D4}";
            bool succeeded = MaterialDropTransaction.TryDrop(
                SelectedAnt.Record.Inventory, slotIndex, quantity, cell,
                navigationWorld.Map, materialDrops, key,
                out _, out message);
            interactionMessage = succeeded
                ? $"Dropped {quantity} material unit(s) at {cell}." : message;
            return succeeded;
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
                SimulationHours,
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
                    SimulationHours + elapsedHours))
            {
                message = "Simulation time must be finite and non-negative.";
                if (updateInteractionMessage) interactionMessage = message;
                return false;
            }

            double newTime = SimulationHours + elapsedHours;
            if (!TryAdvanceSimulationRecords(
                    elapsedHours, newTime, updateInteractionMessage, out message))
                return false;

            if (simulationClock != null &&
                !simulationClock.TryAdvanceDevelopmentHours(elapsedHours))
            {
                message = "Simulation time could not be advanced.";
                if (updateInteractionMessage) interactionMessage = message;
                return false;
            }

            simulationHours = newTime;
            message = $"Developer Only: simulation time is {newTime:0.##}h.";
            if (updateInteractionMessage) interactionMessage = message;
            return true;
        }

        private bool TryAdvanceSimulationRecords(
            double elapsedHours,
            double newTime,
            bool updateInteractionMessage,
            out string message)
        {
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
            message = string.Empty;
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

            SelectedAnt.Record.ResetSkills(SimulationHours);
            message = $"Developer Only: reset {SelectedAnt.DisplayName}'s skills.";
            interactionMessage = message;
            return true;
        }

        /// <summary>Called when the development world creates fresh records.</summary>
        public void ResetSimulationDebugClock()
        {
            simulationClock?.ResetClock();
            simulationHours = 0d;
            nextDevelopmentDropNumber = 1;
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
            bool simulationRunning = simulationClock == null || simulationClock.IsRunning;
            if (simulationRunning)
            {
                double interactionHours = simulationClock != null
                    ? simulationClock.DeltaHours
                    : Time.deltaTime * autonomousDecisionService.Settings
                        .SimulationHoursPerRealSecond;
                foreach (SelectableGridAnt ant in selectableAnts)
                    ant?.RefreshOrderStatus(interactionHours, SimulationHours);

                UpdateAutonomousSimulationClock();
                UpdateAutonomousSurvival();
                UpdateAutonomousColonyWork();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard?.f12Key.wasPressedThisFrame == true)
                SetDevelopmentUiVisible(!developmentUiVisible);

            bool escapeConsumed = playerUiInputBlocker is IPlayerUiKeyboardBlocker keyboardBlocker &&
                keyboardBlocker.ConsumesEscape;
            if (keyboard?.escapeKey.wasPressedThisFrame == true &&
                !escapeConsumed && DesignationMode != WorldDesignationMode.None)
                SetDesignationMode(WorldDesignationMode.None);

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                ClearDesignationPreview();
                return;
            }

            Vector2 screenPoint = mouse.position.ReadValue();

            if (DesignationMode != WorldDesignationMode.None)
            {
                if (IsScreenPointBlocked(screenPoint) || worldCamera == null)
                {
                    // Crossing player UI abandons the pending gesture. This
                    // prevents a later mouse release from committing through UI.
                    CancelDesignationDragPreview();
                    return;
                }
                Vector2Int pointerCell = WorldTileCoordinates.WorldPositionToCell(
                    worldCamera.ScreenToWorldPoint(screenPoint));
                if (DesignationMode is WorldDesignationMode.Excavate or
                        WorldDesignationMode.BackgroundConversion)
                    UpdateDesignationPreview(pointerCell, excavationDragActive);
                else ClearDesignationPreview();

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    TryHandleDesignationRightClick(pointerCell);
                    UpdateDesignationPreview(pointerCell, false);
                    return;
                }
                if (mouse.leftButton.wasPressedThisFrame &&
                    !IsScreenPointBlocked(screenPoint) && worldCamera != null)
                {
                    dragStartCell = pointerCell;
                    excavationDragActive = true;
                    UpdateDesignationPreview(pointerCell, true);
                }
                if (mouse.leftButton.wasReleasedThisFrame && excavationDragActive)
                {
                    excavationDragActive = false;
                    if (IsScreenPointBlocked(screenPoint) || worldCamera == null)
                        return;
                    Vector2Int end = WorldTileCoordinates.WorldPositionToCell(
                        worldCamera.ScreenToWorldPoint(screenPoint));
                    if (DesignationMode == WorldDesignationMode.Excavate)
                        DesignateExcavationArea(dragStartCell, end, out _);
                    else if (DesignationMode == WorldDesignationMode.BackgroundConversion)
                        DesignateConstructionArea(dragStartCell, end, out _);
                    else
                        CancelDesignationArea(dragStartCell, end, out _);
                }
                return;
            }
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

            double now = CurrentSimulationSeconds;
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
            autonomousClockAccumulator += simulationClock != null
                ? simulationClock.DeltaSeconds : Time.deltaTime;
            double tickSeconds = autonomousDecisionService.Settings.SimulationTickSeconds;
            if (autonomousClockAccumulator < tickSeconds) return;
            double elapsedHours = autonomousClockAccumulator *
                autonomousDecisionService.Settings.SimulationHoursPerRealSecond;
            autonomousClockAccumulator = 0d;
            TryAdvanceSimulationRecords(
                elapsedHours, SimulationHours,
                updateInteractionMessage: false, out _);
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
                ExcavationJobOrder previousExcavation = ant.ExcavationOrder;
                MaterialHaulingOrder previousHauling = ant.HaulingOrder;
                ConstructionJobOrder previousConstruction = ant.ConstructionOrder;
                bool failed = previous != null &&
                    (previous.Phase == SyntheticJobOrderPhase.Failed ||
                     previous.Phase == SyntheticJobOrderPhase.Cancelled) ||
                    previousExcavation != null &&
                    (previousExcavation.Phase == ExcavationOrderPhase.Failed ||
                     previousExcavation.Phase == ExcavationOrderPhase.Cancelled) ||
                    previousHauling != null &&
                    (previousHauling.Phase == MaterialHaulingPhase.Failed ||
                     previousHauling.Phase == MaterialHaulingPhase.Cancelled) ||
                    previousConstruction != null &&
                    (previousConstruction.Phase == ConstructionOrderPhase.Failed ||
                     previousConstruction.Phase == ConstructionOrderPhase.Cancelled);
                string failure = previousConstruction?.FailureReason ??
                    previousHauling?.FailureReason ??
                    previousExcavation?.FailureReason ?? previous?.FailureReason;
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
            RefreshDynamicHaulingJobs();
            Vector2Int start =
                WorldTileCoordinates.WorldPositionToCell(ant.Motor.Position);
            if (!colonyJobSelectionService.TryChoose(
                    ant.Record, colonyJobs, navigationWorld.Map, start,
                    job => IsJobEligibleForAnt(job, ant.Record),
                    out ColonyJob job, out Vector2Int approach, out string error))
            {
                RecordWorkFailure(state, currentTimeSeconds, error);
                return false;
            }
            bool issued = constructionDesignations != null &&
                    constructionDesignations.TryGet(job.Id, out _)
                ? ant.IssueConstructionJobOrder(job, approach)
                : haulingPlans.TryGetValue(job.Id,
                    out MaterialHaulingPlan haulingPlan)
                ? ant.IssueMaterialHaulingOrder(haulingPlan, approach)
                : excavationDesignations != null &&
                    excavationDesignations.TryGet(job.Id, out _)
                        ? ant.IssueExcavationJobOrder(job, approach)
                        : ant.IssueSyntheticJobOrder(job, approach);
            if (!issued)
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
            double now = CurrentSimulationSeconds;
            if (now < nextWorkEvaluationTime) return;
            workEvaluationIndex %= selectableAnts.Count;
            EvaluateAutonomousColonyWorkNow(
                selectableAnts[workEvaluationIndex], now);
            workEvaluationIndex = (workEvaluationIndex + 1) % selectableAnts.Count;
            nextWorkEvaluationTime = now +
                autonomousDecisionService.Settings.EvaluationIntervalSeconds /
                selectableAnts.Count;
        }

        private bool IsJobEligibleForAnt(ColonyJob job, AntRecord ant)
        {
            if (!haulingPlans.TryGetValue(job.Id, out MaterialHaulingPlan plan))
                return true;
            return AutonomousHaulingEnabled &&
                (!plan.InventoryOwner.IsValid || plan.InventoryOwner.Equals(ant.Id));
        }

        /// <summary>
        /// Material state is small in the deterministic course. Discovery runs at
        /// the existing staggered work cadence, never from a per-drop Update.
        /// </summary>
        private void RefreshDynamicHaulingJobs()
        {
            if (!AutonomousHaulingEnabled || colonyJobs == null || materialDrops == null ||
                materialStorage == null || navigationWorld?.Map == null) return;

            var stale = new List<ColonyJobId>();
            foreach (KeyValuePair<ColonyJobId, MaterialHaulingPlan> entry in haulingPlans)
            {
                ColonyJob job = entry.Value.Job;
                bool terminal = job.Status is ColonyJobStatus.Completed or
                    ColonyJobStatus.Cancelled or ColonyJobStatus.Blocked;
                bool missingDrop = entry.Value.SourceKind == MaterialHaulSourceKind.WorldDrop &&
                    !materialDrops.TryGet(entry.Value.DropKey, out _) &&
                    job.Status is not ColonyJobStatus.Reserved and not ColonyJobStatus.InProgress;
                if (terminal || missingDrop) stale.Add(entry.Key);
            }
            foreach (ColonyJobId id in stale)
            {
                haulingPlans.Remove(id);
                colonyJobs.RemoveWhere(job => job.Id.Equals(id));
            }

            foreach (WorldMaterialDrop drop in materialDrops.Drops)
            {
                string resourceId = MaterialResourceCatalog.GetStableId(drop.Material);
                ColonyJobId id = new($"hauling.drop.{drop.OwnershipKey}");
                if (string.IsNullOrEmpty(resourceId) || haulingPlans.ContainsKey(id)) continue;
                if (colonyJobs.TryAdd(id, AntWorkCategory.Hauling, drop.WorldCell,
                        out ColonyJob job, out _))
                    haulingPlans.Add(id, new MaterialHaulingPlan(job,
                        MaterialHaulSourceKind.WorldDrop, resourceId, drop.OwnershipKey));
            }

            foreach (SelectableGridAnt selectable in selectableAnts)
            {
                AntRecord record = selectable?.Record;
                if (record == null) continue;
                var seen = new HashSet<string>();
                foreach (AntInventorySlot slot in record.Inventory.OccupiedSlots)
                {
                    if (!seen.Add(slot.ResourceTypeId) ||
                        !MaterialResourceCatalog.TryGetMaterial(slot.ResourceTypeId, out _))
                        continue;
                    Vector2Int start = WorldTileCoordinates.WorldPositionToCell(
                        selectable.Motor.Position);
                    if (!MaterialHaulingOrder.TryChooseDestination(materialStorage,
                            navigationWorld.Map, start, slot.ResourceTypeId,
                            slot.MaximumStack, slot.Quantity, out Vector2Int destination,
                            out _, out _, out _)) continue;
                    ColonyJobId id = new(
                        $"hauling.self.{record.Id.Value}.{slot.ResourceTypeId}");
                    if (haulingPlans.ContainsKey(id)) continue;
                    if (colonyJobs.TryAdd(id, AntWorkCategory.Hauling, destination,
                            out ColonyJob job, out _))
                        haulingPlans.Add(id, new MaterialHaulingPlan(job,
                            MaterialHaulSourceKind.PersonalInventory,
                            slot.ResourceTypeId, inventoryOwner: record.Id));
                }
            }
        }

        private void RemoveDynamicHaulingJobs()
        {
            if (colonyJobs != null)
            {
                var ids = new HashSet<ColonyJobId>(haulingPlans.Keys);
                colonyJobs.RemoveWhere(job => ids.Contains(job.Id));
            }
            haulingPlans.Clear();
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
            return TrySetWorkPriority(
                SelectedAnt.Record, category, priority, out message);
        }

        /// <summary>
        /// Updates the existing record-owned matrix without selecting or loading
        /// the ant and without interrupting work already in progress.
        /// </summary>
        public bool TrySetWorkPriority(
            AntRecord record,
            AntWorkCategory category,
            AntWorkPriority priority,
            out string message)
        {
            if (record == null)
            {
                message = "Choose an active colony ant before changing priorities.";
                return false;
            }
            bool changed = record.WorkPriorities.TrySet(category, priority, out message);
            if (changed)
                message = $"{record.DisplayName}: {category} priority is {priority}.";
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
            // The shared registry panel can display real construction jobs too.
            // Route those through their designation boundary so staged material
            // becomes a recoverable world drop instead of being stranded.
            if (constructionDesignations != null &&
                constructionDesignations.TryGet(id,
                    out ConstructionDesignation construction))
                return TryCancelConstruction(construction.Cell, out message);
            foreach (SelectableGridAnt ant in selectableAnts)
            {
                if (ant?.ActiveColonyJob?.Id.Equals(id) == true && ant.HasActiveOrder)
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
                if (ant != null && ant.OrderSource == AntOrderSource.AutonomousWork &&
                    ant.JobOrder != null && ant.JobOrder.IsActive)
                    ant.CancelOrder("Synthetic job fixtures were reset.");
            }
            // Dynamic hauling plans point at registry-owned jobs. Remove both
            // together before rebuilding the older synthetic fixture set.
            RemoveDynamicHaulingJobs();
            colonyJobs.RemoveWhere(job =>
                (excavationDesignations == null ||
                    !excavationDesignations.TryGet(job.Id, out _)) &&
                (constructionDesignations == null ||
                    !constructionDesignations.TryGet(job.Id, out _)));
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
            // These markers are normal gameplay feedback and remain visible when
            // the separate developer toolbar is closed.
            DrawExcavationWorldOverlay();
            if (!developmentUiVisible)
            {
                GUILayout.BeginArea(DevelopmentReopenGuiRect, GUI.skin.box);
                if (GUILayout.Button("Developer Mode (F12)"))
                    SetDevelopmentUiVisible(true);
                GUILayout.EndArea();
                return;
            }

            DrawDevelopmentToolbar();
            DrawSelectedAntSummary();
            if (expandedDevelopmentCategory != DevelopmentUiCategory.None &&
                expandedDevelopmentCategory != DevelopmentUiCategory.World &&
                expandedDevelopmentCategory != DevelopmentUiCategory.Debug)
            {
                GUILayout.BeginArea(DevelopmentPanelGuiRect, GUI.skin.box);
                developmentPanelScrollPosition = GUILayout.BeginScrollView(
                    developmentPanelScrollPosition);
                DrawSelectedDevelopmentCategory();
                DrawDesignationPreviewDiagnostics();
                GUILayout.Space(6f);
                GUILayout.Label(interactionMessage);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void DrawDevelopmentToolbar()
        {
            GUILayout.BeginArea(DevelopmentToolbarGuiRect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide All", GUILayout.Width(72f)))
                SetDevelopmentUiVisible(false);
            foreach (DevelopmentUiCategory category in DevelopmentToolbarCategories)
            {
                string label = expandedDevelopmentCategory == category
                    ? $"▼ {category}" : category.ToString();
                if (GUILayout.Button(label)) ToggleDevelopmentCategory(category);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawDesignationPreviewDiagnostics()
        {
            if (!hasHoveredDesignationCell) return;
            GUILayout.Space(6f);
            GUILayout.Label("DESIGNATION PREVIEW");
            GUILayout.Label($"Mode / cell: {DesignationMode} / {hoveredDesignationCell}");
            GUILayout.Label($"Eligibility: {PreviewDiagnostic}");
        }

        private void DrawSelectedAntSummary()
        {
            GUILayout.BeginArea(DevelopmentSummaryGuiRect, GUI.skin.box);
            if (SelectedAnt == null)
                GUILayout.Label("Selected ant: None — left-click an ant; right-click terrain for a context order.");
            else
                GUILayout.Label(
                    $"{SelectedAnt.DisplayName} | {SelectedAnt.Record?.Caste} | " +
                    $"{SelectedAnt.ControlState} | {SelectedAnt.OrderStatus} | " +
                    $"Inventory {SelectedAnt.Record?.Inventory.TotalQuantity ?? 0}");
            GUILayout.EndArea();
        }

        private void DrawSelectedDevelopmentCategory()
        {
            AntRecord record = SelectedAnt?.Record;
            switch (expandedDevelopmentCategory)
            {
                case DevelopmentUiCategory.Ant:
                    GUILayout.Label("ANT");
                    if (SelectedAnt == null)
                    {
                        GUILayout.Label("Select an ant to inspect or command it.");
                        return;
                    }
                    GUILayout.Label($"Name / ID: {SelectedAnt.DisplayName} / {record.Id}");
                    GUILayout.Label($"Colony / caste: {record.ColonyId} / {record.Caste}");
                    GUILayout.Label($"Attributes: STR {record.Attributes.Strength}  " +
                        $"AGI {record.Attributes.Agility}  END {record.Attributes.Endurance}");
                    GUILayout.Label($"Lifecycle: {record.LifecycleStatus}");
                    GUILayout.Label($"Control: {SelectedAnt.ControlState}");
                    GUILayout.Label($"Order: {SelectedAnt.OrderStatus}");
                    string destination = SelectedAnt.HasDestination
                        ? SelectedAnt.Destination.ToString() : "None";
                    GUILayout.Label($"Destination: {destination}");
                    string failure = string.IsNullOrEmpty(SelectedAnt.FailureReason)
                        ? "None" : SelectedAnt.FailureReason;
                    GUILayout.Label($"Failure: {failure}");
                    GUI.enabled = SelectedAnt.HasActiveOrder;
                    if (GUILayout.Button("Cancel Selected Order")) CancelSelectedOrder();
                    GUI.enabled = true;
                    break;
                case DevelopmentUiCategory.Needs:
                    GUILayout.Label("NEEDS");
                    DrawAutonomyControl(
                        "Survival", GameplayAutonomySubsystem.Survival,
                        AutonomousSurvivalEnabled);
                    GUILayout.Label(AutonomousSurvivalEnabled
                        ? "Autonomous clock: 1 simulation hour / real second"
                        : "Autonomous survival is disabled.");
                    DrawNeedsInspection(record);
                    if (SelectedAnt != null) DrawAutonomousSurvivalInspection(SelectedAnt);
                    DrawNeedsDebugControls();
                    break;
                case DevelopmentUiCategory.Skills:
                    GUILayout.Label("SKILLS");
                    DrawSkillInspection(record);
                    DrawSkillDebugControls();
                    break;
                case DevelopmentUiCategory.Jobs:
                    GUILayout.Label("JOBS");
                    DrawAutonomyControl(
                        "Colony Work", GameplayAutonomySubsystem.ColonyWork,
                        AutonomousColonyWorkEnabled);
                    if (SelectedAnt != null) DrawAutonomousWorkInspection(SelectedAnt);
                    DrawColonyJobsDebugControls();
                    DrawColonyJobDiagnostics();
                    break;
                case DevelopmentUiCategory.Resources:
                    GUILayout.Label("RESOURCES");
                    DrawAutonomyControl(
                        "Hauling", GameplayAutonomySubsystem.Hauling,
                        AutonomousHaulingEnabled);
                    DrawInventoryInspection(record);
                    DrawInventoryDebugControls();
                    DrawResourceDiagnostics();
                    DrawExcavationDiagnostics();
                    DrawMaterialHaulingDiagnostics();
                    break;
            }
        }

        private void DrawAutonomyControl(
            string label,
            GameplayAutonomySubsystem subsystem,
            bool currentlyEnabled)
        {
            gameplayModeCoordinator ??= GetComponent<GameplayModeCoordinator>();
            if (gameplayModeCoordinator != null)
            {
                GameplayAutonomyOverride value =
                    gameplayModeCoordinator.GetOverride(subsystem);
                string policy = value switch
                {
                    GameplayAutonomyOverride.FollowNormalGameplay =>
                        "Follow Normal Gameplay",
                    GameplayAutonomyOverride.ForcedEnabled => "Forced Enabled",
                    _ => "Forced Disabled"
                };
                if (GUILayout.Button($"{label}: {policy}"))
                    gameplayModeCoordinator.CycleOverride(subsystem);
                GUILayout.Label($"Effective state: {(currentlyEnabled ? "On" : "Off")}");
                return;
            }

            // Compatibility fallback for isolated test objects that do not own
            // the scene gameplay coordinator.
            if (!GUILayout.Button(currentlyEnabled
                    ? $"Disable Autonomous {label}"
                    : $"Enable Autonomous {label}")) return;
            switch (subsystem)
            {
                case GameplayAutonomySubsystem.Survival:
                    SetAutonomousSurvivalEnabled(!currentlyEnabled);
                    break;
                case GameplayAutonomySubsystem.ColonyWork:
                    SetAutonomousColonyWorkEnabled(!currentlyEnabled);
                    break;
                default:
                    SetAutonomousHaulingEnabled(!currentlyEnabled);
                    break;
            }
        }

        private void DrawExcavationWorldOverlay()
        {
            if (worldCamera == null) return;
            if (excavationDesignations != null)
            {
                foreach (ExcavationDesignation designation in
                    excavationDesignations.Designations)
                {
                    if (!DesignationOverlayRules.IsVisible(designation)) continue;
                    Color border = designation.State == ExcavationDesignationState.Active
                        ? new Color(1f, 0.45f, 0.1f, 0.95f)
                        : new Color(1f, 0.68f, 0.08f, 0.95f);
                    DrawCellOverlay(designation.Cell, border,
                        new Color(border.r, border.g, border.b, 0.10f),
                        designation.State == ExcavationDesignationState.Active
                            ? "A" : "X");
                }
            }
            if (constructionDesignations != null)
            {
                foreach (ConstructionDesignation designation in
                    constructionDesignations.Designations)
                {
                    if (!DesignationOverlayRules.IsVisible(designation)) continue;
                    Color border = designation.State == ConstructionDesignationState.Active
                        ? new Color(0.25f, 0.9f, 1f, 0.95f)
                        : new Color(0.2f, 0.85f, 0.35f, 0.95f);
                    DrawCellOverlay(designation.Cell, border,
                        new Color(border.r, border.g, border.b, 0.10f),
                        designation.State == ConstructionDesignationState.Active
                            ? "B" : "C");
                }
            }
            if (materialDrops != null)
            {
                foreach (WorldMaterialDrop drop in materialDrops.Drops)
                {
                    Vector3 screen = worldCamera.WorldToScreenPoint(
                        new Vector3(drop.WorldCell.x + 0.5f, drop.WorldCell.y + 0.5f, 0f));
                    if (screen.z >= 0f)
                        GUI.Box(new Rect(screen.x - 24f, Screen.height - screen.y - 12f,
                            48f, 24f), $"{drop.Material} {drop.Quantity}");
                }
            }

            // Transient hover and drag feedback is drawn last so it stays clear
            // above both terrain and any persistent designation at the cell.
            DrawDesignationPreview();
        }

        private void DrawDesignationPreview()
        {
            foreach (DesignationPreviewCell preview in designationPreview)
            {
                Color border = preview.State switch
                {
                    DesignationPreviewState.ExistingDesignation =>
                        new Color(0.65f, 0.65f, 0.58f, 0.95f),
                    DesignationPreviewState.Invalid =>
                        new Color(0.95f, 0.2f, 0.18f, 0.95f),
                    _ when DesignationMode == WorldDesignationMode.Excavate =>
                        new Color(1f, 0.68f, 0.08f, 0.95f),
                    _ => new Color(0.2f, 0.85f, 0.35f, 0.95f)
                };
                string symbol = preview.State == DesignationPreviewState.Invalid
                    ? "!" : preview.State == DesignationPreviewState.ExistingDesignation
                        ? "≡" : DesignationMode == WorldDesignationMode.Excavate
                            ? "X" : "+";
                DrawCellOverlay(preview.Cell, border,
                    new Color(border.r, border.g, border.b, 0.18f), symbol);
            }
        }

        private void DrawCellOverlay(Vector2Int cell, Color border, Color fill,
            string symbol)
        {
            if (!DesignationOverlayGeometry.TryGetCellGuiRect(worldCamera, cell,
                    Screen.height, out Rect rectangle)) return;

            Color previous = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture);

            float thickness = Mathf.Min(DesignationOverlayGeometry.BorderThickness,
                Mathf.Min(rectangle.width, rectangle.height) * 0.5f);
            GUI.color = border;
            GUI.DrawTexture(new Rect(rectangle.xMin, rectangle.yMin,
                rectangle.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rectangle.xMin, rectangle.yMax - thickness,
                rectangle.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rectangle.xMin, rectangle.yMin,
                thickness, rectangle.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rectangle.xMax - thickness, rectangle.yMin,
                thickness, rectangle.height), Texture2D.whiteTexture);

            float markerSize = Mathf.Min(DesignationOverlayGeometry.MarkerSize,
                Mathf.Min(rectangle.width, rectangle.height));
            GUI.Box(new Rect(rectangle.xMax - markerSize, rectangle.yMin,
                markerSize, markerSize), symbol);
            GUI.color = previous;
        }

        private void DrawInventoryInspection(AntRecord record)
        {
            if (record?.Inventory == null) return;
            AntInventory inventory = record.Inventory;
            int haulingLevel = record.Skills.Get(AntSkillId.Hauling).Level;
            int nextUnlock = haulingLevel >= 80 ? -1 : (haulingLevel / 20 + 1) * 20;
            int soilCapacity = inventory.GetAvailableCapacity(
                MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil),
                MaterialResourceCatalog.ProvisionalMaximumStack);
            int stoneCapacity = inventory.GetAvailableCapacity(
                MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone),
                MaterialResourceCatalog.ProvisionalMaximumStack);
            GUILayout.Space(4f);
            GUILayout.Label("PERSONAL MATERIAL INVENTORY");
            GUILayout.Label(
                $"Slots: {inventory.OccupiedSlotCount}/{inventory.NaturalSlotCapacity} | " +
                $"Total carried: {inventory.TotalQuantity} | Hauling {haulingLevel}");
            GUILayout.Label(
                $"Free capacity: Soil {soilCapacity} | Stone {stoneCapacity}");
            GUILayout.Label(nextUnlock < 0
                ? "Natural slot capacity is at its maximum."
                : $"Next natural slot unlock: Hauling {nextUnlock}");
            for (int index = 0; index < inventory.OccupiedSlots.Count; index++)
            {
                AntInventorySlot slot = inventory.OccupiedSlots[index];
                GUILayout.Label(
                    $"Slot {index + 1}: {slot.ResourceTypeId} " +
                    $"{slot.Quantity}/{slot.MaximumStack}");
            }
            if (inventory.OccupiedSlotCount == 0) GUILayout.Label("Inventory empty.");
        }

        private void DrawInventoryDebugControls()
        {
            AntInventory inventory = SelectedAnt?.Record?.Inventory;
            if (inventory == null) return;
            GUILayout.Space(4f);
            GUILayout.Label("DEVELOPER ONLY — INVENTORY");
            if (GUILayout.Button("Add 1 Test Soil"))
            {
                bool added = inventory.TryAdd(MaterialResourceCatalog.GetStableId(
                        ExcavatedMaterialType.Soil), 1,
                    MaterialResourceCatalog.ProvisionalMaximumStack, true,
                    out _, out string error);
                interactionMessage = added
                    ? "Developer Only: added one test Soil."
                    : error;
            }
            if (GUILayout.Button("Add 1 Test Stone"))
            {
                bool added = inventory.TryAdd(MaterialResourceCatalog.GetStableId(
                        ExcavatedMaterialType.Stone), 1,
                    MaterialResourceCatalog.ProvisionalMaximumStack, true,
                    out _, out string error);
                interactionMessage = added
                    ? "Developer Only: added one test Stone."
                    : error;
            }
            if (GUILayout.Button("Fill Available Slots With Soil"))
            {
                int capacity = inventory.GetAvailableCapacity(
                    MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil),
                    MaterialResourceCatalog.ProvisionalMaximumStack);
                if (capacity > 0)
                {
                    bool filled = inventory.TryAdd(
                        MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil),
                        capacity, MaterialResourceCatalog.ProvisionalMaximumStack,
                        false, out _, out string error);
                    interactionMessage = filled
                        ? $"Developer Only: added {capacity} test Soil."
                        : error;
                }
                else interactionMessage = "Inventory has no available Soil capacity.";
            }
            if (inventory.OccupiedSlotCount > 0)
            {
                string[] labels = new string[inventory.OccupiedSlotCount];
                for (int index = 0; index < labels.Length; index++)
                    labels[index] = $"Slot {index + 1}";
                selectedInventorySlotIndex = Mathf.Clamp(
                    selectedInventorySlotIndex, 0, labels.Length - 1);
                selectedInventorySlotIndex = GUILayout.SelectionGrid(
                    selectedInventorySlotIndex, labels, 3);
                if (GUILayout.Button("Drop 1 From Selected Slot"))
                    TryDropSelectedInventory(
                        selectedInventorySlotIndex, 1, out _);
                if (GUILayout.Button("Remove 1 Test Material"))
                {
                    bool removed = inventory.TryRemove(selectedInventorySlotIndex, 1,
                        out _, out _, out string error);
                    interactionMessage = removed
                        ? "Developer Only: removed one test material."
                        : error;
                }
            }
            if (GUILayout.Button("Reset Selected Inventory"))
            {
                inventory.Clear();
                selectedInventorySlotIndex = 0;
                interactionMessage =
                    $"Developer Only: reset {SelectedAnt.DisplayName}'s inventory.";
            }
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
                0d, state.NextEvaluationTime - CurrentSimulationSeconds);
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
                0d, state.NextEvaluationTime - CurrentSimulationSeconds);
            GUILayout.Label($"Next evaluation/retry: {retryIn:0.##}s");
        }

        private void DrawColonyJobsDebugControls()
        {
            if (SelectedAnt?.Record == null)
            {
                GUILayout.Label("Select an ant to edit individual work priorities.");
                return;
            }

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
            DrawConstructionDiagnostics();
        }

        private void DrawConstructionDiagnostics()
        {
            GUILayout.Space(4f);
            GUILayout.Label("BACKGROUND CONSTRUCTION");
            if (constructionDesignations == null)
            {
                GUILayout.Label("Construction services unavailable.");
                return;
            }
            foreach (ConstructionDesignation designation in
                constructionDesignations.Designations)
            {
                ConstructionJobOrder order = null;
                foreach (SelectableGridAnt ant in selectableAnts)
                    if (ant?.ConstructionOrder?.Job == designation.Job)
                        order = ant.ConstructionOrder;
                GUILayout.Label(
                    $"{designation.Cell} | {designation.Recipe.Result.DisplayName} | " +
                    $"{designation.State} | progress {designation.Job.Progress:P0} | " +
                    $"delivered {designation.Site.DeliveredQuantity}/" +
                    $"{designation.Recipe.RequiredQuantity} {designation.Recipe.ResourceTypeId}");
                GUILayout.Label(
                    $"Builder: {(designation.Job.ReservationOwner.IsValid ? designation.Job.ReservationOwner.ToString() : "None")} | " +
                    $"stage: {order?.Phase.ToString() ?? "Waiting"} | " +
                    $"source: {order?.CurrentSourceCell.ToString() ?? "None"}");
                string reason = order?.FailureReason ?? designation.Job.FailureReason;
                if (!string.IsNullOrEmpty(reason)) GUILayout.Label($"Reason: {reason}");
                if (designation.State is not ConstructionDesignationState.Completed and
                    not ConstructionDesignationState.Cancelled &&
                    GUILayout.Button($"Cancel conversion {designation.Cell}"))
                    TryCancelConstruction(designation.Cell, out _);
            }
        }

        private void DrawExcavationDiagnostics()
        {
            GUILayout.Space(4f);
            GUILayout.Label("EXCAVATION DESIGNATIONS & LOOSE MATERIALS");
            if (excavationDesignations == null)
            {
                GUILayout.Label("Excavation services unavailable.");
                return;
            }
            foreach (ExcavationDesignation designation in
                excavationDesignations.Designations)
            {
                ColonyJob job = designation.Job;
                GUILayout.Label(
                    $"{designation.Cell} | {designation.SolidMaterialId} -> " +
                    $"{designation.OutputQuantity} {designation.OutputMaterial} | " +
                    $"{designation.State} | {job.Progress:P0} | owner " +
                    $"{(job.ReservationOwner.IsValid ? job.ReservationOwner.ToString() : "None")}");
                if (!string.IsNullOrEmpty(job.FailureReason))
                    GUILayout.Label($"Transaction/job reason: {job.FailureReason}");
            }
            if (materialDrops == null || materialDrops.Count == 0)
            {
                GUILayout.Label("Loose material drops: None");
                return;
            }
            foreach (WorldMaterialDrop drop in materialDrops.Drops)
                GUILayout.Label(
                    $"Drop {drop.OwnershipKey}: {drop.Quantity} {drop.Material} at {drop.WorldCell}");
        }

        private void DrawMaterialHaulingDiagnostics()
        {
            GUILayout.Space(4f);
            GUILayout.Label("DEVELOPER ONLY — MATERIAL STORAGE & HAULING");
            GUILayout.Label($"Autonomous hauling: {(AutonomousHaulingEnabled ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Dynamic hauling plans: {haulingPlans.Count}");
            if (GUILayout.Button("Reset Material Hauling Fixtures"))
                ResetMaterialHaulingFixtures(out _);
            if (materialStorage == null)
            {
                GUILayout.Label("Material Storage unavailable.");
                return;
            }
            foreach (MaterialStorageSlot slot in materialStorage.Slots)
            {
                string owner = string.IsNullOrEmpty(slot.ReservationOwnerId)
                    ? "None" : slot.ReservationOwnerId;
                string contents = slot.Quantity == 0
                    ? "Empty" : $"{slot.ResourceTypeId} {slot.Quantity}/{slot.MaximumStack}";
                GUILayout.Label(
                    $"{slot.Cell} | {slot.Filter} | {contents} | " +
                    $"incoming {slot.ReservedIncomingQuantity} | owner {owner}");
                if (GUILayout.Button($"Cycle Filter {slot.Cell}"))
                {
                    MaterialStorageFilter next = slot.Filter == MaterialStorageFilter.Soil
                        ? MaterialStorageFilter.Stone
                        : slot.Filter == MaterialStorageFilter.Stone
                            ? MaterialStorageFilter.AllBuildingMaterials
                            : MaterialStorageFilter.Soil;
                    materialStorage.TrySetFilter(slot.Cell, next, out interactionMessage);
                    nextWorkEvaluationTime = 0d;
                }
            }
            if (SelectedAnt?.HaulingOrder != null)
            {
                MaterialHaulingOrder order = SelectedAnt.HaulingOrder;
                int sourceQuantity = order.SourceKind ==
                        MaterialHaulSourceKind.WorldDrop &&
                    materialDrops.TryGet(order.DropKey, out WorldMaterialDrop drop)
                        ? drop.Quantity : SelectedAnt.Record.Inventory.TotalQuantity;
                int sourceReserved = order.SourceKind ==
                        MaterialHaulSourceKind.WorldDrop
                    ? materialDrops.GetReservedQuantity(order.DropKey) : order.Quantity;
                materialStorage.TryGet(order.DestinationCell,
                    out MaterialStorageSlot destination);
                GUILayout.Label(
                    $"Selected haul: {order.Phase} | {order.Quantity} {order.ResourceTypeId} | " +
                    $"source {order.SourceCell} -> storage {order.DestinationCell}");
                GUILayout.Label(
                    $"Source quantity/reserved: {sourceQuantity}/{sourceReserved} | " +
                    $"destination reserved: {destination?.ReservedIncomingQuantity ?? 0} | " +
                    $"job progress: {order.Job.Progress:P0}");
                string failure = string.IsNullOrEmpty(order.FailureReason)
                    ? "None" : order.FailureReason;
                GUILayout.Label($"Failure: {failure}");
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
            if (SelectedAnt?.Record == null)
            {
                GUILayout.Label("Select an ant to use needs testing controls.");
                return;
            }

            AntNeedsState needs = SelectedAnt.Record.Needs;
            GUILayout.Label("DEVELOPER ONLY — NEEDS TESTING");
            GUILayout.Label($"Simulation time: {SimulationHours:0.##}h");
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
            if (SelectedAnt?.Record == null)
            {
                GUILayout.Label("Select an ant to use skill testing controls.");
                return;
            }

            GUILayout.Label("DEVELOPER ONLY — SKILL TESTING");
            GUILayout.Label($"Simulation time: {SimulationHours:0.##}h");
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
