using System.Collections.Generic;
using AntColony.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>
    /// Verifies classification directly from authoritative world data without
    /// requiring movement components, physics, Tilemaps, or scene objects.
    /// </summary>
    public sealed class WorldNavigationClassifierTests
    {
        private const string BackgroundId = "background.default";
        private const string BlockingSupportId = "solid.blocking-support";
        private const string BlockingNonSupportId = "solid.blocking-no-support";
        private const string NonBlockingSupportId = "solid.nonblocking-support";
        private const string NonBlockingNonSupportId = "solid.nonblocking-no-support";

        private readonly List<ScriptableObject> temporaryDefinitions = new();
        private ChunkedWorldData worldData;
        private WorldNavigationClassifier classifier;

        [SetUp]
        public void SetUp()
        {
            BackgroundTileMaterialDefinition background =
                CreateDefinition<BackgroundTileMaterialDefinition>(
                    BackgroundId,
                    "Default Background");

            var registry = new WorldMaterialRegistry(
                new[] { background },
                new[]
                {
                    CreateSolidDefinition(BlockingSupportId, true, true),
                    CreateSolidDefinition(BlockingNonSupportId, true, false),
                    CreateSolidDefinition(NonBlockingSupportId, false, true),
                    CreateSolidDefinition(NonBlockingNonSupportId, false, false)
                });

            worldData = new ChunkedWorldData(registry);
            classifier = new WorldNavigationClassifier(worldData);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject definition in temporaryDefinitions)
            {
                Object.DestroyImmediate(definition);
            }

            temporaryDefinitions.Clear();
        }

        [Test]
        public void MissingChunk_IsUnavailableAndDoesNotGenerateTerrain()
        {
            WorldNavigationClassification result = classifier.Classify(Vector2Int.zero);

            Assert.That(result.Occupancy, Is.EqualTo(NavigationCellOccupancy.Unavailable));
            Assert.That(result.Support, Is.EqualTo(NavigationSupportStatus.NotApplicable));
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(worldData.ResidentChunkCount, Is.Zero);
        }

        [Test]
        public void MovementBlockingSolid_IsCovered()
        {
            CreateChunkContaining(Vector2Int.zero);
            worldData.SetSolidMaterial(Vector2Int.zero, BlockingNonSupportId);

            WorldNavigationClassification result = classifier.Classify(Vector2Int.zero);

            Assert.That(result.Occupancy, Is.EqualTo(NavigationCellOccupancy.Covered));
            Assert.That(result.Support, Is.EqualTo(NavigationSupportStatus.NotApplicable));
        }

        [Test]
        public void BackgroundOnlyInteriorCell_IsExposedAndUnsupported()
        {
            CreateChunkContaining(new Vector2Int(5, 5));

            WorldNavigationClassification result =
                classifier.Classify(new Vector2Int(5, 5));

            Assert.That(result.Occupancy, Is.EqualTo(NavigationCellOccupancy.Exposed));
            Assert.That(result.Support, Is.EqualTo(NavigationSupportStatus.Unsupported));
        }

        [TestCase(0, 1)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        public void CardinalSupportingSolid_MakesExposedCellSupported(
            int supportOffsetX,
            int supportOffsetY)
        {
            var target = new Vector2Int(5, 5);
            CreateChunkContaining(target);
            worldData.SetSolidMaterial(
                target + new Vector2Int(supportOffsetX, supportOffsetY),
                BlockingSupportId);

            WorldNavigationClassification result = classifier.Classify(target);

            Assert.That(result.Occupancy, Is.EqualTo(NavigationCellOccupancy.Exposed));
            Assert.That(result.Support, Is.EqualTo(NavigationSupportStatus.Supported));
        }

        [Test]
        public void DiagonalOnlySolid_DoesNotProvideSupport()
        {
            var target = new Vector2Int(5, 5);
            CreateChunkContaining(target);
            worldData.SetSolidMaterial(target + Vector2Int.one, BlockingSupportId);

            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Unsupported));
        }

        [Test]
        public void MaterialFlags_AreEvaluatedIndependently()
        {
            var target = new Vector2Int(5, 5);
            CreateChunkContaining(target);

            worldData.SetSolidMaterial(target, NonBlockingNonSupportId);
            WorldNavigationClassification nonBlockingTarget = classifier.Classify(target);
            Assert.That(
                nonBlockingTarget.Occupancy,
                Is.EqualTo(NavigationCellOccupancy.Exposed));

            worldData.SetSolidMaterial(target + Vector2Int.up, BlockingNonSupportId);
            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Unsupported));

            worldData.SetSolidMaterial(target + Vector2Int.up, NonBlockingSupportId);
            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Supported));
        }

        [TestCase(31, 0, 32, 0)]
        [TestCase(32, 0, 31, 0)]
        [TestCase(-1, 0, 0, 0)]
        [TestCase(-32, 0, -33, 0)]
        [TestCase(0, 31, 0, 32)]
        [TestCase(0, -32, 0, -33)]
        [TestCase(-33, 31, -33, 32)]
        [TestCase(32, -33, 31, -33)]
        public void CardinalSupport_CrossesChunkBoundariesAndQuadrants(
            int targetX,
            int targetY,
            int supportX,
            int supportY)
        {
            var target = new Vector2Int(targetX, targetY);
            var support = new Vector2Int(supportX, supportY);
            CreateChunkContaining(target);
            CreateChunkContaining(support);
            worldData.SetSolidMaterial(support, BlockingSupportId);

            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Supported));
        }

        [Test]
        public void MissingNeighbourWithoutConfirmedSupport_IsUnknown()
        {
            var edgeCell = new Vector2Int(31, 5);
            CreateChunkContaining(edgeCell);
            int chunksBefore = worldData.ResidentChunkCount;

            WorldNavigationClassification result = classifier.Classify(edgeCell);

            Assert.That(result.Occupancy, Is.EqualTo(NavigationCellOccupancy.Exposed));
            Assert.That(result.Support, Is.EqualTo(NavigationSupportStatus.Unknown));
            Assert.That(worldData.ResidentChunkCount, Is.EqualTo(chunksBefore));
            Assert.That(worldData.ContainsChunk(Vector2Int.right), Is.False);
        }

        [Test]
        public void ConfirmedSupport_TakesPrecedenceOverUnavailableNeighbour()
        {
            var edgeCell = new Vector2Int(31, 5);
            CreateChunkContaining(edgeCell);
            worldData.SetSolidMaterial(edgeCell + Vector2Int.up, BlockingSupportId);

            Assert.That(
                classifier.Classify(edgeCell).Support,
                Is.EqualTo(NavigationSupportStatus.Supported));
            Assert.That(worldData.ContainsChunk(Vector2Int.right), Is.False);
        }

        [Test]
        public void SolidMutations_AreReflectedImmediatelyWithoutCaching()
        {
            var target = new Vector2Int(5, 5);
            Vector2Int support = target + Vector2Int.up;
            CreateChunkContaining(target);

            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Unsupported));

            worldData.SetSolidMaterial(support, BlockingSupportId);
            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Supported));

            worldData.SetSolidMaterial(support, BlockingNonSupportId);
            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Unsupported));

            worldData.SetSolidMaterial(support, BlockingSupportId);
            worldData.RemoveSolidMaterial(support);
            Assert.That(
                classifier.Classify(target).Support,
                Is.EqualTo(NavigationSupportStatus.Unsupported));
        }

        [Test]
        public void RejectedAndUnrelatedMutations_DoNotChangeClassification()
        {
            var target = new Vector2Int(5, 5);
            CreateChunkContaining(target);
            NavigationSupportStatus original = classifier.Classify(target).Support;

            Assert.That(
                worldData.SetSolidMaterial(target + Vector2Int.up, "solid.unknown"),
                Is.EqualTo(WorldCellMutationResult.InvalidMaterial));
            worldData.SetSolidMaterial(new Vector2Int(20, 20), BlockingSupportId);

            Assert.That(classifier.Classify(target).Support, Is.EqualTo(original));
        }

        [Test]
        public void Classification_IsSceneIndependentAndCreatesNoGameObjects()
        {
            int objectsBefore = Resources.FindObjectsOfTypeAll<GameObject>().Length;
            CreateChunkContaining(Vector2Int.zero);

            classifier.Classify(Vector2Int.zero);

            int objectsAfter = Resources.FindObjectsOfTypeAll<GameObject>().Length;
            Assert.That(
                typeof(WorldNavigationClassifier).IsSubclassOf(typeof(Component)),
                Is.False);
            Assert.That(objectsAfter, Is.EqualTo(objectsBefore));
        }

        [Test]
        public void Classification_DoesNotModifyExistingRigidbodySettings()
        {
            Rigidbody2D[] bodies = Resources.FindObjectsOfTypeAll<Rigidbody2D>();
            var gravityScales = new float[bodies.Length];
            var constraints = new RigidbodyConstraints2D[bodies.Length];
            for (int index = 0; index < bodies.Length; index++)
            {
                gravityScales[index] = bodies[index].gravityScale;
                constraints[index] = bodies[index].constraints;
            }

            CreateChunkContaining(Vector2Int.zero);
            classifier.Classify(Vector2Int.zero);

            for (int index = 0; index < bodies.Length; index++)
            {
                Assert.That(bodies[index].gravityScale, Is.EqualTo(gravityScales[index]));
                Assert.That(bodies[index].constraints, Is.EqualTo(constraints[index]));
            }
        }

        private void CreateChunkContaining(Vector2Int worldCell)
        {
            Vector2Int chunk = WorldTileCoordinates.GetChunkCoordinate(worldCell);
            if (!worldData.ContainsChunk(chunk))
            {
                Assert.That(
                    worldData.CreateChunk(chunk, BackgroundId),
                    Is.EqualTo(WorldChunkCreationResult.Success));
            }
        }

        private SolidTileMaterialDefinition CreateSolidDefinition(
            string materialId,
            bool blocksMovement,
            bool supportsNavigation)
        {
            SolidTileMaterialDefinition definition =
                CreateDefinition<SolidTileMaterialDefinition>(materialId, materialId);
            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("blocksMovement").boolValue = blocksMovement;
            serializedDefinition.FindProperty("supportsNavigation").boolValue =
                supportsNavigation;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private TDefinition CreateDefinition<TDefinition>(
            string materialId,
            string displayName)
            where TDefinition : WorldTileMaterialDefinition
        {
            TDefinition definition = ScriptableObject.CreateInstance<TDefinition>();
            definition.name = displayName;

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stableMaterialId").stringValue = materialId;
            serializedDefinition.FindProperty("displayName").stringValue = displayName;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            temporaryDefinitions.Add(definition);
            return definition;
        }
    }
}
