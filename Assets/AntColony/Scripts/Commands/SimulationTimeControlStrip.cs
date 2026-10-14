using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    /// <summary>
    /// Temporary 3.4B player control strip. The authored uGUI HUD remains 3.4C.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    [DisallowMultipleComponent]
    public sealed class SimulationTimeControlStrip : MonoBehaviour
    {
        private const float Margin = 8f;
        private const float Height = 34f;
        private const float PreferredWidth = 430f;

        [SerializeField] private SimulationClockController simulationClock;
        [SerializeField] private AntSelectionCommandController commandController;

        public SimulationClockController SimulationClock => simulationClock;
        public Rect GuiRect => CalculateRect(Screen.width, Screen.height);

        public static Rect CalculateRect(int screenWidth, int screenHeight)
        {
            float width = Mathf.Min(
                PreferredWidth,
                Mathf.Max(1f, screenWidth - Margin * 2f));
            return new Rect(
                Mathf.Max(0f, (screenWidth - width) * 0.5f),
                Mathf.Max(0f, screenHeight - Height - Margin),
                width,
                Height);
        }

        private void Awake()
        {
            simulationClock ??= GetComponent<SimulationClockController>();
            commandController ??= GetComponent<AntSelectionCommandController>();
            PublishInputBlocker();
        }

        private void Update()
        {
            // Game-view dimensions can change while the Editor is running.
            PublishInputBlocker();
        }

        private void OnGUI()
        {
            if (simulationClock == null) return;
            PublishInputBlocker();

            GUILayout.BeginArea(GuiRect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"Day {simulationClock.DayNumber} · " +
                $"{simulationClock.HourOfDay:00}:{simulationClock.MinuteOfHour:00}",
                GUILayout.Width(112f));
            if (GUILayout.Button(
                    simulationClock.IsRunning ? "❚❚ Pause" : "▶ Play",
                    GUILayout.Width(76f)))
                simulationClock.TogglePause();
            DrawSpeedButton("1×", SimulationSpeed.Normal);
            DrawSpeedButton("2×", SimulationSpeed.Fast);
            DrawSpeedButton("3×", SimulationSpeed.VeryFast);
            GUILayout.Label(
                simulationClock.IsRunning
                    ? $"Running {(int)simulationClock.SelectedSpeed}×"
                    : $"Paused · resume {(int)simulationClock.SelectedSpeed}×",
                GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSpeedButton(string label, SimulationSpeed speed)
        {
            bool selected = simulationClock.SelectedSpeed == speed;
            Color previous = GUI.color;
            if (selected) GUI.color = new Color(0.65f, 1f, 0.7f);
            if (GUILayout.Button(label, GUILayout.Width(42f)))
                simulationClock.SetRunningSpeed(speed);
            GUI.color = previous;
        }

        private void PublishInputBlocker()
        {
            commandController?.SetTimeControlUiBlocker(GuiRect);
        }
    }
}
