using System.Collections.Generic;
using System.Reflection;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused coverage for bounded 2.3A ground and 2.3B grip routes.</summary>
    public sealed class DirectGroundRouteValidatorTests
    {
        private const string BackgroundId = "background.route-test";
        private const string PreparedId = "background.route-prepared";
        private const string GroundId = "solid.route-ground";

        private readonly List<Object> temporaryObjects = new();
        private ChunkedWorldData world;
        private DirectGroundRouteValidator validator;

        [SetUp]
        public void SetUp()
        {
            BackgroundTileMaterialDefinition background =
                Track(ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>());
            SetField(background, "stableMaterialId", BackgroundId);
            SetField(background, "displayName", "Route Test Background");
            SetField(background, "allowsHorizontalMovement", true);
            SetField(background, "gravityAppliesWhenUnsupported", true);

            BackgroundTileMaterialDefinition prepared =
                Track(ScriptableObject.CreateInstance<BackgroundTileMaterialDefinition>());
            SetField(prepared, "stableMaterialId", PreparedId);
            SetField(prepared, "displayName", "Prepared Route Background");
            SetField(prepared, "allowsHorizontalMovement", true);
            SetField(prepared, "allowsUpwardMovement", true);
            SetField(prepared, "allowsDownwardMovement", true);
            SetField(prepared, "providesClimbingGrip", true);
            SetField(prepared, "gravityAppliesWhenUnsupported", false);

            SolidTileMaterialDefinition ground =
                Track(ScriptableObject.CreateInstance<SolidTileMaterialDefinition>());
            SetField(ground, "stableMaterialId", GroundId);
            SetField(ground, "displayName", "Route Test Ground");
            SetField(ground, "blocksMovement", true);
            SetField(ground, "supportsNavigation", true);

            var registry = new WorldMaterialRegistry(
                new[] { background, prepared },
                new[] { ground });
            world = new ChunkedWorldData(registry, 23);
            Assert.That(
                world.CreateChunk(new Vector2Int(-1, 0), BackgroundId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            Assert.That(
                world.CreateChunk(Vector2Int.zero, BackgroundId),
                Is.EqualTo(WorldChunkCreationResult.Success));
            SetGround(-8, 31);
            validator = new DirectGroundRouteValidator(
                world,
                AntCollisionProfile.OrdinaryAnt);
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
        }

        [Test]
        public void LevelSupportedCorridor_IsAcceptedWithBoundedSamples()
        {
            DirectGroundRouteResult result = validator.Validate(
                new Vector2(0f, 0.65f),
                new Vector2(4f, 0.65f));

            Assert.That(result.Status, Is.EqualTo(DirectGroundRouteStatus.Success));
            Assert.That(result.SampleCount, Is.InRange(2, 22));
        }

        [Test]
        public void SelectedFloorOrExposedCell_ResolvesSameGroundedPose()
        {
            DirectGroundRouteResult floor = validator.ResolveGroundedPosition(
                new Vector2Int(3, 0),
                out Vector2 floorPosition);
            DirectGroundRouteResult exposed = validator.ResolveGroundedPosition(
                new Vector2Int(3, 1),
                out Vector2 exposedPosition);

            Assert.That(floor.Succeeded, Is.True);
            Assert.That(exposed.Succeeded, Is.True);
            Assert.That(floorPosition, Is.EqualTo(new Vector2(3f, 0.65f)));
            Assert.That(exposedPosition, Is.EqualTo(floorPosition));
        }

        [Test]
        public void UnsupportedDestination_IsRejected()
        {
            Assert.That(
                world.RemoveSolidMaterial(new Vector2Int(5, 0)),
                Is.EqualTo(WorldCellMutationResult.Success));

            DirectGroundRouteResult result = validator.ResolveGroundedPosition(
                new Vector2Int(5, 1),
                out _);

            Assert.That(
                result.Status,
                Is.EqualTo(DirectGroundRouteStatus.DestinationUnsupported));
        }

        [Test]
        public void BlockingTerrain_PreventsRouteAcceptance()
        {
            Assert.That(
                world.SetSolidMaterial(new Vector2Int(2, 1), GroundId),
                Is.EqualTo(WorldCellMutationResult.Success));

            DirectGroundRouteResult result = validator.Validate(
                new Vector2(0f, 0.65f),
                new Vector2(4f, 0.65f));

            Assert.That(result.Status, Is.EqualTo(DirectGroundRouteStatus.RouteBlocked));
        }

        [Test]
        public void GroundGap_PreventsRouteAcceptance()
        {
            Assert.That(
                world.RemoveSolidMaterial(new Vector2Int(2, 0)),
                Is.EqualTo(WorldCellMutationResult.Success));

            DirectGroundRouteResult result = validator.Validate(
                new Vector2(0f, 0.65f),
                new Vector2(4f, 0.65f));

            Assert.That(
                result.Status,
                Is.EqualTo(DirectGroundRouteStatus.RouteUnsupported));
        }

        [Test]
        public void UnpreparedVerticalClimb_IsRejectedByDirectionPermission()
        {
            DirectGroundRouteResult result = validator.Validate(
                new Vector2(0f, 0.65f),
                new Vector2(2f, 1.65f));

            Assert.That(
                result.Status,
                Is.EqualTo(DirectGroundRouteStatus.DirectionNotPermitted));
        }

        [Test]
        public void PreparedBackground_AllowsUpDownHorizontalAndDiagonalSegments()
        {
            SetPreparedRectangle(-1, 4, 1, 5);

            Assert.That(validator.Validate(
                new Vector2(0f, 0.65f),
                new Vector2(0f, 4f)).Succeeded, Is.True);
            Assert.That(validator.Validate(
                new Vector2(0f, 4f),
                new Vector2(0f, 1f)).Succeeded, Is.True);
            Assert.That(validator.Validate(
                new Vector2(0f, 2f),
                new Vector2(3f, 2f)).Succeeded, Is.True);
            Assert.That(validator.Validate(
                new Vector2(0f, 1f),
                new Vector2(3f, 4f)).Succeeded, Is.True);
        }

        [Test]
        public void PreparedDiagonal_CannotCutBlockingCorner()
        {
            SetPreparedRectangle(0, 3, 1, 4);
            Assert.That(
                world.SetSolidMaterial(new Vector2Int(1, 2), GroundId),
                Is.EqualTo(WorldCellMutationResult.Success));

            DirectGroundRouteResult result = validator.Validate(
                new Vector2(0f, 1f),
                new Vector2(3f, 4f));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status,
                Is.EqualTo(DirectGroundRouteStatus.RouteBlocked).Or
                    .EqualTo(DirectGroundRouteStatus.DiagonalCornerBlocked));
        }

        [Test]
        public void ControlledResolver_UsesPreparedCellCentre()
        {
            Assert.That(
                world.SetBackgroundMaterial(new Vector2Int(2, 3), PreparedId),
                Is.EqualTo(WorldCellMutationResult.Success));

            DirectGroundRouteResult result = validator.ResolveControlledPosition(
                new Vector2Int(2, 3),
                out Vector2 position);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(position, Is.EqualTo(new Vector2(2f, 3f)));
        }

        [Test]
        public void MissingChunk_DoesNotGenerateData()
        {
            int chunksBefore = world.ResidentChunkCount;

            DirectGroundRouteResult result = validator.Validate(
                new Vector2(20f, 0.65f),
                new Vector2(33f, 0.65f));

            Assert.That(
                result.Status,
                Is.EqualTo(DirectGroundRouteStatus.UnavailableWorldData));
            Assert.That(world.ResidentChunkCount, Is.EqualTo(chunksBefore));
        }

        [Test]
        public void LongDestination_IsRejectedBeforeWorldScanning()
        {
            DirectGroundRouteResult result = validator.Validate(
                new Vector2(0f, 0.65f),
                new Vector2(25f, 0.65f));

            Assert.That(result.Status, Is.EqualTo(DirectGroundRouteStatus.RouteTooLong));
            Assert.That(result.SampleCount, Is.Zero);
        }

        private void SetGround(int minimumX, int maximumX)
        {
            for (int x = minimumX; x <= maximumX; x++)
            {
                Assert.That(
                    world.SetSolidMaterial(new Vector2Int(x, 0), GroundId),
                    Is.EqualTo(WorldCellMutationResult.Success));
            }
        }

        private void SetPreparedRectangle(
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY)
        {
            for (int y = minimumY; y <= maximumY; y++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                {
                    Assert.That(
                        world.SetBackgroundMaterial(
                            new Vector2Int(x, y),
                            PreparedId),
                        Is.EqualTo(WorldCellMutationResult.Success));
                }
            }
        }

        private T Track<T>(T item) where T : Object
        {
            temporaryObjects.Add(item);
            return item;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            Assert.Fail($"Serialized field '{fieldName}' was not found.");
        }
    }
}
