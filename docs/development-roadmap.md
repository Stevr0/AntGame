# Development Roadmap

## WV-B2B runtime gate

**Implemented — Pending Manual Acceptance (15 October 2026).** Versioned deterministic
generation, unified generated cells, structural/source validation, both region
adapters, generated entrance/chamber, founding queen, temporary presentation and
development controls are implemented. Generation version `2` /
`wv-b2b.default.002` guarantees one exposed Stone node at `(-4,-8)`. Autonomous
selection leaves unsupplied construction paused and unowned so other eligible
work can proceed, then reconsiders it when physical material becomes reachable.
Relevant regressions passed 58/58 Edit Mode and 5/5 Play Mode. Manual acceptance,
both `WV-B2B-DEF-001`/`WV-B2B-DEF-002`, and `WV-B1-REG-001` remain open. WV-C1
resource interaction must not begin without separate approval.

`WV-B2B-DEF-003 — Founding Colony Storage Bootstrap` is implemented with manual
acceptance pending. Hauling with no compatible destination no longer wins and
rejects after assignment; it remains paused/unowned and leaves loose Soil free
for Construction self-supply. Seed-`2203` coverage verifies the first Storage
can be built without pre-existing Storage and later enables ordinary hauling.
The focused Material Storage/Hauling suite passes 17/17 Edit Mode; broader
relevant regressions pass 75/75 Edit Mode and 5/5 Play Mode. All three WV-B2B
defect IDs and `WV-B1-REG-001` remain open.

## WV-B2A documentation gate

**Historical specification gate; implemented by WV-B2B above (15 October 2026).** One
versioned world seed will generate separate bounded Surface and underground
Colony data, a protected linked entrance, exactly one starting chamber, a valid
founding queen spawn and compatible finite source records. The specification
preserves the WV-B1 entrance/residency/rendering contracts and the existing
development fixture. See [Procedural World
Generation](procedural-world-generation.md).

The implemented WV-B2B slice is a pure `64×64` Surface plus
`128×128` Colony builder/validator and explicit generated-versus-development
scenario selector. It freezes the approved dimensions, chamber geometry, stable
generation-only source IDs, weights and versioned PRNG details. It excludes resource
interaction, AI, persistence, streaming and autonomous cross-view routing.

### Unified-cell amendment

WV-B2A now approves one logical cell model across both regions: required natural
Background plus optional node, placement and one-type loose stack. WV-B2B
generates Background/node slots only and preserves
separate region adapters. Shared node interaction and strict loose occupancy
precede WV-C1; legacy functional-Background migration precedes construction in
the generated scenario. Decay, streaming and full fixture migration remain
later work. `WV-B1-REG-001` remains open.

## WV-B1 runtime gate

**Implemented — Awaiting Validation (14 October 2026).** Typed residency, a deterministic `64×64` Surface, dedicated Surface navigation, manual remembered cameras and one capacity-one physical-approach entrance are running in the existing development scene. Focused verification passed 8/8 Edit and 2/2 Play; complete Play passed 69/69. Complete headless Edit regression was interrupted by a Unity rendered-pixel crash and is not counted as passed. Finish the manual checklist and broad graphics-enabled regression before accepting WV-B1; do not start WV-C/WV-D.

## WV-A documentation gate

**Complete as the approved documentation baseline (14 October 2026).** The two-view foundation defines typed region/coordinate, entrance transaction, residency, view lifecycle, shared-clock, natural-source, water-conservation, natural-rest, failure and test contracts. WV-B1 implements only the first region/navigation/view/entrance slice. WV-C1/C2 resources, WV-D autonomous cross-view work and WV-F unloaded simulation remain separate later scopes. See [World View Foundation](world-view-foundation.md).

## Current implementation — Milestone 3.5B

**Implemented — Pending Manual Acceptance (14 October 2026).** Soil and Stone excavation now always creates exact-once loose output at the excavated authoritative cell, independent of excavator inventory and storage. Existing hauling remains the separate collection/storage activity, and construction retains direct builder self-supply from inventory, storage or drops. Focused verification passed 96/96 Edit and 14/14 Play; complete verification passed 514/514 Edit and 67/67 Play. The 27-step manual checklist remains pending.

Milestone 3.5A also remains **Implemented — Pending Manual Acceptance** as a separate presentation/art gate. 3.5B changed no art mappings, cell projection, designation input or HUD behavior. 3.4E congestion recovery, World View runtime, new Foraging AI and new extraction types remain out of scope.

This page is a concise navigation view of the authoritative `Documentation/DevelopmentRoadmap.md`. The authoritative file retains the full acceptance criteria and historical details.

## Parallel art foundation

**Implemented — Pending Manual Acceptance.** The custom source sheets and separate 32-pixel Colony/World candidate libraries are committed with deterministic extraction and import settings. They are not gameplay-bound or certified seamless. Manual Sprite Editor, camera-scale and repeated-edge review precedes production Tile assets and palettes; World View gameplay remains deferred.

## Immediate boundary

**Current action:** manually verify Milestone 3.5B's 27-step loose-excavation checklist in `Assets/AntColony/Scenes/AntGameDevelopment.unity`, while keeping 3.5A visual acceptance and older pending UI checklists separately tracked.

Milestones 3.4B and 3.4C are manually accepted. Milestone 3.4D remains **Implemented — Pending Manual Acceptance**. Its global Work matrix uses authoritative active ant records, exposes only real Excavation/Construction/Hauling executors, and edits the existing per-ant 1–4/Disabled values shared with developer diagnostics. Verification passed 7/7 focused Edit, 7/7 focused Play, 492/492 full Edit and 65/65 full Play.

## Current milestone state

| Milestone | Outcome | Status |
| --- | --- | --- |
| 1 | Continuous-input player-ant movement prototype | In progress; retained regression baseline |
| 2.1A–2.1E | Two-layer world, classification, visualisation, Tilemaps | Complete |
| 2.2A–2.2B | Authoritative collision and whole-body clearance | Complete |
| 2.2C | Background capability and gravity eligibility | Implemented and automated; dedicated manual Scene-view acceptance pending |
| 2.3A–2.3B | Bounded autonomous ground/grip/gravity movement | Complete under recorded acceptance |
| 2.4A | Natural Background grip and Prepared speed | Complete |
| 2.4B | Grid navigation proof of concept | Complete |
| 2.4C | Scheduler stabilisation and multi-ant readiness | Complete |
| 2.4D | Production grid navigation integration | Implemented — Pending Manual Acceptance |
| 3.1A | Selection and independent move orders | Complete |
| 3.1B | Persistent ant identity and attributes | Complete |
| 3.1C | Learn-by-doing skill foundation | Complete |
| 3.2A | Needs and survival foundation | Complete |
| 3.2B | Physical food, water, and rest interactions | Complete |
| 3.2C | Individual autonomous survival | Complete |
| 3.3A | Synthetic colony jobs and per-ant priorities | Complete |
| 3.3B | Real excavation and loose Soil/Stone output | Complete |
| 3.3C | Per-ant inventory and explicit physical material transfers | Complete |
| 3.3D | Physical Material Storage and autonomous Soil/Stone hauling | Complete |
| 3.4A | Functional Background conversion and builder self-supply | Complete |
| 3.4B | Unified simulation clock and normal gameplay mode | Complete |
| 3.4C | Colony View player HUD and developer UI separation | Complete |
| 3.4D | Colony management and work priorities UI | Implemented — Pending Manual Acceptance |
| 3.5A | Colony View presentation, preview and first art integration | Implemented — Pending Manual Acceptance |
| 3.5B | Loose-only excavation output and activity separation | Implemented — Pending Manual Acceptance |

## Approved World View direction

The WV-B1 World View foundation is implemented and awaiting its remaining broad
regression/manual acceptance. It provides a bounded top-down Surface connected
to side-on Colony View through one nest entrance. Persistent ants and the
simulation clock are shared; switching views must not duplicate ants or stop the
hidden region. WV-B2A specifies future generation but does not authorise WV-B2B
runtime work.

The specified delivery order is:

1. **WV-A complete:** approve two-view coordinate, residency, entrance, navigation and survival contracts as documentation;
2. **WV-B1:** build one small bounded surface, manual view switching and one entrance transition;
3. **WV-C1:** add finite-source discovery, direct survival and natural rest;
4. **WV-C2:** add recoverable seed/water extraction, hauling and storage;
5. **WV-D:** add bounded autonomous cross-view jobs;
6. expand surface chunks and environments incrementally;
7. **WV-F:** add lightweight unloaded-chunk simulation; and
8. consider predators, rival colonies and wider ecology only through later approved scopes.

The authoritative prerequisites and phase boundaries are in `Documentation/DevelopmentRoadmap.md`.

## Ordered future direction

The numbered roadmap beyond the accepted foundation remains direction, not authorised implementation:

1. Complete the separate 3.5A visual and 3.5B gameplay manual acceptance checklists.
2. Complete any still-pending earlier UI acceptance without expanding scope.
3. Multiple-ant movement, local avoidance, and congestion recovery.
4. Extend functional construction into rooms, doors and quality through separate scope.
5. Add Nursery, brood, reproduction and colony growth through separate scope.
6. Address population/performance scaling and persistence.
7. Phase World View through its separately approved small-surface, cross-view and chunk-expansion milestones.
8. Add enemy colonies, equipment, combat and defence only when approved.
9. Complete finished-slice onboarding, objectives, save/load and polish.

Detailed scope and acceptance criteria must be approved when each milestone begins. See [Design Decision Log](design-decision-log.md) for unresolved dependencies.
