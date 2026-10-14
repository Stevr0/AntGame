using System;
using AntColony.Ants;
using AntColony.World;

namespace AntColony.Commands
{
    public enum EntranceAvailability
    {
        Open, Blocked, SourceRegionUnavailable, DestinationRegionUnavailable
    }

    public enum EntranceTransitionState
    {
        Requested, Approaching, ReadyToCommit, Committing,
        Completed, Cancelled, Failed
    }

    public sealed class NestEntranceDefinition
    {
        public NestEntranceDefinition(EntranceId id, ColonyCell colonyApproach,
            ColonyCell colonyArrival, SurfaceCell surfaceApproach,
            SurfaceCell surfaceArrival)
        {
            Id = id; ColonyApproach = colonyApproach; ColonyArrival = colonyArrival;
            SurfaceApproach = surfaceApproach; SurfaceArrival = surfaceArrival;
        }
        public EntranceId Id { get; }
        public ColonyCell ColonyApproach { get; }
        public ColonyCell ColonyArrival { get; }
        public SurfaceCell SurfaceApproach { get; }
        public SurfaceCell SurfaceArrival { get; }
    }

    public sealed class EntranceTransition
    {
        internal EntranceTransition(TransitionId id, AntResidency source,
            RegionDestination destination)
        { Id = id; AntId = source.AntId; Source = source; Destination = destination; }
        public TransitionId Id { get; }
        public AntId AntId { get; }
        public AntResidency Source { get; internal set; }
        public RegionDestination Destination { get; }
        public EntranceTransitionState State { get; internal set; } =
            EntranceTransitionState.Requested;
        public string Status { get; internal set; } = "Transition requested.";
        public bool IsTerminal => State == EntranceTransitionState.Completed ||
            State == EntranceTransitionState.Cancelled ||
            State == EntranceTransitionState.Failed;
    }

    /// <summary>
    /// Capacity-one, idempotent residency transaction. Visual handoff is a
    /// callback and never becomes authoritative ant state.
    /// </summary>
    public sealed class NestEntranceTransitionService
    {
        private readonly AntRegionResidencyRegistry residencies;
        private readonly NestEntranceDefinition definition;
        private EntranceTransition owner;

        public NestEntranceTransitionService(AntRegionResidencyRegistry residencies,
            NestEntranceDefinition definition)
        { this.residencies = residencies; this.definition = definition; }

        public EntranceTransition Owner => owner;
        public EntranceAvailability Availability { get; set; } = EntranceAvailability.Open;

        public bool TryRequest(AntId antId, TransitionId transitionId,
            out EntranceTransition transition)
        {
            transition = null;
            if (owner != null && !owner.IsTerminal)
            {
                if (owner.Id.Equals(transitionId) && owner.AntId.Equals(antId))
                { transition = owner; return true; }
                return false;
            }
            if (Availability != EntranceAvailability.Open ||
                !residencies.TryGet(antId, out AntResidency source)) return false;

            RegionDestination destination;
            if (source.Location.Kind == RegionLocationKind.Colony &&
                source.RegionId == AntRegionIds.Colony)
                destination = RegionDestination.Surface(AntRegionIds.Surface,
                    definition.SurfaceArrival);
            else if (source.Location.Kind == RegionLocationKind.Surface &&
                source.RegionId == AntRegionIds.Surface)
                destination = RegionDestination.Colony(AntRegionIds.Colony,
                    definition.ColonyArrival);
            else return false;

            owner = transition = new EntranceTransition(transitionId, source, destination)
            { State = EntranceTransitionState.Approaching,
              Status = "Entrance reserved; approaching locally." };
            return true;
        }

        public bool MarkApproachComplete(TransitionId id)
        {
            if (!Matches(id) || owner.State != EntranceTransitionState.Approaching)
                return false;
            RegionDestination approach = owner.Source.Location.Kind ==
                RegionLocationKind.Colony
                    ? RegionDestination.Colony(AntRegionIds.Colony,
                        definition.ColonyApproach)
                    : RegionDestination.Surface(AntRegionIds.Surface,
                        definition.SurfaceApproach);
            if (!residencies.TryMove(owner.AntId, owner.Source.Revision,
                approach, out AntResidency atApproach))
                return Fail("Residency changed during the local approach.");
            owner.Source = atApproach;
            owner.State = EntranceTransitionState.ReadyToCommit;
            owner.Status = "At entrance; ready for safe handoff.";
            return true;
        }

        public bool TryCommit(TransitionId id, bool simulationRunning,
            Func<RegionDestination, bool> validateDestination,
            Func<RegionDestination, bool> activateDestinationRepresentation,
            Action<AntResidency> restoreSourceRepresentation = null)
        {
            if (!Matches(id)) return false;
            if (owner.State == EntranceTransitionState.Completed) return true;
            if (owner.State != EntranceTransitionState.ReadyToCommit ||
                !simulationRunning)
            { if (!simulationRunning) owner.Status = "Waiting for simulation."; return false; }
            if (Availability != EntranceAvailability.Open ||
                validateDestination == null || !validateDestination(owner.Destination))
                return Fail("Destination is unavailable or occupied.");

            owner.State = EntranceTransitionState.Committing;
            if (!residencies.TryMove(owner.AntId, owner.Source.Revision,
                owner.Destination, out AntResidency destinationResidency))
                return Fail("Residency changed before commit.");

            if (activateDestinationRepresentation == null ||
                !activateDestinationRepresentation(owner.Destination))
            {
                residencies.TryRestore(owner.Source, destinationResidency.Revision);
                restoreSourceRepresentation?.Invoke(owner.Source);
                return Fail("Representation handoff failed; source residency restored.");
            }

            owner.State = EntranceTransitionState.Completed;
            owner.Status = "Transition completed exactly once.";
            return true;
        }

        public bool Cancel(TransitionId id)
        {
            if (!Matches(id) || owner.State == EntranceTransitionState.Committing ||
                owner.IsTerminal) return false;
            owner.State = EntranceTransitionState.Cancelled;
            owner.Status = "Travel cancelled; source residency retained.";
            return true;
        }

        public void ReleaseTerminal()
        { if (owner != null && owner.IsTerminal) owner = null; }

        private bool Fail(string status)
        { owner.State = EntranceTransitionState.Failed; owner.Status = status; return false; }
        private bool Matches(TransitionId id) => owner != null && owner.Id.Equals(id);
    }
}
