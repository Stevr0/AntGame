using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.World;
using NUnit.Framework;

namespace AntColony.Tests.EditMode
{
    public sealed class WorldViewFoundationTests
    {
        [Test]
        public void DevelopmentSurfaceIsBoundedDeterministicAndFutureChunkCompatible()
        {
            SurfaceWorldData first = SurfaceWorldData.CreateDevelopmentFixture();
            SurfaceWorldData second = SurfaceWorldData.CreateDevelopmentFixture();

            Assert.That(first.Width, Is.EqualTo(64));
            Assert.That(first.Height, Is.EqualTo(64));
            Assert.That(SurfaceWorldData.FutureChunkSize, Is.EqualTo(32));
            Assert.That(first.Obstacles, Is.EqualTo(second.Obstacles));
            Assert.That(first.IsTraversable(new SurfaceCell(0, 0)), Is.True);
            Assert.That(first.Contains(new SurfaceCell(-1, 0)), Is.False);
            Assert.That(first.IsObstacle(new SurfaceCell(18, 8)), Is.True);
        }

        [Test]
        public void SurfacePathIsDeterministicBoundedAndAvoidsObstacles()
        {
            SurfaceWorldData world = SurfaceWorldData.CreateDevelopmentFixture();
            var finder = new SurfacePathfinder();
            SurfacePathResult first = finder.FindPath(world,
                new SurfaceCell(4, 4), new SurfaceCell(30, 18));
            SurfacePathResult second = finder.FindPath(world,
                new SurfaceCell(4, 4), new SurfaceCell(30, 18));

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Cells, Is.EqualTo(second.Cells));
            Assert.That(first.Cells.All(world.IsTraversable), Is.True);
            Assert.That(first.VisitedCells, Is.LessThanOrEqualTo(64 * 64));
            Assert.That(finder.FindPath(world, new SurfaceCell(4, 4),
                new SurfaceCell(18, 8)).Status,
                Is.EqualTo(SurfacePathStatus.InvalidDestination));
            Assert.That(finder.FindPath(world, new SurfaceCell(4, 4),
                new SurfaceCell(64, 4)).Status,
                Is.EqualTo(SurfacePathStatus.InvalidDestination));
        }

        [Test]
        public void SurfaceDiagonalCannotCutAClosedCorner()
        {
            var world = new SurfaceWorldData(3, 3, new[]
            {
                new SurfaceCell(1, 0), new SurfaceCell(0, 1)
            });
            SurfacePathResult result = new SurfacePathfinder().FindPath(world,
                new SurfaceCell(0, 0), new SurfaceCell(1, 1));
            Assert.That(result.Status, Is.EqualTo(SurfacePathStatus.NoRoute));
        }

        [Test]
        public void ResidencyOwnsExactlyOneTypedLocationAndRequiresRevision()
        {
            var ant = new AntId("ant-test-region-001");
            var registry = new AntRegionResidencyRegistry();
            Assert.That(registry.Register(ant, RegionDestination.Colony(
                AntRegionIds.Colony, new ColonyCell(2, -8))), Is.True);
            Assert.That(registry.Register(ant, RegionDestination.Surface(
                AntRegionIds.Surface, new SurfaceCell(2, 2))), Is.False);
            Assert.That(registry.TryMove(ant, 1, RegionDestination.Surface(
                AntRegionIds.Surface, new SurfaceCell(6, 4)), out _), Is.False);
            Assert.That(registry.TryMove(ant, 0, RegionDestination.Surface(
                AntRegionIds.Surface, new SurfaceCell(6, 4)), out AntResidency moved),
                Is.True);
            Assert.That(moved.RegionId, Is.EqualTo(AntRegionIds.Surface));
            Assert.That(moved.Location.Kind, Is.EqualTo(RegionLocationKind.Surface));
        }

        [Test]
        public void EntranceEnforcesCapacityPauseAndIdempotentCommit()
        {
            var first = new AntId("ant-transition-001");
            var second = new AntId("ant-transition-002");
            AntRegionResidencyRegistry registry = CreateResidencies(first, second);
            NestEntranceTransitionService service = CreateEntrance(registry);
            var id = new TransitionId("transition-001");

            Assert.That(service.TryRequest(first, id, out EntranceTransition tx), Is.True);
            Assert.That(service.TryRequest(second, new TransitionId("transition-002"),
                out _), Is.False);
            Assert.That(service.MarkApproachComplete(id), Is.True);
            Assert.That(service.TryCommit(id, false, _ => true, _ => true), Is.False);
            Assert.That(registry.IsInRegion(first, AntRegionIds.Colony), Is.True);
            Assert.That(service.TryCommit(id, true, _ => true, _ => true), Is.True);
            Assert.That(service.TryCommit(id, true, _ => true, _ => true), Is.True);
            Assert.That(registry.TryGet(first, out AntResidency residency), Is.True);
            Assert.That(residency.RegionId, Is.EqualTo(AntRegionIds.Surface));
            Assert.That(residency.Revision, Is.EqualTo(2));
        }

        [Test]
        public void EntranceCancellationAndRepresentationFailureKeepSourceSafe()
        {
            var ant = new AntId("ant-transition-safe-001");
            AntRegionResidencyRegistry registry = CreateResidencies(ant);
            NestEntranceTransitionService service = CreateEntrance(registry);
            var cancelledId = new TransitionId("transition-cancelled");
            Assert.That(service.TryRequest(ant, cancelledId, out _), Is.True);
            Assert.That(service.Cancel(cancelledId), Is.True);
            Assert.That(registry.IsInRegion(ant, AntRegionIds.Colony), Is.True);
            service.ReleaseTerminal();

            var failedId = new TransitionId("transition-handoff-failure");
            Assert.That(service.TryRequest(ant, failedId, out _), Is.True);
            Assert.That(service.MarkApproachComplete(failedId), Is.True);
            bool restored = false;
            Assert.That(service.TryCommit(failedId, true, _ => true, _ => false,
                _ => restored = true), Is.False);
            Assert.That(restored, Is.True);
            Assert.That(registry.IsInRegion(ant, AntRegionIds.Colony), Is.True);
        }

        [Test]
        public void EntranceSupportsRepeatedBidirectionalRoundTrip()
        {
            var ant = new AntId("ant-round-trip-001");
            AntRegionResidencyRegistry registry = CreateResidencies(ant);
            NestEntranceTransitionService service = CreateEntrance(registry);
            Commit(service, new TransitionId("outbound"), ant);
            service.ReleaseTerminal();
            Commit(service, new TransitionId("return"), ant);
            Assert.That(registry.TryGet(ant, out AntResidency residency), Is.True);
            Assert.That(residency.RegionId, Is.EqualTo(AntRegionIds.Colony));
            Assert.That(residency.Revision, Is.EqualTo(4));
        }

        [Test]
        public void EntranceRejectsUnavailableRegionAndBlockedArrivalSafely()
        {
            var ant = new AntId("ant-unavailable-001");
            AntRegionResidencyRegistry registry = CreateResidencies(ant);
            NestEntranceTransitionService service = CreateEntrance(registry);
            service.Availability = EntranceAvailability.DestinationRegionUnavailable;
            Assert.That(service.TryRequest(ant, new TransitionId("unavailable"),
                out _), Is.False);
            Assert.That(registry.IsInRegion(ant, AntRegionIds.Colony), Is.True);

            service.Availability = EntranceAvailability.Open;
            var blocked = new TransitionId("blocked-arrival");
            Assert.That(service.TryRequest(ant, blocked, out _), Is.True);
            Assert.That(service.MarkApproachComplete(blocked), Is.True);
            Assert.That(service.TryCommit(blocked, true, _ => false, _ => true),
                Is.False);
            Assert.That(registry.IsInRegion(ant, AntRegionIds.Colony), Is.True);
        }

        private static AntRegionResidencyRegistry CreateResidencies(params AntId[] ants)
        {
            var registry = new AntRegionResidencyRegistry();
            foreach (AntId ant in ants)
                registry.Register(ant, RegionDestination.Colony(AntRegionIds.Colony,
                    new ColonyCell(-2, -8)));
            return registry;
        }

        private static NestEntranceTransitionService CreateEntrance(
            AntRegionResidencyRegistry registry) => new(registry,
                new NestEntranceDefinition(new EntranceId("entrance-test"),
                    new ColonyCell(-1, -8), new ColonyCell(0, -8),
                    new SurfaceCell(5, 4), new SurfaceCell(6, 4)));

        private static void Commit(NestEntranceTransitionService service,
            TransitionId id, AntId ant)
        {
            Assert.That(service.TryRequest(ant, id, out _), Is.True);
            Assert.That(service.MarkApproachComplete(id), Is.True);
            Assert.That(service.TryCommit(id, true, _ => true, _ => true), Is.True);
        }
    }
}
