# Brood and Reproduction

## WV-B3D.2 Nursery projection

Completed legacy Nursery Backgrounds now receive deterministic one-cell
Placement projections. The registry does not activate or duplicate Nursery
capacity, invent HP, or add brood state. Reproduction, laying, care, room
quality, damage and Repair remain unimplemented.

## WV-B3D.1 Nursery boundary

Nursery currently has a functional Background definition, Construction recipe
and declared capacity one, but no brood runtime. Its later Placement migration
must preserve that data capacity without inventing eggs, larvae, feeding or
reassignment. An unenclosed Nursery retains base use; future room quality may
modify efficiency, never base capacity. See
[Placement Migration](placement-migration.md).

Brood and reproduction are approved design, not implemented gameplay.

## Founding sequence

The intended first controllable ant is a working founding queen. The early colony sequence is:

1. Excavate an accessible chamber.
2. Construct at least three Nursery Background tiles.
3. Explicitly authorise an initial brood of three larvae.
4. Physically feed the larvae on multiple occasions.
5. Allow surviving brood to mature into three individually controllable general-purpose workers.

This supersedes older plans in which an ordinary player ant later transformed into a queen.

## Approved rules

- Reproduction always requires explicit player authorisation; spare capacity alone cannot trigger it.
- Each functional Nursery Background tile supplies exactly one brood slot.
- Nursery function can operate without an enclosing room.
- Proper enclosure may grant room-quality efficiency, but not extra base capacity.
- Removing occupied nursery capacity pauses development rather than silently deleting brood.
- Brood Care is one of the seven universal learn-by-doing skills.
- Individual ant death is permanent once death gameplay exists.

## Open decisions

- Exact egg-to-larva-to-pupa-to-adult presentation and timing.
- Feeding thresholds, resource types, and failure consequences.
- Brood mortality and reassignment when capacity is removed.
- Later brood sizes and queen work/reproduction trade-offs.
- Colony behavior after queen death.

## Dependencies

Brood implementation depends on accepted excavation, resource-backed construction, Nursery conversion, physical food logistics, autonomous jobs, lifecycle transitions, and persistence. Those dependencies are not yet complete, so brood must not be introduced as an isolated shortcut.

See [Colony Construction and Rooms](colony-construction-rooms.md), [Food, Water and Survival](food-water-survival.md), and [Ant AI and Job Management](ant-ai-job-management.md).
