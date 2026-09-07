# Sorophy.Engine 2 Roadmap — Krono

Krono is developed as an incremental second-generation engine rather than a single giant rewrite.

The roadmap distinguishes the capabilities that are part of the current stable foundation from areas that may shape future versions. Post-v2 directions are intentionally kept broad: future features will be designed only when their principles and boundaries can be accommodated cleanly within the existing architecture.

## V2.0.0 — Krono — Completed

### Core Graph

- Entity storage
- Relationship storage
- Referential integrity
- Adjacency management
- Relationship indexing
- Tag indexing
- Traversal and reachability
- Relationship identity retirement

### Entity 2.0

- Expanded entity semantics
- Description support
- Tags
- Embedded structured content
- Event classification through entity type
- Event temporal anchoring through `OccurredAt`

### Temporal Model

- `SorophyTime`
- Temporal schemas
- Temporal units
- Position definitions
- Precision
- Relationship validity
- Strict temporal schema consistency checks
- Explicit distinction between `At`, `ValidFrom`, and `ValidTill`

### Event Entities

- First-class Event Entities
- Passive/declarative temporal anchors
- `OccurredAt`
- Event provenance through `EventEntityId`
- Event-anchored Evolution semantics
- No automatic Event execution

### History

- `SorophyRelationshipFact`
- `SorophyRelationshipHistory`
- Graph-level relationship history storage
- Append-only fact recording
- Historical state capture around relationship Evolutions
- Event provenance in historical facts
- Permanent relationship identity retirement

### Relationship Evolution

- Creation
- Type change
- Property modification
- Validity change
- Termination
- `SorophyRelationshipEvolutionExecutor`

### Temporal Projection

- Read-only graph snapshots
- Event-based snapshot creation
- Temporal Query Domain (TQD)
- Point-in-time and interval/history queries
- Event provenance queries
- Structural Graph Diff

### Persistence

- `.lore` v2
- Temporal state persistence
- Historical relationship persistence
- Retired relationship identity persistence
- Event provenance persistence
- Validation during deserialization
- Deterministic serialization behavior
- Cross-platform persistence verification

### Hardening and Verification

- Full v1 + v2 hardening suite
- Adversarial temporal/evolution testing
- Metamorphic verification
- Determinism and repeatability testing
- Corruption/invariant testing
- Multi-scale soak/endurance verification
- Cross-platform CI
- Cross-platform persistence portability verification

### Release Verification

The Krono stable release is backed by the project's stability and verification standards, including deterministic unit verification, adversarial stress campaigns, persistence portability, and endurance testing.

The current stable verification record is maintained by the release documentation and verification reports. The roadmap records the completed verification scope rather than treating individual test counts as feature milestones.

## Explicitly Deferred

### EventPkg

Not required for the Krono core.

The current Event Entity, Evolution objects, and Evolution Executor provide the required structural boundaries without introducing another orchestration wrapper.

A higher-level orchestration abstraction may be considered later if a concrete ecosystem requirement justifies it.

### Whimsy / NLP

Deferred until the semantic and temporal substrate is mature enough to provide a stable foundation for language-driven interaction.

### Application-Specific UI

Sorophy.Engine remains UI-agnostic. Orbpad and other applications own presentation, workflow, and user-experience concerns.

### Broad Domain-Specific Semantics

The engine will not become a hard-coded worldbuilding, research, project-management, or game-rules engine. Those domains should build vocabularies and policies on top of the core model.

## V2 Completion Philosophy

Krono v2 is complete as a coherent Temporal Graph Evolution Core when its implemented capabilities reinforce one another without requiring applications to reinvent graph state, relationship lifecycle, temporal semantics, and historical state.

The goal is not maximum feature count.

The goal is a **coherent engine whose features reinforce one another instead of becoming a pile of unrelated conveniences.**

## Beyond V2

Potential post-v2 directions include:

```text
Event orchestration packages
State reconstruction / replay
Advanced graph algorithms
Broader temporal analysis
Domain adapters
Whimsy / natural-language querying
Additional Saga ecosystem services
```

These are deliberately **directions rather than commitments**.

Future features will be evaluated and designed against the existing Krono architecture, invariants, and design principles. The roadmap will be updated when the principles and designs for those capabilities are sufficiently mature to integrate cleanly with the architecture.

Until then, post-v2 items should be treated as possibilities rather than promises.
