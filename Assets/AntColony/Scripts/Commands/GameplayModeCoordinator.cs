using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum GameplayAutonomyOverride
    {
        FollowNormalGameplay,
        ForcedEnabled,
        ForcedDisabled
    }

    public enum GameplayAutonomySubsystem
    {
        Survival,
        ColonyWork,
        Hauling
    }

    /// <summary>
    /// Applies the normal-game bundle without conflating autonomy with pause.
    /// Developer overrides remain explicit and never change clock state.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class GameplayModeCoordinator : MonoBehaviour
    {
        [SerializeField] private SimulationClockController simulationClock;
        [SerializeField] private AntSelectionCommandController commandController;

        public GameplayAutonomyOverride SurvivalOverride { get; private set; }
        public GameplayAutonomyOverride ColonyWorkOverride { get; private set; }
        public GameplayAutonomyOverride HaulingOverride { get; private set; }
        public SimulationClockController SimulationClock => simulationClock;

        private void Awake()
        {
            simulationClock ??= GetComponent<SimulationClockController>();
            commandController ??= GetComponent<AntSelectionCommandController>();
            commandController?.BindSimulationClock(simulationClock);
            ApplyAll();
        }

        public GameplayAutonomyOverride GetOverride(GameplayAutonomySubsystem subsystem)
        {
            return subsystem switch
            {
                GameplayAutonomySubsystem.Survival => SurvivalOverride,
                GameplayAutonomySubsystem.ColonyWork => ColonyWorkOverride,
                _ => HaulingOverride
            };
        }

        public void CycleOverride(GameplayAutonomySubsystem subsystem)
        {
            GameplayAutonomyOverride current = GetOverride(subsystem);
            GameplayAutonomyOverride next = current switch
            {
                GameplayAutonomyOverride.FollowNormalGameplay =>
                    GameplayAutonomyOverride.ForcedEnabled,
                GameplayAutonomyOverride.ForcedEnabled =>
                    GameplayAutonomyOverride.ForcedDisabled,
                _ => GameplayAutonomyOverride.FollowNormalGameplay
            };
            SetOverride(subsystem, next);
        }

        public void SetOverride(
            GameplayAutonomySubsystem subsystem,
            GameplayAutonomyOverride value)
        {
            switch (subsystem)
            {
                case GameplayAutonomySubsystem.Survival:
                    SurvivalOverride = value;
                    break;
                case GameplayAutonomySubsystem.ColonyWork:
                    ColonyWorkOverride = value;
                    break;
                default:
                    HaulingOverride = value;
                    break;
            }
            Apply(subsystem);
        }

        public void ApplyAll()
        {
            Apply(GameplayAutonomySubsystem.Survival);
            Apply(GameplayAutonomySubsystem.ColonyWork);
            Apply(GameplayAutonomySubsystem.Hauling);
        }

        private void Apply(GameplayAutonomySubsystem subsystem)
        {
            if (commandController == null) return;
            bool enabled = GetOverride(subsystem) !=
                GameplayAutonomyOverride.ForcedDisabled;
            switch (subsystem)
            {
                case GameplayAutonomySubsystem.Survival:
                    commandController.SetAutonomousSurvivalEnabled(enabled);
                    break;
                case GameplayAutonomySubsystem.ColonyWork:
                    commandController.SetAutonomousColonyWorkEnabled(enabled);
                    break;
                default:
                    commandController.SetAutonomousHaulingEnabled(enabled);
                    break;
            }
        }
    }
}
