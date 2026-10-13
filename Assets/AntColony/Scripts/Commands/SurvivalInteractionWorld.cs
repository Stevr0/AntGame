using System;
using System.Collections.Generic;
using AntColony.World;
using UnityEngine;

namespace AntColony.Commands
{
    public enum SurvivalResourceKind { Food, Water }
    public enum SurvivalInteractionKind { Eat, Drink, Rest }
    public enum SurvivalInteractionPhase { None, Travelling, Interacting, Completed, Failed, Cancelled }

    /// <summary>One physical stack owned by one functional world cell.</summary>
    public sealed class SurvivalStorageSlot
    {
        private readonly Dictionary<string, int> reservations = new(StringComparer.Ordinal);

        internal SurvivalStorageSlot(
            Vector2Int cell,
            SurvivalResourceKind kind,
            string resourceTypeId,
            int quantity)
        {
            Cell = cell;
            Kind = kind;
            ResourceTypeId = resourceTypeId;
            Quantity = quantity;
        }

        public Vector2Int Cell { get; }
        public SurvivalResourceKind Kind { get; }
        public string ResourceTypeId { get; }
        public int Quantity { get; private set; }
        public int ReservedQuantity { get; private set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;

        internal bool TryReserve(string ownerId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || quantity <= 0 ||
                quantity > AvailableQuantity || reservations.ContainsKey(ownerId))
            {
                return false;
            }

            reservations.Add(ownerId, quantity);
            ReservedQuantity += quantity;
            return true;
        }

        internal bool TryConsume(string ownerId, int quantity)
        {
            if (!reservations.TryGetValue(ownerId, out int reserved) ||
                quantity <= 0 || quantity > reserved || quantity > Quantity)
            {
                return false;
            }

            Quantity -= quantity;
            reservations.Remove(ownerId);
            ReservedQuantity -= reserved;
            return true;
        }

        internal void Release(string ownerId)
        {
            if (reservations.Remove(ownerId, out int reserved))
            {
                ReservedQuantity -= reserved;
            }
        }
    }

    /// <summary>
    /// Small authoritative owner for 3.2B physical stacks and reservations.
    /// It is plain C# data: Tilemaps display the world but never own quantities.
    /// </summary>
    public sealed class SurvivalInteractionWorld
    {
        private readonly ChunkedWorldData worldData;
        private readonly Dictionary<Vector2Int, SurvivalStorageSlot> storage = new();
        private readonly Dictionary<Vector2Int, string> sleepingReservations = new();
        private readonly HashSet<Vector2Int> sleepingPositions = new();

        public SurvivalInteractionWorld(ChunkedWorldData worldData)
        {
            this.worldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
        }

        public IEnumerable<SurvivalStorageSlot> StorageSlots => storage.Values;
        public IEnumerable<Vector2Int> SleepingPositions => sleepingPositions;

        public bool TryAddStorage(
            Vector2Int cell,
            SurvivalResourceKind kind,
            string resourceTypeId,
            int quantity,
            out string error)
        {
            BackgroundFunction required = kind == SurvivalResourceKind.Food
                ? BackgroundFunction.FoodStorage
                : BackgroundFunction.WaterStorage;
            if (!TryValidateFunction(cell, required, out error) ||
                string.IsNullOrWhiteSpace(resourceTypeId) || quantity < 0)
            {
                error = string.IsNullOrWhiteSpace(error)
                    ? "Storage requires a stable resource ID and non-negative quantity."
                    : error;
                return false;
            }

            if (storage.ContainsKey(cell))
            {
                error = $"Storage {cell} already owns its one slot.";
                return false;
            }

            storage.Add(cell, new SurvivalStorageSlot(
                cell, kind, resourceTypeId.Trim(), quantity));
            error = string.Empty;
            return true;
        }

        public bool TryGetStorage(Vector2Int cell, out SurvivalStorageSlot slot) =>
            storage.TryGetValue(cell, out slot);

        public bool TryAddSleepingPosition(Vector2Int cell, out string error)
        {
            if (!TryValidateFunction(cell, BackgroundFunction.Sleeping, out error))
            {
                return false;
            }

            if (!sleepingPositions.Add(cell))
            {
                error = $"Sleeping position {cell} is already registered.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Collects only currently compatible and unreserved candidates. The
        /// caller supplies the reusable list so periodic AI evaluation does not
        /// allocate a new collection for every ant.
        /// </summary>
        public void CollectAvailableTargets(
            SurvivalInteractionKind kind,
            List<Vector2Int> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (kind == SurvivalInteractionKind.Rest)
            {
                foreach (Vector2Int cell in sleepingPositions)
                {
                    if (!sleepingReservations.ContainsKey(cell) &&
                        TryValidateFunction(cell, BackgroundFunction.Sleeping, out _))
                    {
                        results.Add(cell);
                    }
                }
                return;
            }

            SurvivalResourceKind resourceKind = kind == SurvivalInteractionKind.Eat
                ? SurvivalResourceKind.Food
                : SurvivalResourceKind.Water;
            foreach (SurvivalStorageSlot slot in storage.Values)
            {
                if (slot.Kind == resourceKind && slot.AvailableQuantity > 0 &&
                    TryValidateFunction(
                        slot.Cell,
                        kind == SurvivalInteractionKind.Eat
                            ? BackgroundFunction.FoodStorage
                            : BackgroundFunction.WaterStorage,
                        out _))
                {
                    results.Add(slot.Cell);
                }
            }
        }

        public bool TryGetFunction(Vector2Int cell, out BackgroundFunction function)
        {
            function = BackgroundFunction.None;
            if (worldData.GetCell(cell, out WorldTileCellData data) !=
                    WorldCellQueryResult.Available || data.HasSolidTile ||
                !worldData.TryGetBackgroundMaterialDefinition(
                    data.BackgroundMaterialId, out BackgroundTileMaterialDefinition definition))
            {
                return false;
            }

            function = definition.Function;
            return function != BackgroundFunction.None;
        }

        public bool TryReserveResource(
            Vector2Int cell,
            SurvivalResourceKind kind,
            string ownerId,
            int quantity,
            out string error)
        {
            BackgroundFunction required = kind == SurvivalResourceKind.Food
                ? BackgroundFunction.FoodStorage
                : BackgroundFunction.WaterStorage;
            if (!TryValidateFunction(cell, required, out error) ||
                !storage.TryGetValue(cell, out SurvivalStorageSlot slot) ||
                slot.Kind != kind)
            {
                if (string.IsNullOrEmpty(error)) error = $"No compatible physical {kind} stack exists at {cell}.";
                return false;
            }

            if (!slot.TryReserve(ownerId, quantity))
            {
                error = $"{kind} storage {cell} has insufficient unreserved quantity.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryConsumeReserved(
            Vector2Int cell,
            string ownerId,
            int quantity,
            out string error)
        {
            if (!storage.TryGetValue(cell, out SurvivalStorageSlot slot) ||
                !slot.TryConsume(ownerId, quantity))
            {
                error = "The reserved physical quantity is no longer available.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryReserveSleeping(Vector2Int cell, string ownerId, out string error)
        {
            if (!TryValidateFunction(cell, BackgroundFunction.Sleeping, out error) ||
                (sleepingPositions.Count > 0 && !sleepingPositions.Contains(cell)))
            {
                if (string.IsNullOrEmpty(error))
                    error = $"Sleeping position {cell} is not registered.";
                return false;
            }

            if (sleepingReservations.TryGetValue(cell, out string existing) &&
                !string.Equals(existing, ownerId, StringComparison.Ordinal))
            {
                error = $"Sleeping position {cell} is already reserved.";
                return false;
            }

            sleepingReservations[cell] = ownerId;
            error = string.Empty;
            return true;
        }

        public string GetSleepingReservation(Vector2Int cell) =>
            sleepingReservations.TryGetValue(cell, out string owner) ? owner : string.Empty;

        public void ReleaseReservations(string ownerId)
        {
            foreach (SurvivalStorageSlot slot in storage.Values) slot.Release(ownerId);
            var released = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, string> reservation in sleepingReservations)
            {
                if (string.Equals(reservation.Value, ownerId, StringComparison.Ordinal))
                    released.Add(reservation.Key);
            }
            foreach (Vector2Int cell in released) sleepingReservations.Remove(cell);
        }

        public bool TryValidateFunction(
            Vector2Int cell,
            BackgroundFunction expected,
            out string error)
        {
            if (worldData.GetCell(cell, out WorldTileCellData data) != WorldCellQueryResult.Available)
            {
                error = $"Interaction target {cell} is outside the resident world.";
                return false;
            }
            if (data.HasSolidTile)
            {
                error = $"Interaction target {cell} is covered by blocking foreground terrain.";
                return false;
            }
            if (!worldData.TryGetBackgroundMaterialDefinition(
                    data.BackgroundMaterialId, out BackgroundTileMaterialDefinition definition) ||
                definition.Function != expected)
            {
                error = $"Interaction target {cell} is no longer {expected}.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
