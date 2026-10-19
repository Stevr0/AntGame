# Colony Construction and Rooms

## WV-B3D.2 registry boundary

The implemented Placement registry observes completed legacy Construction as
one deterministic projection and publishes its identity/footprint/revision. It
does not replace site staging or `ConstructionWorldTransaction`, consume
materials, award XP, or activate another function/capacity. Definitions carry
nullable durability metadata; legacy projections have unknown HP. Damage,
Repair, destruction, Doors, room scoring and dismantling remain unimplemented.

## WV-B3D.1 Placement migration specification

**Documentation complete.** The
[Placement Migration](placement-migration.md) specification defines one-cell
Placement identity, footprint occupancy, exact-once Construction completion,
function-by-function authority transfer, dismantling boundaries and legacy
retirement proof. Existing functional Background gameplay remains authoritative
until its named cutover is implemented and accepted.

## Extracted material flow

WV-B3C.3B does not change Construction authority. Whole-node Extraction creates
canonical loose Soil/Stone stacks only; separate Hauling may deliver them to the
site. Construction remains ineligible until every requirement is staged, and an
active Construction designation is not an Extraction overflow destination.
This integration is accepted as part of completed WV-B3C.3B; Construction
ownership and staging behavior did not change.

## Natural Background rule

Natural Backgrounds are environmental only. Nursery, Sleeping, Food/Water/
Material Storage, Workshop, Refining, Doors and other functions are constructed
Placements over the retained Background. Current functional Background IDs are
legacy compatibility pending WV-B3D migration; they are not the approved target
model. See [Resource and Node System](resource-and-node-system.md).

The complete audited catalogue, Placement Definition contract and migration
matrix are in
[`Documentation/ConstructionPlacementSpecification.md`](https://github.com/Stevr0/AntGame/blob/main/Documentation/ConstructionPlacementSpecification.md).
Existing recipes remain provisional: Prepared, Nursery, Sleeping, Food Storage
and Material Storage cost 1 Soil; Water Storage costs 1 Stone; all use four base
simulation hours. No Door, Refining Station or room-scoring runtime exists.

## WV-B3A Placement migration boundary

Construction's staged workflow remains unchanged, but its approved end-state
creates a Placement over the unchanged natural Background. Functional
Backgrounds migrate one function at a time with contents, reservations,
capacity, progress and fixtures preserved; the compatibility and replacement
function can never both be active. Material Storage is the recommended first
function for a separately approved WV-B3D slice. See [Unified Runtime
Cells](unified-runtime-cells.md).

## Construction staging correction

Under WV-B2B-ARCH-001, builders do not self-supply. Each Construction Site owns
its required, reserved and delivered resource quantities, including multiple
recipe types. Hauling delivers physical resources to that site; only then does
Construction become eligible and consume staging once on successful completion.
Cancelling returns staged material to recoverable loose output. Material Storage
is an alternative destination, never a prerequisite for first-Storage building.

## Approved placement model amendment

**Design approved; migration not implemented.** Sleeping, Food/Water/Material
Storage, Nursery, doors and future specialised functions are
Constructions/Placements over an unchanged natural Background. A cell normally
requires no node, no existing placement and no incompatible loose stack.
Construction still consumes physical staged materials exactly once and awards
Construction skill through work; Hauling owns supply transport.

One Storage placement owns one functional slot/capacity unit where the 1:1 rule
applies. That slot contains zero or one resource type up to its capacity; stored
content is not the cell's independent ground stack. The current functional-
Background implementation below remains compatibility behavior for existing
fixtures until an authorised migration preserves contents, reservations,
progress and identities.

## Approved construction model

Construction is physical, location-based work. Milestone 3.4A implements conversion of exposed Background cells after required materials are gathered and consumed. Recipes and balance remain provisional.

Hauling alone gathers and delivers physical materials, potentially through
multiple trips. Construction performs work only after every site-owned
requirement is staged. Reservations prevent double allocation; cancellation
leaves unused staged or reserved material physical and recoverable.

## Functional Backgrounds (implemented compatibility representation)

Approved functions include:

- Nursery
- Sleeping
- Food Storage
- Water Storage
- Material Storage
- production functions still to be specified
- burial

Each legacy functional Background tile currently provides one declared unit of
base capacity. Material Storage uses one filtered slot; Sleeping uses one
exclusive reservation; Food/Water each use one resource slot; Nursery declares
one slot but has no brood runtime. This behavior migrates to completed
Placements. Doors, Prepared Surface and Refining Stations do not inherit a
meaningless generic capacity.

Milestone 3.4A can construct Prepared, Nursery, Sleeping, Food Storage, Water Storage and Material Storage Background. Functional results expose one empty capacity unit; Nursery is capacity-only because brood remains excluded. Function, presentation and navigation refresh from the authoritative Background transaction.

## Implemented builder workflow

The selected conversion becomes a Construction job in the shared registry. Its reserved builder uses compatible carried material first, otherwise reserves the closest reachable compatible Material Storage or loose drop, collects into personal inventory, stages the material at the site and repeats if necessary. Timed work uses Construction skill and Rested modifiers. Completion consumes staged material exactly once and changes only the Background. Interruption preserves progress and physical ownership; designation cancellation exposes staged material as a recoverable loose drop.

If the builder lacks inventory capacity, it retains material needed by this construction job and unloads unnecessary stacks. It first reserves the closest reachable compatible Material Storage and travels there before depositing only enough to free capacity. With no reachable capacity, it creates a validated recoverable drop at its current anchor. The same construction order then resumes. Bounded failure and interruption release only pending reservations; they do not erase cargo, staged material, progress or job ownership.

Loose drops remain traversable and cannot by themselves prevent ordinary progression. Future clutter concentration may slow movement and loose resources may reduce cleanliness or quality in enclosed rooms, while stored material is excluded. Those thresholds and penalties are provisional and are not implemented in 3.4A.

Current provisional recipes cost one Soil for Prepared, Nursery, Sleeping, Food Storage or Material Storage, and one Stone for Water Storage. Base duration is four simulation hours.

Milestone 3.5A previews the selected recipe over a bounded hover/drag rectangle by calling the construction registry's non-mutating authoritative validation. Valid cells are green, invalid/occupied cells red and existing conversions muted with a separate symbol. Right-click in Background Conversion mode invokes the existing cancellation transaction for only that cell, so staged material still becomes a recoverable loose drop and no movement order leaks through.

## Rooms and doors

The approved design allows mixed-use spaces. Enclosure is not required for base tile function, but an enclosed specialised room may earn quality or efficiency bonuses. Every opening into an enclosed room requires a door. Exact enclosure detection, door rules, scoring, mixed-use penalties, and quality formulas remain unresolved.

## Status

| Capability | Status |
| --- | --- |
| Functional Background data seam | Implemented for selected development functions |
| Authored Food/Water/Sleeping fixtures | Implemented and accepted |
| Physical construction reservations and staging | Implemented in 3.4A |
| Material Storage | Implemented and accepted in 3.3D |
| Destination staging and Construction work | Implemented — manual acceptance pending |
| Conversion progress and cancellation recovery | Implemented — manual acceptance pending |
| Room enclosure, doors, and quality | Planned, not implemented |

<div class="screenshot-slot">
Future screenshot: functional Background overlays, room boundary, door openings, and quality diagnostics.
</div>

See [Excavation and Terrain](excavation-terrain.md) for layer semantics and [Ant AI and Job Management](ant-ai-job-management.md) for reservation and interruption rules.
