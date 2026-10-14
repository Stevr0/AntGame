using AntColony.World;
using AntColony.Commands;
using UnityEngine;

namespace AntColony.Tests.PlayMode
{
    /// <summary>
    /// Legacy play-mode scenarios predate the player-facing paused start. They
    /// opt into running time explicitly so their original intent is unchanged.
    /// </summary>
    internal static class PlayModeSimulationTestUtility
    {
        public static void Resume()
        {
            Object.FindFirstObjectByType<SimulationClockController>()?.Resume();
            AntSelectionCommandController controller =
                Object.FindFirstObjectByType<AntSelectionCommandController>();
            if (controller == null) return;

            // Preserve the pre-3.4B isolated-test baseline. Individual legacy
            // tests explicitly enable the one subsystem they are exercising.
            controller.SetAutonomousSurvivalEnabled(false);
            controller.SetAutonomousColonyWorkEnabled(false);
            controller.SetAutonomousHaulingEnabled(false);
        }
    }
}
