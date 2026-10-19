using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.World
{
    internal interface IRuntimeCellPublicationParticipant
    {
        void SetPublicationRevision(long revision);
    }

    /// <summary>Shared revision plumbing; gameplay data remains in its existing owner.</summary>
    public abstract class RuntimeCellRegionAdapterBase : IReadOnlyRuntimeCellRegion,
        IRuntimeCellPublicationParticipant, IDisposable
    {
        private readonly Dictionary<RegionCellId, long> cellRevisions = new();

        protected RuntimeCellRegionAdapterBase(RegionId regionId,
            RegionLocationKind coordinateKind)
        {
            RegionId = regionId;
            CoordinateKind = coordinateKind;
            Changes = new RuntimeCellChangeFeed(regionId);
        }

        public RegionId RegionId { get; }
        public RegionLocationKind CoordinateKind { get; }
        public long PublicationRevision { get; private set; }
        public RuntimeCellChangeFeed Changes { get; }

        public abstract RuntimeCellReadResult TryGetCell(RegionCellId cellId,
            out RuntimeCellSnapshot snapshot);
        public abstract IEnumerable<RegionCellId> EnumerateResidentCells();
        public abstract void Dispose();

        protected long GetPublishedCellRevision(RegionCellId cellId,
            long authorityRevision = 0L)
        {
            if (cellRevisions.TryGetValue(cellId, out long revision)) return revision;
            // Revision one is the first published view. Existing authority
            // revisions are folded in without copying any mutable cell content.
            revision = Math.Max(1L, authorityRevision + 1L);
            cellRevisions[cellId] = revision;
            return revision;
        }

        protected void PublishCellChange(RegionCellId cellId,
            RuntimeCellChangeCategory category, long authorityRevision = -1L)
        {
            long oldRevision = GetPublishedCellRevision(cellId);
            long newRevision = authorityRevision >= 0L
                ? Math.Max(oldRevision + 1L, authorityRevision + 1L)
                : oldRevision + 1L;
            cellRevisions[cellId] = newRevision;
            Changes.Publish(new[]
            {
                new RuntimeCellChange(cellId, oldRevision, newRevision, category)
            });
        }

        void IRuntimeCellPublicationParticipant.SetPublicationRevision(long revision)
        {
            PublicationRevision = revision;
        }

        protected RuntimeCellReadResult ValidateIdentity(RegionCellId cellId)
        {
            if (cellId.RegionId != RegionId) return RuntimeCellReadResult.InvalidRegion;
            return cellId.Kind == CoordinateKind
                ? RuntimeCellReadResult.Success
                : RuntimeCellReadResult.InvalidCoordinateKind;
        }
    }

    /// <summary>
    /// Read-only Colony projection over ChunkedWorldData and the existing drop
    /// registry. It owns only snapshot revisions, never terrain or quantities.
    /// </summary>
    public sealed class ColonyRuntimeCellRegionAdapter : RuntimeCellRegionAdapterBase
    {
        private readonly ChunkedWorldData world;
        private readonly WorldMaterialDropRegistry drops;
        private readonly GeneratedWorldSnapshot generated;
        private readonly ColonyResourceNodeRegistry resourceNodes;
        private readonly PlacementRegistry placements;
        private readonly MaterialStorageWorld materialStorage;

        public ColonyRuntimeCellRegionAdapter(ChunkedWorldData world,
            WorldMaterialDropRegistry drops, GeneratedWorldSnapshot generated = null,
            ColonyResourceNodeRegistry resourceNodes = null,
            PlacementRegistry placements = null,
            MaterialStorageWorld materialStorage = null)
            : base(AntRegionIds.Colony, RegionLocationKind.Colony)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.drops = drops ?? throw new ArgumentNullException(nameof(drops));
            this.generated = generated;
            this.resourceNodes = resourceNodes;
            this.placements = placements;
            this.materialStorage = materialStorage;
            world.CellChanged += OnTerrainChanged;
            drops.GroundCellChanged += OnGroundCellChanged;
            if (resourceNodes != null) resourceNodes.NodeChanged += OnNodeChanged;
            if (placements != null) placements.PlacementChanged += OnPlacementChanged;
            if (materialStorage != null)
                materialStorage.StorageChanged += OnMaterialStorageChanged;
        }

        public override RuntimeCellReadResult TryGetCell(RegionCellId cellId,
            out RuntimeCellSnapshot snapshot)
        {
            snapshot = null;
            RuntimeCellReadResult identity = ValidateIdentity(cellId);
            if (identity != RuntimeCellReadResult.Success) return identity;
            Vector2Int cell = new(cellId.ColonyCell.X, cellId.ColonyCell.Y);
            if (world.GetCell(cell, out WorldTileCellData data) !=
                    WorldCellQueryResult.Available)
                return RuntimeCellReadResult.CellUnavailable;
            if (string.IsNullOrWhiteSpace(data.BackgroundMaterialId))
                return RuntimeCellReadResult.MissingBackground;

            RuntimeNodeSnapshot node = CreateNode(cell, data);
            RuntimePlacementSnapshot placement = CreatePlacement(cell, data);
            RuntimeLooseStackSnapshot loose = CreateLooseStack(cell);
            bool obscuresNaturalBackground =
                world.TryGetBackgroundMaterialDefinition(data.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition visibleBackground) &&
                visibleBackground.Function != BackgroundFunction.None;
            snapshot = new RuntimeCellSnapshot(cellId, data.BackgroundMaterialId,
                GetPublishedCellRevision(cellId, Math.Max(world.GetCellRevision(cell),
                    node?.NodeRevision ?? 0L)),
                PublicationRevision, node, placement, loose,
                obscuresNaturalBackground ? string.Empty : data.BackgroundMaterialId,
                obscuresNaturalBackground
                    ? RuntimeOccupantProjectionState.DetailsUnavailable
                    : RuntimeOccupantProjectionState.Authoritative,
                obscuresNaturalBackground ? data.BackgroundMaterialId : string.Empty);
            return RuntimeCellReadResult.Success;
        }

        public override IEnumerable<RegionCellId> EnumerateResidentCells()
        {
            foreach (Vector2Int chunk in world.GetResidentChunkCoordinates())
            for (int y = 0; y < WorldTileCoordinates.ChunkSize; y++)
            for (int x = 0; x < WorldTileCoordinates.ChunkSize; x++)
            {
                Vector2Int worldCell = WorldTileCoordinates.GetWorldCellCoordinate(
                    chunk, new Vector2Int(x, y));
                yield return RegionCellId.Colony(RegionId,
                    new ColonyCell(worldCell.x, worldCell.y));
            }
        }

        public override void Dispose()
        {
            world.CellChanged -= OnTerrainChanged;
            drops.GroundCellChanged -= OnGroundCellChanged;
            if (resourceNodes != null) resourceNodes.NodeChanged -= OnNodeChanged;
            if (placements != null)
            {
                placements.PlacementChanged -= OnPlacementChanged;
                placements.InvalidateContext();
            }
            if (materialStorage != null)
                materialStorage.StorageChanged -= OnMaterialStorageChanged;
        }

        private RuntimeNodeSnapshot CreateNode(Vector2Int cell,
            WorldTileCellData data)
        {
            if (!data.HasSolidTile) return null;
            RegionCellId cellId = RegionCellId.Colony(RegionId,
                new ColonyCell(cell.x, cell.y));
            if (resourceNodes != null &&
                resourceNodes.TryGet(cellId, out ColonyResourceNodeSnapshot active) ==
                    ColonyResourceNodeReadResult.Success)
            {
                RuntimeNodeInteractionState interaction = active.ExtractionStatus is
                    ColonyResourceNodeExtractionStatus.LegacyQuantityOneCompatible or
                    ColonyResourceNodeExtractionStatus.Interactive
                        ? RuntimeNodeInteractionState.Interactive
                        : RuntimeNodeInteractionState.CompatibilityOnly;
                return new RuntimeNodeSnapshot(
                    RuntimeOccupantProjectionState.Authoritative,
                    active.NodeId, active.DefinitionId, active.BlocksMovement,
                    active.InitialQuantity, active.RemainingQuantity, interaction,
                    active.OutputResourceId, active.Revision, active.Lifecycle,
                    active.ExtractionStatus);
            }
            string stableId = string.Empty;
            if (TryGetGeneratedCell(cell, out GeneratedCellRecord generatedCell) &&
                generatedCell.Node != null)
                stableId = generatedCell.Node.Id;
            bool? obstructs = null;
            if (world.TryGetSolidMaterialDefinition(data.SolidMaterialId,
                    out SolidTileMaterialDefinition definition))
                obstructs = definition.BlocksMovement;
            return new RuntimeNodeSnapshot(
                RuntimeOccupantProjectionState.CompatibilityProjected,
                stableId, data.SolidMaterialId, obstructs,
                null, null, RuntimeNodeInteractionState.CompatibilityOnly);
        }

        private RuntimePlacementSnapshot CreatePlacement(Vector2Int cell,
            WorldTileCellData data)
        {
            RegionCellId cellId = RegionCellId.Colony(RegionId,
                new ColonyCell(cell.x, cell.y));
            if (placements != null && placements.TryGetByCell(cellId,
                    out PlacementSnapshot projected) == PlacementReadResult.Success)
            {
                placements.TryGetDefinition(projected.DefinitionId,
                    out PlacementDefinition placementDefinition);
                string functionId = string.Empty;
                if (world.TryGetBackgroundMaterialDefinition(
                        data.BackgroundMaterialId,
                        out BackgroundTileMaterialDefinition projectedBackground))
                    functionId = projectedBackground.Function == BackgroundFunction.None
                        ? PlacementCategory.MovementImprovement.ToString()
                        : projectedBackground.Function.ToString();
                MaterialStorageSlot slot = null;
                bool hasStorage = placementDefinition?.Category ==
                        PlacementCategory.Storage &&
                    materialStorage != null &&
                    materialStorage.TryGet(cell, out slot);
                if (hasStorage)
                    functionId = BackgroundFunction.MaterialStorage.ToString();
                bool delegated = hasStorage &&
                    materialStorage.GetFunctionAuthority(cell) ==
                        MaterialStorageFunctionAuthority.AuthoritativePlacement &&
                    materialStorage.TryGetPlacementHandle(cell,
                        out PlacementHandle handle) &&
                    handle.PlacementId == projected.PlacementId;
                return new RuntimePlacementSnapshot(
                    projected.AuthorityState ==
                        PlacementAuthorityState.LegacyBackgroundProjection
                            ? RuntimeOccupantProjectionState.CompatibilityProjected
                            : RuntimeOccupantProjectionState.Authoritative,
                    projected.PlacementId.Value, projected.DefinitionId, functionId,
                    contentsArePlacementOwned: false,
                    projected.ScenarioContextId, projected.Anchor,
                    projected.Footprint, projected.Lifecycle,
                    projected.AuthorityState, projected.Revision,
                    projected.CurrentHitPoints, projected.MaximumHitPoints,
                    functionalCapacity: hasStorage ? 1 : null,
                    functionAuthority: delegated
                        ? RuntimePlacementFunctionAuthority.DelegatedMaterialStorage
                        : hasStorage
                            ? RuntimePlacementFunctionAuthority.LegacyBackground
                            : RuntimePlacementFunctionAuthority.None,
                    functionRevision: hasStorage ? slot.Revision : null,
                    resourceFilterId: hasStorage
                        ? slot.Filter.ToString() : string.Empty,
                    storedResourceId: hasStorage
                        ? slot.ResourceTypeId : string.Empty,
                    storedQuantity: hasStorage ? slot.Quantity : null,
                    availableCapacity: hasStorage ? slot.FreeCapacity : null,
                    reservedIncomingQuantity: hasStorage
                        ? slot.ReservedIncomingQuantity : null,
                    reservedWithdrawalQuantity: hasStorage
                        ? slot.ReservedWithdrawalQuantity : null,
                    visualDefinitionId: placementDefinition?.VisualDefinitionId,
                    visualState: PlacementVisualStateResolver.Resolve(false,
                        projected.Lifecycle, projected.CurrentHitPoints,
                        projected.MaximumHitPoints, 0.5f));
            }
            if (!world.TryGetBackgroundMaterialDefinition(data.BackgroundMaterialId,
                    out BackgroundTileMaterialDefinition definition) ||
                definition.Function == BackgroundFunction.None)
                return null;
            return new RuntimePlacementSnapshot(
                RuntimeOccupantProjectionState.CompatibilityProjected,
                $"legacy.placement.colony.{cell.x}.{cell.y}",
                data.BackgroundMaterialId, definition.Function.ToString(), false);
        }

        private RuntimeLooseStackSnapshot CreateLooseStack(Vector2Int cell)
        {
            if (!drops.TryFindAtCell(cell, out WorldMaterialDrop drop)) return null;
            return new RuntimeLooseStackSnapshot(
                RuntimeOccupantProjectionState.Authoritative,
                drop.StackId.Value, drop.ResourceTypeId,
                drop.Quantity, drop.MaximumStack, false,
                drop.ReservedQuantity, drop.AvailableQuantity,
                drop.Revision, drop.CellId);
        }

        private bool TryGetGeneratedCell(Vector2Int cell,
            out GeneratedCellRecord result)
        {
            result = null;
            if (generated == null) return false;
            int column = cell.x + 64;
            int depth = -1 - cell.y;
            if (column < 0 || depth < 0 ||
                column >= generated.ColonyCells.GetLength(0) ||
                depth >= generated.ColonyCells.GetLength(1)) return false;
            result = generated.ColonyCells[column, depth];
            return true;
        }

        private void OnTerrainChanged(WorldCellAuthorityChange change)
        {
            RegionCellId id = RegionCellId.Colony(RegionId,
                new ColonyCell(change.WorldCell.x, change.WorldCell.y));
            RuntimeCellChangeCategory category = change.Category;
            if (placements != null && world.GetCell(change.WorldCell,
                    out WorldTileCellData cell) == WorldCellQueryResult.Available &&
                placements.RefreshLegacyProjection(id,
                    cell.BackgroundMaterialId))
                category |= RuntimeCellChangeCategory.Placement;
            PublishCellChange(id, category, change.NewRevision);
        }

        private void OnGroundCellChanged(Vector2Int cell)
        {
            PublishCellChange(RegionCellId.Colony(RegionId,
                new ColonyCell(cell.x, cell.y)),
                RuntimeCellChangeCategory.LooseResource);
        }

        private void OnNodeChanged(RegionCellId cellId, long nodeRevision)
        {
            PublishCellChange(cellId, RuntimeCellChangeCategory.Node, nodeRevision);
        }

        private void OnPlacementChanged(IReadOnlyList<RegionCellId> cells)
        {
            foreach (RegionCellId cell in cells)
                PublishCellChange(cell, RuntimeCellChangeCategory.Placement);
        }

        private void OnMaterialStorageChanged(Vector2Int cell, long revision)
        {
            PublishCellChange(RegionCellId.Colony(RegionId,
                new ColonyCell(cell.x, cell.y)),
                RuntimeCellChangeCategory.Placement, revision);
        }
    }

    /// <summary>Read-only Surface projection; generated Nodes remain non-interactive.</summary>
    public sealed class SurfaceRuntimeCellRegionAdapter : RuntimeCellRegionAdapterBase
    {
        private readonly SurfaceWorldData surface;
        private readonly GeneratedWorldSnapshot generated;

        public SurfaceRuntimeCellRegionAdapter(SurfaceWorldData surface,
            GeneratedWorldSnapshot generated = null)
            : base(AntRegionIds.Surface, RegionLocationKind.Surface)
        {
            this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
            this.generated = generated;
        }

        public override RuntimeCellReadResult TryGetCell(RegionCellId cellId,
            out RuntimeCellSnapshot snapshot)
        {
            snapshot = null;
            RuntimeCellReadResult identity = ValidateIdentity(cellId);
            if (identity != RuntimeCellReadResult.Success) return identity;
            if (!surface.Contains(cellId.SurfaceCell))
                return RuntimeCellReadResult.CellUnavailable;

            GeneratedCellRecord generatedCell = GetGeneratedCell(cellId.SurfaceCell);
            string background = generatedCell?.BackgroundDefinitionId ??
                ProceduralWorldGenerator.GrassBackgroundId;
            if (string.IsNullOrWhiteSpace(background))
                return RuntimeCellReadResult.MissingBackground;
            RuntimeNodeSnapshot node = CreateNode(cellId.SurfaceCell, generatedCell);
            snapshot = new RuntimeCellSnapshot(cellId, background,
                GetPublishedCellRevision(cellId), PublicationRevision, node);
            return RuntimeCellReadResult.Success;
        }

        public override IEnumerable<RegionCellId> EnumerateResidentCells()
        {
            for (int y = 0; y < surface.Height; y++)
            for (int x = 0; x < surface.Width; x++)
                yield return RegionCellId.Surface(RegionId, new SurfaceCell(x, y));
        }

        public override void Dispose()
        {
            // SurfaceWorldData has no runtime mutation API in WV-B3B.
        }

        private GeneratedCellRecord GetGeneratedCell(SurfaceCell cell)
        {
            if (generated == null || cell.X >= generated.SurfaceCells.GetLength(0) ||
                cell.Y >= generated.SurfaceCells.GetLength(1)) return null;
            return generated.SurfaceCells[cell.X, cell.Y];
        }

        private RuntimeNodeSnapshot CreateNode(SurfaceCell cell,
            GeneratedCellRecord generatedCell)
        {
            if (generatedCell?.Node != null)
            {
                GeneratedResourceNodeRecord node = generatedCell.Node;
                return new RuntimeNodeSnapshot(
                    RuntimeOccupantProjectionState.CompatibilityProjected,
                    node.Id, node.DefinitionId, node.ObstructsMovement,
                    node.InitialQuantity, node.RemainingQuantity,
                    RuntimeNodeInteractionState.GenerationOnly);
            }
            if (!surface.IsObstacle(cell)) return null;
            return new RuntimeNodeSnapshot(
                RuntimeOccupantProjectionState.DetailsUnavailable,
                string.Empty, string.Empty, true, null, null,
                RuntimeNodeInteractionState.Unsupported);
        }
    }

    public sealed class RuntimeCellRegionSet : IDisposable
    {
        private readonly Dictionary<RegionId, IReadOnlyRuntimeCellRegion> regions;

        internal RuntimeCellRegionSet(long publicationRevision,
            IReadOnlyRuntimeCellRegion colony, IReadOnlyRuntimeCellRegion surface)
        {
            PublicationRevision = publicationRevision;
            Colony = colony;
            Surface = surface;
            regions = new Dictionary<RegionId, IReadOnlyRuntimeCellRegion>
            {
                [colony.RegionId] = colony,
                [surface.RegionId] = surface
            };
        }

        public long PublicationRevision { get; }
        public IReadOnlyRuntimeCellRegion Colony { get; }
        public IReadOnlyRuntimeCellRegion Surface { get; }
        public bool TryGetRegion(RegionId regionId,
            out IReadOnlyRuntimeCellRegion region) => regions.TryGetValue(regionId,
            out region);

        public void Dispose()
        {
            if (Colony is IDisposable colony) colony.Dispose();
            if (Surface is IDisposable surface) surface.Dispose();
        }
    }

    /// <summary>
    /// Atomically publishes only the read model. Existing gameplay authorities
    /// are built by the host and remain unchanged.
    /// </summary>
    public sealed class RuntimeCellPublicationRegistry
    {
        public RuntimeCellRegionSet Current { get; private set; }
        public long PublicationRevision { get; private set; }
        public event Action<RuntimeCellRegionSet> Published;

        public RuntimeCellReadResult TryGetCell(RegionCellId cellId,
            out RuntimeCellSnapshot snapshot)
        {
            snapshot = null;
            if (Current == null || !Current.TryGetRegion(cellId.RegionId,
                    out IReadOnlyRuntimeCellRegion region))
                return RuntimeCellReadResult.RegionUnpublished;
            return region.TryGetCell(cellId, out snapshot);
        }

        public bool TryPublish(IReadOnlyRuntimeCellRegion colony,
            IReadOnlyRuntimeCellRegion surface, out string error)
        {
            if (colony == null || surface == null ||
                colony.RegionId != AntRegionIds.Colony ||
                colony.CoordinateKind != RegionLocationKind.Colony ||
                surface.RegionId != AntRegionIds.Surface ||
                surface.CoordinateKind != RegionLocationKind.Surface ||
                colony.RegionId == surface.RegionId)
            {
                error = "Publication requires one correctly typed Colony and Surface region.";
                return false;
            }
            if (!ValidateRegion(colony, out error) ||
                !ValidateRegion(surface, out error)) return false;

            long nextRevision = PublicationRevision + 1L;
            if (colony is IRuntimeCellPublicationParticipant colonyParticipant)
                colonyParticipant.SetPublicationRevision(nextRevision);
            if (surface is IRuntimeCellPublicationParticipant surfaceParticipant)
                surfaceParticipant.SetPublicationRevision(nextRevision);

            var next = new RuntimeCellRegionSet(nextRevision, colony, surface);
            RuntimeCellRegionSet previous = Current;
            Current = next;
            PublicationRevision = nextRevision;
            Published?.Invoke(next);
            previous?.Dispose();
            error = string.Empty;
            return true;
        }

        private static bool ValidateRegion(IReadOnlyRuntimeCellRegion region,
            out string error)
        {
            int count = 0;
            foreach (RegionCellId cellId in region.EnumerateResidentCells())
            {
                count++;
                RuntimeCellReadResult result = region.TryGetCell(cellId,
                    out RuntimeCellSnapshot snapshot);
                if (result != RuntimeCellReadResult.Success || snapshot == null ||
                    string.IsNullOrWhiteSpace(snapshot.BackgroundDefinitionId))
                {
                    error = $"Region {region.RegionId} has invalid cell {cellId}: {result}.";
                    return false;
                }
                RuntimeOccupancyValidation compatibility =
                    RuntimeCellCompatibilityValidator.ValidateObserved(snapshot);
                if (compatibility.Result == RuntimeOccupancyCompatibility.Invalid)
                {
                    error = $"Region {region.RegionId} has incompatible cell {cellId}: " +
                        compatibility.Reason;
                    return false;
                }
            }
            if (count == 0)
            {
                error = $"Region {region.RegionId} has no resident cells.";
                return false;
            }
            error = string.Empty;
            return true;
        }
    }
}
