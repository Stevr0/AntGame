# Resource and Node System v1.0

**Status:** Approved design; runtime migration is incomplete.

**Approved:** 16 October 2026

This specification is the authoritative design contract for natural resources,
Resource Nodes and physical loose resources. It supersedes earlier assumptions
that natural Backgrounds provide colony functions, that normal Soil/Stone
Solids permanently yield one unit, or that Freshwater is an unlimited or
automatically replenishing source. Where this target differs from the current
runtime, the compatibility implementation is identified explicitly.

Built improvements and Construction Site/Placement lifecycle are defined in
[`ConstructionPlacementSpecification.md`](ConstructionPlacementSpecification.md).
That companion contract keeps Placement definition metadata and changing
instance state separate from natural-resource authority.

## 1. Core model

Every resident cell has exactly one natural **Background**. A cell may also
contain a Solid, Construction/Placement, and one compatible loose-resource
stack according to the unified cell rules.

Every **Solid terrain cell is a finite Resource Node**. It has a specific
resource type and a positive quantity fixed during deterministic generation or
explicit creation. Extraction work and resource quantity are independent. For
normal Solid cells, completed Extraction removes/depletes the whole Node,
preserves its natural Background, and converts the entire remaining quantity
into recoverable physical output.

Not every Resource Node must be a Solid. Future non-terrain natural sources or
timed World View events may create a Node or a loose stack, but they use the
same stable identity, finite quantity and conservation rules.

## 2. Resource Definition contract

A reusable Resource Definition describes material properties, not the changing
quantity in a particular cell:

- stable Resource ID and display name;
- category;
- unit weight;
- physical stack capacity;
- default or allowed Node quantity range;
- compatible Storage types;
- compatible Extraction rules;
- Survival, Refining and Construction uses; and
- valid physical representations, including any required carrier or cache.

Node instances store their actual generated quantity. Resource Definitions may
provide balancing ranges, but changing a definition never rewrites an existing
Node's quantity.

## 3. Resource catalogue

The status column distinguishes approved design from verified implementation.
Generic **Food** is a category and need-facing concept, not an additional
implemented material unless a concrete food Resource Definition is approved.

| Resource | Classification | Current evidence and approved role |
| --- | --- | --- |
| Soil | Implemented gameplay resource; target model approved | Colony Soil Solids Extract into physical `material.soil`; current Nodes/yields are quantity-one compatibility. Future normal Soil Nodes have variable quantities. |
| Stone | Implemented Colony gameplay resource; generated Surface record | Colony Stone Solids Extract into physical `material.stone`; current Nodes/yields are quantity-one compatibility. Surface Stone exists only as `GenerationOnly` metadata/obstruction. |
| Seeds | Generated, non-interactive record; approved future resource | Deterministic Surface records use `resource.food.seed`; no runtime source mutation, loose parcel, hauling or consumption path exists. |
| Twigs | Generated, non-interactive record; approved future resource | Deterministic Surface records use `resource.material.twig`; no runtime Extraction or loose-resource path exists. |
| Food | Category / unresolved concrete identity | Development survival fixtures use `resource.food.development`; this does not establish a generic Food material. Future fruit, fungi, insects or seeds require concrete definitions. |
| Water | Approved finite future resource; generated, non-interactive Freshwater record | Development fixtures use `resource.water.development`; generated Surface records use `resource.water.fresh`. No runtime natural-source depletion, liquid output or hauling path exists. |
| Sand | Approved future Solid resource | Variable finite Solid Node and Refining input; no definition or runtime implementation exists. |
| Clay | Approved future Solid resource | Variable finite Solid Node for Construction/Refining; no definition or runtime implementation exists. |
| Bark | Approved future resource | Natural Construction/Refining material; source and physical representation remain to be specified. |
| Sticks | Unresolved overlap | Potential natural material. It is not silently merged with Twigs; identity and distinction require approval. |
| Leaves | Approved future resource | Potential timed drop or natural source; no runtime definition exists. |
| Roots | Approved future resource | Potential underground or surface source; no runtime definition exists. |
| Mushrooms | Approved future concrete food candidate | It must not be represented only by generic Food; source, use and renewability remain to be specified. |
| Insects | Approved future concrete food candidate | Timed World View appearance is approved in principle; capture, perishability and identity remain unresolved. |
| Other minerals/natural materials | Provisional examples only | No resource identity, balance or implementation is approved until named explicitly. |

## 4. Resource Node contract

Every Solid Node has:

- stable Node ID;
- typed region and cell location;
- Resource Definition ID;
- positive initial quantity and non-negative remaining quantity;
- Extraction work requirement and, where needed, work progress;
- obstruction/navigation properties;
- depletion state;
- revision; and
- generation or authored-fixture provenance.

A Node's actual quantity is fixed at generation/creation and changes only
through authorised gameplay transactions. Quantity cannot become negative.
Node work is not derived mechanically from quantity. Partial harvesting is
reserved for an explicitly authorised future Node definition; it does not
replace whole-node Extraction for normal Solid terrain.

### Variable quantities

The following values are provisional balancing examples, not runtime defaults:

| Resource | Example quantity range |
| --- | --- |
| Soil | 8–12 |
| Stone | 5–15 |
| Sand | 8–14 |
| Clay | 4–10 |

Generation selects the actual quantity through a named, versioned random domain
derived from the world seed, generation version, Resource Definition and stable
Node location/identity. The selection must not depend on iteration order or a
mutable shared random stream. Rebuilding the same generation contract produces
the same quantity; gameplay thereafter mutates the stored Node instance, not
the generator record.

## 5. Whole-node Extraction

Normal Solid Extraction follows one conserving transaction:

1. Select an eligible Node and validate typed location, access and ownership.
2. Reserve the job/Node and perform the required Extraction work.
3. Prepare capacity for the Node's complete remaining physical output.
4. Revalidate Node, terrain and output revisions.
5. Atomically deplete/remove the Solid Node and commit its entire remaining
   quantity into recoverable physical output.
6. Preserve the natural Background, publish coherent changes, release
   reservations and complete the job.

Extraction never fills ant inventory, Construction staging or Storage. It
awards the authoritative Extraction skill only after valid work. If output
cannot be represented, the Node is not depleted and its quantity is not lost.

## 6. Loose resources and overflow

Approved physical accounting rules are:

- one resource type and at most one canonical loose stack per cell;
- same-type quantities may merge only up to capacity;
- different types never share a ground cell;
- Hauling may split a stack into smaller reserved transport quantities;
- splitting, merging, movement and consumption conserve exact quantities;
- loose output persists when no valid destination or job exists; and
- Storage is optional and is never an Extraction prerequisite.

If a whole-node yield exceeds one stack's capacity, the transaction reserves a
deterministic set of valid additional output cells and commits all output as one
logical operation. Search tests the source cell first, then a bounded,
versioned order of compatible resident cells. Exact radius/candidate limits and
accessibility rules require approval during the implementation gate.

If complete placement cannot be prepared, retain a recoverable pending-output
operation holding the Node/output reservations. It must retry only after a
relevant revision or explicit recovery action. Excess is never discarded,
placed in inventory, or silently sent to Storage/staging. Operation identity is
separate from stable stack identity so replay cannot duplicate output.

## 7. Natural Backgrounds and constructed functions

Backgrounds are natural/environmental only. They define presentation and
supported physical/environmental properties, remain after Solid removal, and
remain behind Constructions/Placements. They are not stacks and do not provide
Storage, Nursery, Sleeping, Workshop, Refining, production or burial function.

All colony functions arise from constructed or explicitly placed improvements,
including Sleeping Spots, Nurseries, Storage, Workshops, Refining Stations and
Doors. Enclosure and room quality may modify a Placement; they do not convert a
natural Background into the functional owner.

The current functional-Background implementation is preserved as **legacy
compatibility pending WV-B3D migration**. Existing Construction, Storage,
Nursery, Sleeping and survival behaviour must remain working while each
function, contents, reservations and rollback path migrates to a Placement.

## 8. Finite Water Nodes

Water is a finite Resource Node, including when represented as a Solid. It has
stable identity, finite quantity, typed location, Extraction rules, revision and
depletion. Initial gameplay must not provide unlimited extraction or implicit
replenishment. A Solid Water Node follows whole-node depletion.

Extracted Water requires a liquid-compatible physical carrier/cache; it must
not be assumed to behave like an ordinary Soil/Stone stack. Direct drinking and
Extraction are distinct interactions but both must reserve and decrement the
same finite authoritative source exactly once. The unresolved design question
is whether direct drinking consumes units without removing a multi-unit Solid,
or whether direct drinking is unavailable for a whole-node-only Solid Water
definition. That decision must be approved before Water implementation.

## 9. Future timed World View resources

Future environmental events may create insects/bugs, fallen seeds or fruit,
other natural food, and rain-created finite water deposits. An event creates a
new Node at a valid cell or a new physical loose stack; it never implicitly
regenerates extracted terrain.

Each event validates cell compatibility, resource identity, quantity,
placement, ownership, interaction access, conservation and stable/deterministic
event identity where applicable. Timers, rain, food spawning, renewability and
decay remain future gameplay.

## 10. Five-stage workflow

The conceptual workflow remains:

**Foraging → Extraction → Hauling → Refining → Construction**

- Foraging discovers resources and explores. Its approved future skill effects
  increase visibility radius and reduce exploration fatigue in both regions;
  Fog of War and underground deposit sensing remain future systems.
- Extraction converts finite Nodes into physical output.
- Hauling alone moves loose/carried resources to Construction staging, Storage
  or future Refining stations.
- Refining transforms physical inputs into physical materials/items and remains
  future gameplay.
- Construction consumes fully staged requirements and performs building work;
  it never self-supplies.

Not every resource must pass through every stage.

## 11. Current runtime compatibility and migration

### WV-B3C.1 — complete

WV-B3C.1 is complete after automated validation and user-confirmed manual Unity
acceptance. It provides an **inactive** Colony Soil/Stone registry with stable
typed IDs, immutable quantity-one snapshots, provenance, revision and lifecycle
diagnostics. `ChunkedWorldData` remains the live terrain/effective quantity-one
authority. The registry does not own active quantity, mutate terrain, reserve
work or produce output. Surface Nodes remain `GenerationOnly`.

The quantity-one registry is a transitional foundation, not the final v1.0
Resource Node model.

### WV-B3C.2A — complete

The existing `WorldMaterialDropRegistry` is now the canonical physical
Soil/Stone ground-stack authority. It owns one stack and one resource type per
Colony cell, stable scenario-local Stack ID, quantity, provisional
resource-defined capacity 10, exclusive reservation and available quantity,
revision, provenance and lifecycle. Same-resource commits merge under the
existing identity. Mixed types, overflow and invalid/unknown ordinary runtime
placement are rejected without consuming the incoming source. The existing
explicit inventory-drop command keeps its validated current-anchor legacy
functional-Background behavior until Placement migration; this narrow
compatibility path is not available to ordinary producers. Empty removal terminates
the ID; cell reuse creates a new ID; scenario replacement creates a new context.

The unified-cell snapshot reads this registry directly. Ant inventory,
Construction staging and Material Storage remain distinct capacities and
authorities. Current quantity-one Node Extraction, partial pickup/Hauling and
Construction behavior are preserved. B3C.2B multi-cell overflow recovery and
B3C.3 variable Node authority/transaction cutover remain unimplemented.

The user confirmed all **23/23 authoritative manual checks passed** after the
recorded automated suites passed. WV-B3C.2A is complete; this documentation-only
closure did not rerun Unity tests.

### WV-B3C.2B — implementation-ready planning; runtime unstarted

The approved physical rules above are elaborated by the documentation-only
[Partial Hauling and Canonical Stack Transaction Plan](PartialHaulingIntegrationPlan.md).
It proposes a scenario-local transfer identity distinct from Stack, job, ant and
producer-operation identity. One exact transfer quantity is bounded by available
source, compatible ant capacity and reserved destination capacity; the source
keeps its Stack ID while positive. Pickup and delivery become replay-safe phase
commits while physical quantity remains in the current ground, inventory,
Storage or Construction-staging authority.

The proposed first implementation retains exclusive source/destination
reservations and performs multiple large-stack moves as successive one-trip
jobs. Same-type merges, capacity rejection and current-owner retention on no-fit
remain authoritative. Prepared deterministic multi-cell placement supports
conserving recovery/tests without enabling variable Node output. Runtime code is
unchanged, and B3C.2B is not complete or authorised by this plan.

### Quantity-badge presentation contract

Visible loose stacks show their current quantity continuously and disappear at
zero. Visible completed Material Storage compatibility cells show actual stored
quantity continuously, with zero hidden. Node quantities are intentionally
contextual for scalability: display only after selection or Extraction
designation. WV-B3C.2A supports designated quantity-one Colony Solids; a general
Node-selection authority does not yet exist. Generation-only Surface Nodes and
unknown quantities never appear as live interactive quantities. Badge reads do
not mutate quantities or revisions, and Colony presentation is gated off in
World View.

Selected Construction Site staging (`Resource delivered/required`) is approved
but deferred because there is no current site-selection authority. Future Fog
of War must suppress undiscovered Node quantities. Placement migration must
retain one Storage badge per Placement identity rather than per footprint cell.

### Remaining smaller future gates

WV-B3C.2 and WV-B3C.3 are too broad if each attempts Resource Definitions,
variable Node authority, stack canonicalisation, multi-cell overflow and the
Extraction cutover together. Split future approval into independently testable
gates:

1. **B3C.2A — Resource Definitions and canonical stack authority (complete):** introduce
   Soil/Stone definitions, stable Stack IDs, one-stack/type/capacity validation,
   revisions and legacy-drop migration without changing Extraction yields.
2. **B3C.2B — deterministic placement and pending-output recovery:** add bounded
   multi-cell capacity reservation, overflow placement, replay protection and
   recovery while retaining quantity-one Nodes.
3. **B3C.3A — active variable-quantity Colony Nodes:** make the registry the sole
   Soil/Stone quantity owner and project Solid occupancy without changing
   Surface resources.
4. **B3C.3B — whole-node Extraction cutover:** coordinate Node, terrain and
   canonical stack changes atomically, then retire the quantity-one compatibility
   path after full conservation/regression acceptance.

Surface Nodes, finite Water interaction, functional-Background migration,
Foraging, Refining and timed spawning remain separate later milestones. These
gate names are planning recommendations, not implementation authorisation.

## 12. Invariants and retained limitation

- No Solid exists without a Resource Definition and positive Node quantity in
  the target model.
- A successful whole-node Extraction produces exactly the recorded remaining
  quantity and preserves the Background.
- Node, stack, inventory, staging, Storage and consumption transfers conserve
  quantities and commit exactly once.
- Failed preparation/placement loses nothing.
- Existing authoritative gameplay owners remain in force until their explicit
  migration gate passes.
- `WV-B3B-LIM-001` remains: atomic replacement of the Colony/Surface unified
  read-model pair is implemented, but complete gameplay-scenario construction,
  restoration and publication are not a whole-world transaction.

## 13. Unresolved approvals

The following must not be chosen silently in implementation:

- final Resource Definition weights, stack capacities and quantity ranges;
- Twig versus Stick identity;
- concrete Food resource types and recipes;
- exact overflow search bounds, accessibility and reservation lifetime;
- liquid carrier/cache capacity and presentation;
- direct drinking behavior for a Solid whole-node Water source;
- timed-event cadence, determinism, renewability and decay; and
- Surface Node interaction/navigation invalidation and persistence policy.
