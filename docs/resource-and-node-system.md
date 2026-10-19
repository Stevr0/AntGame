# Resource and Node System

## WV-B3D.3A Storage ownership

**Complete.** The authoritative Unity checklist passed 29/29 by user report;
the retained automated evidence passed with zero failures, skips or
inconclusive results. No tests were rerun for this documentation-only closure.

Authored Material Storage now has authoritative Placement identity, but the
existing `MaterialStorageWorld` slot remains the single physical ledger for its
filter, contents, capacity and reservations. No resource is copied or changes
owner during promotion. Nodes, loose stacks, ant inventory and Construction
staging remain separate authorities. Generated/new Storage stays legacy until
WV-B3D.3B.

## WV-B3C.3B — complete

Colony Soil 8–12 and Stone 5–15 Nodes are interactive. Whole-node Extraction
conserves the complete quantity into canonical stacks of at most 10, using
exclusive prepared capacity and exact-once operation identity. If every unit
cannot be placed, the Node and Solid remain intact. Surface resources and Water
interaction remain deferred.

Focused Edit/Play passed 43/43 and 22/22; complete Edit/Play passed 597/597 and
81/81 with no failures, skips or inconclusive results. The user confirmed all
**8/8 authoritative manual acceptance groups**; Codex did not independently
perform those checks or rerun Unity for this documentation-only closure.

## WV-B3C.3A — complete

The existing Colony registry is now the active Soil/Stone Node quantity
authority. Generated Soil contains 8–12 units and Stone 5–15, derived
deterministically per Node; Mara/Pip/Tala remains quantity one. Terrain still
owns Solid occupancy and Backgrounds, with guarded mutation preventing resource
loss. At B3C.3A closure, variable Nodes could be inspected and designated but
remained unowned by the legacy one-unit path. WV-B3C.3B now supersedes that
temporary gate with the whole-node transaction described above.

Runtime snapshots and designated badges read actual remaining quantity and Node
revision/lifecycle directly from the authority. Surface Nodes remain
`GenerationOnly`. Unity evidence passes 11/11 focused Edit, 16/16 focused Play,
594/594 full Edit and 81/81 full Play with no failures, skips or inconclusive
results. The user confirmed **25/25** authoritative manual Unity checks passed;
Codex did not independently perform them. Evidence under `Logs/WV-B3C3A-*`
came from the implementation working tree based on parent revision `1fab6541`.

## WV-B3C.2B — complete

The acceptance-recovery implementation coordinates one replay-safe,
single-destination run across existing physical owners. A run may reserve and
commit several compatible ground pickups before one delivery, or withdraw from
completed filtered Material Storage for Construction staging. Each pickup has a
separate operation identity; a positive ground remainder retains its Stack ID.

The implementation uses a bounded 24-candidate/eight-pickup route evaluation,
exclusive source and destination reservations, current-owner retention when
capacity is unavailable, fully staged Construction and committed-only quantity
badges. Storage never supplies Storage. Transfer-owned carrier allocation protects in-flight material,
replayed commits are no-ops, and scenario replacement invalidates old transfer
IDs. The approved capacity bands reach six slots × six base units at levels
90–100 and reuse the shared decay-above-50 rules without cargo loss. Production
Node yields are unchanged. After the initial partial pass and recovery, the user
confirmed the complete authoritative manual Unity checklist passed **30/30**.
See the authoritative
[`Partial Hauling Integration Plan`](https://github.com/Stevr0/AntGame/blob/main/Documentation/PartialHaulingIntegrationPlan.md).

## WV-B3C.2A acceptance status

**Complete.** Automated validation passed and the user confirmed all **23/23
authoritative manual checks passed**. No Unity tests were rerun for this
documentation-only closure.

## Quantity badges

WV-B3C.2A continuously shows quantity for visible loose stacks and non-empty
legacy Material Storage, using each existing authority directly. Node badges are
contextual for scale: active Colony Nodes show their authoritative remaining
quantity after Extraction designation, while undesignated Nodes stay uncluttered.
Generation-only Surface Nodes and unknown quantities are never shown as live
interactive resources. General Node selection, selected-site staging details,
Fog-of-War discovery and one-badge-per-future-Placement integration remain
deferred to the milestones that create those authorities.

The approved v1.0 design makes every Solid terrain cell a finite Resource Node.
Each Node has stable identity, typed location, a Resource Definition, its own
positive generated quantity, independent Extraction work, revision, provenance
and depletion state. Normal Solid Extraction removes the whole Node, preserves
the natural Background and commits the entire remaining quantity as recoverable
physical output.

The authoritative detailed contract is
[`Documentation/ResourceAndNodeSystem.md`](https://github.com/Stevr0/AntGame/blob/main/Documentation/ResourceAndNodeSystem.md).

## Approved boundaries

- Resource Definitions own material metadata; Node instances own actual
  quantity.
- Variable per-cell quantities are approved. Soil `8–12`, Stone `5–15`, Sand
  `8–14` and Clay `4–10` are provisional examples only.
- One loose resource type and at most one canonical stack may occupy a cell.
- Same-type stacks merge only to capacity; Hauling may split quantities; all
  transfers conserve exact physical ownership.
- Output exceeding one stack uses deterministic additional valid cells or a
  recoverable pending-output transaction. It is never discarded or inserted
  into inventory, Storage or Construction staging.
- Natural Backgrounds provide environmental/physical properties only. Nursery,
  Sleeping, Storage, Workshop, Refining, Doors and other colony functions are
  constructed Placements.
- Water is finite, including as a Solid Node. Its future loose representation
  must be liquid-compatible rather than assumed to be a Soil/Stone stack.
- Timed Surface events may later create new Nodes or loose stacks for insects,
  seeds, fruit, other food or rainwater; they do not regrow extracted terrain.

## Catalogue status

| Resource | Current status |
| --- | --- |
| Soil | Active generated Colony Node quantity 8–12; fixture quantity one |
| Stone | Active generated Colony Node quantity 5–15; Surface Stone generation-only |
| Seeds | Generated Surface record; interaction future |
| Twigs | Generated Surface record; interaction future |
| Food | Category; current fixture supply is not a generic material definition |
| Water | Generated/fixture records; finite natural-source interaction future |
| Sand, Clay, Bark, Leaves, Roots, Mushrooms, Insects | Approved future resources; no runtime definitions |
| Sticks | Unresolved possible overlap with Twigs |
| Other minerals/materials | Provisional examples only |

## Runtime compatibility

WV-B3C.1 is **Complete**. The user accepted its manual Unity checklist after
focused Edit `8/8`, focused Play `4/4`, full Edit `565/565` and full Play
`78/78` passed with no failures, skips or inconclusive results. It supplies only
an inactive quantity-one Soil/Stone registry. `ChunkedWorldData`, existing loose
drops, Hauling, Construction and the `GenerationOnly` Surface records retained
their ownership through that foundation. WV-B3C.3A subsequently activated the
same Colony registry as sole Node quantity owner without implementing the
WV-B3C.3B output transaction.

Functional Backgrounds remain an active legacy compatibility representation
pending WV-B3D Placement migration. No gameplay behavior changed in the v1.0
documentation amendment.

The workflow remains **Foraging → Extraction → Hauling → Refining →
Construction**. Hauling alone transports physical material; Construction begins
work only after all requirements are staged.

`WV-B3B-LIM-001` remains active: the read-model pair publishes atomically, but
complete gameplay-scenario construction/restoration is not one transaction.
