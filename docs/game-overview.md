# Game Overview

Milestone 3.4A now permits player-designated conversion of exposed Background into Prepared, Nursery, Sleeping, Food Storage, Water Storage or Material Storage tiles. A Construction-priority ant personally supplies physical Soil/Stone, stages it at the site and performs timed work. The implementation is automated but awaits the documented 30-step manual Play Mode acceptance; doors, rooms, brood and production remain future work.

## Vision

Ant Colony is a single-player, scrollable and zoomable 2D side-view colony simulation. The intended world is procedural, persistent, chunk-loaded, and destructible. The player manages a colony at strategic scale while issuing explicit orders to persistent individual ants.

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

Only the foundations of this loop exist today. See [Gameplay Mechanics](gameplay-mechanics.md) for the system-by-system status.

## Current playable development state

The integrated `AntGameDevelopment.unity` scene is a deterministic development environment, not a finished game loop. It currently demonstrates:

- authoritative two-layer world data and Tilemap projection;
- whole-body clearance, Background grip, gravity, and grid navigation;
- three selectable ants with persistent identity, attributes, skills, needs, priorities, and personal inventories;
- independent movement orders through the production grid motor;
- explicit physical Eat, Drink, Rest, material Pickup and Developer Only Drop interactions;
- autonomous survival, synthetic jobs, real foreground excavation and autonomous hauling;
- physical one-slot Material Storage tiles with Soil, Stone and general filters;
- developer diagnostics and deterministic fixtures.

Development controls are grouped in one compact top IMGUI toolbar. Ant, Needs, Skills, Jobs, Resources, World and Debug share a single bounded scrollable expansion area, so panels cannot overlap. Hide All leaves a small reopen control, and the centralized UI hit test prevents panel interaction from issuing world commands.

Milestones through 3.3C are accepted. Milestone 3.3D implements physical Material Storage and autonomous loose-drop/self-deposit hauling with full automated verification, while its 30-step manual Play Mode checklist remains pending. Construction, brood, room quality, combat, persistence, and a finished onboarding flow are not implemented.

## Scope boundaries

- Single-player only; no networking package is planned.
- PC-first prototype.
- The earlier continuous-input movement course remains a developer regression fixture, not the intended normal-play control scheme.
- Numerical balance is provisional unless the GDD explicitly marks it approved.
- Future ideas are not an implementation checklist and require a separately authorised milestone.

## Related pages

- [Ants, Attributes and Skills](ants-attributes-skills.md)
- [Food, Water and Survival](food-water-survival.md)
- [Brood and Reproduction](brood-reproduction.md)
- [Development Roadmap](development-roadmap.md)
