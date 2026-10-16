# Resource and Node System

## WV-B3C.2A acceptance status

Automated validation is complete and the user confirmed 20/20 supplied manual
summary checks. Because the summary text is unavailable for safe comparison
with the authoritative 23-step checklist, manual acceptance remains partially
confirmed. Presentation checks 18–23 require explicit reconciliation. No Unity
tests were rerun for this documentation-only update.

## Quantity badges

WV-B3C.2A continuously shows quantity for visible loose stacks and non-empty
legacy Material Storage, using each existing authority directly. Node badges are
contextual for scale: current quantity-one Colony Solids show `1` after
Extraction designation, while undesignated Nodes stay uncluttered.
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
| Soil | Implemented Colony resource; quantity-one compatibility Node/yield |
| Stone | Implemented Colony resource; Surface Stone is generation-only |
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
drops, Hauling, Construction and the `GenerationOnly` Surface records retain
their existing ownership.

Functional Backgrounds remain an active legacy compatibility representation
pending WV-B3D Placement migration. No gameplay behavior changed in the v1.0
documentation amendment.

The workflow remains **Foraging → Extraction → Hauling → Refining →
Construction**. Hauling alone transports physical material; Construction begins
work only after all requirements are staged.

`WV-B3B-LIM-001` remains active: the read-model pair publishes atomically, but
complete gameplay-scenario construction/restoration is not one transaction.
