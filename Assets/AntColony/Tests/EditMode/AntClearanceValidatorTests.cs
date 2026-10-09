using System.Collections.Generic;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused coverage for authoritative whole-capsule occupancy.</summary>
    public sealed class AntClearanceValidatorTests
    {
        private const string BackgroundId = "background.clearance-test";
        private const string BlockingSupportingId = "solid.blocking-supporting";
        private const string BlockingOnlyId = "solid.blocking-only";
        private const string SupportOnlyId = "solid.support-only";
        private const string PassiveId = "solid.passive";

        private readonly List<Object> temporaryObjects = new();
        private BackgroundTileMaterialDefinition background;
        private SolidTileMaterialDefinition blockingSupporting;
        private SolidTileMaterialDefinition blockingOnly;
        private SolidTileMaterialDefinition supportOnly;
        private SolidTileMaterialDefinition passive;
        private ChunkedWorldData world;
        private AntClearanceValidator validator;

        [SetUp]
        public void SetUp()
        {
            background = CreateDefinition<BackgroundTileMaterialDefinition>(BackgroundId);
            blockingSupporting = CreateSolid(
                BlockingSupportingId,
                blocksMovement: true,
                supportsNavigation: true);
            blockingOnly = CreateSolid(
                BlockingOnlyId,
                blocksMovement: true,
                supportsNavigation: false);
            supportOnly = CreateSolid(
                SupportOnlyId,
                blocksMovement: false,
                supportsNavigation: true);
            passive = CreateSolid(
                PassiveId,
                blocksMovement: false,
                supportsNavigation: false);
            world = new ChunkedWorldData(
                new WorldMaterialRegistry(
                    new[] { background },
                    new[]
                    {
                        blockingSupporting,
                        blockingOnly,
                        supportOnly,
                        passive
                    }));
            // Ordinary unit tests sit around cell (0,0), which is itself a
            // signed chunk corner. Keep all four neighbouring chunks resident
            // so only tests that deliberately omit data receive Unknown results.
            EnsureChunk(new Vector2Int(-1, -1));
            EnsureChunk(new Vector2Int(-1, 0));
            EnsureChunk(new Vector2Int(0, -1));
            EnsureChunk(Vector2Int.zero);
            validator = new AntClearanceValidator(world);
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
        public void SupportedOpenPosition_WithCompleteFootprint_IsClear()
        {
            AddSupport(Vector2Int.zero);

            AntClearanceResult result = Validate(Vector2.zero);

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(result.CentreClassification.HasConfirmedSupport, Is.True);
            Assert.That(result.TouchedCellCount, Is.GreaterThan(0));
        }

        [Test]
        public void UnsupportedCentre_IsRejectedBeforeClearanceCanBeClear()
        {
            AntClearanceResult result = Validate(Vector2.zero);

            Assert.That(
                result.Status,
                Is.EqualTo(AntClearanceStatus.UnsupportedLocation));
        }

        [Test]
        public void CoveredCentre_IsBlocked()
        {
            SetSolid(Vector2Int.zero, BlockingSupportingId);

            AntClearanceResult result = Validate(Vector2.zero);

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
            Assert.That(
                result.CentreClassification.Occupancy,
                Is.EqualTo(NavigationCellOccupancy.Covered));
        }

        [Test]
        public void OpenCentre_WithCapsuleOverlappingNeighbour_IsBlocked()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, BlockingOnlyId);

            AntClearanceResult result = Validate(new Vector2(0.2f, 0f));

            Assert.That(result.CentreCell, Is.EqualTo(Vector2Int.zero));
            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
            Assert.That(result.ProblemCell, Is.EqualTo(Vector2Int.right));
        }

        [Test]
        public void BackgroundOnlyFootprint_DoesNotBlock()
        {
            AddSupport(Vector2Int.zero);

            AntClearanceResult result = Validate(new Vector2(-0.2f, 0f));

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [Test]
        public void NonBlockingSolidInsideFootprint_DoesNotBlock()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, PassiveId);

            AntClearanceResult result = Validate(new Vector2(0.2f, 0f));

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [TestCase(BlockingSupportingId, true)]
        [TestCase(BlockingOnlyId, true)]
        [TestCase(SupportOnlyId, false)]
        [TestCase(PassiveId, false)]
        public void BodyBlocking_UsesBlocksMovementOnly(
            string materialId,
            bool expectedBlocked)
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, materialId);

            AntClearanceResult result = Validate(new Vector2(0.2f, 0f));

            Assert.That(
                result.Status == AntClearanceStatus.BlockedByTerrain,
                Is.EqualTo(expectedBlocked));
        }

        [Test]
        public void Support_UsesSupportsNavigationIndependentlyOfBlocking()
        {
            SetSolid(Vector2Int.up, SupportOnlyId);

            AntClearanceResult result = Validate(Vector2.zero);

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [Test]
        public void HorizontalAndVerticalOrientations_UseTheFullRotatedCapsule()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, BlockingOnlyId);

            var testPosition = new Vector2(0.1f, 0f);
            AntClearanceResult horizontal = Validate(testPosition, 0f);
            AntClearanceResult vertical = Validate(testPosition, 90f);

            Assert.That(horizontal.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
            Assert.That(vertical.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [Test]
        public void IntermediateRotation_DetectsDiagonalCapsuleOverlap()
        {
            SetSolid(Vector2Int.left, SupportOnlyId);
            SetSolid(Vector2Int.one, BlockingOnlyId);

            AntClearanceResult horizontal = Validate(new Vector2(0.2f, 0.2f), 0f);
            AntClearanceResult diagonal = Validate(new Vector2(0.2f, 0.2f), 45f);

            Assert.That(horizontal.Status, Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(diagonal.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
        }

        [Test]
        public void RotatedBodyOffset_ChangesTheTestedCapsuleCentre()
        {
            SetSolid(Vector2Int.left, SupportOnlyId);
            SetSolid(Vector2Int.up, BlockingOnlyId);
            var offsetProfile = new AntCollisionProfile(
                new Vector2(0.6f, 0.4f),
                new Vector2(0.3f, 0f),
                AntCapsuleAxis.Horizontal);

            AntClearanceResult unrotated = validator.Validate(
                Vector2.zero,
                0f,
                offsetProfile);
            AntClearanceResult rotated = validator.Validate(
                Vector2.zero,
                90f,
                offsetProfile);

            Assert.That(unrotated.Status, Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(rotated.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
            Assert.That(rotated.BodyCentre.y, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void ClearanceMargin_ExpandsTheCapsuleConservatively()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, BlockingOnlyId);
            var noMargin = new AntCollisionProfile(
                new Vector2(0.6f, 0.4f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);
            var margin = new AntCollisionProfile(
                new Vector2(0.6f, 0.4f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal,
                0.21f);

            Assert.That(
                validator.Validate(Vector2.zero, 0f, noMargin).Status,
                Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(
                validator.Validate(Vector2.zero, 0f, margin).Status,
                Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
        }

        [Test]
        public void NarrowPassage_AcceptsSmallBodyAndRejectsWideBody()
        {
            SetSolid(Vector2Int.up, BlockingSupportingId);
            SetSolid(Vector2Int.down, BlockingSupportingId);
            var small = new AntCollisionProfile(
                new Vector2(0.6f, 0.6f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);
            var wide = new AntCollisionProfile(
                new Vector2(1.1f, 1.1f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);

            Assert.That(
                validator.Validate(Vector2.zero, 0f, small).Status,
                Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(
                validator.Validate(Vector2.zero, 0f, wide).Status,
                Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
        }

        [Test]
        public void ConcaveCorner_BlocksRoundedCapsuleCorner()
        {
            SetSolid(Vector2Int.left, SupportOnlyId);
            SetSolid(Vector2Int.right, BlockingOnlyId);
            SetSolid(Vector2Int.up, BlockingOnlyId);
            var roundProfile = new AntCollisionProfile(
                new Vector2(0.6f, 0.6f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);

            AntClearanceResult result = validator.Validate(
                new Vector2(0.25f, 0.25f),
                0f,
                roundProfile);

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
        }

        [Test]
        public void ExactTangency_IsClear_ButNearOverlapIsBlocked()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, BlockingOnlyId);
            var profile = new AntCollisionProfile(
                new Vector2(0.6f, 0.4f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);

            AntClearanceResult touching = validator.Validate(
                new Vector2(0.2f, 0f),
                0f,
                profile);
            AntClearanceResult overlapping = validator.Validate(
                new Vector2(0.2001f, 0f),
                0f,
                profile);

            Assert.That(touching.Status, Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(overlapping.Status, Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
        }

        [TestCase(31.5f, 0f, 1, 0)]
        [TestCase(-0.5f, 0f, -1, 0)]
        [TestCase(0f, 31.5f, 0, 1)]
        [TestCase(0f, -0.5f, 0, -1)]
        public void ClearanceCrossesExistingSignedChunkBoundaries(
            float x,
            float y,
            int neighbourChunkX,
            int neighbourChunkY)
        {
            EnsureChunk(new Vector2Int(neighbourChunkX, neighbourChunkY));
            Vector2 position = new Vector2(x, y);
            Vector2Int centreCell = WorldTileCoordinates.WorldPositionToCell(position);
            Vector2Int supportCell = centreCell +
                (Mathf.Abs(x) > 1f ? Vector2Int.up : Vector2Int.right);
            SetSolid(supportCell, SupportOnlyId);
            var profile = Mathf.Abs(x) > 1f
                ? AntCollisionProfile.OrdinaryAnt
                : new AntCollisionProfile(
                    new Vector2(0.6f, 1.2f),
                    Vector2.zero,
                    AntCapsuleAxis.Vertical);

            AntClearanceResult result = validator.Validate(position, 0f, profile);

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [TestCase(31.5f, 31.5f, 1, 1)]
        [TestCase(-0.5f, 31.5f, -1, 1)]
        [TestCase(31.5f, -0.5f, 1, -1)]
        [TestCase(-0.5f, -0.5f, -1, -1)]
        public void FootprintCanSpanFourResidentChunksInEveryQuadrant(
            float x,
            float y,
            int otherChunkX,
            int otherChunkY)
        {
            EnsureChunk(new Vector2Int(otherChunkX, 0));
            EnsureChunk(new Vector2Int(0, otherChunkY));
            EnsureChunk(new Vector2Int(otherChunkX, otherChunkY));
            Vector2 position = new Vector2(x, y);
            Vector2Int centreCell = WorldTileCoordinates.WorldPositionToCell(position);
            SetSolid(centreCell + Vector2Int.right, SupportOnlyId);
            var profile = new AntCollisionProfile(
                new Vector2(0.6f, 0.6f),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);

            AntClearanceResult result = validator.Validate(position, 0f, profile);

            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
        }

        [Test]
        public void MissingTargetChunk_ReturnsUnavailableWithoutGeneration()
        {
            int chunkCount = world.ResidentChunkCount;

            AntClearanceResult result = Validate(new Vector2(32f, 0f));

            Assert.That(
                result.Status,
                Is.EqualTo(AntClearanceStatus.UnavailableWorldData));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunkCount));
        }

        [Test]
        public void MissingFootprintNeighbour_ReturnsUnavailableWithoutGeneration()
        {
            SetSolid(new Vector2Int(31, 1), SupportOnlyId);
            int chunkCount = world.ResidentChunkCount;

            AntClearanceResult result = Validate(new Vector2(31.2f, 0f));

            Assert.That(
                result.Status,
                Is.EqualTo(AntClearanceStatus.UnavailableWorldData));
            Assert.That(result.ProblemCell.x, Is.EqualTo(32));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunkCount));
        }

        [Test]
        public void RemovingBlockingTerrain_ChangesBlockedToClearWithoutCaching()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, BlockingOnlyId);
            Assert.That(
                Validate(new Vector2(0.2f, 0f)).Status,
                Is.EqualTo(AntClearanceStatus.BlockedByTerrain));

            world.RemoveSolidMaterial(Vector2Int.right);

            Assert.That(
                Validate(new Vector2(0.2f, 0f)).Status,
                Is.EqualTo(AntClearanceStatus.Clear));
        }

        [Test]
        public void AddingBlockingTerrain_ChangesClearToBlockedWithoutCaching()
        {
            AddSupport(Vector2Int.zero);
            Assert.That(
                Validate(new Vector2(0.2f, 0f)).Status,
                Is.EqualTo(AntClearanceStatus.Clear));

            SetSolid(Vector2Int.right, BlockingOnlyId);

            Assert.That(
                Validate(new Vector2(0.2f, 0f)).Status,
                Is.EqualTo(AntClearanceStatus.BlockedByTerrain));
        }

        [Test]
        public void NonBlockingSubstitution_ClearsBodyAndPreservesBackground()
        {
            AddSupport(Vector2Int.zero);
            SetSolid(Vector2Int.right, BlockingOnlyId);
            world.GetCell(Vector2Int.right, out WorldTileCellData before);

            SetSolid(Vector2Int.right, PassiveId);
            AntClearanceResult result = Validate(new Vector2(0.2f, 0f));

            world.GetCell(Vector2Int.right, out WorldTileCellData after);
            Assert.That(result.Status, Is.EqualTo(AntClearanceStatus.Clear));
            Assert.That(after.BackgroundMaterialId, Is.EqualTo(before.BackgroundMaterialId));
        }

        [Test]
        public void Query_CreatesNoGameObjectsAndChangesNoClassification()
        {
            AddSupport(Vector2Int.zero);
            int objectCount = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length;
            var classifier = new WorldNavigationClassifier(world);
            WorldNavigationClassification before = classifier.Classify(Vector2Int.zero);

            Validate(Vector2.zero);

            WorldNavigationClassification after = classifier.Classify(Vector2Int.zero);
            Assert.That(
                Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(objectCount));
            Assert.That(after.Occupancy, Is.EqualTo(before.Occupancy));
            Assert.That(after.Support, Is.EqualTo(before.Support));
        }

        [Test]
        public void Query_DoesNotModifyExistingPhysicsComponents()
        {
            AddSupport(Vector2Int.zero);
            var physicsObject = new GameObject(
                "Clearance Independence Fixture",
                typeof(Rigidbody2D),
                typeof(BoxCollider2D));
            temporaryObjects.Add(physicsObject);
            Rigidbody2D body = physicsObject.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            Vector2 colliderSize = physicsObject.GetComponent<BoxCollider2D>().size;

            Validate(Vector2.zero);

            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(
                physicsObject.GetComponent<BoxCollider2D>().size,
                Is.EqualTo(colliderSize));
            Assert.That(physicsObject.GetComponents<Collider2D>().Length, Is.EqualTo(1));
        }

        [TestCase(0f, 0f, -1f, 0.6f)]
        [TestCase(float.NaN, 0f, 1.2f, 0.6f)]
        [TestCase(0f, float.PositiveInfinity, 1.2f, 0.6f)]
        public void InvalidQueryOrProfile_ReturnsExplicitStatus(
            float x,
            float orientation,
            float width,
            float height)
        {
            var profile = new AntCollisionProfile(
                new Vector2(width, height),
                Vector2.zero,
                AntCapsuleAxis.Horizontal);

            AntClearanceResult result = validator.Validate(
                new Vector2(x, 0f),
                orientation,
                profile);

            Assert.That(
                result.Status,
                Is.EqualTo(AntClearanceStatus.InvalidQueryOrProfile));
        }

        private AntClearanceResult Validate(
            Vector2 position,
            float orientation = 0f)
        {
            return validator.Validate(
                position,
                orientation,
                AntCollisionProfile.OrdinaryAnt);
        }

        private void AddSupport(Vector2Int centreCell)
        {
            SetSolid(centreCell + Vector2Int.up, SupportOnlyId);
        }

        private void SetSolid(Vector2Int worldCell, string materialId)
        {
            Assert.That(
                world.SetSolidMaterial(worldCell, materialId),
                Is.EqualTo(WorldCellMutationResult.Success));
        }

        private void EnsureChunk(Vector2Int chunkCoordinate)
        {
            if (!world.ContainsChunk(chunkCoordinate))
            {
                Assert.That(
                    world.CreateChunk(chunkCoordinate, BackgroundId),
                    Is.EqualTo(WorldChunkCreationResult.Success));
            }
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
            serialized.FindProperty("supportsNavigation").boolValue =
                supportsNavigation;
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
    }
}
