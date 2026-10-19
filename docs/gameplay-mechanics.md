# Gameplay Mechanics

## Whole-node Colony Extraction

One completed Soil or Stone Extraction job removes the entire Node and creates
recoverable physical stacks totaling its remaining quantity. Stacks hold at most
10; overflow uses deterministic nearby reachable cells. Extraction never fills
inventory, Storage or site staging. Hauling moves the result, and Construction
works only after delivery. A blocked full-output plan leaves the Node intact.
WV-B3C.3B is complete after passing automated evidence and user-confirmed
**8/8 authoritative manual acceptance groups**.

## Resource and Node System v1.0

The approved target makes every Solid a finite variable-quantity Resource Node.
Normal Solid Extraction commits its complete remaining quantity as physical
output while preserving the natural Background. Water is finite. Natural
Backgrounds provide no colony function; constructed Placements do. Full details
and the runtime compatibility catalogue are in
[Resource and Node System](resource-and-node-system.md).

Completed improvements follow the companion
[`Construction and Placement Specification`](https://github.com/Stevr0/AntGame/blob/main/Documentation/ConstructionPlacementSpecification.md):
Designation → Hauling → site-owned staging → Construction work → completed
Placement. No Storage, Sleeping, Nursery or other function exists before
completion, and completion does not replace the natural Background.

## WV-B3B compatibility boundary

The runtime-cell foundation is observational. Extraction, loose resources,
Hauling, Storage, Construction Site staging, ant inventory, job priorities,
residency and the shared clock retain their existing authorities and behavior.
Storage contents remain Placement-owned and Construction staging remains
site-owned; neither is projected as an ordinary ground stack. Pure compatibility
checks prepare future migrations but do not reject current gameplay actions.

## WV-B3A runtime-cell architecture

The shared gameplay model is one natural Background plus optional Node,
Placement and one-type loose stack per resident cell. Current Colony runtime
representations remain compatibility implementations; the approved migration is
documented in [Unified Runtime Cells](unified-runtime-cells.md). No Foraging,
Fog of War, Refining, persistence or WV-C1 behavior was implemented.

## WV-B2B-ARCH-001 implemented correction

Extraction replaces Excavation as the official skill and activity for removing
all resource nodes. Colony Soil/Stone work is implemented; generated Seed/Twig
records declare the same future contract, but Surface interaction remains
unimplemented. Extraction produces loose ground resources only. Hauling moves
them to explicit Construction staging or Storage. Construction starts only when
all staging is complete and performs no pickup or transport.

## Unified resources and placements

**Approved design; not fully implemented.** Extraction and excavation act on the same
finite Solid/Resource Node model. Successful work reduces quantity exactly once,
removes a depleted node while retaining natural Background, and commits complete
physical output exactly once, using deterministic additional stacks or pending
recovery if necessary. It never auto-fills inventory. Direct
eating/drinking is a separate source-consumption transaction.

Each cell permits at most one loose resource type/stack. Storage is a
Construction-owned slot, not a ground stack or natural Background; one placement
provides one capacity unit/slot where the 1:1 rule applies. Destination-aware
Hauling, material staging, interruption recovery and Construction skill
progression remain authoritative behavior to preserve during migration.

## Generated founding-colony start

WV-B2A approves the future new-game layout: one founding queen starts in one
small excavated underground chamber connected to the approximately central
top-boundary Colony exit. The linked `64×64` Surface is predominantly Grass and
guarantees reachable finite food and freshwater nodes near its central
entrance. No storage, sleeping facility, nursery or other completed
infrastructure exists initially. This is approved design, not implemented
gameplay; source consumption, extraction, hauling and foraging remain later
scopes. See [Procedural World Generation](procedural-world-generation.md).

## Status map

| System | Design | Implementation | Validation |
| --- | --- | --- | --- |
| Ant selection and individual move orders | Approved | Implemented | Accepted in Play Mode |
| Identity and attributes | Approved | Implemented | Accepted in Play Mode |
| Learn-by-doing skills | Approved with provisional balance | Implemented foundation | Accepted through 3.2A transition |
| Needs and Rested state | Approved with provisional balance | Implemented foundation | Accepted in Play Mode |
| Physical Eat, Drink, and Rest | Approved vertical slice | Implemented | Accepted in Play Mode |
| Two-layer terrain and Tilemap projection | Approved | Implemented | Accepted milestones |
| Grid navigation | Approved foundation | Production integration implemented | 2.4D manual acceptance remains pending |
| Excavation gameplay | Approved loose-output rule | 3.5B implemented | Automated passed; revised manual acceptance pending |
| Personal inventory and explicit material pickup/drop | Approved slice | Complete | Automated and manual accepted |
| Material Storage and autonomous Soil/Stone hauling | Approved slice | Complete | Automated and manual accepted |
| Construction and functional conversion | Approved slice with provisional recipes | Complete | Automated and manual accepted |
| Colony View player HUD | Approved 3.4C slice | Complete | Automated and manual accepted |
| Colony work-priority matrix | Approved 3.4D slice | Implemented | Automated passed; manual pending |
| Two-view region/entrance contract | WV-B1 | Implemented | 8/8 focused Edit and 2/2 focused Play; manual pending |
| Seeded two-region generation contract | WV-B2A | Documentation complete | Runtime not started; WV-B2B awaits approval |
| Unified cell/node/placement/loose contract | WV-B2A amendment | Documentation complete | Migration not started |
| World View resources and autonomous cross-view work | Later WV-C/WV-D | Not implemented | Not tested |
| Colony priorities and autonomous jobs | Approved design | Implemented foundation | Accepted in Play Mode |
| Brood and reproduction | Approved design | Not implemented | Not tested |
| Rooms, doors, and quality bonuses | Approved design | Not implemented | Not tested |
| Combat, equipment, and enemies | Partial design | Not implemented | Not tested |
| Save/load and chunk streaming | Required future architecture | Not implemented | Not tested |

## Control model

The approved player-facing model has three states:

- **Autonomous:** an ant selects work and survival behavior from allowed priorities when those systems exist.
- **Temporary explicit order:** the player requests a specific task. Completion, cancellation, or invalidation returns the ant to autonomy.
- **Draft Mode:** a future persistent tactical state for move, attack, hold, defend-area, retreat, and group control.

Current implementation supports selection plus temporary Move, Eat, Drink, Rest and material Pickup orders, along with autonomous survival, colony work selection and autonomous hauling. It does not yet implement explicit forced-work orders or Draft Mode. The old WASD `PlayerMovementInput` path is retained strictly as a developer movement regression tool.

The accepted player HUD provides roster selection, shared ant inspection, the global simulation clock, physical-resource ownership, readable job state and Colony View Select/Extract/Build/Cancel tools without opening Developer Mode. WV-B1 adds a small development view/entrance strip, hides Colony commands in World View and provides Surface selection/right-click movement. It does not add Surface resources, cross-view jobs, alerts or Draft Mode.

## Physical-world principles

- Terrain, resources, capacity, and work should exist at world locations rather than as unexplained global counters.
- Extraction always produces loose physical output; pickup and transport are separate Hauling actions.
- Ants must navigate to a valid adjacent interaction position before acting.
- Hauling alone gathers and delivers material; Construction starts after complete staging.
- Valid partial work survives safe interruptions.
- Reservations prevent duplicate consumption and must be released on cancellation or failure.

The current systems prove these principles through location-owned food/water, sleeping reservations, real excavation, per-ant inventory, reserved loose-drop pickup and cell-owned Material Storage. Material remains in exactly one authoritative inventory, world drop or storage stack; no global resource wallet exists.

Future natural sources preserve the same conservation rule. Direct consumption
and Extraction must both reserve and decrement a finite source exactly once.
Water requires a liquid-compatible cache/carrier rather than an ordinary
Soil/Stone stack; exact capacity and direct-drinking behavior for Solid Water
remain unresolved. See [World View Foundation](world-view-foundation.md).

## Progression and failure

Skills improve through successful work. Hunger, thirst, fatigue, and health create survival pressure. Individual ant death is intended to be permanent, but actual death and corpse gameplay are not implemented. The colony outcome after queen death remains unresolved.

## Related pages

- [Ant AI and Job Management](ant-ai-job-management.md)
- [Colony Construction and Rooms](colony-construction-rooms.md)
- [Food, Water and Survival](food-water-survival.md)
