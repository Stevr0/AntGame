# Food, Water and Survival

## Unified node and storage amendment

Future finite food/freshwater availability uses the shared Resource Node model
on a required natural Background. Extraction creates conserved physical output;
direct eating/drinking is a separate exact source-consumption transaction.
Food/Water Storage is a Construction-owned slot, not a natural Background or
ground stack. These are approved target contracts, not implemented WV-B2B/WV-C1
behavior. Existing functional-Background survival fixtures remain unchanged
through a compatibility adapter until migration is authorised.

## Accepted needs foundation

Milestone 3.2A is complete and manually accepted. Every authoritative ant record owns Health, Hunger, Thirst, Fatigue, and Rested qualification. State persists through selection changes and visual rebinding, and a deterministic development-world rebuild resets it.

### Provisional values

| Rule | Development value |
| --- | ---: |
| Starting Health / Hunger / Thirst | 100 / 100 / 100 |
| Starting Fatigue | 20 |
| Awake Hunger loss | 1 per simulation hour |
| Awake Thirst loss | 1.4 per simulation hour |
| Awake Fatigue gain | 1.4 per simulation hour |
| Rest recovery | 16 Fatigue per simulation hour |
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

Eat reserves and consumes one `resource.food.development` unit after 0.5 simulation hour and restores 40 Hunger. Drink consumes one `resource.water.development` unit after 0.5 simulation hour and restores 50 Thirst. Rest recovers Fatigue over time and completes at the accepted Rested threshold. Interaction time advances at one simulation hour per real-time second in the development scene.

Milestone 3.4B routes needs, interactions, autonomous evaluation and retry/backoff through the authoritative simulation clock. Pause advances none of them and preserves any existing survival reservation. Running 1×/2×/3× changes simulated time per real second without changing the provisional per-simulation-hour rates above.

### Validation status

- Focused Edit Mode: 25/25 passed.
- Focused Play Mode: 5/5 passed.
- Complete graphics-enabled Edit Mode: 340/340 passed.
- Complete Play Mode: 26/26 passed.
- Unity compilation and structural scene validation passed.
- The 3.2B and 3.2C manual checklists are accepted through their recorded milestone transitions.

## Future founding-colony contract

The WV-A/WV-B2A specifications add design only, not runtime behavior: finite
Surface food/freshwater nodes, direct eating/drinking without storage,
extraction to recoverable output, one-unit non-solid water payloads, and safe
natural rest. A constructed Sleeping Placement keeps one-ant capacity and the
existing 16/hour recovery; a provisional 8/hour natural-rest rate requires
WV-C1 approval. Water must remain conserved across node/source, ground cache,
ant inventory and Water Storage placement ownership.

## Not implemented

Natural source discovery, seed/water extraction, food parcels, water caches, survival-supply hauling, cooking, food varieties, spoilage, water production, containers, death, corpses, and save/load remain outside the current slice. General ant inventory, Soil/Stone hauling, Material Storage and constructed survival fixtures do exist; they do not yet transport natural survival supplies.

See [World View Foundation](world-view-foundation.md) and [Development Roadmap](development-roadmap.md) before extending this system.
