# Documentation Workflow

Documentation maintenance is part of milestone completion. It is not a separate optional cleanup task.

## Source hierarchy

1. Inspect code, authored Unity content, and actual test output.
2. Update `Documentation/CurrentGameState.md` with what is implemented and verified.
3. Update `Documentation/DevelopmentRoadmap.md` with status, evidence, and the next authorised action.
4. Update `Documentation/GameDesignDocument.md` only when the user approves a design change.
5. Synchronise the relevant pages in `docs/`.

If evidence conflicts, record the discrepancy. Do not convert a plan into an implementation claim or an automated result into manual acceptance.

## Per-change checklist

- [ ] Update the relevant system page in `docs/`.
- [ ] Update [Technical Architecture](technical-architecture.md) for ownership, data-flow, assembly, or scene-flow changes.
- [ ] Add an entry to the [Design Decision Log](design-decision-log.md) for a significant approved decision.
- [ ] Update [Development Roadmap](development-roadmap.md) and [Milestone History](milestone-history.md) when status or evidence changes.
- [ ] Link related pages so readers can move between design, architecture, and milestone context.
- [ ] Add screenshots or diagrams only when they show observed current behavior.
- [ ] Run a clean strict Zensical build.
- [ ] Report compilation, automated tests, and manual Unity acceptance separately.

## Status language

| Label | Meaning |
| --- | --- |
| Planned | Approved or proposed intent; no implementation claim |
| Implemented | Relevant code/assets exist; validation may still be incomplete |
| Automated tested | Named test runs passed; not a claim about unobserved visuals or input |
| Implemented — Pending Manual Acceptance | Implementation and relevant automation exist, but required Unity checklist is still open |
| Complete / Accepted | All milestone acceptance evidence, including required manual checks, is confirmed |
| Blocked | A required check could not run; record the exact blocker rather than claiming success |

## Local preview

From the Unity project root in PowerShell:

```powershell
.\.venv-zensical\Scripts\zensical.exe serve
```

Open `http://127.0.0.1:8000/`. The server rebuilds the wiki when Markdown or configuration changes.

For the same validation used by CI:

```powershell
.\.venv-zensical\Scripts\zensical.exe build --clean --strict
```

The generated `site/` directory, `.cache/`, temporary setup files, and `.venv-zensical/` are intentionally ignored by Git.

## Images and diagrams

- Store screenshots in `docs/assets/screenshots/`.
- Store hand-authored diagram assets in `docs/assets/diagrams/`.
- Prefer Mermaid for architecture flows that should remain text-reviewable.
- Include concise alt text and identify the Unity scene and milestone shown.
- Never use a future mock-up as evidence that a feature is implemented.
