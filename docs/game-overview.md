# Game Overview

WV-B2B is **Complete** and WV-B2B-ARCH-001 is closed. Extraction is
the single node-removal skill/activity; Hauling supplies explicit Construction
staging or Storage; Construction is work-only after full staging. Legacy
Excavation code names remain compatibility seams, and Surface resource
interaction remains future work.

The accepted seed-`2203` run completed the generated Colony production loop,
view switching and entrance travel. The founding queen later died on Day 8 with
all three needs critical; usable generated food/water and natural rest remain
separately deferred WV-C1 work. Job selection now uses a bounded fair full-registry
pass so later reachable work cannot remain hidden behind 32 unreachable jobs.

Milestone 3.5B is **Implemented — Pending Manual Acceptance**; 3.5A remains separately pending visual acceptance. Extraction always leaves physical Soil/Stone output loose at the worked cell, independent of the worker's inventory and Storage. A separate Hauling job performs every normal pickup and delivery to Construction staging or compatible Storage; builders never self-supply. The development game opens paused at Day 1 08:00 with an authored uGUI player HUD and a global Work screen where priorities for active colony ants can be inspected and changed. Developer IMGUI starts hidden and remains deliberately accessible through F12 without replacing the player interface.

Accepted Milestone 3.4A permits player-designated conversion of exposed Background into Prepared, Nursery, Sleeping, Food Storage, Water Storage or Material Storage tiles. Under the later authoritative logistics correction, Hauling delivers physical Soil/Stone into site staging and a Construction-priority ant performs timed work only after the site is fully supplied. Doors, rooms, brood and production remain future work.

## Vision

Ant Colony is a single-player, scrollable and zoomable 2D colony simulation with two interconnected perspectives. **Colony View** is the existing side-on underground space for excavation, construction, storage, brood and colony activity. **World View** is an approved but unimplemented top-down surface space for exploration, foraging, resource collection and later environmental threats. The intended world is persistent and chunk-based, while individual ants remain authoritative across both views.

The game should remain small enough to finish. Depth comes from physical work, individual needs and skills, shared terrain rules, and colony logistics rather than from a large collection of disconnected systems.

## Player fantasy

The player guides a founding queen and, later, a growing colony through excavation, survival, construction, brood care, and defence. Ants are individuals rather than anonymous counters: each retains identity, attributes, learned skills, health, hunger, thirst, fatigue, equipment, and lifecycle state as the relevant systems are implemented.

## Intended core loop

1. Explore and understand the local underground space.
2. Excavate accessible foreground material without removing the permanent Background.
3. Convert exposed Background cells into useful colony functions using physical resources.
4. Secure food, water, sleeping capacity, and nursery capacity.
5. Authorise brood and support it until new workers mature.
6. Assign work through colony priorities or temporary explicit orders.
7. Expand while maintaining individual survival and preparing for hostile encounters.
8. Travel through nest entrances to explore a small nearby surface, collect useful resources and return to the colony.

Only the foundations of this loop exist today. See [Gameplay Mechanics](gameplay-mechanics.md) for the system-by-system status.

The documentation-only WV-A contract now defines how the two views will connect without claiming implementation: distinct region coordinates, one persistent ant residency, a capacity-one entrance transaction, manual remembered cameras, a shared clock, finite nearby seed/freshwater, recoverable one-unit water carriage and natural rest. See [World View Foundation](world-view-foundation.md).

## Current playable development state

The integrated `AntGameDevelopment.unity` scene is a deterministic development environment, not a finished game loop. It currently demonstrates:

- authoritative two-layer world data and Tilemap projection;
- whole-body clearance, Background grip, gravity, and grid navigation;
- three selectable ants with persistent identity, attributes, skills, needs, priorities, and personal inventories;
- a global work-priority matrix over authoritative ant records, including records without a loaded Colony View visual;
- independent movement orders through the production grid motor;
- explicit physical Eat, Drink, Rest, material Pickup and Developer Only Drop interactions;
- autonomous survival, synthetic jobs, real foreground excavation and autonomous hauling;
- physical one-slot Material Storage tiles with Soil, Stone and general filters;
- developer diagnostics and deterministic fixtures.

Normal gameplay uses the authored player HUD. Development controls remain grouped in one compact top IMGUI toolbar: Ant, Needs, Skills, Jobs, Resources, World and Debug share a single bounded scrollable expansion area. Developer Mode starts hidden, can be opened deliberately with F12 or its separate reopen control, and can close without changing the clock or autonomy. One uGUI/IMGUI input boundary prevents either interface from issuing world commands or moving the camera through pointer leakage.

Milestones 3.3D and 3.4A are accepted, providing physical storage/hauling and functional Background construction with builder self-supply. Milestone 3.5B changes only excavation output ownership and has complete automated verification; its 27-step manual checklist is pending. Brood, room quality, combat, persistence, and a finished onboarding flow are not implemented.

## Scope boundaries

- Single-player only; no networking package is planned.
- PC-first prototype.
- The earlier continuous-input movement course remains a developer regression fixture, not the intended normal-play control scheme.
- Numerical balance is provisional unless the GDD explicitly marks it approved.
- Future ideas are not an implementation checklist and require a separately authorised milestone.
- World View, nest transitions, cross-view jobs and unloaded-chunk simulation are approved design only; no surface gameplay is currently implemented.

## Related pages

- [Ants, Attributes and Skills](ants-attributes-skills.md)
- [Food, Water and Survival](food-water-survival.md)
- [Brood and Reproduction](brood-reproduction.md)
- [Development Roadmap](development-roadmap.md)
