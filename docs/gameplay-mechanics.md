# Gameplay Mechanics

## Status map

| System | Design | Implementation | Validation |
| --- | --- | --- | --- |
| Ant selection and individual move orders | Approved | Implemented | Accepted in Play Mode |
| Identity and attributes | Approved | Implemented | Accepted in Play Mode |
| Learn-by-doing skills | Approved with provisional balance | Implemented foundation | Accepted through 3.2A transition |
| Needs and Rested state | Approved with provisional balance | Implemented foundation | Accepted in Play Mode |
| Physical Eat, Drink, and Rest | Approved vertical slice | Implemented | Automated passed; manual pending |
| Two-layer terrain and Tilemap projection | Approved | Implemented | Accepted milestones |
| Grid navigation | Approved foundation | Production integration implemented | 2.4D manual acceptance remains pending |
| Excavation gameplay and soil logistics | Approved direction | Not implemented | Not tested |
| Construction and functional conversion | Approved direction | Only authored development fixtures | Gameplay not tested |
| Colony priorities and autonomous jobs | Approved design | Not implemented | Not tested |
| Brood and reproduction | Approved design | Not implemented | Not tested |
| Rooms, doors, and quality bonuses | Approved design | Not implemented | Not tested |
| Combat, equipment, and enemies | Partial design | Not implemented | Not tested |
| Save/load and chunk streaming | Required future architecture | Not implemented | Not tested |

## Control model

The approved player-facing model has three states:

- **Autonomous:** an ant selects work and survival behavior from allowed priorities when those systems exist.
- **Temporary explicit order:** the player requests a specific task. Completion, cancellation, or invalidation returns the ant to autonomy.
- **Draft Mode:** a future persistent tactical state for move, attack, hold, defend-area, retreat, and group control.

Current implementation supports selection plus temporary Move, Eat, Drink, and Rest orders. It does not yet implement the general autonomous job system or Draft Mode. The old WASD `PlayerMovementInput` path is retained strictly as a developer movement regression tool.

## Physical-world principles

- Terrain, resources, capacity, and work should exist at world locations rather than as unexplained global counters.
- Ants must navigate to a valid adjacent interaction position before acting.
- The constructing ant is responsible for gathering and delivering its own materials, including multiple trips.
- Valid partial work survives safe interruptions.
- Reservations prevent duplicate consumption and must be released on cancellation or failure.

The current 3.2B interaction slice proves those principles only for location-owned food, water, and sleeping reservations. It is not a generic inventory or job framework.

## Progression and failure

Skills improve through successful work. Hunger, thirst, fatigue, and health create survival pressure. Individual ant death is intended to be permanent, but actual death and corpse gameplay are not implemented. The colony outcome after queen death remains unresolved.

## Related pages

- [Ant AI and Job Management](ant-ai-job-management.md)
- [Colony Construction and Rooms](colony-construction-rooms.md)
- [Food, Water and Survival](food-water-survival.md)
