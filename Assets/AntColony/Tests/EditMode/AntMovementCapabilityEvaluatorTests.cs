using System.Collections.Generic;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused coverage for Background-driven local movement capability.</summary>
    public sealed class AntMovementCapabilityEvaluatorTests
    {
        private const string SkyId = "background.sky-test";
        private const string EarthId = "background.earth-test";
        private const string StoneId = "background.stone-test";
        private const string PreparedId = "background.prepared-test";
        private const string GroundId = "solid.ground-test";
        private const string BlockingOnlyId = "solid.blocking-only-test";
        private const string SupportOnlyId = "solid.support-only-test";

        private readonly List<Object> temporaryObjects = new();
        private BackgroundTileMaterialDefinition sky;
        private BackgroundTileMaterialDefinition earth;
        private BackgroundTileMaterialDefinition stone;
        private BackgroundTileMaterialDefinition prepared;
        private SolidTileMaterialDefinition ground;
        private SolidTileMaterialDefinition blockingOnly;
        private SolidTileMaterialDefinition supportOnly;
        private ChunkedWorldData world;
        private AntMovementCapabilityEvaluator evaluator;

        [SetUp]
        public void SetUp()
        {
            sky = CreateBackground(SkyId, true, false, false, false, true);
            earth = CreateBackground(EarthId, true, false, false, false, true);
            stone = CreateBackground(StoneId, true, false, false, false, true);
            prepared = CreateBackground(
                PreparedId,
                true,
                true,
                true,
                true,
                false);
            ground = CreateSolid(GroundId, true, true);
            blockingOnly = CreateSolid(BlockingOnlyId, true, false);
            supportOnly = CreateSolid(SupportOnlyId, false, true);
            world = CreateWorld();

            EnsureChunk(new Vector2Int(-1, -1));
            EnsureChunk(new Vector2Int(-1, 0));
            EnsureChunk(new Vector2Int(0, -1));
            EnsureChunk(Vector2Int.zero);
            evaluator = new AntMovementCapabilityEvaluator(world);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = temporaryObjects.Count - 1; index >= 0; index--)
            {
                if (temporaryObjects[index] != null)
                {
                    Object.DestroyImmediate(temporaryObjects[index]);
                }
            }

            temporaryObjects.Clear();
        }

        [Test]
        public void SkyProperties_HaveNoGripOrVoluntaryVerticalMovement()
        {
            AssertUnpreparedProperties(sky);
        }

        [Test]
        public void UnpreparedEarthProperties_HaveNoGrip()
        {
            AssertUnpreparedProperties(earth);
        }

        [Test]
        public void UnpreparedStoneProperties_HaveNoGrip()
        {
            AssertUnpreparedProperties(stone);
        }

        [Test]
        public void PreparedProperties_ProvideGripAndAllDirections()
        {
            Assert.That(prepared.ProvidesClimbingGrip, Is.True);
            Assert.That(prepared.AllowsHorizontalMovement, Is.True);
            Assert.That(prepared.AllowsUpwardMovement, Is.True);
            Assert.That(prepared.AllowsDownwardMovement, Is.True);
            Assert.That(prepared.GravityAppliesWhenUnsupported, Is.False);
        }

        [Test]
        public void SuitableFloorBeneathBody_ProducesGroundedWalking()
        {
            SetSolid(Vector2Int.zero, GroundId);

            AntMovementCapabilityResult result = Evaluate(new Vector2(0f, 0.65f));

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Grounded));
            Assert.That(result.HasGroundSupport, Is.True);
            Assert.That(result.AllowsHorizontalMovement, Is.True);
            Assert.That(result.AllowsUpwardMovement, Is.False);
            Assert.That(result.AllowsDownwardMovement, Is.False);
            Assert.That(result.ShouldApplyGravity, Is.False);
        }

        [Test]
        public void WallBesideBody_DoesNotProduceGrounded()
        {
            SetSolid(Vector2Int.right, GroundId);

            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.HasGroundSupport, Is.False);
        }

        [Test]
        public void CeilingAboveBody_DoesNotProduceGrounded()
        {
            SetSolid(Vector2Int.up, GroundId);

            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.HasGroundSupport, Is.False);
        }

        [Test]
        public void NonBlockingSolidBelowBody_DoesNotProduceGrounded()
        {
            SetSolid(Vector2Int.zero, SupportOnlyId);

            AntMovementCapabilityResult result = Evaluate(new Vector2(0f, 0.65f));

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.HasGroundSupport, Is.False);
        }

        [Test]
        public void BlockingSolidWithoutSupportFlag_DoesNotProduceGrounded()
        {
            SetSolid(Vector2Int.zero, BlockingOnlyId);

            AntMovementCapabilityResult result = Evaluate(new Vector2(0f, 0.65f));

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.HasGroundSupport, Is.False);
        }

        [Test]
        public void BackgroundTileAlone_DoesNotProduceGrounded()
        {
            AntMovementCapabilityResult result = Evaluate(new Vector2(0f, 0.65f));

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.HasGroundSupport, Is.False);
        }

        [Test]
        public void ClearUnsupportedSky_ProducesFallingAndRemainsPhysicallyClear()
        {
            SetBackground(Vector2Int.zero, SkyId);

            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(result.Status, Is.EqualTo(AntMovementCapabilityStatus.Success));
            Assert.That(result.IsPhysicallyClear, Is.True);
            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.ShouldApplyGravity, Is.True);
            Assert.That(result.AllowsDownwardMovement, Is.False);
        }

        [Test]
        public void ClearUnpreparedShaft_ProducesFalling()
        {
            SetSolid(Vector2Int.left, GroundId);
            SetSolid(Vector2Int.right, GroundId);

            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(result.IsPhysicallyClear, Is.True);
            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Falling));
            Assert.That(result.AllowsHorizontalMovement, Is.False);
        }

        [Test]
        public void PreparedBackground_ProducesGripAndSuppressesFalling()
        {
            SetBackground(Vector2Int.zero, PreparedId);

            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Gripping));
            Assert.That(result.HasBackgroundGrip, Is.True);
            Assert.That(result.AllowsHorizontalMovement, Is.True);
            Assert.That(result.AllowsUpwardMovement, Is.True);
            Assert.That(result.AllowsDownwardMovement, Is.True);
            Assert.That(result.ShouldApplyGravity, Is.False);
        }

        [Test]
        public void FallingDownward_IsNotVoluntaryDownwardMovement()
        {
            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(result.ShouldApplyGravity, Is.True);
            Assert.That(result.AllowsDownwardMovement, Is.False);
        }

        [Test]
        public void BlockingForeground_PreventsValidMovementCapability()
        {
            SetSolid(Vector2Int.zero, GroundId);

            AntMovementCapabilityResult result = Evaluate(Vector2.zero);

            Assert.That(
                result.Status,
                Is.EqualTo(AntMovementCapabilityStatus.BlockedByTerrain));
            Assert.That(result.IsPhysicallyClear, Is.False);
            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.None));
        }

        [Test]
        public void BackgroundPreparation_DoesNotChangePhysicalClearance()
        {
            AntMovementCapabilityResult unprepared = Evaluate(Vector2.zero);
            SetBackground(Vector2Int.zero, PreparedId);
            AntMovementCapabilityResult gripping = Evaluate(Vector2.zero);

            Assert.That(unprepared.Clearance.Status, Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(gripping.Clearance.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [Test]
        public void ReplacingEarthWithPrepared_ImmediatelyGrantsGrip()
        {
            Assert.That(Evaluate(Vector2.zero).HasBackgroundGrip, Is.False);

            SetBackground(Vector2Int.zero, PreparedId);

            Assert.That(Evaluate(Vector2.zero).HasBackgroundGrip, Is.True);
        }

        [Test]
        public void ReplacingPreparedWithEarth_ImmediatelyRemovesGrip()
        {
            SetBackground(Vector2Int.zero, PreparedId);
            Assert.That(Evaluate(Vector2.zero).HasBackgroundGrip, Is.True);

            SetBackground(Vector2Int.zero, EarthId);

            Assert.That(Evaluate(Vector2.zero).HasBackgroundGrip, Is.False);
        }

        [Test]
        public void BackgroundReplacement_PreservesForegroundSolid()
        {
            SetSolid(Vector2Int.zero, BlockingOnlyId);
            world.GetCell(Vector2Int.zero, out WorldTileCellData before);

            SetBackground(Vector2Int.zero, PreparedId);

            world.GetCell(Vector2Int.zero, out WorldTileCellData after);
            Assert.That(after.SolidMaterialId, Is.EqualTo(before.SolidMaterialId));
        }

        [Test]
        public void InvalidBackgroundReplacement_IsRejectedWithoutMutation()
        {
            WorldCellMutationResult result = world.SetBackgroundMaterial(
                Vector2Int.zero,
                "background.unknown");

            Assert.That(result, Is.EqualTo(WorldCellMutationResult.InvalidMaterial));
            world.GetCell(Vector2Int.zero, out WorldTileCellData cell);
            Assert.That(cell.BackgroundMaterialId, Is.EqualTo(EarthId));
        }

        [Test]
        public void MissingChunkReplacement_IsRejectedWithoutGeneration()
        {
            int chunkCount = world.ResidentChunkCount;

            WorldCellMutationResult result = world.SetBackgroundMaterial(
                new Vector2Int(96, 96),
                PreparedId);

            Assert.That(result, Is.EqualTo(WorldCellMutationResult.ChunkUnavailable));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunkCount));
        }

        [TestCase(31.5f, 0f, 1, 0)]
        [TestCase(-0.5f, 0f, -1, 0)]
        [TestCase(0f, 31.5f, 0, 1)]
        [TestCase(0f, -0.5f, 0, -1)]
        public void GripEvaluation_CrossesResidentSignedChunkBoundaries(
            float x,
            float y,
            int chunkX,
            int chunkY)
        {
            EnsureChunk(new Vector2Int(chunkX, chunkY));
            Vector2 position = new Vector2(x, y);
            SetBackground(
                WorldTileCoordinates.WorldPositionToCell(position),
                PreparedId);

            AntMovementCapabilityResult result = Evaluate(position);

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Gripping));
        }

        [TestCase(31.5f, 31.5f, 1, 1)]
        [TestCase(-0.5f, 31.5f, -1, 1)]
        [TestCase(31.5f, -0.5f, 1, -1)]
        [TestCase(-0.5f, -0.5f, -1, -1)]
        public void GripEvaluation_CrossesFourChunkCornerInEveryQuadrant(
            float x,
            float y,
            int otherChunkX,
            int otherChunkY)
        {
            EnsureChunk(new Vector2Int(otherChunkX, 0));
            EnsureChunk(new Vector2Int(0, otherChunkY));
            EnsureChunk(new Vector2Int(otherChunkX, otherChunkY));
            Vector2 position = new Vector2(x, y);
            SetBackground(
                WorldTileCoordinates.WorldPositionToCell(position),
                PreparedId);

            AntMovementCapabilityResult result = Evaluate(position);

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Gripping));
        }

        [TestCase(31.5f, 0.65f, 1, 0, 31)]
        [TestCase(-0.5f, 0.65f, -1, 0, -1)]
        public void GroundSupport_CrossesHorizontalChunkBoundary(
            float x,
            float y,
            int chunkX,
            int chunkY,
            int floorX)
        {
            EnsureChunk(new Vector2Int(chunkX, chunkY));
            SetSolid(new Vector2Int(floorX, 0), GroundId);

            AntMovementCapabilityResult result = Evaluate(new Vector2(x, y));

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Grounded));
        }

        [TestCase(31.65f, 1, 31)]
        [TestCase(-0.35f, -1, -1)]
        public void GroundSupport_CrossesVerticalChunkBoundary(
            float bodyY,
            int otherChunkY,
            int floorY)
        {
            EnsureChunk(new Vector2Int(0, otherChunkY));
            SetSolid(new Vector2Int(0, floorY), GroundId);

            AntMovementCapabilityResult result = Evaluate(new Vector2(0f, bodyY));

            Assert.That(result.SuggestedState, Is.EqualTo(AntMovementEligibilityState.Grounded));
        }

        [Test]
        public void MissingTargetChunk_ReturnsUnavailableWithoutGeneration()
        {
            int chunkCount = world.ResidentChunkCount;

            AntMovementCapabilityResult result = Evaluate(new Vector2(96f, 96f));

            Assert.That(
                result.Status,
                Is.EqualTo(AntMovementCapabilityStatus.UnavailableWorldData));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunkCount));
        }

        [Test]
        public void MissingFootprintOrSupportChunk_ReturnsUnavailable()
        {
            int chunkCount = world.ResidentChunkCount;

            AntMovementCapabilityResult result = Evaluate(new Vector2(31.2f, 0f));

            Assert.That(
                result.Status,
                Is.EqualTo(AntMovementCapabilityStatus.UnavailableWorldData));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunkCount));
        }

        [Test]
        public void Query_CreatesNoObjectsOrPhysicsComponents()
        {
            int objectCount = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length;
            var physicsObject = new GameObject(
                "Capability Physics Independence Fixture",
                typeof(Rigidbody2D),
                typeof(BoxCollider2D));
            temporaryObjects.Add(physicsObject);
            Rigidbody2D body = physicsObject.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            Vector2 colliderSize = physicsObject.GetComponent<BoxCollider2D>().size;
            objectCount++;

            Evaluate(Vector2.zero);

            Assert.That(
                Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(objectCount));
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(
                physicsObject.GetComponent<BoxCollider2D>().size,
                Is.EqualTo(colliderSize));
        }

        [Test]
        public void InvalidProfile_ReturnsExplicitDiagnostic()
        {
            var invalid = new AntCollisionProfile(
                new Vector2(-1f, 0.3f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);

            AntMovementCapabilityResult result = evaluator.Evaluate(
                Vector2.zero,
                0f,
                invalid);

            Assert.That(
                result.Status,
                Is.EqualTo(AntMovementCapabilityStatus.InvalidQueryOrProfile));
        }

        private AntMovementCapabilityResult Evaluate(Vector2 position)
        {
            return evaluator.Evaluate(position, 0f, AntCollisionProfile.OrdinaryAnt);
        }

        private ChunkedWorldData CreateWorld()
        {
            return new ChunkedWorldData(
                new WorldMaterialRegistry(
                    new[] { sky, earth, stone, prepared },
                    new[] { ground, blockingOnly, supportOnly }));
        }

        private void EnsureChunk(Vector2Int chunkCoordinate)
        {
            if (!world.ContainsChunk(chunkCoordinate))
            {
                Assert.That(
                    world.CreateChunk(chunkCoordinate, EarthId),
                    Is.EqualTo(WorldChunkCreationResult.Success));
            }
        }

        private void SetBackground(Vector2Int cell, string materialId)
        {
            Assert.That(
                world.SetBackgroundMaterial(cell, materialId),
                Is.EqualTo(WorldCellMutationResult.Success));
        }

        private void SetSolid(Vector2Int cell, string materialId)
        {
            Assert.That(
                world.SetSolidMaterial(cell, materialId),
                Is.EqualTo(WorldCellMutationResult.Success));
        }

        private BackgroundTileMaterialDefinition CreateBackground(
            string materialId,
            bool horizontal,
            bool upward,
            bool downward,
            bool grip,
            bool gravity)
        {
            BackgroundTileMaterialDefinition definition =
                CreateDefinition<BackgroundTileMaterialDefinition>(materialId);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("allowsHorizontalMovement").boolValue = horizontal;
            serialized.FindProperty("allowsUpwardMovement").boolValue = upward;
            serialized.FindProperty("allowsDownwardMovement").boolValue = downward;
            serialized.FindProperty("providesClimbingGrip").boolValue = grip;
            serialized.FindProperty("gravityAppliesWhenUnsupported").boolValue = gravity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private SolidTileMaterialDefinition CreateSolid(
            string materialId,
            bool blocksMovement,
            bool supportsNavigation)
        {
            SolidTileMaterialDefinition definition =
                CreateDefinition<SolidTileMaterialDefinition>(materialId);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("blocksMovement").boolValue = blocksMovement;
            serialized.FindProperty("supportsNavigation").boolValue = supportsNavigation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private TDefinition CreateDefinition<TDefinition>(string materialId)
            where TDefinition : WorldTileMaterialDefinition
        {
            TDefinition definition = ScriptableObject.CreateInstance<TDefinition>();
            temporaryObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableMaterialId").stringValue = materialId;
            serialized.FindProperty("displayName").stringValue = materialId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void AssertUnpreparedProperties(
            BackgroundTileMaterialDefinition material)
        {
            Assert.That(material.AllowsHorizontalMovement, Is.True);
            Assert.That(material.AllowsUpwardMovement, Is.False);
            Assert.That(material.AllowsDownwardMovement, Is.False);
            Assert.That(material.ProvidesClimbingGrip, Is.False);
            Assert.That(material.GravityAppliesWhenUnsupported, Is.True);
        }
    }
}
