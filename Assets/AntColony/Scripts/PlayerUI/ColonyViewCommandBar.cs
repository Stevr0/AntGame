using System;
using System.Collections.Generic;
using AntColony.Commands;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>Colony View-only terrain commands; no World View tools are implied.</summary>
    public sealed class ColonyViewCommandBar : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Button excavateButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button cancelDesignationButton;
        [SerializeField] private GameObject buildMenu;
        [SerializeField] private Button[] recipeButtons;
        [SerializeField] private Text[] recipeLabels;
        [SerializeField] private Text statusText;

        private AntSelectionCommandController commandController;
        private IReadOnlyList<ConstructionRecipe> recipes = Array.Empty<ConstructionRecipe>();

        public PlayerViewContext ViewContext => PlayerViewContext.Colony;
        public int VisibleRecipeCount { get; private set; }
        public bool BuildMenuVisible => buildMenu != null && buildMenu.activeSelf;

        private void Update()
        {
            if (commandController != null) statusText.text = commandController.InteractionMessage;
        }

        public void Bind(AntSelectionCommandController controller,
            IReadOnlyList<ConstructionRecipe> constructionRecipes)
        {
            commandController = controller;
            recipes = constructionRecipes ?? Array.Empty<ConstructionRecipe>();
            selectButton.onClick.RemoveAllListeners();
            excavateButton.onClick.RemoveAllListeners();
            buildButton.onClick.RemoveAllListeners();
            cancelDesignationButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(SelectNormalTool);
            excavateButton.onClick.AddListener(() => SetMode(WorldDesignationMode.Excavate));
            buildButton.onClick.AddListener(ToggleBuildMenu);
            cancelDesignationButton.onClick.AddListener(() =>
                SetMode(WorldDesignationMode.CancelDesignation));

            VisibleRecipeCount = Mathf.Min(recipes.Count, recipeButtons.Length);
            for (int index = 0; index < recipeButtons.Length; index++)
            {
                bool visible = index < VisibleRecipeCount;
                recipeButtons[index].gameObject.SetActive(visible);
                if (!visible) continue;
                ConstructionRecipe recipe = recipes[index];
                recipeLabels[index].text = $"{recipe.Result.DisplayName}\n{ReadableResource(recipe.ResourceTypeId)} {recipe.RequiredQuantity} · {recipe.BaseDurationHours:0.#}h";
                recipeButtons[index].onClick.RemoveAllListeners();
                recipeButtons[index].onClick.AddListener(() => SelectRecipe(recipe.StableId));
            }
            buildMenu.SetActive(false);
            RefreshButtonState();
        }

        public void Configure(Button select, Button excavate, Button build,
            Button cancel, GameObject recipeMenu, Button[] buttons,
            Text[] labels, Text status)
        {
            selectButton = select;
            excavateButton = excavate;
            buildButton = build;
            cancelDesignationButton = cancel;
            buildMenu = recipeMenu;
            recipeButtons = buttons ?? Array.Empty<Button>();
            recipeLabels = labels ?? Array.Empty<Text>();
            statusText = status;
        }

        private void SelectNormalTool()
        {
            buildMenu.SetActive(false);
            SetMode(WorldDesignationMode.None);
        }

        private void ToggleBuildMenu()
        {
            buildMenu.SetActive(!buildMenu.activeSelf);
            RefreshButtonState();
        }

        private void SelectRecipe(string stableId)
        {
            commandController?.TrySelectConstructionRecipe(stableId);
            RefreshButtonState();
        }

        private void SetMode(WorldDesignationMode mode)
        {
            buildMenu.SetActive(false);
            commandController?.SetDesignationMode(mode);
            RefreshButtonState();
        }

        private void RefreshButtonState()
        {
            WorldDesignationMode mode = commandController != null
                ? commandController.DesignationMode : WorldDesignationMode.None;
            SetSelected(selectButton, mode == WorldDesignationMode.None);
            SetSelected(excavateButton, mode == WorldDesignationMode.Excavate);
            SetSelected(buildButton, mode == WorldDesignationMode.BackgroundConversion ||
                BuildMenuVisible);
            SetSelected(cancelDesignationButton,
                mode == WorldDesignationMode.CancelDesignation);
        }

        private static string ReadableResource(string stableId)
        {
            return MaterialResourceCatalog.TryGetMaterial(stableId, out ExcavatedMaterialType material)
                ? material.ToString() : stableId;
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button?.targetGraphic != null)
                button.targetGraphic.color = selected
                    ? new Color32(189, 139, 62, 255)
                    : new Color32(77, 67, 55, 255);
        }
    }
}
