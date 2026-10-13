# Food, Water and Survival

## Accepted needs foundation

Milestone 3.2A is complete and manually accepted. Every authoritative ant record owns Health, Hunger, Thirst, Fatigue, and Rested qualification. State persists through selection changes and visual rebinding, and a deterministic development-world rebuild resets it.

### Provisional values

| Rule | Development value |
| --- | ---: |
| Starting Health / Hunger / Thirst | 100 / 100 / 100 |
| Starting Fatigue | 20 |
| Awake Hunger loss | 2 per simulation hour |
| Awake Thirst loss | 3 per simulation hour |
| Awake Fatigue gain | 4 per simulation hour |
| Rest recovery | 12 Fatigue per simulation hour |
| Critical Hunger threshold | 10 or lower |
| Critical Hunger damage | 2 Health per simulation hour |
| Critical Thirst effect | Blocks ordinary skill XP |
| Rested qualification | Completed rest at Fatigue 20 or lower |
| Maximum Rested work speed | 1.15x |
| Maximum Rested skill XP | 1.25x |
| Rested fade | Returns to 1.0x by Fatigue 80 |

The work multiplier is exposed for future jobs and does not alter navigation speed. Zero Health pauses ordinary survival advancement but does not yet create death or corpse gameplay.

## Physical interactions and autonomy

Milestone 3.2B physical Eat, Drink, and Rest interactions and Milestone 3.2C autonomous survival are complete:

- Food and water quantities belong to specific functional Background cells.
- Resource and sleeping reservations belong to stable ant identities.
- The ant chooses the lowest-cost reachable adjacent anchor through the shared grid pathfinder.
- Arrival revalidates location and function before interaction.
- Cancellation, replacement, failure, inactive/deceased state, and world reset release reservations.
- Changing selection does not cancel another ant's valid order.
- Autonomous ants seek Hunger/Thirst at 50 or below and Rest at Fatigue 60 or above.
- Critical Thirst, Hunger, and Fatigue use deterministic precedence and may safely interrupt work/player orders only after a viable physical target is confirmed.
- Ordinary needs use normalized urgency and hysteresis, and do not interrupt active colony work.
- Failed searches use bounded retry backoff rather than per-frame path requests.

Eat reserves and consumes one `resource.food.development` unit after one simulation hour and restores 40 Hunger. Drink consumes one `resource.water.development` unit after one simulation hour and restores 50 Thirst. Rest recovers Fatigue over time and completes at the accepted Rested threshold. Interaction time advances at one simulation hour per real-time second in the development scene.

### Validation status

- Focused Edit Mode: 25/25 passed.
- Focused Play Mode: 5/5 passed.
- Complete graphics-enabled Edit Mode: 340/340 passed.
- Complete Play Mode: 26/26 passed.
- Unity compilation and structural scene validation passed.
- The 3.2B and 3.2C manual checklists are accepted through their recorded milestone transitions.

## Not implemented

Gathering, hauling, general inventories, cooking, food varieties, spoilage, water production, containers, permanent beds, colony-wide stock accounting, death, corpses, and save/load remain outside the current slice. Milestone 3.3B produces only loose Soil/Stone world drops; it does not produce or transport survival supplies.

See [Development Roadmap](development-roadmap.md) before extending this system.
