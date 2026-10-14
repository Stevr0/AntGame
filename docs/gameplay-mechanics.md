# Gameplay Mechanics

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

The accepted 3.4C player HUD provides roster selection, shared ant inspection, the global simulation clock, physical-resource ownership, readable job state and Colony View Select/Excavate/Build/Cancel tools without opening Developer Mode. WV-B1 adds a small development view/entrance strip, hides Colony commands in World View and provides Surface selection/right-click movement. It does not add Surface resources, cross-view jobs, alerts or Draft Mode.

## Physical-world principles

- Terrain, resources, capacity, and work should exist at world locations rather than as unexplained global counters.
- Extraction always produces loose physical output; pickup and transport are separate Hauling actions.
- Ants must navigate to a valid adjacent interaction position before acting.
- The constructing ant is responsible for gathering and delivering its own materials, including multiple trips.
- Valid partial work survives safe interruptions.
- Reservations prevent duplicate consumption and must be released on cancellation or failure.

The current systems prove these principles through location-owned food/water, sleeping reservations, real excavation, per-ant inventory, reserved loose-drop pickup and cell-owned Material Storage. Material remains in exactly one authoritative inventory, world drop or storage stack; no global resource wallet exists.

Future natural sources preserve the same conservation rule. Direct consumption reserves and decrements a source exactly once without creating cargo. Extraction creates recoverable physical output before source quantity commits. Water uses a stable one-unit liquid payload/cache rather than an ordinary Soil/Stone drop. See [World View Foundation](world-view-foundation.md).

## Progression and failure

Skills improve through successful work. Hunger, thirst, fatigue, and health create survival pressure. Individual ant death is intended to be permanent, but actual death and corpse gameplay are not implemented. The colony outcome after queen death remains unresolved.

## Related pages

- [Ant AI and Job Management](ant-ai-job-management.md)
- [Colony Construction and Rooms](colony-construction-rooms.md)
- [Food, Water and Survival](food-water-survival.md)
