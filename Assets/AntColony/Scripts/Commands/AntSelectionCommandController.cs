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
        private const float PlayerPanelHeight = 292f;
        private const float MinimumClickRadiusPixels = 16f;

        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridNavigationWorldService navigationWorld;

        private readonly List<SelectableGridAnt> selectableAnts = new();
        private Rect additionalBlockedGuiRect;
        private bool hasAdditionalBlockedGuiRect;
        private string interactionMessage = "Left-click an ant to select it.";

        public IReadOnlyList<SelectableGridAnt> SelectableAnts => selectableAnts;
        public SelectableGridAnt SelectedAnt { get; private set; }
        public string InteractionMessage => interactionMessage;
        public Rect PlayerPanelGuiRect => new(
            Mathf.Max(12f, Screen.width - PlayerPanelWidth - 12f),
            12f,
            PlayerPanelWidth,
            PlayerPanelHeight);

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
            }

            GUILayout.Label(interactionMessage);
            GUILayout.EndArea();
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
