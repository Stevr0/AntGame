using System.Collections.Generic;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    public sealed class GroundStackFoundationTests
    {
        private readonly List<ScriptableObject> definitions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject definition in definitions)
                Object.DestroyImmediate(definition);
            definitions.Clear();
        }

        [Test]
        public void CompatibleOutputsShareOneStableIdentityAndAdvanceRevision()
        {
            var drops = new WorldMaterialDropRegistry();
            Vector2Int cell = new(2, -3);

            Assert.That(drops.TryAdd("first", ExcavatedMaterialType.Soil, 4,
                cell, out WorldMaterialDrop first, out string error), Is.True, error);
            Assert.That(drops.TryAdd("second", ExcavatedMaterialType.Soil, 6,
                cell, out WorldMaterialDrop merged, out error), Is.True, error);

            Assert.That(drops.Count, Is.EqualTo(1));
            Assert.That(merged.StackId, Is.EqualTo(first.StackId));
            Assert.That(merged.Quantity, Is.EqualTo(10));
            Assert.That(merged.Revision, Is.EqualTo(first.Revision + 1));
            Assert.That(merged.Provenance, Is.EqualTo("first"));
        }

        [Test]
        public void RejectedTypeAndOverflowPreserveExistingStack()
        {
            var drops = new WorldMaterialDropRegistry();
            Vector2Int cell = new(1, 1);
            drops.TryAdd("source", ExcavatedMaterialType.Soil, 9, cell,
                out WorldMaterialDrop before, out _);

            Assert.That(drops.TryAdd("mixed", ExcavatedMaterialType.Stone, 1,
                cell, out _, out string mixedError), Is.False);
            Assert.That(mixedError, Does.Contain("different resource types"));
            Assert.That(drops.TryAdd("overflow", ExcavatedMaterialType.Soil, 2,
                cell, out _, out string overflowError), Is.False);
            Assert.That(overflowError, Does.Contain("exceed"));
            Assert.That(drops.TryFindAtCell(cell, out WorldMaterialDrop after), Is.True);
            Assert.That(after.Quantity, Is.EqualTo(before.Quantity));
            Assert.That(after.Revision, Is.EqualTo(before.Revision));
        }

        [Test]
        public void EmptyStackTerminatesAndVacatedCellReceivesNewIdentity()
        {
            var drops = new WorldMaterialDropRegistry();
            Vector2Int cell = Vector2Int.zero;
            drops.TryAdd("first", ExcavatedMaterialType.Soil, 1, cell,
                out WorldMaterialDrop first, out _);
            Assert.That(drops.TryReserve(first.OwnershipKey, "ant", 1,
                out _, out string error), Is.True, error);
            Assert.That(drops.TryCommitReservedPickup(first.OwnershipKey, "ant", 1,
                () => true, out _, out error), Is.True, error);

            Assert.That(drops.TryGet(first.OwnershipKey, out _), Is.False);
            Assert.That(drops.TryAdd("second", ExcavatedMaterialType.Soil, 1,
                cell, out WorldMaterialDrop second, out error), Is.True, error);
            Assert.That(second.StackId, Is.Not.EqualTo(first.StackId));
        }

        [Test]
        public void NewScenarioContextCannotResolveAnOldStackIdentity()
        {
            var firstScenario = new WorldMaterialDropRegistry();
            firstScenario.TryAdd("source", ExcavatedMaterialType.Stone, 1,
                Vector2Int.zero, out WorldMaterialDrop oldStack, out _);
            var replacementScenario = new WorldMaterialDropRegistry();

            Assert.That(replacementScenario.ScenarioContextId,
                Is.Not.EqualTo(firstScenario.ScenarioContextId));
            Assert.That(replacementScenario.TryGet(oldStack.OwnershipKey, out _),
                Is.False);
            Assert.That(replacementScenario.TryReserve(oldStack.OwnershipKey,
                "ant", 1, out _, out _), Is.False);
        }

        [Test]
        public void WorldPlacementRejectsMissingAndObstructedCells()
        {
            ChunkedWorldData world = CreateWorld();
            var drops = new WorldMaterialDropRegistry(world);
            Assert.That(world.SetSolidMaterial(Vector2Int.zero, "solid.test"),
                Is.EqualTo(WorldCellMutationResult.Success));

            Assert.That(drops.TryAdd("blocked", ExcavatedMaterialType.Soil, 1,
                Vector2Int.zero, out _, out string blockedError), Is.False);
            Assert.That(blockedError, Does.Contain("Resource Node"));
            Assert.That(drops.TryAdd("missing", ExcavatedMaterialType.Soil, 1,
                new Vector2Int(1000, 1000), out _, out string missingError), Is.False);
            Assert.That(missingError, Does.Contain("not resident"));
            Assert.That(drops.Count, Is.Zero);
        }

        [Test]
        public void UnknownCompatibilityIsRejectedWithoutConsumingSource()
        {
            var drops = new WorldMaterialDropRegistry(_ =>
                GroundStackPlacementValidation.Unknown("test policy unavailable"));
            bool sourceCommitted = false;

            Assert.That(drops.TryAddFromSource("unknown", ExcavatedMaterialType.Soil,
                1, Vector2Int.zero, () => sourceCommitted = true,
                out _, out string error), Is.False);
            Assert.That(error, Does.Contain("unknown"));
            Assert.That(sourceCommitted, Is.False);
            Assert.That(drops.Count, Is.Zero);
        }

        [Test]
        public void ReservationStateIsVisibleAndRevisionedWithoutChangingQuantity()
        {
            var drops = new WorldMaterialDropRegistry();
            drops.TryAdd("source", ExcavatedMaterialType.Soil, 3,
                Vector2Int.zero, out WorldMaterialDrop created, out _);

            Assert.That(drops.TryReserve(created.OwnershipKey, "ant", 2,
                out int reserved, out string error), Is.True, error);
            Assert.That(reserved, Is.EqualTo(2));
            Assert.That(drops.TryGet(created.OwnershipKey,
                out WorldMaterialDrop held), Is.True);
            Assert.That(held.Quantity, Is.EqualTo(3));
            Assert.That(held.ReservedQuantity, Is.EqualTo(2));
            Assert.That(held.AvailableQuantity, Is.EqualTo(1));
            Assert.That(held.Revision, Is.EqualTo(created.Revision + 1));

            drops.ReleaseReservation(created.OwnershipKey, "ant");
            drops.TryGet(created.OwnershipKey, out WorldMaterialDrop released);
            Assert.That(released.ReservedQuantity, Is.Zero);
            Assert.That(released.AvailableQuantity, Is.EqualTo(3));
            Assert.That(released.Revision, Is.EqualTo(held.Revision + 1));
        }

        [Test]
        public void CapacitiesAreDefinitionsNotInventoryOrStorageCapacity()
        {
            Assert.That(MaterialResourceCatalog.TryGetDefinition(
                ExcavatedMaterialType.Soil, out MaterialResourceDefinition soil),
                Is.True);
            Assert.That(MaterialResourceCatalog.TryGetDefinition(
                ExcavatedMaterialType.Stone, out MaterialResourceDefinition stone),
                Is.True);
            Assert.That(soil.GroundStackCapacity, Is.EqualTo(10));
            Assert.That(stone.GroundStackCapacity, Is.EqualTo(10));
            Assert.That(soil.CapacityIsProvisional, Is.True);

            Assert.That(AntInventory.GetNaturalSlotCapacity(0), Is.Not.EqualTo(
                soil.GroundStackCapacity),
                "Ant slot count is independent from a ground stack's quantity cap.");
        }

        [Test]
        public void QuantityBadgeRulesAreContextualRegionSafeAndReadOnly()
        {
            Assert.That(ResourceQuantityBadgeRules.ShouldShowNode(
                true, false, false, true, false, 1), Is.False,
                "Unselected and undesignated Nodes must not clutter the map.");
            Assert.That(ResourceQuantityBadgeRules.ShouldShowNode(
                true, true, false, true, false, 1), Is.True);
            Assert.That(ResourceQuantityBadgeRules.ShouldShowNode(
                true, false, true, true, false, 1), Is.True);
            Assert.That(ResourceQuantityBadgeRules.ShouldShowNode(
                true, true, false, true, true, 6), Is.False,
                "Generation-only Surface data is not a live Extraction target.");
            Assert.That(ResourceQuantityBadgeRules.ShouldShowNode(
                true, true, false, false, false, 1), Is.False,
                "Unknown quantity must not be presented as authoritative.");
            Assert.That(ResourceQuantityBadgeRules.ShouldShowLooseStack(true, 3),
                Is.True);
            Assert.That(ResourceQuantityBadgeRules.ShouldShowLooseStack(false, 3),
                Is.False);
            Assert.That(ResourceQuantityBadgeRules.ShouldShowStorage(true, 0),
                Is.False);
            Assert.That(ResourceQuantityBadgeRules.ShouldShowStorage(true, 4),
                Is.True);

            var drops = new WorldMaterialDropRegistry();
            drops.TryAdd("badge", ExcavatedMaterialType.Soil, 3,
                Vector2Int.zero, out WorldMaterialDrop stack, out _);
            long revision = stack.Revision;
            int quantity = stack.Quantity;
            ResourceQuantityBadgeRules.ShouldShowLooseStack(true, quantity);
            drops.TryGet(stack.OwnershipKey, out WorldMaterialDrop afterRead);
            Assert.That(afterRead.Quantity, Is.EqualTo(quantity));
            Assert.That(afterRead.Revision, Is.EqualTo(revision));
        }

        private ChunkedWorldData CreateWorld()
        {
            BackgroundTileMaterialDefinition background =
                CreateDefinition<BackgroundTileMaterialDefinition>("background.test");
            SolidTileMaterialDefinition solid =
                CreateDefinition<SolidTileMaterialDefinition>("solid.test");
            var world = new ChunkedWorldData(new WorldMaterialRegistry(
                new[] { background }, new[] { solid }));
            Assert.That(world.CreateChunk(Vector2Int.zero, "background.test"),
                Is.EqualTo(WorldChunkCreationResult.Success));
            return world;
        }

        private T CreateDefinition<T>(string stableId)
            where T : WorldTileMaterialDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = stableId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            definitions.Add(definition);
            return definition;
        }
    }
}
