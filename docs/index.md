# Ant Colony Development Wiki

Ant Colony is a small, finishable, single-player 2D side-view colony simulation built in Unity 6. The design centres on persistent individual ants, physical work, destructible terrain, and an authored-content-first development approach.

This wiki is a readable development companion to the repository's authoritative documents. It describes the confirmed design, the current implementation, and the evidence behind milestone status without treating planned work as finished gameplay.

<div class="status-grid" markdown>
  <div class="status-card" markdown>
  **Current accepted baseline**

  WV-B2B generated Colony workflow and scheduler fairness follow-up.
  </div>
  <div class="status-card" markdown>
  **Latest completed gate**

  WV-B3A runtime-cell architecture is specified; migration is not started.
  </div>
  <div class="status-card" markdown>
  **Engine**

  Unity 6000.3.8f1, 2D URP, C#, new Input System.
  </div>
</div>

!!! warning "Status vocabulary"
    **Planned** means approved design or roadmap intent, not code. **Implemented** means the repository contains the feature. **Automated tested** means named tests were executed successfully. **Accepted** means the required manual Unity checks were also confirmed. These labels are not interchangeable.

## Start here

- [Game Overview](game-overview.md) explains the vision, player role, and scope.
- [Gameplay Mechanics](gameplay-mechanics.md) maps the main loops and their current status.
- [Gameplay Architecture Audit](gameplay-architecture-audit.md) compares the approved five-stage workflow with the current runtime, tests, compatibility layers, and post-WV-B2B risks.
- [Ant Movement and Navigation](ant-movement-navigation.md) covers the most mature technical gameplay foundation.
- [Technical Architecture](technical-architecture.md) shows ownership and data flow.
- [Unified Runtime Cells](unified-runtime-cells.md) defines the approved WV-B3 migration contract and gates.
- [World View Foundation](world-view-foundation.md) records the documentation-only WV-A contracts and future acceptance gates.
- [Development Roadmap](development-roadmap.md) identifies the next authorised action and future milestones.
- [Milestone History](milestone-history.md) records implementation and validation evidence.
- [Design Decision Log](design-decision-log.md) summarises approved decisions and unresolved choices.

## Sources of truth

The wiki is derived from, but does not replace:

1. `Documentation/GameDesignDocument.md` for approved design intent.
2. `Documentation/CurrentGameState.md` for implemented and verified behavior.
3. `Documentation/DevelopmentRoadmap.md` for milestone ordering, scope, and acceptance gates.
4. The Unity assets, C# source, tests, and recorded results for repository reality.

If these disagree, contributors must report the discrepancy instead of silently choosing a version. See the [documentation workflow](documentation-workflow.md).

## Visual gallery

<div class="screenshot-slot">
Future screenshot: integrated development world, three selectable ants, functional Background tiles, and navigation diagnostics.
</div>

Place future images in `docs/assets/screenshots/` and diagrams in `docs/assets/diagrams/`, then link them from the relevant system page.
