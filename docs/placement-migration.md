# Placement Migration

## WV-B3D.2 status

**Implemented — Automated Tests Passed, Manual Acceptance Pending.** A
scenario-scoped registry now publishes the six existing recipe-backed
definitions and deterministic one-cell legacy projections. It owns only
identity, footprint occupancy, lifecycle, revision and HP-ready structural
state. Existing movement, Storage, Sleeping, Nursery, Construction, Hauling and
Extraction owners remain unchanged.

Legacy projections report unknown current/max HP and condition, no
Placement-owned contents and no active Placement capacity. Future authoritative
instances require approved positive Max HP, start full and clamp mutations to
`0..Max`; final HP values and all damage, Repair and destruction gameplay remain
unapproved. Focused Edit/Play passed **11/11** and **3/3**; complete
graphics-enabled suites passed **608/608** and **84/84**. Manual acceptance is
pending.

## WV-B3D.1 status

**Documentation complete.** The authoritative
technical contract is
[`Documentation/PlacementMigrationTechnicalSpecification.md`](https://github.com/Stevr0/AntGame/blob/main/Documentation/PlacementMigrationTechnicalSpecification.md).
It extends the approved
[`ConstructionPlacementSpecification.md`](https://github.com/Stevr0/AntGame/blob/main/Documentation/ConstructionPlacementSpecification.md)
without changing existing gameplay.

## Current implementation

Construction currently converts one Background material into another.
`ConstructionSite` owns staging, Hauling supplies it, and Construction begins
only after every requirement is delivered. On completion,
`ConstructionWorldTransaction` consumes staging and registers the legacy
Background function.

Current function owners are:

| Function | Current owner | Important state |
| --- | --- | --- |
| Prepared Surface | Background plus movement evaluators | `1.25x` speed |
| Sleeping | `SurvivalInteractionWorld` | one exclusive reservation |
| Food/Water Storage | `SurvivalInteractionWorld` | resource, quantity and withdrawal reservations |
| Material Storage | `MaterialStorageWorld` | filter, resource, quantity, capacity 10, incoming/withdrawal reservations |
| Nursery | Background definition only | declared capacity 1; no brood runtime |

Door, Refining, room scoring and dismantling are not implemented.

## Target authority

Every resident cell retains exactly one natural Background and may also contain
one Node, one Construction/Placement and one loose stack. One scenario-local
Placement registry owns completed Placement identity, anchor/footprint,
lifecycle and revision. Function ledgers are keyed by Placement ID; they do not
duplicate mutable quantities in the registry and a legacy cell dictionary.

A reusable Placement Definition owns immutable metadata: stable ID/version,
display/category, supported regions, footprint/orientation, compatibility,
recipe/work, skill, obstruction/traversal, interaction offsets, function and
capacity, contents policy, room modifiers, removal rules and presentation.

An instance owns stable Placement ID, definition, typed anchor, complete
footprint, lifecycle, revision, function state and reservations/users. Initial
runtime support is one cell, but one identity must continue to own all future
multi-cell footprint cells.

## Occupancy and Construction

Standard Construction requires a resident compatible Background, no obstructing
Node, no existing Placement, no incompatible loose stack, no conflicting site,
and a reachable interaction position. Unknown legacy detail fails closed.

The workflow remains:

**Designation → Hauling → Material Staging → Construction Work → Completed Placement**

Completion revalidates the complete footprint, prepares one Placement, consumes
staging once, retires the site, activates function once and publishes Placement
changes. The natural Background never changes. Cancellation retains staged
quantity until complete physical recovery output can be prepared.

## Migration order

1. **WV-B3D.2:** one-cell definitions, registry, footprint index, revisions,
   snapshots, compatibility and read-only legacy projections.
2. **WV-B3D.3A:** migrate existing Material Storage authority/state behind a
   narrow adapter.
3. **WV-B3D.3B:** make Material Storage Construction create a Placement.
4. **WV-B3D.4A/B:** Food, then Water after liquid representation approval.
5. **WV-B3D.5A/B:** Sleeping, then Nursery data capacity.
6. **WV-B3D.6:** Prepared Surface after representation approval.
7. **WV-B3D.7:** remove legacy behavior only after proof.

Material Storage preserves filters, contents, provisional capacity 10, incoming
and withdrawal reservations, transfer IDs, partial/multi-source Hauling,
Storage-to-Construction withdrawal, badges and first-Storage bootstrap.

Prepared Surface is recommended as an exclusive non-obstructing Placement that
modifies travel cost while the natural Background retains grip and movement
directions. That choice requires explicit approval because it prevents another
Placement sharing the same cell.

## Deferred systems

- Water Droplet/carrier rules and final Food/Water capacities.
- Door definition, states, access and navigation.
- Room detection, quality scores and bonuses.
- Dismantling work and refund quantities.
- Brood runtime and occupied-Nursery reassignment.
- Refining bills and input/output transactions.
- Surface Placements, persistence, streaming and unloaded simulation.

The Nest Entrance remains a specialised cross-region feature, not an ordinary
Door. `WV-B3B-LIM-001` remains accepted.

## Validation strategy

Later slices require identity/revision/occupancy tests, exact-once Construction,
conservation and reservation migration, no dual function, scenario retirement,
runtime-cell change publication and full regressions for WV-B1, WV-B2B, WV-B3B,
WV-B3C, Hauling, survival, Construction, navigation and scheduler fairness.
Each slice also receives a focused manual Unity gate. WV-B3D.1 ran documentation
validation only and provides no runtime test evidence.
