# Ant Colony Current Game State

## Latest implemented milestone — 3.1B Persistent Ant Identity & Attributes (13 October 2026)

**Status: COMPLETE — all 12 manual Unity Play Mode checks confirmed by the user on 13 October 2026.** Previously reported focused Edit Mode 16/16, focused Play Mode 5/5, complete Edit Mode 268/268 and complete Play Mode 19/19 passed. No tests were rerun during this documentation update.

`AntColony.Ants` is a focused runtime assembly containing plain C# identity data with no MonoBehaviours, navigation or visual state. `AntId` and `ColonyId` wrap stable text values suitable for future serialization without using Unity instance IDs or GameObject names. `AntRecord` is the one model used by both `Queen` and `Worker`; caste currently changes no movement, carrying or other statistics. `AntRegistry` owns one colony's records, rejects duplicate Ant IDs and rejects records whose Colony ID does not match the registry.

`AntAttributes` stores Strength, Agility and Endurance independently. The documented provisional development range is **1–10 inclusive**; construction rejects out-of-range values instead of silently clamping them. These values have no progression, derived-stat, equipment, combat, carrying or movement-speed effects. The deterministic development records are provisional fixtures: **Mara** (`ant-dev-queen-001`, Queen, Strength 7 / Agility 5 / Endurance 8), **Pip** (`ant-dev-worker-002`, Worker, 4 / 8 / 5), and **Tala** (`ant-dev-worker-003`, Worker, 6 / 6 / 7), all in `development-colony`.

`AntEntityBinding` resolves a configured Ant ID to an existing record and never creates or removes authoritative data. `AntEntityBindingRegistry` rejects a second live visual claiming the same ID. Destroying a visual releases only that transient claim, so a replacement visual can bind to the same retained record. `SelectableGridAnt` reads its display name and lifecycle from the binding while `GridNavigationMotor` remains the only movement executor. Active ants accept the existing temporary movement orders; inactive or deceased records reject new orders, and an active order is cancelled safely if its record stops being active. No health, damage, ageing, corpse, burial or equipment-drop mechanics exist.

The compact top-right selection panel now shows Ant ID, name, colony, caste, Strength, Agility, Endurance and lifecycle above the existing control, navigation order, destination and failure fields. Selection changes only which bound record is displayed; it does not alter identity or another ant's order. The development tools remain separately collapsible.

### 3.1B automated verification

- Unity imported and compiled `AntColony.Ants`, Commands, Navigation, Development and both test assemblies successfully.
- Focused identity/selection Edit Mode: **16/16 passed**. Coverage includes unique creation, duplicate rejection, colony ownership, shared Queen/Worker model, independent attributes, range rejection, lifecycle retention, duplicate visual rejection, missing-record rejection, visual rebinding, deterministic identities, selection stability, correct motor targeting and deceased-order rejection.
- Focused selection/identity Play Mode: **5/5 passed**.
- Complete graphics-enabled Edit Mode: **268/268 passed**. A preceding `-nographics` attempt reached the retained rendered-pixel test and crashed while creating a RenderTexture; the graphics-enabled rerun passed completely.
- Complete Play Mode: **19/19 passed**, including all production grid-navigation and retained continuous 2.4A regressions. Separate confirmation runs also passed selection/identity 5/5, grid navigation 8/8 and autonomous movement 6/6.
- Manual Scene/Game-view acceptance was not performed and remains pending user confirmation.

### Exact 3.1B manual Unity Play Mode acceptance

No Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm three distinct cyan, pink and green selectable ants appear.
2. Select the cyan ant and confirm the panel reports Mara, `ant-dev-queen-001`, `development-colony`, Queen, attributes 7/5/8 and Active.
3. Select the pink and green ants and confirm Pip and Tala are Workers with IDs `ant-dev-worker-002` and `ant-dev-worker-003`, and attributes 4/8/5 and 6/6/7 respectively.
4. Switch repeatedly among all three ants. Confirm each identity and attribute set remains unchanged and exactly one selection ring follows the selected ant.
5. Right-click separate valid destinations for each ant. Confirm the selected record receives the order, all three retain independent destinations and movement remains smooth through `GridNavigationMotor`.
6. While one ant moves, select another. Confirm the first ant keeps its identity and order while the panel changes to the second ant's correct record.
7. Allow an order to complete, cancel another and issue an unreachable destination. Confirm the existing order/navigation status and failure information remain readable alongside identity data.
8. Pan with WASD/arrow keys, zoom with the wheel and reset with Home. Confirm selection and displayed identity remain correct.
9. Open and collapse `Show Developer Tools`. Confirm terrain inspection, mutation and stress tools remain functional and do not replace the three authoritative records.
10. Exit and re-enter Play Mode. Confirm the same three deterministic fixture IDs, names, castes and provisional attributes return in a clean state.
11. Confirm one Queen and two Workers share the same movement and selection behavior; caste grants no visible bonus or restriction.
12. Confirm the Unity Console has no relevant error, assertion or missing-reference messages.

This checklist does not expose a player-facing death control. Deceased-order rejection is automated-test coverage only; actual death gameplay and presentation remain excluded.

Known limitations are deliberate: the registry is in-memory only; save/load and chunk streaming are absent; records contain no skills, needs, health depletion, jobs, work priorities, inventory, equipment, brood, reproduction, ageing or combat; lifecycle transitions have no gameplay cause; and this fixture does not define the finished starting population. The approved future game begins with one working founding queen.

## Latest accepted milestone — 3.1A Ant Selection & Command Integration (13 October 2026)

**Status: COMPLETE — user confirmed all 14 manual Unity Play Mode acceptance checks passed on 13 October 2026.** The prior reported automated results were focused Edit Mode 6/6, focused Play Mode 4/4, full Edit Mode 258/258, full Play Mode 18/18, plus scene validation. This documentation update did not rerun tests.

`AntGameDevelopment.unity` now enters Play Mode with three player-facing grid ants named `Ant 1`, `Ant 2` and `Ant 3`. The development host chooses three deterministic valid anchors at least three cells apart, instantiates the existing `PrimaryGridAnt.prefab`, gives the ants cyan, pink and green placeholder colours, and connects every motor to the same production `GridNavigationWorldService`. Stress-test ants are stored separately, so spawning or clearing a stress group does not replace the three selectable ants.

`AntColony.Commands` is a small runtime assembly with two focused components. `SelectableGridAnt` adds a yellow selection ring and translates terminal `GridNavigationMotor` results into a temporary order outcome; it does not pathfind or move transforms. `AntSelectionCommandController` tracks one selected ant, converts camera-correct mouse positions into selection or destination cells, validates destination anchors through the shared map, and calls the existing `MoveTo`/`CancelMovement` API. Selecting, deselecting or switching ants never cancels an active order. Completed, failed and cancelled orders return to the player-facing `Autonomous / Idle` state while retaining the last destination/outcome for inspection.

Controls are now player-facing: left-click an ant to select it, left-click empty world space to deselect, and right-click a traversable destination to order only the selected ant. The compact top-right panel reports the selected ant, control state, order state, destination and failure reason, and provides a selected-order cancellation button. Its rectangle and the development panel rectangle block world input. The old development interface is collapsed behind `Show Developer Tools` at the top left; terrain inspection, `F`/`P`, legacy comparison, falling fixture, background isolation, world regeneration and 1/100/300/500 stress controls remain available without covering the ordinary command view. Existing WASD/arrow pan, middle-drag, wheel zoom and Home reset are unchanged.

### 3.1A automated verification

- Unity compiled `AntColony.Commands`, Navigation, Development, Editor tooling, and both test assemblies without errors. The integrated-scene structural validator passed with exactly one shared navigation service and one selection controller.
- Focused 3.1A Edit Mode: **6/6 passed**. Coverage includes three distinct shared-service ants, selection, deselection, switching while ordered, selection indicator ownership, independent/replaced requests, selected-only cancellation, invalid destination rejection, UI input blocking, and camera pan/zoom selection accuracy.
- Focused 3.1A Play Mode: **4/4 passed**. Coverage includes automatic three-ant spawn, moving selection indicator, arrival/completion, independent simultaneous orders, selected-only cancellation, and unreachable-order failure returning to Autonomous/Idle with a reason.
- Complete Edit Mode: **258/258 passed**.
- Complete Play Mode: **18/18 passed**, including all production grid-navigation and retained continuous 2.4A regressions.
- `git diff --check` passes after reverting Unity's unrelated automatic `Physics2DSettings.asset` serialization migration. No environment-blocked suite remains in this verification pass.

Known limitations are deliberate for 3.1A: selection is single-ant only; placeholder visuals are runtime-only; temporary move orders are not saved; completed/failed ants have no autonomous colony work to choose and therefore remain safely idle; local avoidance, traffic ownership, persistent identity, attributes, needs, work priorities, jobs, inventories, Draft Mode and combat are not implemented.

### Exact 3.1A manual Unity Play Mode acceptance

No Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm three grid ants appear automatically, visibly separated and coloured cyan, pink and green. The amber legacy comparison ant may also appear.
2. Confirm the small `ANT SELECTION & MOVE ORDER` panel is readable at the top right and the large development panel is hidden behind `Show Developer Tools` at the top left.
3. Left-click each coloured ant in turn. Confirm the panel names `Ant 1`, `Ant 2` or `Ant 3`, exactly one yellow selection ring appears, and the ring follows a moving ant.
4. Left-click exposed empty world space. Confirm the selection and ring clear without stopping any ant that already has a move order.
5. Select one ant and right-click a nearby exposed natural-background cell. Confirm only that ant moves smoothly through production grid anchors and the panel reports `Ordered`, its navigation status and destination.
6. While the first ant is moving, select a second ant and right-click a different valid destination. Then do the same with the third ant. Confirm all three retain independent destinations and can move concurrently through the one shared scheduler.
7. While multiple ants are moving, select one and right-click another valid cell. Confirm only that ant replaces its previous order; the other two continue toward their existing destinations.
8. Allow an ordered ant to arrive. Confirm the panel reports completion and `Autonomous / Idle`, the ant stays selected, and no fake follow-up job begins.
9. Select a moving ant and press `Cancel Selected Move Order`. Confirm only that ant returns to idle and other ants keep moving.
10. Right-click a covered/invalid cell and then the enclosed unreachable destination near `(12,-16)`. Confirm immediate invalid anchors are rejected without replacing a valid order, while an unreachable valid anchor eventually reports failure and returns the ant to idle without infinite retries.
11. Click buttons and blank areas inside both UI panels. Confirm no ant is selected/deselected and no movement order is issued through the UI.
12. Pan with WASD/arrows and middle-drag, zoom with the wheel, and use Home. At several zoom levels, confirm the ant under the pointer is selected and right-click destinations match the intended world cell.
13. Expand `Show Developer Tools`. Confirm `F`/`P`, terrain inspection, legacy comparison and the falling fixture remain usable. Spawn a 100-ant simultaneous/staggered stress group and clear it; confirm the three coloured selectable ants remain present and individually commandable.
14. Exit Play Mode and confirm the Console contains no relevant exceptions, assertions, missing references, stale-order delivery or infinite-retry messages.

Manual acceptance remains outstanding until the user confirms these checks.

## Latest implemented milestone — 2.4D Production Grid Navigation Integration (12 October 2026)

**Status: Implemented — Pending Manual Acceptance.**

Grid-authoritative navigation is now the primary reusable ant movement architecture. `PrimaryGridAnt.prefab` contains one `GridNavigationMotor`, Rigidbody2D, ordinary-ant capsule and presentation child. It contains no `PlayerMovementInput`, `AntSurfaceMotor` or `AntAutonomousMotor`. Stable grid traversal keeps the Rigidbody2D outside simulation; Falling temporarily restores Dynamic Rigidbody2D gravity and terrain collision before controlled recovery. The prefab is instantiated and bound by a world/spawn owner, so it has no dependency on the development inspector or simulated keyboard input.

Runtime navigation is separated into `AntColony.Navigation`. `GridNavigationWorldService` is one ordinary scene component per active world: it receives authoritative `ChunkedWorldData`, owns the one shared ordinary-ant `GridNavigationMap` and `GridNavigationScheduler`, advances the configured 128-node/update and 8-node/request slice, and forwards local terrain invalidation. Reinitialising it with the same world is idempotent. Individual motors reference this service and never create terrain graphs or schedulers. This keeps future traversal-profile support possible without introducing a singleton or service-locator framework.

The public movement-command boundary is intentionally small: `InitializeNavigation(service, spawnCell)`, `MoveTo(destination, priority)`, `CancelMovement(reason)`, `State`, `IsMovementActive`, `HasArrived`, `HasFailed`, `FailureReason` and `StatusMessage`. Destination replacement and cancellation continue to invalidate earlier scheduler request IDs; terrain revisions prevent stale results; failures and arrivals remain stable terminal states; Falling stores and resumes a valid destination with bounded retries. Callers do not need to know about A*, anchors or Rigidbody2D transitions.

`AntGameDevelopment.unity` authors exactly one shared navigation service and assigns `PrimaryGridAnt.prefab` as the reusable grid-ant prefab. Milestone 3.1A now places its player-facing selection and right-click command layer above this API. The amber `AutonomousAnt.prefab`, `AntAutonomousMotor`, `DirectGroundRouteValidator`, `AntSurfaceMotor`, `PlayerMovementInput`, prototype scenes and continuous regression tests remain developer-only comparison tooling. They do not attach to or issue competing commands for selectable grid ants.

### 2.4D automated verification and performance status

- The accepted 2.4C implementation is preserved at commit `97e50fe` and branch `codex/milestone-2.4c-grid-accepted`. Earlier 2.4B grid and continuous snapshots remain available.
- Unity Bee response-file Roslyn compilation passed for the World plus new Navigation sources, Development, EditMode-test, PlayMode-test and Editor assemblies. This verifies C# compilation but is not a Unity test-run result.
- New focused tests cover primary-prefab composition, absence of legacy/input motors, exactly one shared service, idempotent same-world initialisation, inspector-independent commands, shared service references across ants, stable arrival/failure reporting and stale-command cancellation.
- `git diff --check` passes, and the scene/prefab retain their existing asset references and GUID-based script linkage.
- The previous Licensing Client blocker cleared during 3.1A verification. Focused 2.4D Edit Mode passed 3/3 before 3.1A; the final complete suites pass 258/258 Edit Mode and 18/18 Play Mode, including retained grid and continuous movement regressions.
- The required 1/100/300/500 benchmark rerun is therefore blocked by the same Unity failure. The accepted 2.4C baseline remains 12.993/18.937/72.932/187.776 ms total batch time, 2.044/17.971/69.704/171.911 ms accumulated scheduler work, and 2.044/1.267/1.890/3.197 ms maximum scheduler update respectively, with all requests successful. Production integration moves that same scheduler update from the development host into the one shared world service and adds no new per-ant polling system. These are baseline comparison figures, not fresh 2.4D measurements or a production frame-rate claim.

Known limitations remain unchanged: the scheduler is main-thread only; searches and returned routes allocate managed memory; only the ordinary-ant traversal profile is instantiated; friendly overlap is allowed; and route-result caching, flow fields, hierarchical navigation, local avoidance and traffic ownership are not implemented.

### Exact 2.4D manual Unity Play Mode acceptance

No extra Inspector setup is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console and enter Play Mode. Confirm a cyan `Primary Grid Ant` and amber legacy comparison ant spawn, with no missing-script or missing-reference messages.
2. Confirm the panel starts on `Commanding: PRIMARY GRID`. Click a nearby exposed natural-background cell and confirm the cyan ant moves smoothly and reaches stable `Arrived` without keyboard input or teleportation.
3. Command the main passage and the detour destination around the blocker. Confirm multi-turn routing follows valid anchors and never enters foreground terrain.
4. Command horizontal, vertical and diagonal cells on exposed natural Earth/Stone, including the grip fixture top. Confirm natural backgrounds support traversal in all valid directions and diagonal blocked corners are not cut.
5. Traverse the prepared strip and compare it with adjacent natural terrain. Confirm the prepared material retains the visible 1.25x speed bonus without changing access.
6. Command the positive chunk-boundary destination near `(33,-8)` and confirm the route crosses the resident chunk boundary smoothly.
7. While a route is waiting or active, click a different valid destination. Confirm the new command replaces the old one and the old route never regains control. Use `Cancel First Grid Request` during a stress batch and confirm only that ant cancels.
8. Command the enclosed unreachable destination `(12,-16)` and an invalid/covered cell. Confirm the primary ant reaches stable `Blocked`, displays a meaningful failure, and does not retry indefinitely.
9. While the primary ant and a stress group are moving, point at a route cell and press `F`. Confirm the shared invalidation count increases and affected ants replan or fail clearly without following a stale route. Remove the blocker and retry.
10. Press `Start Grid Falling / Recovery Fixture`. Confirm grid control stops, Rigidbody2D gravity produces a physical fall, stable terrain contact is acquired, recovery recentres smoothly, and a stored valid destination resumes without oscillation.
11. Choose 100 ants, press `Spawn Selected Grid Group`, and run simultaneous then staggered requests. Confirm all ants reference one shared cache/scheduler, pending work is bounded across updates, friendly overlap does not deadlock movement, and clearing the group leaves the primary ant usable. Repeat at 300 and 500 if practical.
12. Switch to `Commanding: LEGACY 2.4A` and repeat a direct surface, prepared climb and falling comparison. Confirm only the amber ant responds and the primary grid ant is not controlled by the legacy system.
13. Exit Play Mode and confirm the Console contains no relevant exceptions, assertions, missing scripts, stale-result messages or infinite-retry behaviour.

This pre-3.1A checklist's old click-command wording is superseded by the player-facing controls above. Manual acceptance remains outstanding unless separately confirmed by the user. Milestone 2.4D adds no ant identity, skills, needs, colony jobs, excavation, hauling, construction, brood, rooms, Draft Mode, combat, enemies, persistence, streaming or networking.

## Latest implemented milestone — 2.4C Grid Navigation Stabilisation and Multi-Ant Readiness (12 October 2026)

**Status: Complete — manual Unity Play Mode acceptance confirmed by the user. Production integration is authorised separately as Milestone 2.4D.**

Milestone 2.4C turns the accepted grid experiment into a bounded shared navigation service without adding colony simulation. `GridNavigationScheduler` accepts many ant-owned requests against the one shared `GridNavigationMap`, defers allocating each A* frontier until that request receives service, and limits both total node expansions and per-request work per update. The development host currently uses a 128-node total budget and an 8-node request slice. Its deterministic `High, High, Normal, High, Normal, Low` service pattern favours urgent navigation while guaranteeing recurring low-priority service. These priorities are navigation-service hints only; they are not colony job priorities.

`GridPathSearch` is now resumable. It retains its deterministic heap, costs and predecessor data between scheduler updates, detects terrain-revision changes, and reports success, unreachable, search-limit or terrain-changed outcomes. The scheduler additionally reports cancellation, reuses an identical outstanding owner request, cancels a replaced destination, and checks request ID, owner, destination and map revision before delivery. Terrain remains shared cached authority; agents are never inserted into terrain navigation.

`GridNavigationMotor` now exposes `Idle`, `WaitingForPath`, `FollowingPath`, `Arrived`, `Blocked`, `Falling` and `Recovering`. Invalidation cancels stale work, stops at the last valid position, smoothly recentres to a valid anchor and submits a bounded retry when appropriate. Falling cancels controlled traversal, enables existing Rigidbody2D gravity, requires stable grounded contact or grip before recovery, recentres smoothly, and resumes the stored valid destination. The default automatic retry limit is two; unreachable or search-limit results become stable `Blocked` outcomes instead of infinite replanning.

The `AntGameDevelopment` host can create development-only groups of 1, 100, 300 or 500 experimental ants, issue simultaneous or staggered requests, cancel one ant independently, mutate terrain, clear the group and inspect live scheduler statistics. All experimental ants share the map and scheduler. Friendly grid ants exclude the Default agent layer from contact response, so temporary overlap cannot create traffic deadlocks; the Terrain layer still supplies physics contact while Falling. Only the first 40 stress ants receive debug visuals. The accepted amber 2.4A ant remains available unchanged for comparison.

### 2.4C automated verification and repeatable measurements

- Unity Bee response-file Roslyn compilation passed for `AntColony.World`, `AntColony.Development`, `AntColony.EditModeTests` and `AntColony.PlayModeTests`.
- Focused scheduler EditMode: **11/11 passed**, covering budgets, yielding/resumption, fair priority, duplicate suppression, destination replacement, queued/active cancellation, terrain revision safety, search limits, unreachable results and the four benchmark sizes.
- Focused PlayMode evidence: shared-scheduler three-ant independence/cancellation **1/1 passed**; active-route terrain mutation/replan **1/1 passed**; falling interruption/stored-destination recovery **1/1 passed**. The previously accepted 2.4B focused 3/3 and full 9/9 PlayMode baselines remain unchanged.
- A combined focused PlayMode run initially passed four tests and timed out only on the long mutation fixture; that test passed after its test-only bound was raised from 20 to 35 seconds. Subsequent combined-suite retries could not start because Unity lost its Licensing Client connection and crashed in the editor REST-service Mono thread. No combined post-fix or full-suite result is claimed.
- The benchmark warms shared terrain classification separately, submits deterministic requests, advances the scheduler with the production-development limits (128 total nodes and 8 nodes per request/update), retains returned routes for the managed-memory lower bound, and times 100 Physics2D steps with stable grid bodies disabled. `scheduler ms` is accumulated path-service work; `batch ms` also contains request/result test-harness work. Travel figures are path-cost-derived individual traversal times at the ordinary-ant speed, not wall-clock stress-scene completion times.

| Ants | Batch completion | Scheduler work | Maximum update | Average / worst latency | Total nodes / updates | Peak queue | Predicted average / worst travel | Physics2D, 100 steps | Retained bytes | Results |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 12.993 ms | 2.044 ms | 2.044 ms | 1 / 1 updates | 51 / 1 | 1 | 16.057 / 16.057 s | 1.431 ms | 0 | 1 success, 0 failures |
| 100 | 18.937 ms | 17.971 ms | 1.267 ms | 35.580 / 44 updates | 5,100 / 44 | 100 | 16.057 / 16.057 s | 0.223 ms | 217,088 | 100 successes, 0 failures |
| 300 | 72.932 ms | 69.704 ms | 1.890 ms | 106.137 / 132 updates | 15,300 / 132 | 300 | 16.057 / 16.057 s | 0.756 ms | 692,224 | 300 successes, 0 failures |
| 500 | 187.776 ms | 171.911 ms | 3.197 ms | 176.614 / 219 updates | 25,500 / 219 | 500 | 16.057 / 16.057 s | 1.143 ms | 1,118,208 | 500 successes, 0 failures |

All four runs recorded zero cache invalidations because mutation behaviour is measured separately. Cold cache warm-up varied from 7.839–8.455 ms except for one 70.016 ms one-ant startup outlier. These synthetic editor measurements demonstrate bounded work and correctness, not a production frame-rate guarantee. Request/result objects, per-search collections and route results still allocate managed memory; the scheduler is main-thread only; no route-result cache, flow field, hierarchical pathfinding, local avoidance or traffic ownership has been added.

### Exact 2.4C manual Unity Play Mode acceptance

No extra Inspector wiring is required.

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, clear the Console, enter Play Mode and confirm the amber 2.4A ant and cyan experimental grid ant appear without errors.
2. Select `Commanding: 2.4A CONTINUOUS`, issue the accepted direct surface, prepared climb and falling/recovery checks, and confirm the regression ant still behaves as in 2.4A.
3. Select `Commanding: GRID POC`, click valid direct, detour, vertical, prepared and chunk-boundary destinations, and confirm the single grid ant moves smoothly and arrives without teleporting.
4. Choose group size `100`, press `Spawn Selected Grid Group`, then `Start Simultaneous Requests`. Confirm pending work decreases across many updates rather than all searches executing in one frame; inspect `Nodes last update` and confirm it never exceeds 128.
5. Confirm the group continues moving independently and reaches its deterministic destinations. Temporary friendly overlap is expected and must not deadlock routes.
6. While requests are pending, click a different valid destination for the primary grid ant or start a different deterministic batch. Confirm the old request is cancelled/replaced and no stale route later takes control.
7. Press `Cancel First Grid Request`. Confirm only the first experimental ant stops or changes state while other ants continue and complete.
8. Issue the enclosed `(12,-16)` target to the primary grid ant. Confirm it reaches stable `Blocked` and does not oscillate or retry forever.
9. Start a moving group, point at a route cell and press `F` to add/remove blocking Solid terrain. Confirm invalidation count increases, stale requests are cancelled, affected ants replan or fail clearly, and unaffected navigation remains stable. `P` may be used to toggle prepared Background and verify local speed-cost invalidation.
10. Press `Start Grid Falling / Recovery Fixture`. Confirm controlled traversal suspends, Rigidbody2D gravity drops the ant, stable contact is acquired, the ant recentres smoothly and a stored valid destination resumes without teleporting or oscillation.
11. Repeat steps 4–5 with `300` and `500`, then use `Start Staggered Requests`. Confirm both request modes finish without crashes; wait for pending/processing/delivery counts to reach zero before changing group size.
12. Confirm the panel reports selected/live agents, pending, processing, delivery, last/total nodes, peak queue, last/maximum scheduler milliseconds, invalidations, submitted, completed and cancelled counts with plausible changes during each action.
13. Press `Clear Extra Grid Ants`, exit Play Mode and confirm the Console contains no relevant exception, assertion, missing-reference or infinite-retry messages.

The user subsequently confirmed the manual Play Mode checks, completing Milestone 2.4C. This milestone does not implement colony jobs, excavation, resources, construction, identity/skills, needs, brood, rooms, Draft Mode, combat, enemy AI, persistence, streaming or networking.

## Latest implemented milestone — revised 2.4B Grid Navigation Proof of Concept (12 October 2026)

**Status: Complete — manual Unity Play Mode acceptance confirmed by the user. Production movement is not replaced.**

The approved proof of concept adds a separate grid-authoritative traversal path for one ordinary-ant profile. `GridNavigationMap` lazily classifies grounded and Background-grip anchors from `ChunkedWorldData`, caches eight-direction terrain connections, checks the complete ordinary-ant capsule along every edge, and rejects diagonal corner cutting. `GridPathfinder` performs deterministic A* with travel-time costs derived from the Background material speed multipliers. A binary-heap open set keeps searches bounded without scanning the entire open list.

`GridNavigationMotor` smoothly interpolates the cyan experimental ant between the returned anchor poses. Its Rigidbody2D is excluded from simulation during stable grid traversal and is enabled with gravity only in `Falling`; after contact restores Grounded or Gripping capability, the motor disables physics and smoothly recentres on a valid anchor. The map is shared terrain data: moving ants are not stored as obstacles. Successful Solid or Background mutations invalidate a local five-by-five cache neighbourhood and cause the experimental motor to re-evaluate or replan. Local avoidance, congestion and moving-enemy steering remain deliberately deferred.

The existing 2.4A `AntAutonomousMotor`, `DirectGroundRouteValidator`, prefab and tests remain available in the same scene. The inspector defaults clicks to the grid proof of concept and has a button to switch click commands back to the 2.4A continuous ant. The two comparison colliders ignore one another so the experiment cannot change the retained movement regression. The paused continuous-route 2.4B work is recoverable on branch `codex/milestone-2.4b-continuous-snapshot` at commit `28864f9`.

### Automated verification and benchmark

- Focused grid EditMode: 11/11 passed.
- Full graphics-enabled EditMode: 238/238 passed.
- Focused grid PlayMode: 3/3 passed, covering smooth non-teleport traversal, prepared vertical travel, Rigidbody2D falling and grid recovery.
- Full PlayMode: 9/9 passed, including all six retained 2.4A physics regressions.
- Benchmark method: warm the shared terrain cache, run independent A* requests across a 39-cell representative route, simulate every returned smooth route to completion at a `0.02` fixed step, and time 100 Physics2D steps with the stable grid bodies excluded from simulation. Allocations are the retained managed-heap lower bound after a full collection, not total transient allocation traffic.

| Simulated ants | A* batch | Nodes examined | Complete smooth-route simulation | 100 Physics2D steps | Retained managed bytes | Correct arrivals/failure |
| ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 1.657 ms | 501 | 1.079 ms | 0.938 ms | 0 | 1/1; invalid target rejected |
| 100 | 123.151 ms | 50,100 | 5.632 ms | 0.273 ms | 241,664 | 100/100; invalid target rejected |
| 300 | 363.736 ms | 150,300 | 17.218 ms | 0.597 ms | 1,085,440 | 300/300; invalid target rejected |
| 500 | 645.436 ms | 250,500 | 24.277 ms | 1.086 ms | 1,875,968 | 500/500; invalid target rejected |

The result supports grid-authoritative execution as the simpler per-frame foundation: stable ants require no Rigidbody2D simulation and movement work is a small deterministic interpolation loop. It also exposes the next scaling constraint clearly: 500 uncached searches must not be launched in one frame. A production version needs request budgeting and likely shared flow fields or route/subpath reuse for common destinations before hundreds of ants are enabled. This proof of concept does not claim local avoidance or mass-agent production readiness.

### Exact manual Play Mode acceptance

1. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode, and confirm one amber 2.4A ant plus one cyan `Experimental Grid Ant` appear with no Console errors.
2. Leave the panel on `Commanding: GRID POC`. Click exposed natural cells along the surface, the natural access shaft near X=-9, and the main passage at `(-2,-8)`. Confirm the cyan ant follows connected anchor-to-anchor motion smoothly and never teleports.
3. Click the prepared strip between X=-18 and X=-8 at Y=-8, then the prepared fixture top. Confirm travel remains valid in every direction and visibly speeds up over prepared Background.
4. Command `(18,-8)`. Confirm the route detours around the blocker at `(10,-8)` and does not cut diagonally through its corners. Command `(33,-8)` to cross the positive chunk boundary. Command `(12,-16)` and confirm the enclosed target is rejected without movement.
5. While the cyan ant is moving, select a remaining route cell and press `F`. Confirm the cache revision changes and the ant replans or reports a bounded failure without entering the new Solid. Remove the Solid and retry. Press `P` on an exposed route cell and confirm the route cost/speed responds after local invalidation.
6. Click `Start Grid Falling / Recovery Fixture`. Confirm the Rigidbody2D gravity drop occurs, the ant lands, smoothly recentres on a valid anchor, and returns to Idle with physics simulation disabled.
7. Switch the panel to `Commanding: 2.4A CONTINUOUS`. Repeat a direct ground move, prepared climb, terrain block and falling/recovery check. Confirm the amber ant retains the accepted 2.4A behaviour and neither ant pushes the other.
8. Exit Play Mode and confirm the Console contains no exceptions, assertion failures or missing-reference messages.

The user subsequently confirmed the manual comparison and stress-fixture behaviour, completing 2.4B. This acceptance does not authorize replacing the production motor or adding jobs, inventories, construction, survival, combat, reproduction, networking, local avoidance or enemy movement.

## Accepted baseline — 2.4A Natural Background Movement Realignment (12 October 2026)

**Status: COMPLETE — manual Unity Play Mode acceptance confirmed by the user.**

The user reports natural Earth and Stone background supports valid horizontal/vertical/diagonal grip at 1.0× movement speed; prepared background grants identical traversal with provisional 1.25× speed; Sky remains non-grippable with physics-driven falling. Movement capability, route validity and speed react to terrain/background changes. Existing developer input, collision and movement architecture remain intact.

**Reported implementation validation:** Unity Roslyn compilation passed; focused Edit Mode 56/56; graphics-enabled full Edit Mode 227/227; physics-backed Play Mode 6/6; navigation/development scene validators and `git diff --check` passed. A previous `-nographics` run crashed in a rendered-pixel test, while graphics-enabled rerun passed. The user subsequently confirmed manual Play Mode tests were good, completing the milestone. This is **user-reported validation**, not independently executed in this documentation-only update.

Earlier 2.3B and obsolete prepared-only grip statements below are historical and superseded by this accepted milestone.

The revised 2.4B proof of concept described above is the only later implementation. Keep jobs, construction, inventory and reproduction outside it.


## Design-versus-implementation status — 12 October 2026

Milestone 2.4A now implements the approved natural-background movement realignment. Exposed Natural Earth and Natural Stone provide full horizontal, vertical and diagonal grip at normal speed. Prepared Background provides the same traversal plus a provisional `1.25` movement-speed multiplier. Sky remains explicitly non-grippable. The other newly approved systems—starting queen, player-facing direct ant control, functional backgrounds, rooms, brood, needs and shared stores—remain design targets, **not implemented or verified features**.

**Next step:** complete the exact 3.1B manual checklist at the top of this document. Do not begin colony jobs, needs, inventory, progression, reproduction, Draft Mode or another later milestone without separate approval.

## Current milestone

**Milestone 3.1B — Persistent Ant Identity & Attributes**

**Status:** **Implemented — Pending Manual Acceptance.** Three ants use authoritative plain-C# records through focused runtime bindings while retaining the accepted grid selection and movement architecture; legacy continuous systems remain developer-only regression tools.

Milestones through accepted 3.1A retain their previously reported status. Milestone 2.4D remains implemented pending its separately recorded manual acceptance. 3.1B adds identity data but no colony jobs or other simulation.

## Repository and architecture

- The Unity 2D URP project uses Unity `6000.3.8f1` and the new Input System.
- `Assets/AntColony/Scenes/AntMovementPrototype.unity` is the first build scene.
- `Assets/AntColony/Scenes/AntGameDevelopment.unity` is the integrated development scene. The movement and navigation prototype scenes remain available temporarily as regression fixtures.
- The prototype controls one ordinary player ant. Becoming a queen is later progression.
- Input and movement retain the established `PlayerMovementInput -> AntSurfaceMotor -> Rigidbody2D` flow.
- `AntSurfaceMotor.SetMovementIntent(Vector2)` remains a useful prototype boundary while the future movement model is developed and validated.
- The current authored collider course is not evidence of the approved Background Tile/Solid Tile data model.
- The integrated scene has a small authored `World Bootstrap` coordinator, not a global manager or second world-data implementation. The world-data authority remains an ordinary, scene-independent C# object; excavation gameplay, multiplayer and multi-ant systems remain absent.

### Milestone 2.1B world-data foundation

- `ChunkedWorldData` explicitly owns resident chunks in a sparse dictionary. Creating or reading world data does not create GameObjects, components, colliders, or neighbouring chunks.
- Each `WorldChunk` is 32×32 cells and immediately fills all 1,024 Background Tile entries. A parallel compact array stores zero or one optional Solid Tile ID per cell.
- Central coordinate utilities use floor division for negative positions, keep local coordinates in the 0–31 range, and preserve the prototype convention that integer coordinates are tile centres in world space.
- Missing chunks return `ChunkUnavailable`; they never masquerade as exposed cells. Duplicate creation and unknown material IDs are rejected without overwriting data.
- `Background_Default` (`background.default`) and `Solid_Soil` (`solid.soil`) are initial authored ScriptableObject definitions. Milestone 2.1E assigns each a simple development-only visual Tile while retaining the material definition as the source of that reference.
- The 2.1B foundation stores a world seed without implicit generation. The later integrated checkpoint now consumes that seed through one explicit bounded generator; unloading, streaming and persistence still do not exist, and ordinary queries still never generate terrain.
- The existing `DirtBlock` objects have not been imported. The prototype scene, prefab, movement code, Rigidbody2D, colliders, gravity, and input remain unchanged.
- The focused Milestone 2.1B Unity Edit Mode suite passed 25/25, and the user confirmed that existing ant movement still works in Play Mode. Milestone 2.1B is complete.

### Milestone 2.1C navigation classification

- `WorldNavigationClassifier` is a plain C# service that reads `ChunkedWorldData` directly. It does not use scene colliders, physics queries, Tilemaps, movement components, or a scene host.
- Occupancy results distinguish `Unavailable`, `Covered`, and `Exposed`. Only a Solid Tile whose registered material has `BlocksMovement` enabled makes its cell Covered.
- Exposed cells separately report `Supported`, `Unsupported`, or `Unknown` support. This does not claim Traversable or Reachable status because body clearance, obstacles, movement-edge checks, and connectivity are not implemented.
- Only north, south, east, and west neighbours can support a cell. The neighbouring Solid Material must have `SupportsNavigation` enabled; Background Tiles and diagonal solids never qualify.
- Classification crosses positive and negative chunk boundaries. Missing target chunks are Unavailable. A missing neighbour produces Unknown only when no resident cardinal neighbour already provides confirmed support.
- Results are uncached and therefore reflect successful Solid Tile mutations immediately. Change notifications are deferred until a later cached visualisation or incremental navigation consumer genuinely requires them.
- The user-confirmed Milestone 2.1C acceptance suite passed 50/50 Edit Mode tests. The subsequent full Milestone 2.1D run also retained all classifier coverage, and Milestone 2.1C is complete.

### Milestone 2.1D navigation visualisation

- `Assets/AntColony/Scenes/AntNavigationPrototype.unity` is a separate developer-only scene. It does not alter `AntMovementPrototype.unity` or the existing movement, Rigidbody2D, input, and collider prototype.
- `NavigationTestWorldHost` authors a deterministic eight-chunk fixture through `ChunkedWorldData` APIs. It includes open floor, a wall and overhang, a narrow tunnel, a large unsupported chamber centre, negative coordinates, chunk boundaries, diagonal-only geometry, and an absent neighbouring chunk.
- One bounded batched mesh visualises 3,564 queried cells without per-cell GameObjects or colliders: Covered is brown, Exposed and Supported is green, Unsupported is red, Unknown is amber, and Unavailable is grey.
- The renderer reads the real `WorldNavigationClassifier`. Optional chunk-boundary lines, overlay visibility, Scene-view cell selection, world/chunk/local inspection, and an explicit Solid Tile mutation-and-refresh control are developer diagnostics only.
- Supported still means local cardinal terrain support only. The display does not claim body clearance, obstacle clearance, connectivity, reachability, or pathfinding.
- The complete automated suite passed 61/61 and the user confirmed all 11 graphics-enabled manual visual checks. Milestone 2.1D is complete.

### Milestone 2.1E world Tilemap integration

- `AntNavigationPrototype.unity` now contains one explicitly half-cell-offset `World Grid`, a `Background Tilemap`, and a `Foreground Solid Tilemap`. Cell `(0,0)` is therefore centred at world `(0,0)` while signed Tilemap indices remain identical to authoritative world-cell coordinates.
- `WorldTilemapRenderer` is a one-way projection from `ChunkedWorldData` through the material definitions to the two Tilemaps. It never reads Tilemap contents as simulation state and exposes explicit cell, chunk, and bounded selected-region refresh methods.
- Resident cells receive their registered Background visual. Optional Solid visuals are written independently above them, including for non-blocking Solid Materials; removing a Solid clears only the foreground presentation.
- Missing chunks are cleared rather than generated. Unknown IDs and missing visual references return precise diagnostics, clear stale presentation, and leave world data and navigation classification unchanged.
- The initial material definitions reference two colour-tinted development Tiles that reuse the existing one-pixel placeholder soil Sprite. Both Tile assets explicitly disable collider generation.
- Background, foreground, and navigation-overlay visibility remain independent. The overlay still reads only `WorldNavigationClassifier` and renders above both Tilemaps.
- Milestone 2.1E added no collision components, per-tile GameObjects, pathfinding, excavation, or autonomous movement.
- Unity compilation, scene validation, and all 89/89 Edit Mode tests passed. The user subsequently confirmed all 10 graphics-enabled manual visual checks, so Milestone 2.1E is complete.

### Milestone 2.2A authoritative terrain collision

- `AntNavigationPrototype.unity` contains a third, collision-only `Foreground Collision Tilemap` beneath the same half-cell-offset Grid. The Background and visual foreground Tilemaps remain collider-free.
- `WorldTerrainCollisionRenderer` is a one-way projection from `ChunkedWorldData`: it places an invisible Grid-collider Tile only when the registered Solid Material has `BlocksMovement` enabled. It never treats Tilemap or collider occupancy as authoritative world state.
- Visual foreground occupancy, physical collision and navigation support remain independent. The fixture includes a non-blocking supporting Solid and a blocking non-supporting Solid to demonstrate that `BlocksMovement` and `SupportsNavigation` are not aliases.
- One `TilemapCollider2D` feeds one manually generated `CompositeCollider2D` on a static `Rigidbody2D` using the existing `ClimbableSurface` layer. Adjacent cells merge without per-tile GameObjects or duplicate terrain hits.
- Explicit cell, chunk and bounded-region refreshes update only requested collision occupancy. `SynchronizeCollisionGeometry` processes pending Tilemap collider changes, regenerates composite geometry and synchronizes transforms before development physics queries.
- The custom inspector reports authoritative blocking state and projected collision occupancy for the selected debug cell, refreshes a cell or selected region, coordinates the existing controlled Solid mutation, and can toggle collision-outline Gizmos.
- Unity compilation, the expanded scene/material/collider validator, and all 117/117 Edit Mode tests passed. The user subsequently confirmed all 12 manual Unity collision checks, so Milestone 2.2A is complete.
- This collision projection is development-scene infrastructure only. It does not establish whole-body clearance, connectivity, reachability, pathfinding, autonomous movement, procedural generation or excavation gameplay.

### Milestone 2.2B ant body clearance validation

- `AntClearanceValidator` is a scene-independent, read-only service that checks a complete rotated capsule directly against authoritative movement-blocking Solid cells. It does not read Tilemaps or collider geometry and therefore cannot return a stale result after a successful world-data mutation.
- `AntCollisionProfile` carries local capsule size, offset, long axis and optional clearance margin. The initial `OrdinaryAnt` profile matches the unchanged movement-scene ant: its prefab-local 1.2×0.6 capsule is scaled by the authored 0.8×0.5 instance scale to an effective horizontal 0.96×0.3-world-unit body with zero offset.
- Results distinguish Clear, Blocked by terrain, Unavailable world data and invalid query/profile. Clearance does not inspect foreground support, Background movement permissions, grip, grounding, gravity, movement state, connectivity or reachability.
- Capsule-versus-cell testing uses the exact central segment plus radius against candidate 1×1 axis-aligned cells. Rotation and collider offset are applied mathematically, only the footprint's bounded cell range is queried, exact tangency is permitted within a `0.00001`-unit tolerance, and an optional margin provides conservative separation.
- A valid capsule is Clear when every touched cell is available and no overlapping Solid Material blocks movement. A known blocking overlap takes precedence over missing data elsewhere in the footprint because it already proves the pose invalid; otherwise any missing touched cell returns Unavailable. Physically fitting open Sky and unprepared shafts are therefore Clear.
- `AntNavigationPrototype.unity` includes an `Ant Clearance Inspector` with an editable position, orientation and profile, a colour-coded exact capsule outline, a Scene-view position handle, a result label and explicit query button. It creates no gameplay ant, collider or Rigidbody2D.
- Unity compilation, focused 38/38 clearance tests, the expanded scene validator, the focused 2/2 movement regression and the full 155/155 Edit Mode suite passed. The user subsequently confirmed all 9 graphics-enabled manual clearance checks, so Milestone 2.2B is complete.

### Milestone 2.2C Background movement capability and gravity eligibility

- `BackgroundTileMaterialDefinition` stores explicit horizontal, upward and downward voluntary movement permissions, intrinsic climbing grip, unsupported gravity and a data-driven movement-speed multiplier.
- Authored development materials define Natural Earth and Natural Stone as full-direction grippable terrain at `1.0x`. Prepared Grippable Background has the same directions and grip at a provisional `1.25x`. Sky is non-grippable and gravity-eligible.
- `AntMovementCapabilityEvaluator` is a read-only, scene-independent service. It composes the unchanged `AntClearanceValidator`, the centre Background Material and an authoritative ground-contact query into explicit `Grounded`, `Gripping` or `Falling` eligibility plus detailed permissions and failure diagnostics.
- Ground support uses the full rotated capsule's lowest extent and a `0.02`-world-unit contact tolerance against top faces of registered Solid Materials that both block movement and support navigation. Side walls, ceilings, non-blocking solids and Background Tiles do not become floor support.
- Grounded and exposed natural terrain permits its configured directions through normal grip. Falling occurs only where neither valid ground nor a grippable Background exists. Prepared conversion changes speed rather than access and does not change physical clearance.
- The existing `SetBackgroundMaterial` mutation safely demonstrates Unprepared Earth to Prepared replacement without changing foreground occupancy, creating chunks or introducing Surface Preparation gameplay. `WorldTilemapRenderer.RefreshCell` explicitly refreshes its presentation.
- `AntNavigationPrototype.unity` includes one `Ant Movement Capability Inspector` at the unsupported chamber centre. It reports Background ID, clearance, ground, grip, direction permissions, gravity and suggested state; its explicit toggle button replaces the selected Background, refreshes the visual cell and requeries.
- Focused capability tests pass 37/37. The expanded structural scene validator, focused 2/2 legacy movement regression and complete 192/192 Edit Mode suite pass. Graphics-enabled manual Scene-view acceptance remains pending, so Milestone 2.2C is in progress rather than complete.

### Integrated development sample world checkpoint

- `AntGameDevelopment.unity` combines the existing authoritative chunk data, material registries, visual Tilemaps, collision-only Tilemap, clearance validator and movement-capability evaluator in one Play Mode inspection environment. It does not contain the legacy manually controlled ant.
- `SampleWorldGenerator` explicitly creates a bounded 4×4 group of 32×32 chunks from `(-2,-2)` through `(1,1)`, for an initial 128×128-cell resident world. Queries outside that region remain unavailable and do not create chunks.
- Seed `2203` deterministically produces gently varied surface elevation, exposed Sky, Earth and Stone Background regions, Soil and Stone foreground terrain, two chambers, connected passages, one unprepared vertical shaft, the existing underground prepared strip and a prepared surface grip fixture. All authoritative cells are populated before Tilemap or collision projection.
- The scene hierarchy contains `World Bootstrap`, one half-cell-offset `World Grid`, separate Background and foreground visual Tilemaps, one hidden foreground collision Tilemap, `Main Camera`, and `Development Inspection UI`. No per-tile GameObjects or duplicate terrain colliders are introduced.
- WASD or arrow keys pan, the mouse wheel zooms, middle-mouse drag pans and Home returns to the starting view. Clicking outside the panel selects a cell. The panel reports authoritative cell/chunk/material data, blocking state, ordinary-ant clearance, grip, grounded/falling eligibility and voluntary directions.
- `F` or the foreground button toggles the selected Solid Tile and explicitly refreshes both foreground presentation and collision. `P` or the Background button switches exposed space between prepared and its surface/depth-appropriate unprepared material, preserving foreground occupancy and refreshing the inspection result.
- The first manual Play Mode pass confirmed world generation and middle-mouse dragging, but found that the material palette rendered too dark and wheel zoom appeared inactive. The visual cause was multiplication of every Tile tint by the old dark-brown placeholder sprite. Integrated-world Tiles now use a neutral white pixel plus the explicit Sky `#719BC1`, Soil `#986342`, Stone `#737E89`, Earth Background `#514439`, Stone Background `#434C56` and Prepared Background `#648B62` palette.
- Wheel zoom previously assumed a roughly 120-unit Windows mouse delta and scaled the raw value directly, making approximately 1-unit device deltas visually negligible. Scroll is now normalised into device-independent steps, changes the target by 4 orthographic units per step, smoothly approaches that target over `0.08` seconds and remains clamped to an orthographic size of 4–64. Home resets both the current and target size to 30; pan and middle-drag remain in the same controller.
- A subsequent manual check showed that authoritative Unprepared-to-Prepared mutation, grip and movement eligibility changed while the visible Background remained brown. A graphics-enabled authored-scene test at the reported cell `(-1,15)` ruled out coordinate mismatch and occlusion. `Tilemap.SetTile` stored the prepared Tile asset but preserved the cell's previous Earth `TileData` colour; neither `RefreshTile` nor the bounded `RefreshAllTiles` diagnostic replaced that stale colour. Single-cell projection now copies a standard Tile's authored colour into the cell, temporarily unlocking only `LockColor` and restoring the effective flags before refreshing that cell. The effective authored flag remains `LockTransform`.
- The development inspection panel now reports the world and Tilemap coordinates, material ID, Tile name, Tile asset colour, effective cell colour, Tilemap colour, sampled Sprite pixel, composed expected colour, asset and cell flags, renderer material/shader, sorting, and active/enabled state. A development-only button isolates the Background visual Tilemap by hiding the foreground visual layer, without changing authoritative data or collision.
- The graphics-backed regression renders the authored camera at `(-1,15)`: Earth is `RGBA(0.318, 0.271, 0.220)`, prepared is `RGBA(0.388, 0.545, 0.384)`, the isolated prepared pixel is identical, and toggling back restores Earth. The user subsequently confirmed the corrected palette, camera controls, inspection, foreground mutation and prepared-Background colour round trip in Play Mode. The integrated sample-world checkpoint is manually accepted.
- This checkpoint does not complete Milestone 2.2C, begin general directional connections, perform real excavation or Surface Preparation jobs, stream chunks, or save/load the world.

### Milestone 2.3A first autonomous ant — ground movement

- `AutonomousAnt.prefab` is separate from `PlayerAnt.prefab`. It uses the ordinary ant's 0.96×0.3 horizontal capsule, a dynamic continuous-collision `Rigidbody2D` with frozen rotation, a bright development visual and `AntAutonomousMotor`. It contains no `PlayerInput`, `PlayerMovementInput` or `AntSurfaceMotor`.
- `AntGameDevelopmentWorldHost` searches at most 20 surface columns around preferred X `-12`. Each candidate must be resident, physically clear, centre-supported, horizontally permitted and reported Grounded by `AntMovementCapabilityEvaluator`. A failed bounded search logs a clear error and creates no ant; repeated initialisation resets the existing instance rather than duplicating it.
- Left-clicking outside the inspection panel still selects and inspects the clicked cell, and now also requests a destination for the ant. Clicking either a supporting floor cell or its exposed cell resolves the same standing pose. Invalid commands hide the marker and report the rejection reason.
- For 2.3A, `DirectGroundRouteValidator` was a scene-independent, read-only horizontal corridor check: at most 24 world units and samples at most every 0.2 world units, with whole-body clearance, Grounded horizontal capability and centre support required throughout.
- For 2.3A, `AntAutonomousMotor` advanced through `Rigidbody2D.MovePosition` in `FixedUpdate`, turned its visual when direction changed and stopped within a configurable 0.08-unit tolerance. It revalidated each immediate step so new blocking terrain or removed support stopped movement.
- The existing panel reports ant position, Idle/Moving/Arrived/Blocked state, destination and readable status. A small yellow marker shows an accepted destination and becomes green on arrival.
- Focused coverage included eight route-validator cases, five authored-scene spawn/destination/wiring cases and three physics-backed Play Mode cases for arrival, new blockers and lost support. Unity scene structural validation, the complete 221/221 Edit Mode suite and the complete 3/3 Play Mode suite passed. The user subsequently confirmed all 11 manual Play Mode checks, so Milestone 2.3A is complete.

### Milestone 2.3B Background grip and gravity movement

- `AntAutonomousMotor` now reports physical `Grounded`, `Gripping` and `Falling` separately from command progress (`Idle`, `Moving`, `Arrived`, `Blocked`). Each physics step uses `AntMovementCapabilityEvaluator`; no material ID is hardcoded in the motor.
- Grounded and Gripping are controlled states: gravity scale is zero and a prevalidated direct command may use `Rigidbody2D.MovePosition`. Falling is mutually exclusive in the same physics step: the command is interrupted, horizontal command velocity is cleared, the authored gravity scale of 4 is restored and Rigidbody2D collision moves the ant until ground or grip is reacquired.
- Ground contact keeps the existing `0.02`-world-unit capability tolerance. State selection otherwise follows the stable centre Background cell; there is no timer-based hysteresis. Entering Grounded or Gripping clears fall velocity once, preventing boundary oscillation from retaining downward momentum.
- The bounded direct validator supports horizontal, vertical and diagonal segments up to 24 units. Every sample requires whole-body clearance and either valid Grounded support or material-provided grip; direction flags are checked at every interval, missing chunks fail closed and diagonal cell transitions require both orthogonal neighbours to be resident and unblocked. It still performs no detours, graph search or waypoint planning.
- The deterministic sample adds a short flat Natural Earth floor across the seven-cell Prepared vertical strip at X `-12`, plus the prepared top region. This exposes Grounded natural movement, natural-to-prepared transition, vertical/diagonal movement, speed comparison, explicit Sky grip loss, recovery and safe falling without teleporting the ant.
- The inspection panel reports ant position, locomotion, command progress, Background ID, grip, gravity eligibility, base/effective movement speed, destination and interruption/rejection reason. Foreground and Background mutations refresh the relevant projections and notify the motor immediately, which rechecks local state and invalidates an active direct route if necessary.
- Unity's generated Roslyn response files compile the changed World, Development, Editor and test assemblies. The focused 2.4A Edit Mode groups pass 56/56; the full graphics-enabled Edit Mode suite passes 227/227; all physics-backed Play Mode tests pass 6/6. An earlier full `-nographics` attempt crashed in the existing rendered-pixel test, so the successful full suite was correctly rerun with graphics enabled.

## Approved design boundaries and future work

- Normal play manages ants through selection, inspection, designations, colony priorities and specific task assignments; it does not directly steer them.
- Every coordinate in a generated chunk contains one permanently present Background Tile whose material may be replaced, plus zero or one foreground Solid Tile; ungenerated chunks remain unavailable.
- Background Materials are the primary authority for voluntary directions, intrinsic grip, gravity behavior and movement-speed multipliers. Natural Earth/Stone and Prepared terrain grant grip; Background existence alone still grants nothing when the material is Sky or explicitly non-grippable.
- Excavation removes an eligible foreground Solid, leaves its Background unchanged and must update affected systems. Exposing intact natural Earth or Stone therefore exposes its already-grippable Background; excavation gameplay itself remains unimplemented.
- Surface Preparation is an approved future resource-consuming job. Its movement benefit is faster travel over an already-traversable Background, not access to climbing.
- Physical clearance, Background movement capability and directional navigation connections are separate. Falling through physically clear, non-grippable space may be valid even when upward voluntary movement is not.
- Natural and Prepared grip can permit clearance-valid two-dimensional movement without immediate Solid adjacency. Foreground `BlocksMovement` always remains authoritative, and `SupportsNavigation` stays independent.
- Navigation connections may be directional; diagonal corner-cutting through blocking geometry remains invalid.
- Every ant starts every ordinary skill at 0, can perform ordinary tasks without a specialist gate, and improves from successful work on a 0–100 scale. Roles remain preferences and priorities.
- Independently tracked skills above 50 slowly decay after inactivity and stop at exactly 50; skills at or below 50 do not decay through inactivity.
- The proposed carrying system remains future work: one initial carrying slot plus one slot per 20 Hauling skill, capped at five slots, with one item per slot.
- Multiple ants may share a tile and use hybrid separation, local avoidance and bounded recovery rather than universal one-ant-per-tile occupancy.
- Friendly, neutral and hostile colonies share the same terrain navigation model.
- The proposed architecture separates world tile data, terrain/excavation and preparation changes, physical clearance, Background movement capability, directional navigation, colony task decisions, per-ant route following, continuous movement, local avoidance and authoritative collision.
- The design target allows potentially hundreds of simulated ants, but no DOTS/ECS or other substantial dependency is approved before profiling demonstrates a need.

## Implemented and Play Mode confirmed

The user has confirmed the previous movement prototype works in Unity Play Mode:

- A/D and Left/Right horizontal floor movement.
- Stopping and direction reversal.
- Floor attachment.
- W upward climbing and S downward movement on left and right walls.
- Wall attachment without sliding when input is released.
- Surface-aligned floor and wall rotation.

## Newly implemented, pending Play Mode acceptance

### Ceiling traversal

- A fourth non-allocating collider cast detects a nearby climbable ceiling.
- Ceiling attachment uses the same motor, attachment state, gravity handling, and collision system as floors and walls.
- A/D is projected onto the ceiling tangent, and the ant rotates so its feet face the ceiling.
- Releasing input produces zero traversal velocity while a valid ceiling remains attached.

### Directional surface choice

- The current surface remains preferred while stationary or while intent still matches its tangent.
- A connected perpendicular surface is selected only when its movement direction clearly matches world-space input.
- Floor-to-wall selection therefore requires upward intent rather than automatically converting A/D into a climb.
- At a wall-to-ceiling junction, W continues toward the obstruction but does not choose left or right. A or D selects the corresponding valid ceiling direction.
- Ceiling-to-wall and wall-to-floor choices use the same intent projection rather than surface-specific input code.
- Selection thresholds and bias are configurable on `AntSurfaceMotor`.

### Inside and outside corners

- Direct down/left/right/up casts provide candidate surfaces at concave inside corners.
- A short look-ahead from the current contact detects the perpendicular face around a convex outside edge. Zero-distance results are rejected so a ray beginning inside ordinary terrain cannot create a false corner.
- Candidate surface lines must intersect within the configurable connected-corner distance, and separate source/target colliders must physically touch or fall within a small seam tolerance. This prevents attachment across unsupported gaps.
- A successful transition follows a smooth quadratic corner path through `Rigidbody2D.MovePosition` and `MoveRotation`; collision is never disabled and the Transform is not moved directly.
- The complete path is sampled before transition using a slightly inset copy of the ant collider. Every movement step also performs a collider sweep and a pose-overlap check.
- If clearance fails, the transition is rejected and the ant remains on the current surface. Releasing input pauses an active transition; opposite intent can reverse it.
- Position and orientation advance at the slower of configured movement and rotation rates to avoid snapping or uncontrolled acceleration.

### Corner-transition deadlock fix

- Live Play Mode testing confirmed that an ant could enter a corner transition and become unable to move or reverse.
- The transition step previously recorded a failed sweep/overlap and returned while leaving the motor permanently active. A progress-zero reversal was also validated as a collision step before it could exit to the source, and only the source-opposite direction counted as reverse input.
- A blocked first step now cancels immediately to the source. A later blocked forward step is latched instead of retried every physics frame, preserving reversal along the validated path.
- Either the opposite source direction or the opposite destination direction reverses a transition, so escaping does not require an unintuitive key combination at the midpoint.
- If an already-travelled reverse step becomes blocked by changed geometry, the motor cancels onto a currently detected source/target surface or explicitly detaches at its last valid physical pose.
- A rejected transition pair is not retried with unchanged held input. Releasing or changing input clears the suppression.
- Source and destination colliders are ignored only by the redundant movement sweep because they are intentional support contacts; inset pose-overlap validation remains active to prevent penetration.

The user subsequently confirmed that the original deadlock no longer reproduces in Play Mode. Broader ceiling and corner-route acceptance is still separate pending work.

### Auto Step-Over

- Continued tangent input near a support edge starts an explicit controlled crossing only when a same-type, same-normal landing surface is detected across an opening no wider than the ant's configured capability.
- The player prefab defines one prototype tile as one world unit and limits this ant to one tile. Openings wider than that limit and openings without a destination are rejected.
- Floor, ceiling, and vertical-wall openings use the same surface-relative detector; there is no floor-only jump path.
- A strong perpendicular input wins over automatic crossing, allowing later explicit opening entry or junction behavior without competing state changes.
- The ant has no ordinary `SurfaceType` while crossing. Gravity and ordinary attachment movement are replaced by a prevalidated straight `Rigidbody2D.MovePosition` path between real supports; collision is never disabled and no Transform teleport is used.
- The whole inset capsule is checked at 24 poses before entry. A collider cast and pose overlap are repeated for every movement step so a blocking obstacle cannot be bypassed.
- Releasing input pauses progress. Opposite tangent input reverses toward the source. A new block at the first step cancels to the source; a later block preserves the already validated reverse route, with safe surface recovery or explicit detachment if changed geometry blocks both directions.
- Rejected geometry is not queried again every physics tick while the same direction remains held. Input release/change or `NotifyTraversalGeometryChanged()` permits a fresh query.
- Read-only diagnostics expose active state, progress, measured gap start/end and distance, configured maximum distance, and a specific rejection reason. Optional gizmos show the measured opening and controlled path.

### Attachment and debugging

- Attachment states now include `None`, `Floor`, `LeftWall`, `RightWall`, and `Ceiling`, plus explicit active corner-transition and Auto Step-Over states.
- Gravity is disabled only while a real surface or prevalidated connected-corner path is active. Losing support restores the Rigidbody2D's authored gravity without teleportation.
- Fixed-size hit, candidate, and overlap arrays avoid routine movement allocations.
- Optional gizmos show all four direct probes, input intent, candidate points/normals, the selected attachment, movement direction, active corner path, and the latest rejected transition point.
- Read-only diagnostics expose transition source, destination, progress, accepted direction, and the last failure reason. Optional `Log Transition Events` output reports state changes and failures without per-frame logging.

## Authored prototype course

`AntMovementPrototype.unity` now contains:

- the existing climbable floor;
- left and right climbable walls;
- a full-width climbable ceiling, forming a continuous enclosed rectangular loop;
- a floor-connected central block whose upper edges expose outside corners; and
- the player ant starting left of the central block with room to approach and reverse;
- separate one-tile floor, ceiling, and vertical-wall opening fixtures;
- a two-tile floor opening that must be rejected; and
- a one-tile opening with a non-climbable obstacle at its landing path.

`Milestone1PrototypeSetup` updates or regenerates these named objects without duplicating them. The Editor menu now also provides `Apply Auto Step-Over Course` and `Validate Auto Step-Over Course`; the older surface-transition and wall-climbing commands remain compatible.

## Automated validation performed

- Unity compiled the updated runtime and Editor assemblies successfully.
- The Editor migration serialized the ceiling course, central outside-corner block, new starting position, and new motor configuration.
- `ValidateSurfaceTransitionPrototype` passed, confirming prefab wiring, the climbable mask, rotatable Rigidbody2D, player scene instance, enclosed floor/wall/ceiling course, and outside-corner block.
- Repository whitespace validation passed with `git diff --check` after Unity-generated whitespace was cleaned up.
- A focused `AntColony.EditModeTests` Unity Test Framework assembly was added without moving the runtime scripts out of Unity's existing predefined assembly.
- The focused Edit Mode test passed 1/1. `AntSurfaceMotorRegressionValidation` exercised the actual motor transition method and covered release/pause, both intuitive reversal directions, blocked-first-step cancellation, later-step forward blocking, blocked-reverse safe cancellation, failure-reason recording, retry suppression, and suppression reset after input release.

These checks validate compilation and authored structure. The new ceiling and transition behavior has not been visually verified in Play Mode.

For the Auto Step-Over slice:

- Unity loaded the updated runtime and Editor assemblies without C# compilation errors during two command-line launches.
- Static authored-scene checks confirmed all 11 new fixture roots exist exactly once, all 74 YAML document identifiers are unique, the prefab serializes a one-world-unit tile and one-tile crossing limit, and `git diff --check` passes.
- A second Unity Test Framework case and `ValidateAutoStepOverRegression` were added. They exercise real floor/ceiling/wall detection, release pause, reversal to source, over-wide rejection, blocked-path rejection, failure reasons, and retry-suppression release.
- The current machine's Unity Licensing Client repeatedly lost its IPC connection before the migration method or test runner could execute. Consequently, the new 2/2 Edit Mode result, Editor structural validator, and automated scene import remain **not run**, not passed.

For the Milestone 2.1B world-data slice:

- Focused Edit Mode tests cover all required coordinate examples, negative boundaries, cell/world round trips, explicit and duplicate chunk creation, 1,024-cell Background coverage, optional Solid mutation, invalid IDs, read/mutation isolation, four-direction expansion, and the absence of gameplay GameObjects.
- The focused Unity suite passed 25/25. The user also confirmed that the existing ant movement remained functional in Play Mode.

For the Milestone 2.1C navigation-classification slice:

- Focused Edit Mode coverage was added for occupancy, all four support directions, diagonal rejection, independent material flags, positive and negative chunk boundaries, all quadrants, unavailable-neighbour uncertainty, support precedence, live mutation results, isolation, and scene independence.
- The user-confirmed acceptance run passed 50/50 Edit Mode tests. The current full run passed 61/61, including all 23 `WorldNavigationClassifierTests`; Milestone 2.1C is complete.

For the Milestone 2.1D navigation-visualisation slice:

- Unity imported and compiled the new `AntColony.NavigationDebug` runtime assembly, its custom Editor tooling, the prototype scene, and the expanded Edit Mode tests.
- The structural validator passed the authoritative eight-chunk fixture, bounded 3,564-cell overlay, representative classification examples, and absence of 2D or 3D colliders.
- The complete Edit Mode suite passed 61/61: 25 world-data, 23 classifier, 11 visualisation, and 2 movement-regression cases.
- The user confirmed all 11 graphics-enabled manual acceptance checks, including colours, labels, boundaries, toggles, mutation refresh, and preservation of the original ant movement. Milestone 2.1D is complete.

For the Milestone 2.1E world-Tilemap slice:

- Unity compiled `AntColony.WorldPresentation`, the expanded Editor tooling, and the updated tests without errors.
- The scene validator passed the one-root hierarchy, two material-driven Tilemaps, all 8,192 resident Background visuals, foreground ordering, signed coordinate alignment, remove/restore refresh sequence, unchanged classifier behavior, and absence of colliders or terrain Rigidbody2D components.
- The complete Edit Mode suite passed 89/89: the existing 61 regression cases plus 28 new Tilemap projection cases.
- The new cases cover both material layers, foreground removal with Background persistence, non-blocking and differently identified Solid visuals, missing references and unknown IDs, missing chunks, positive and negative boundaries, exact cell refresh scope, bulk negative-chunk refresh, repeated refresh object stability, classification isolation, and collision absence.
- The user confirmed all 10 graphics-enabled checks for visual alignment, layer ordering, visibility toggles, removal/restore behaviour, Background persistence, classifier refresh, and unchanged original ant movement. Milestone 2.1E is complete.

For the Milestone 2.2A terrain-collision slice:

- Unity compiled the collision projection, custom Editor tooling, generated assets, updated prototype scene and focused tests without C# errors.
- The scene validator passed the single collision-only Tilemap hierarchy, collider-free visual layers, material references and independent flag examples, `ClimbableSurface` assignment, static composite configuration, signed boundary alignment, mutation refresh, Background persistence, classifier isolation and duplicate-hit check.
- The complete Edit Mode suite passed 117/117: the existing 89 regression cases plus 28 new collision cases.
- The new cases exercise real Physics2D queries for blocking and non-blocking materials, all four blocking/support flag combinations, add/remove/replace mutations, merged neighbours, immediate explicit synchronization, requested-region isolation, unavailable chunks, repeated-refresh component stability, integer-centred cells and positive/negative X and Y chunk boundaries.
- The user confirmed all 12 graphics-enabled manual collision checks. Milestone 2.2A is complete.

For the Milestone 2.2B ant-clearance slice:

- Unity compiled the authoritative clearance API, Scene-view inspector and focused tests without C# errors.
- The corrected focused clearance suite passed 38/38, covering physically clear unsupported Sky, a support-free unprepared shaft, prepared/unprepared Background neutrality, complete-body overlap, four foreground material-flag combinations, blocked-over-unavailable precedence, horizontal/vertical/intermediate rotation, rotated offset, margin, narrow passages, concave corners, tangency tolerance, all signed chunk boundaries and quadrants, four-chunk footprints, missing target/footprint chunks, live mutations, and scene/physics independence.
- The scene validator passed the authored ordinary-ant 0.96×0.3 effective profile, Clear test pose, and absence of gameplay physics components on the inspector.
- The complete Edit Mode suite passed 155/155: all 117 pre-clearance regression cases plus 38 clearance cases. The existing movement regression fixture also passed separately at 2/2.
- Physical clearance now matches GDD v3.2 and is independent of Background grip/grounding eligibility. The user confirmed all 9 graphics-enabled manual clearance checks; Milestone 2.2B is complete.

For the Milestone 2.2C movement-capability slice:

- Unity compiled the Background properties, movement-capability evaluator, debug inspector, scene authoring and focused tests without C# errors or warnings.
- The focused capability suite passed 37/37, covering all four Background configurations, floor-only grounding, wall/ceiling/non-blocking rejection, grounded walking, prepared grip, unsupported falling, voluntary-versus-gravity descent, clearance separation, controlled Background replacement, signed chunk boundaries and all quadrants, unavailable data and scene/physics independence.
- The scene validator passed the four authored Background definitions, capability inspector, Falling-to-Gripping-to-Falling replacement round trip, explicit Tilemap refresh, unchanged classifier examples and absence of extra physics components.
- The complete Edit Mode suite passed 192/192, and the existing movement regression fixture passed separately at 2/2.
- Manual graphics-enabled Scene-view checks remain pending, so Milestone 2.2C is implemented and automated but not complete.

For the Milestone 2.4A natural-background realignment:

- Natural Earth, Natural Stone, Prepared and Sky properties are covered directly, including normal `1.0x` natural speed and provisional `1.25x` Prepared speed.
- Route tests cover natural horizontal/up/down/diagonal movement, equivalent Stone behavior, blocking foreground, explicit non-grippable space, missing chunks and diagonal corner rejection.
- Runtime tests cover Grounded, Gripping and Falling transitions, effective Prepared speed, blocker/support invalidation, Sky grip loss, recovery and collision-backed landing.
- Unity compilation passed. Focused Edit Mode tests passed 56/56, the complete graphics-enabled Edit Mode suite passed 227/227 and Play Mode tests passed 6/6. `git diff --check` is clean.
- Manual Play Mode acceptance remains pending and is not implied by these automated results.

### Milestone 2.2C manual Scene-view checklist

1. Open `AntNavigationPrototype.unity`, select `Ant Movement Capability Inspector`, and confirm the default Natural Earth chamber-centre result is physically Clear, ungrounded, Gripping and gravity-ineligible at `1.0x` speed.
2. Click `Toggle Prepared Background, Refresh, and Requery`; confirm the selected cell changes visual, reports the Prepared material ID, retains horizontal/up/down grip and changes the speed multiplier to the provisional `1.25x`.
3. Toggle again; confirm the Natural Earth visual, Gripping result and `1.0x` speed return immediately.
4. Move the handle to a clearance-valid pose exactly `0.15` units above a floor top; confirm Grounded while the natural Background still permits its configured directions.
5. Place the handle beside a wall and below a ceiling without a floor; confirm neither produces Grounded.
6. Place the capsule over blocking foreground; confirm `BlockedByTerrain` and no valid movement state.
7. Move the handle into a missing chunk or across a missing footprint boundary; confirm an explicit unavailable result rather than Falling or Gripping.
8. Confirm the existing navigation-classification overlay does not change when only Background preparation changes.
9. Confirm the Console remains clean and the original `AntMovementPrototype` movement still behaves unchanged.

## Manual Play Mode checklist

1. Confirm the previously working floor movement, stopping, reversal, and both wall directions still behave identically.
2. Approach a wall using A/D only; confirm collision stops the ant without an automatic climb.
3. Press W near the wall; confirm floor-to-wall rotation and movement remain smooth and collision-safe.
4. Climb toward the ceiling using W only; confirm the ant stops without arbitrarily turning left or right.
5. At the wall/ceiling corner, press the direction leading into the room; confirm a wall-to-ceiling transition and approximately 180-degree ceiling alignment.
6. Use A/D along the ceiling; confirm normal speed, stopping, and reversal.
7. Press the appropriate W/S direction at each ceiling end; confirm ceiling-to-wall transitions in both directions.
8. Descend to the floor and deliberately select the valid A/D floor direction; confirm wall-to-floor transition.
9. Climb a side of `Outside Corner Block`, then use diagonal intent toward and around an upper edge; confirm a smooth outside-corner transition to its top.
10. Traverse the opposite outside corner from the block top onto its side, then reverse midway through a transition.
11. Try choosing the solid-facing or unavailable direction at inside and outside corners; confirm the transition is rejected without clipping, teleporting, or falling through terrain.
12. Release input midway through and after transitions; confirm the ant stops and remains supported.
13. Enable `Show Debug Gizmos` and inspect candidate normals, selected contact, orange transition path, and any red rejected point.
14. Confirm the Console contains no errors or warnings throughout repeated clockwise and counter-clockwise loops.
15. Reproduce the original deadlock path, then try release, the opposite source direction, and the opposite destination direction; confirm transition state always pauses, reverses, completes, or cancels instead of permanently consuming input.
16. On `One Tile Floor Approach`, hold the tangent direction and confirm the ant crosses continuously to `One Tile Floor Landing` without jumping, falling, or changing speed unexpectedly.
17. Repeat across `One Tile Ceiling Approach`/`Landing` and `One Tile Wall Lower`/`Upper` in both directions.
18. At a valid opening, press a strong perpendicular or diagonal input; confirm explicit directional intent takes priority and Auto Step-Over does not seize control.
19. Release input partway across a valid opening, then reverse; confirm the ant pauses and returns safely to its source support.
20. Approach the two-tile floor opening and the blocked landing; confirm both stop safely, do not oscillate or retry every frame, and become eligible for a new attempt after input release/change.
21. Enable `Show Debug Gizmos`; confirm the measured gap endpoints/path match the authored opening and rejection reasons are useful without per-frame Console spam.

### Milestone 2.4A manual acceptance checklist

No extra Inspector setup is required. Open `Assets/AntColony/Scenes/AntGameDevelopment.unity`, enter Play Mode, use left-click to select/command cells, `P` to toggle Natural/Prepared Background on an exposed selected cell and `F` to toggle foreground Solid occupancy.

1. Confirm exactly one yellow autonomous ant appears near X `-12`, reports `Grounded`, and can traverse the flat Natural Earth floor in both directions.
2. Command exposed Natural Earth cells above/beside the fixture; confirm upward, downward, horizontal and valid diagonal movement without preparation or teleportation.
3. Command through an exposed Natural Stone region; confirm the same directions and normal movement speed.
4. Use the prepared vertical strip/top region; confirm it permits the same directions and the panel reports effective speed `2.5` from base speed `2.0` (`1.25x`, provisional).
5. Toggle a Natural cell to Prepared with `P`, then traverse across the boundary; confirm movement remains valid and only effective speed/visual/material diagnostics change.
6. Attempt a diagonal route beside blocking foreground; confirm the ant does not cut the blocked corner or overlap terrain.
7. Add a foreground blocker with `F` during an active route; confirm the route becomes Blocked before entry. Remove it and confirm a new command can be accepted.
8. At the explicit Sky/grip-loss fixture, remove valid grip and support; confirm the active command is invalidated, the state becomes `Falling`, gravity moves the Rigidbody2D and no teleport/collision bypass occurs.
9. Restore or enter a valid Natural/Prepared grippable cell; confirm recovery to `Gripping` (or `Grounded` on the landing) without oscillation.
10. Toggle Background at runtime while moving; confirm capability, route validity, rendered tile and effective speed update immediately. Confirm a remote/missing-chunk destination is rejected readably.
11. Repeat Grounded, Natural Earth, Natural Stone, diagonal, Prepared-speed, transition, fall/recovery and blocked-route checks while confirming the Console remains free of relevant errors or warnings.

## Known limitations and remaining P1 work

- The original corner-deadlock reproduction is user-confirmed resolved; the wider ceiling/corner route still needs full Play Mode acceptance.
- The newly implemented ceiling and wider corner behavior is not yet Play Mode accepted and may require tuning of probe distance, look-ahead distance, collider inset, or rotation speed.
- Corners are intentionally limited to nearby, approximately perpendicular surfaces. Irregular or curved terrain is not supported by this slice.
- Outside corners require deliberate diagonal intent because both approaching the edge and selecting the outgoing surface must be unambiguous.
- The transition path is validated against current static collision geometry; runtime excavation and dynamic traversal invalidation do not exist yet.
- Auto Step-Over and its authored fixtures are implemented but have not completed automated Unity execution or manual Play Mode acceptance on this machine.
- The current Step-Over path is intentionally straight between parallel supports. Irregular, curved, offset, or routed openings remain unsupported.
- The ant-follow camera remains unimplemented; the integrated sample scene has only a developer-controlled inspection camera.
- World data, material visuals and authoritative terrain collision are connected in both the separate navigation fixture and the bounded integrated development scene. The integrated scene now demonstrates bounded direct grounded/gripping commands and gravity-driven falling, but persistence, general pathfinding, excavation gameplay and legacy DirtBlock conversion remain absent.
- The collision projection currently requires an explicit cell, chunk or bounded-region refresh after authoritative Solid mutation; no terrain-change event stream or chunk streaming lifecycle exists yet. Clearance itself reads world data directly and does not share that staleness risk.
- Static clearance does not validate Background permissions, grip, grounding, gravity, a swept movement segment, directional movement edge, route connectivity, Reachable Space, dynamic objects or ant-to-ant separation.
- Mathematical tangency is permitted within a `0.00001`-unit tolerance; future movement integration may use a small positive profile margin to remain conservative relative to Unity Physics2D contact handling.
- Background Material movement/grip/gravity/speed properties now drive the single development ant. General directional connection generation, detours, falling route planning, Surface Preparation jobs, resource consumption, skills and colony work are not implemented.
- Milestone 2.2C needs graphics-enabled manual Scene-view acceptance before it can be marked complete.
- The integrated sample-world and prior 2.3 acceptance remain historical evidence. Milestone 2.4A still needs the current natural-background, speed-transition, mutation, falling, landing and clean-Console checklist above.
- Full directional navigation, multi-ant occupancy and colony simulation remain later milestones.

## Recommended next implementation step

Complete the exact 3.1A manual checklist and wait for user acceptance. Keep the separate 2.4D and 2.2C manual statuses explicit; do not begin attributes, priority matrices, autonomous jobs, Surface Preparation, skills, Draft Mode or other colony work without separate approval.
