using AntColony.World;
using UnityEngine;

namespace AntColony.Navigation
{
    /// <summary>
    /// Owns the single shared grid map and bounded scheduler for one active world.
    /// Ants reference this component instead of building private terrain graphs.
    /// It is deliberately a normal scene component rather than a global singleton.
    /// </summary>
    public sealed class GridNavigationWorldService : MonoBehaviour
    {
        [SerializeField, Min(1)] private int nodeBudgetPerUpdate = 128;
        [SerializeField, Min(1)] private int nodesPerRequestSlice = 8;

        public ChunkedWorldData WorldData { get; private set; }
        public GridNavigationMap Map { get; private set; }
        public GridNavigationScheduler Scheduler { get; private set; }
        public AntCollisionProfile CollisionProfile { get; private set; }
        public bool IsInitialized =>
            WorldData != null && Map != null && Scheduler != null;
        public int InitializationCount { get; private set; }
        public int NodeBudgetPerUpdate => nodeBudgetPerUpdate;
        public int NodesPerRequestSlice => nodesPerRequestSlice;

        /// <summary>
        /// Creates shared navigation for the supplied authoritative world. Calling
        /// this again with the same world is harmless and does not duplicate it.
        /// </summary>
        public bool Initialize(
            ChunkedWorldData worldData,
            AntCollisionProfile collisionProfile)
        {
            if (worldData == null || !collisionProfile.IsValid)
            {
                return false;
            }

            if (ReferenceEquals(WorldData, worldData) && IsInitialized &&
                ProfilesMatch(CollisionProfile, collisionProfile))
            {
                return true;
            }

            WorldData = worldData;
            CollisionProfile = collisionProfile;
            Map = new GridNavigationMap(worldData, collisionProfile);
            Scheduler = new GridNavigationScheduler(Map);
            InitializationCount++;
            return true;
        }

        private static bool ProfilesMatch(
            AntCollisionProfile first,
            AntCollisionProfile second)
        {
            return first.Size == second.Size &&
                first.Offset == second.Offset &&
                first.CapsuleAxis == second.CapsuleAxis &&
                Mathf.Approximately(
                    first.ClearanceMargin,
                    second.ClearanceMargin);
        }

        /// <summary>Changes development budgets without changing ownership.</summary>
        public void ConfigureBudgets(int totalNodes, int perRequestSlice)
        {
            nodeBudgetPerUpdate = Mathf.Max(1, totalNodes);
            nodesPerRequestSlice = Mathf.Max(1, perRequestSlice);
        }

        /// <summary>
        /// Invalidates shared terrain data and cancels searches from the previous
        /// revision before any stale result can reach an ant.
        /// </summary>
        public void NotifyTerrainChanged(Vector2Int worldCell)
        {
            if (!IsInitialized)
            {
                return;
            }

            Map.InvalidateCell(worldCell);
            Scheduler.NotifyTerrainInvalidated();
        }

        private void Update()
        {
            Scheduler?.ProcessUpdate(
                nodeBudgetPerUpdate,
                nodesPerRequestSlice);
        }
    }
}
