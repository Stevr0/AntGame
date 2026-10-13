# Ants, Attributes and Skills

## Implemented identity model

Each ant is represented by one authoritative plain-C# `AntRecord`. Stable `AntId` and `ColonyId` values, caste, attributes, lifecycle, skills, needs, work priorities and personal inventory belong to the record rather than to the visual GameObject. `AntEntityBinding` connects a runtime visual to an existing record, while a visual-claim registry prevents duplicate live representations.

The deterministic development ants are:

| Name | Caste | Strength | Agility | Endurance |
| --- | --- | ---: | ---: | ---: |
| Mara | Queen | 7 | 5 | 8 |
| Pip | Worker | 4 | 8 | 5 |
| Tala | Worker | 6 | 6 | 7 |

This identity and attribute foundation is complete and manually accepted. Visual destruction does not delete the record, and deceased records reject movement orders. Save/load and actual death gameplay are not implemented.

## Implemented skill foundation

Queen and Worker records share seven ordinary skills:

- Excavation
- Surface Preparation
- Construction
- Hauling
- Foraging
- Combat
- Brood Care

All begin at level 0 and can reach level 100. A successful future activity can call the explicit learn-by-doing API; starting or cancelling work must not award experience.

### Provisional progression

- XP needed from level `L` to `L+1`: `100 + 5L`.
- Cumulative XP at level `L`: `100L + 2.5L(L-1)`.
- Level 100 caps at 34,750 total XP.
- After a provisional 24-hour inactivity grace period, skill value above 50 decays by 0.25 levels per inactive hour and stops at 50.
- Needs and Rested modifiers can multiply a valid XP award without changing stored base progression rules.

These values are development placeholders. The foundation is implemented and automated-tested; its manual acceptance was recorded through the approved transition to milestone 3.2A.

## Implemented personal inventory

Each Queen or Worker record owns an independent stack-based inventory. Natural slot capacity is based only on current Hauling level: one slot at 0–19, two at 20–39, three at 40–59, four at 60–79 and five at 80–100. Soil and Stone provisionally stack to 10. Compatible additions merge before opening another slot; incompatible resources never share a slot.

If Hauling decay lowers capacity, occupied overflow remains intact and recoverable. New additions are blocked until occupied usage is within the reduced capacity. Inventory survives visual rebinding and selection changes. Equipment slots, weight, saddlebags and final balance are not implemented.

## Planned individuality

The approved design expects equipment, permanent death, work efficiency from skills, and persistent individual histories. Roles will affect autonomous priorities rather than lock ants out of normal skills. Exact attribute formulas, equipment effects, ageing, and save serialization remain unresolved or unimplemented.

## Boundaries

- Skills never silently outrank player-assigned work priorities.
- The current work-speed multiplier is exposed for future jobs and does not alter navigation speed.
- The temporary ordered-work productivity bonus is approved in principle, but its value and implementation remain TBD.
- A working founding queen is the intended start; older ordinary-ant-to-queen progression language is superseded.

See [Food, Water and Survival](food-water-survival.md) for needs modifiers and [Ant AI and Job Management](ant-ai-job-management.md) for future priority selection.
