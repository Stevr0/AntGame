using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    [Flags]
    public enum MaterialStorageFilter
    {
        None = 0,
        Soil = 1,
        Stone = 2,
        AllBuildingMaterials = Soil | Stone
    }

    public enum MaterialStorageFunctionAuthority
    {
        LegacyBackground,
        AuthoritativePlacement
    }

    /// <summary>
    /// Authoritative one-stack contents record. A migrated Placement owns the
    /// completed function identity while this delegated record remains the only
    /// mutable quantity and reservation authority.
    /// </summary>
    public sealed class MaterialStorageSlot
    {
        internal MaterialStorageSlot(Vector2Int cell, MaterialStorageFilter filter,
            string resourceTypeId, int quantity, int maximumStack)
        {
            Cell = cell; Filter = filter; ResourceTypeId = resourceTypeId;
            Quantity = quantity; MaximumStack = maximumStack;
        }
        public Vector2Int Cell { get; }
        public MaterialStorageFilter Filter { get; internal set; }
        public string ResourceTypeId { get; internal set; }
        public int Quantity { get; internal set; }
        public int MaximumStack { get; internal set; }
        public string ReservationOwnerId { get; internal set; } = string.Empty;
        public string ReservedResourceTypeId { get; internal set; } = string.Empty;
        public int ReservedIncomingQuantity { get; internal set; }
        public string WithdrawalOwnerId { get; internal set; } = string.Empty;
        public int ReservedWithdrawalQuantity { get; internal set; }
        public long Revision { get; internal set; } = 1L;
        public int FreeCapacity => Math.Max(0, MaximumStack - Quantity - ReservedIncomingQuantity);
        public int AvailableQuantity => Math.Max(0, Quantity - ReservedWithdrawalQuantity);
    }

    /// <summary>
    /// Plain-C# physical Material Storage contents and reservation authority.
    /// Migrated cells are keyed to an authoritative Placement without copying
    /// their mutable state. Unmigrated cells keep the legacy Background bridge.
    /// </summary>
    public sealed class MaterialStorageWorld
    {
        private readonly ChunkedWorldData world;
        private readonly Dictionary<Vector2Int, MaterialStorageSlot> slots = new();
        private readonly Dictionary<Vector2Int, PlacementHandle>
            authoritativePlacements = new();
        private PlacementRegistry placementRegistry;
        private string placementDefinitionId = string.Empty;
        public MaterialStorageWorld(ChunkedWorldData world) =>
            this.world = world ?? throw new ArgumentNullException(nameof(world));
        public IEnumerable<MaterialStorageSlot> Slots => slots.Values;
        public int Count => slots.Count;
        public int AuthoritativePlacementCount => authoritativePlacements.Count;
        public event Action<Vector2Int, long> StorageChanged;

        public bool TryAdd(Vector2Int cell, MaterialStorageFilter filter,
            string resourceTypeId, int quantity, int maximumStack, out string error)
        {
            resourceTypeId ??= string.Empty;
            if (!HasStorageFunction(cell) || filter == MaterialStorageFilter.None ||
                quantity < 0 || maximumStack <= 0 || quantity > maximumStack ||
                quantity > 0 && (!Accepts(filter, resourceTypeId) ||
                    !MaterialResourceCatalog.TryGetMaterial(resourceTypeId, out _)))
            {
                error = "Material Storage requires its functional Background, a valid filter and bounded compatible contents.";
                return false;
            }
            if (slots.ContainsKey(cell))
            {
                error = $"Material Storage {cell} already owns its one slot.";
                return false;
            }
            slots.Add(cell, new MaterialStorageSlot(cell, filter,
                quantity == 0 ? string.Empty : resourceTypeId, quantity, maximumStack));
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        public bool TryGet(Vector2Int cell, out MaterialStorageSlot slot) =>
            slots.TryGetValue(cell, out slot);

        public MaterialStorageFunctionAuthority GetFunctionAuthority(Vector2Int cell) =>
            authoritativePlacements.ContainsKey(cell)
                ? MaterialStorageFunctionAuthority.AuthoritativePlacement
                : MaterialStorageFunctionAuthority.LegacyBackground;

        public bool TryGetPlacementHandle(Vector2Int cell,
            out PlacementHandle handle) => authoritativePlacements.TryGetValue(
                cell, out handle);

        /// <summary>
        /// Validates the delegated half of a direct Storage completion without
        /// changing capacity or contents. The Placement transaction calls this
        /// before it creates structural identity.
        /// </summary>
        public bool CanRegisterAuthoritativePlacementSlot(Vector2Int cell,
            string definitionId, int maximumStack, out string error)
        {
            if (placementRegistry == null ||
                placementRegistry.Lifecycle != PlacementRegistryLifecycle.Active ||
                !string.Equals(placementDefinitionId, definitionId,
                    StringComparison.Ordinal))
            {
                error = "Material Storage Placement authority is unavailable or mismatched.";
                return false;
            }
            if (maximumStack <= 0 || slots.ContainsKey(cell) ||
                authoritativePlacements.ContainsKey(cell))
            {
                error = "The target cannot register another Material Storage slot.";
                return false;
            }
            if (world.GetCell(cell, out WorldTileCellData data) !=
                    WorldCellQueryResult.Available || data.HasSolidTile ||
                !world.TryGetBackgroundMaterialDefinition(data.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition background) ||
                background.Function != BackgroundFunction.None)
            {
                error = "Direct Material Storage completion requires an exposed natural Background.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Creates one empty delegated slot for a prepared authoritative
        /// Placement. Publication is supplied by the Placement commit, so no
        /// intermediate Storage-only cell change is emitted.
        /// </summary>
        public bool TryCommitAuthoritativePlacementSlot(
            PlacementSnapshot placement, MaterialStorageFilter filter,
            int maximumStack, out string error)
        {
            if (placement == null || filter == MaterialStorageFilter.None ||
                placement.AuthorityState !=
                    PlacementAuthorityState.AuthoritativePlacement ||
                placement.Lifecycle != PlacementLifecycle.Completed ||
                placement.Anchor.Kind != RegionLocationKind.Colony ||
                placement.Footprint.Count != 1 ||
                placement.Footprint[0] != placement.Anchor)
            {
                error = "A completed one-cell Colony Placement is required for Storage delegation.";
                return false;
            }
            var cell = new Vector2Int(placement.Anchor.ColonyCell.X,
                placement.Anchor.ColonyCell.Y);
            if (!CanRegisterAuthoritativePlacementSlot(cell,
                    placement.DefinitionId, maximumStack, out error) ||
                placementRegistry.TryGet(placement.Handle, out PlacementSnapshot current) !=
                    PlacementReadResult.Success ||
                current.PlacementId != placement.PlacementId)
                return false;

            slots.Add(cell, new MaterialStorageSlot(cell, filter,
                string.Empty, 0, maximumStack));
            try
            {
                authoritativePlacements.Add(cell, placement.Handle);
            }
            catch
            {
                // Keep this local commit all-or-nothing even if an unexpected
                // dictionary failure occurs after the slot was inserted.
                slots.Remove(cell);
                throw;
            }
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Performs the WV-B3D.3A cutover at scenario initialisation. All slots
        /// and projections are validated first; the registry then commits one
        /// batch promotion and these existing records become its delegated
        /// contents authority. No quantity or reservation field is copied.
        /// </summary>
        public bool TryMigrateExistingSlotsToPlacements(PlacementRegistry registry,
            string definitionId, out string error)
        {
            if (registry == null || string.IsNullOrWhiteSpace(definitionId))
            {
                error = "Material Storage cutover requires a Placement registry and definition.";
                return false;
            }
            if (placementRegistry != null || authoritativePlacements.Count > 0)
            {
                error = "Material Storage Placement authority was already configured.";
                return false;
            }

            var cells = new List<RegionCellId>(slots.Count);
            foreach (MaterialStorageSlot slot in slots.Values)
            {
                RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                    new ColonyCell(slot.Cell.x, slot.Cell.y));
                if (registry.TryGetByCell(cellId, out PlacementSnapshot placement) !=
                        PlacementReadResult.Success ||
                    placement.AuthorityState !=
                        PlacementAuthorityState.LegacyBackgroundProjection ||
                    !string.Equals(placement.DefinitionId, definitionId,
                        StringComparison.Ordinal))
                {
                    error = $"Material Storage {slot.Cell} has no matching legacy Placement projection.";
                    return false;
                }
                cells.Add(cellId);
            }

            PlacementMutationResult result = registry.TryPromoteLegacyProjections(
                cells, definitionId, out error);
            if (result != PlacementMutationResult.Success &&
                result != PlacementMutationResult.NoChange) return false;

            placementRegistry = registry;
            placementDefinitionId = definitionId;
            foreach (RegionCellId cellId in cells)
            {
                registry.TryGetByCell(cellId, out PlacementSnapshot placement);
                var cell = new Vector2Int(cellId.ColonyCell.X, cellId.ColonyCell.Y);
                authoritativePlacements.Add(cell, placement.Handle);
                NotifyChanged(cell);
            }
            error = string.Empty;
            return true;
        }

        public bool TrySetFilter(Vector2Int cell, MaterialStorageFilter filter,
            out string error)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                filter == MaterialStorageFilter.None)
            { error = "Material Storage and a non-empty filter are required."; return false; }
            slot.Filter = filter;
            if (!string.IsNullOrEmpty(slot.ReservationOwnerId) &&
                !Accepts(filter, slot.ReservedResourceTypeId))
                ClearIncoming(slot);
            if (!string.IsNullOrEmpty(slot.WithdrawalOwnerId) &&
                !Accepts(filter, slot.ResourceTypeId))
                ClearWithdrawal(slot);
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        public int GetAvailableCapacity(Vector2Int cell, string resourceTypeId,
            int maximumStack)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                !HasStorageFunction(cell) || !Accepts(slot.Filter, resourceTypeId) ||
                slot.MaximumStack != maximumStack ||
                slot.Quantity > 0 && slot.ResourceTypeId != resourceTypeId) return 0;
            return slot.FreeCapacity;
        }

        public bool TryReserveIncoming(Vector2Int cell, string resourceTypeId,
            int maximumStack, string ownerId, int requested, out int reserved,
            out string error)
        {
            reserved = 0;
            int available = GetAvailableCapacity(cell, resourceTypeId, maximumStack);
            if (string.IsNullOrWhiteSpace(ownerId) || requested <= 0 || available <= 0 ||
                !slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                !string.IsNullOrEmpty(slot.ReservationOwnerId))
            { error = "Material Storage has no compatible unreserved capacity."; return false; }
            reserved = Math.Min(requested, available);
            slot.ReservationOwnerId = ownerId;
            slot.ReservedResourceTypeId = resourceTypeId;
            slot.ReservedIncomingQuantity = reserved;
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        public bool TryCommitReservedDeposit(Vector2Int cell, string resourceTypeId,
            int maximumStack, string ownerId, int quantity, Func<bool> commitSource,
            out string error)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                slot.ReservationOwnerId != ownerId || quantity <= 0 ||
                quantity > slot.ReservedIncomingQuantity ||
                !Accepts(slot.Filter, resourceTypeId) || !HasStorageFunction(cell) ||
                slot.Quantity > 0 && slot.ResourceTypeId != resourceTypeId ||
                slot.Quantity + quantity > slot.MaximumStack)
            { error = "The reserved Material Storage deposit is no longer valid."; return false; }
            if (commitSource == null || !commitSource())
            { error = "The carrier inventory rejected the prepared deposit."; return false; }
            if (slot.Quantity == 0) slot.ResourceTypeId = resourceTypeId;
            slot.Quantity += quantity;
            ClearIncoming(slot);
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        public bool TryWithdraw(Vector2Int cell, int quantity,
            out string resourceTypeId, out int maximumStack, out string error)
        {
            resourceTypeId = string.Empty; maximumStack = 0;
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                !HasStorageFunction(cell) || quantity <= 0 ||
                quantity > slot.Quantity)
            { error = "Withdrawal exceeds the physical Material Storage quantity."; return false; }
            resourceTypeId = slot.ResourceTypeId; maximumStack = slot.MaximumStack;
            slot.Quantity -= quantity;
            if (slot.Quantity == 0) slot.ResourceTypeId = string.Empty;
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Reserves existing physical contents for one builder. Incoming storage
        /// reservations remain independent because they protect empty capacity,
        /// while this reservation protects material which already exists.
        /// </summary>
        public bool TryReserveWithdrawal(Vector2Int cell, string resourceTypeId,
            string ownerId, int requested, out int reserved, out string error)
        {
            reserved = 0;
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                string.IsNullOrWhiteSpace(ownerId) || requested <= 0 ||
                slot.ResourceTypeId != resourceTypeId || slot.AvailableQuantity <= 0 ||
                !string.IsNullOrEmpty(slot.WithdrawalOwnerId) ||
                !HasStorageFunction(cell) || !Accepts(slot.Filter, resourceTypeId))
            {
                error = "Material Storage has no compatible unreserved contents.";
                return false;
            }
            reserved = Math.Min(requested, slot.AvailableQuantity);
            slot.WithdrawalOwnerId = ownerId;
            slot.ReservedWithdrawalQuantity = reserved;
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        public bool TryCommitReservedWithdrawal(Vector2Int cell,
            string resourceTypeId, string ownerId, int quantity,
            Func<bool> commitDestination, out int maximumStack, out string error)
        {
            maximumStack = 0;
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot) ||
                slot.ResourceTypeId != resourceTypeId ||
                slot.WithdrawalOwnerId != ownerId || quantity <= 0 ||
                quantity > slot.ReservedWithdrawalQuantity || quantity > slot.Quantity ||
                !HasStorageFunction(cell) || !Accepts(slot.Filter, resourceTypeId))
            {
                error = "The reserved Material Storage withdrawal is no longer valid.";
                return false;
            }
            if (commitDestination == null || !commitDestination())
            {
                error = "The builder inventory rejected the prepared withdrawal.";
                return false;
            }
            maximumStack = slot.MaximumStack;
            slot.Quantity -= quantity;
            if (slot.Quantity == 0) slot.ResourceTypeId = string.Empty;
            ClearWithdrawal(slot);
            NotifyChanged(cell);
            error = string.Empty;
            return true;
        }

        public void ReleaseWithdrawal(Vector2Int cell, string ownerId)
        {
            if (slots.TryGetValue(cell, out MaterialStorageSlot slot) &&
                slot.WithdrawalOwnerId == ownerId)
            {
                ClearWithdrawal(slot);
                NotifyChanged(cell);
            }
        }

        public bool IsOccupied(Vector2Int cell)
        {
            return slots.TryGetValue(cell, out MaterialStorageSlot slot) &&
                (slot.Quantity > 0 || slot.ReservedIncomingQuantity > 0 ||
                 slot.ReservedWithdrawalQuantity > 0);
        }

        public void ReleaseIncoming(Vector2Int cell, string ownerId)
        {
            if (slots.TryGetValue(cell, out MaterialStorageSlot slot) &&
                slot.ReservationOwnerId == ownerId)
            {
                ClearIncoming(slot);
                NotifyChanged(cell);
            }
        }

        public bool Remove(Vector2Int cell)
        {
            bool removed = slots.Remove(cell);
            authoritativePlacements.Remove(cell);
            if (removed) StorageChanged?.Invoke(cell, 0L);
            return removed;
        }

        public void Clear()
        {
            var cells = new List<Vector2Int>(slots.Keys);
            slots.Clear();
            authoritativePlacements.Clear();
            placementRegistry = null;
            placementDefinitionId = string.Empty;
            foreach (Vector2Int cell in cells) StorageChanged?.Invoke(cell, 0L);
        }

        private bool HasStorageFunction(Vector2Int cell)
        {
            if (authoritativePlacements.TryGetValue(cell,
                    out PlacementHandle handle))
                return placementRegistry != null &&
                    placementRegistry.TryGet(handle, out PlacementSnapshot placement) ==
                        PlacementReadResult.Success &&
                    placement.AuthorityState ==
                        PlacementAuthorityState.AuthoritativePlacement &&
                    placement.Lifecycle == PlacementLifecycle.Completed &&
                    string.Equals(placement.DefinitionId, placementDefinitionId,
                        StringComparison.Ordinal);

            // WV-B3D.3B will replace this bridge for newly completed Storage.
            return world.GetCell(cell, out WorldTileCellData data) ==
                    WorldCellQueryResult.Available &&
                !data.HasSolidTile && world.TryGetBackgroundMaterialDefinition(
                    data.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition definition) &&
                definition.Function == BackgroundFunction.MaterialStorage;
        }

        private void NotifyChanged(Vector2Int cell)
        {
            if (!slots.TryGetValue(cell, out MaterialStorageSlot slot)) return;
            slot.Revision++;
            StorageChanged?.Invoke(cell, slot.Revision);
        }

        private static void ClearIncoming(MaterialStorageSlot slot)
        {
            slot.ReservationOwnerId = string.Empty;
            slot.ReservedResourceTypeId = string.Empty;
            slot.ReservedIncomingQuantity = 0;
        }

        private static void ClearWithdrawal(MaterialStorageSlot slot)
        {
            slot.WithdrawalOwnerId = string.Empty;
            slot.ReservedWithdrawalQuantity = 0;
        }

        public static bool Accepts(MaterialStorageFilter filter, string resourceTypeId) =>
            resourceTypeId == MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil)
                ? (filter & MaterialStorageFilter.Soil) != 0
                : resourceTypeId == MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone) &&
                    (filter & MaterialStorageFilter.Stone) != 0;
    }
}
