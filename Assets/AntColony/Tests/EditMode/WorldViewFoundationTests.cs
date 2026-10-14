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

        [Test]
        public void EntranceReusesCapacityAfterAntMovesClearInBothDirections()
        {
            var antA = new AntId("ant-reuse-a");
            var antB = new AntId("ant-reuse-b");
            AntRegionResidencyRegistry registry = CreateResidencies(antA, antB);
            NestEntranceTransitionService service = CreateEntrance(registry);

            var outboundA = new TransitionId("reuse-outbound-a");
            Assert.That(service.TryRequest(antA, outboundA, out _), Is.True);
            Assert.That(service.MarkApproachComplete(outboundA), Is.True);
            Assert.That(service.TryCommit(outboundA, true,
                destination => IsDestinationFree(registry, destination, antA,
                    antA, antB),
                _ => true), Is.True);
            service.ReleaseTerminal();
            Assert.That(service.IsReserved, Is.False);
            Assert.That(service.CapacityStatus, Is.EqualTo("Available."));
            Assert.That(service.TryRequest(antA, outboundA, out _), Is.False,
                "A released transaction ID must not be replayed.");

            var blockedB = new TransitionId("reuse-blocked-b");
            Assert.That(service.TryRequest(antB, blockedB, out _), Is.True);
            Assert.That(service.MarkApproachComplete(blockedB), Is.True);
            Assert.That(service.TryCommit(blockedB, true,
                destination => IsDestinationFree(registry, destination, antB,
                    antA, antB),
                _ => true), Is.False,
                "Ant A still occupies the Surface arrival cell.");
            Assert.That(registry.IsInRegion(antB, AntRegionIds.Colony), Is.True);
            service.ReleaseTerminal();

            Assert.That(registry.TryGet(antA, out AntResidency surfaceA), Is.True);
            Assert.That(registry.TryRelocate(antA, surfaceA.Revision,
                RegionDestination.Surface(AntRegionIds.Surface,
                    new SurfaceCell(10, 10)), out surfaceA), Is.True);

            var outboundB = new TransitionId("reuse-outbound-b");
            Assert.That(service.TryRequest(antB, outboundB, out _), Is.True);
            Assert.That(service.MarkApproachComplete(outboundB), Is.True);
            Assert.That(service.TryCommit(outboundB, true,
                destination => IsDestinationFree(registry, destination, antB,
                    antA, antB),
                _ => true), Is.True);
            service.ReleaseTerminal();

            Assert.That(registry.TryGet(antA, out surfaceA), Is.True);
            Assert.That(registry.TryGet(antB, out AntResidency surfaceB), Is.True);
            Assert.That(surfaceA.AntId, Is.EqualTo(antA));
            Assert.That(surfaceB.AntId, Is.EqualTo(antB));
            Assert.That(surfaceA.RegionId, Is.EqualTo(AntRegionIds.Surface));
            Assert.That(surfaceB.RegionId, Is.EqualTo(AntRegionIds.Surface));
            Assert.That(surfaceA.Location, Is.Not.EqualTo(surfaceB.Location));

            Assert.That(registry.TryRelocate(antB, surfaceB.Revision,
                RegionDestination.Surface(AntRegionIds.Surface,
                    new SurfaceCell(11, 10)), out surfaceB), Is.True);
            var returnA = new TransitionId("reuse-return-a");
            Assert.That(service.TryRequest(antA, returnA, out _), Is.True);
            Assert.That(service.MarkApproachComplete(returnA), Is.True);
            Assert.That(service.TryCommit(returnA, true,
                destination => IsDestinationFree(registry, destination, antA,
                    antA, antB),
                _ => true), Is.True);
            service.ReleaseTerminal();

            Assert.That(registry.TryGet(antA, out AntResidency colonyA), Is.True);
            Assert.That(registry.TryRelocate(antA, colonyA.Revision,
                RegionDestination.Colony(AntRegionIds.Colony,
                    new ColonyCell(-5, -8)), out _), Is.True);
            var returnB = new TransitionId("reuse-return-b");
            Assert.That(service.TryRequest(antB, returnB, out _), Is.True);
            Assert.That(service.MarkApproachComplete(returnB), Is.True);
            Assert.That(service.TryCommit(returnB, true,
                destination => IsDestinationFree(registry, destination, antB,
                    antA, antB),
                _ => true), Is.True);
            service.ReleaseTerminal();

            Assert.That(registry.IsInRegion(antA, AntRegionIds.Colony), Is.True);
            Assert.That(registry.IsInRegion(antB, AntRegionIds.Colony), Is.True);
            Assert.That(service.IsReserved, Is.False);
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

        private static bool IsDestinationFree(
            AntRegionResidencyRegistry registry,
            RegionDestination destination,
            AntId movingAnt,
            params AntId[] knownAnts)
        {
            foreach (AntId ant in knownAnts)
            {
                if (ant.Equals(movingAnt)) continue;
                if (registry.TryGet(ant, out AntResidency residency) &&
                    residency.Location.Equals(destination)) return false;
            }
            return true;
        }
    }
}
