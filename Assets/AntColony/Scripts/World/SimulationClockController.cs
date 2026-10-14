using UnityEngine;

namespace AntColony.World
{
    /// <summary>
    /// Scene-owned authority for gameplay time. Physics uses Time.timeScale;
    /// fixedDeltaTime and maximumDeltaTime remain at their project baselines.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class SimulationClockController : MonoBehaviour, ISimulationClock
    {
        [SerializeField, Min(0.0001f)]
        private float simulationHoursPerRealSecond = 1f;

        private SimulationClockState state;
        private float originalTimeScale;
        private float originalFixedDeltaTime;
        private float originalMaximumDeltaTime;
        private bool capturedUnityTimeSettings;

        public bool IsRunning => State.IsRunning;
        public SimulationSpeed SelectedSpeed => State.SelectedSpeed;
        public double SpeedMultiplier => State.SpeedMultiplier;
        public double DeltaSeconds => State.DeltaSeconds;
        public double DeltaHours => State.DeltaHours;
        public double ElapsedSeconds => State.ElapsedSeconds;
        public double ElapsedHours => State.ElapsedHours;
        public int DayNumber => State.DayNumber;
        public int HourOfDay => State.HourOfDay;
        public int MinuteOfHour => State.MinuteOfHour;
        public SimulationClockState State => state ??=
            new SimulationClockState(simulationHoursPerRealSecond);

        private void Awake()
        {
            CaptureUnityTimeSettings();
            State.Reset();
            ApplyUnityTimeScale();
        }

        private void OnEnable()
        {
            if (state == null || capturedUnityTimeSettings) return;
            CaptureUnityTimeSettings();
            ApplyUnityTimeScale();
        }

        private void Update()
        {
            State.Advance(Time.unscaledDeltaTime, Time.maximumDeltaTime);
        }

        private void OnDisable()
        {
            RestoreUnityTimeSettings();
        }

        private void OnDestroy()
        {
            RestoreUnityTimeSettings();
        }

        public void Pause()
        {
            State.Pause();
            ApplyUnityTimeScale();
        }

        public void Resume()
        {
            State.Resume();
            ApplyUnityTimeScale();
        }

        public void TogglePause()
        {
            if (IsRunning) Pause();
            else Resume();
        }

        public void SetRunningSpeed(SimulationSpeed speed)
        {
            State.SetRunningSpeed(speed);
            ApplyUnityTimeScale();
        }

        public bool TryAdvanceDevelopmentHours(double hours) =>
            State.TryAdvanceDevelopmentHours(hours);

        public void ResetClock()
        {
            State.Reset();
            ApplyUnityTimeScale();
        }

        private void CaptureUnityTimeSettings()
        {
            if (capturedUnityTimeSettings) return;
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            originalMaximumDeltaTime = Time.maximumDeltaTime;
            capturedUnityTimeSettings = true;
        }

        private void ApplyUnityTimeScale()
        {
            CaptureUnityTimeSettings();
            Time.timeScale = IsRunning ? (int)SelectedSpeed : 0f;
            // Retaining a constant simulation step keeps collision/falling
            // integration equivalent across speeds. Unity simply runs more fixed
            // steps per real second at 2x and 3x.
            Time.fixedDeltaTime = originalFixedDeltaTime;
            Time.maximumDeltaTime = originalMaximumDeltaTime;
        }

        private void RestoreUnityTimeSettings()
        {
            if (!capturedUnityTimeSettings) return;
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            Time.maximumDeltaTime = originalMaximumDeltaTime;
            capturedUnityTimeSettings = false;
        }
    }
}
