# Colony Construction and Rooms

## Approved construction model

Construction is physical, location-based work. Functional colony tiles are created by converting exposed Background cells after required materials are gathered and consumed. Recipes and balance remain unapproved.

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

Milestone 3.2B authors Food Storage, Water Storage, and Sleeping cells as deterministic development fixtures. They replace the function/material of existing Background cells, remain traversable at `1.0x` speed, and expose exactly one slot each. This validates direct interactions, not general construction, conversion, or room gameplay.

## Rooms and doors

The approved design allows mixed-use spaces. Enclosure is not required for base tile function, but an enclosed specialised room may earn quality or efficiency bonuses. Every opening into an enclosed room requires a door. Exact enclosure detection, door rules, scoring, mixed-use penalties, and quality formulas remain unresolved.

## Status

| Capability | Status |
| --- | --- |
| Functional Background data seam | Implemented for selected development functions |
| Authored Food/Water/Sleeping fixtures | Implemented; 3.2B manual acceptance pending |
| Physical resource reservation seam | Implemented narrowly for Eat/Drink |
| Material Storage | Approved design only |
| Construction jobs and builder self-supply | Approved design only |
| Conversion progress and cancellation recovery | Planned, not implemented |
| Room enclosure, doors, and quality | Planned, not implemented |

<div class="screenshot-slot">
Future screenshot: functional Background overlays, room boundary, door openings, and quality diagnostics.
</div>

See [Excavation and Terrain](excavation-terrain.md) for layer semantics and [Ant AI and Job Management](ant-ai-job-management.md) for reservation and interruption rules.
