# Colony Construction and Rooms

## Approved construction model

Construction is physical, location-based work. Milestone 3.4A implements conversion of exposed Background cells after required materials are gathered and consumed. Recipes and balance remain provisional.

The ant performing construction must personally gather and deliver its own materials, including multiple trips. A separate hauler must not silently complete the builder's supply responsibility. Reservations prevent double allocation; cancelling construction leaves unused delivered or reserved materials physical and recoverable.

## Functional Backgrounds

Approved functions include:

- Nursery
- Sleeping
- Food Storage
- Water Storage
- Material Storage
- production functions still to be specified
- burial

Each functional Background tile provides one unit of base capacity. Material Storage uses one tile per inventory slot and may later support optional filters. Function and locomotion remain separate properties: a functional tile can still be traversable and grippable.

Milestone 3.4A can construct Prepared, Nursery, Sleeping, Food Storage, Water Storage and Material Storage Background. Functional results expose one empty capacity unit; Nursery is capacity-only because brood remains excluded. Function, presentation and navigation refresh from the authoritative Background transaction.

## Implemented builder workflow

The selected conversion becomes a Construction job in the shared registry. Its reserved builder uses compatible carried material first, otherwise reserves the closest reachable compatible Material Storage or loose drop, collects into personal inventory, stages the material at the site and repeats if necessary. Timed work uses Construction skill and Rested modifiers. Completion consumes staged material exactly once and changes only the Background. Interruption preserves progress and physical ownership; designation cancellation exposes staged material as a recoverable loose drop.

If the builder lacks inventory capacity, it retains material needed by this construction job and unloads unnecessary stacks. It first reserves the closest reachable compatible Material Storage and travels there before depositing only enough to free capacity. With no reachable capacity, it creates a validated recoverable drop at its current anchor. The same construction order then resumes. Bounded failure and interruption release only pending reservations; they do not erase cargo, staged material, progress or job ownership.

Loose drops remain traversable and cannot by themselves prevent ordinary progression. Future clutter concentration may slow movement and loose resources may reduce cleanliness or quality in enclosed rooms, while stored material is excluded. Those thresholds and penalties are provisional and are not implemented in 3.4A.

Current provisional recipes cost one Soil for Prepared, Nursery, Sleeping, Food Storage or Material Storage, and one Stone for Water Storage. Base duration is four simulation hours.

## Rooms and doors

The approved design allows mixed-use spaces. Enclosure is not required for base tile function, but an enclosed specialised room may earn quality or efficiency bonuses. Every opening into an enclosed room requires a door. Exact enclosure detection, door rules, scoring, mixed-use penalties, and quality formulas remain unresolved.

## Status

| Capability | Status |
| --- | --- |
| Functional Background data seam | Implemented for selected development functions |
| Authored Food/Water/Sleeping fixtures | Implemented and accepted |
| Physical construction reservations and staging | Implemented in 3.4A |
| Material Storage | Implemented and accepted in 3.3D |
| Construction jobs and builder self-supply | Implemented — manual acceptance pending |
| Conversion progress and cancellation recovery | Implemented — manual acceptance pending |
| Room enclosure, doors, and quality | Planned, not implemented |

<div class="screenshot-slot">
Future screenshot: functional Background overlays, room boundary, door openings, and quality diagnostics.
</div>

See [Excavation and Terrain](excavation-terrain.md) for layer semantics and [Ant AI and Job Management](ant-ai-job-management.md) for reservation and interruption rules.
