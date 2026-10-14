using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>Player-facing controls for the one scene-owned simulation clock.</summary>
    public sealed class SimulationControlsView : MonoBehaviour
    {
        [SerializeField] private Text timeText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Text playPauseLabel;
        [SerializeField] private Button normalSpeedButton;
        [SerializeField] private Button fastSpeedButton;
        [SerializeField] private Button veryFastSpeedButton;

        private SimulationClockController simulationClock;

        public SimulationClockController SimulationClock => simulationClock;
        public string DisplayedTime => timeText != null ? timeText.text : string.Empty;

        private void Update() => Refresh();

        public void Bind(SimulationClockController clock)
        {
            simulationClock = clock;
            playPauseButton.onClick.RemoveAllListeners();
            normalSpeedButton.onClick.RemoveAllListeners();
            fastSpeedButton.onClick.RemoveAllListeners();
            veryFastSpeedButton.onClick.RemoveAllListeners();
            playPauseButton.onClick.AddListener(() => simulationClock?.TogglePause());
            normalSpeedButton.onClick.AddListener(() =>
                simulationClock?.SetRunningSpeed(SimulationSpeed.Normal));
            fastSpeedButton.onClick.AddListener(() =>
                simulationClock?.SetRunningSpeed(SimulationSpeed.Fast));
            veryFastSpeedButton.onClick.AddListener(() =>
                simulationClock?.SetRunningSpeed(SimulationSpeed.VeryFast));
            Refresh();
        }

        public void Refresh()
        {
            if (simulationClock == null) return;
            timeText.text = $"Day {simulationClock.DayNumber}  {simulationClock.HourOfDay:00}:{simulationClock.MinuteOfHour:00}";
            playPauseLabel.text = simulationClock.IsRunning ? "Pause" : "Play";
            statusText.text = simulationClock.IsRunning
                ? $"Running {(int)simulationClock.SelectedSpeed}×"
                : $"Paused · resume {(int)simulationClock.SelectedSpeed}×";
            SetSelected(normalSpeedButton, simulationClock.SelectedSpeed == SimulationSpeed.Normal);
            SetSelected(fastSpeedButton, simulationClock.SelectedSpeed == SimulationSpeed.Fast);
            SetSelected(veryFastSpeedButton, simulationClock.SelectedSpeed == SimulationSpeed.VeryFast);
        }

        public void Configure(Text clockText, Text runningStatus, Button playPause,
            Text playLabel, Button normal, Button fast, Button veryFast)
        {
            timeText = clockText;
            statusText = runningStatus;
            playPauseButton = playPause;
            playPauseLabel = playLabel;
            normalSpeedButton = normal;
            fastSpeedButton = fast;
            veryFastSpeedButton = veryFast;
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
