# Sorophy.Engine 2 Roadmap — Krono

Krono is being developed as an incremental second-generation engine rather than a single giant rewrite.

The roadmap below distinguishes what is already implemented from what remains and what is deliberately deferred.

## Completed

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

### Temporal Model

- `SorophyTime`
- Temporal schemas
- Temporal units
- Position definitions
- Precision
- Relationship validity
- Temporal schema consistency checks

### History

- `SorophyRelationshipFact`
- `SorophyRelationshipHistory`
- Graph-level relationship history storage
- Append-only fact recording
- Historical state capture before relationship mutations
- Permanent relationship identity retirement

### Relationship Evolution

- Creation
- Type change
- Property modification
- Validity change
- Termination
- `SorophyRelationshipEvolutionExecutor`

### Verification

- 469 / 469 deterministic unit tests passing

## Next: Temporal Projection

### 1. Graph Snapshot

Build a read-only materialized representation of graph state at a selected temporal point.

Desired conceptual API:

```text
SorophyGraph
   ↓
CreateSnapshot(T)
   ↓
SorophyGraphSnapshot
```

The snapshot should be immutable from the consumer's perspective and independent of later mutations to the live graph.

## 2. Query Layer

Expose higher-level read operations over graph and snapshot state.

Potential categories:

```text
Entity lookup
Relationship lookup
Tag lookup
Type filtering
Neighbor queries
Relationship filtering
```

## 3. Temporal Queries

Use the temporal model and history to answer questions such as:

```text
What relationships existed at T?
What was the state of relationship X at T?
What facts surround T?
```

## 4. Graph Diff / Change Sets

Compare two snapshots or temporal projections.

Conceptually:

```text
Snapshot(T1)
     ↓
    Diff
     ↓
Snapshot(T2)
```

Potential outputs include added, removed, and changed entities and relationships.

## 5. Validation Hardening

As the number of interacting subsystems grows, validation should cover:

```text
Graph invariants
Index invariants
Temporal invariants
History invariants
Evolution invariants
Serialization invariants
```

The goal is to make the engine capable of detecting inconsistencies in itself, not merely relying on happy-path method behavior.

## 6. Serialization Hardening

Expand persistence and round-trip verification for the richer v2 semantic model.

Particular attention should go to:

- Temporal values
- Relationship validity
- Historical facts
- Relationship history
- Evolution data
- Compatibility and migration
- Deterministic output
- Large documents

## 7. Cross-Platform Hardening

Maintain Sorophy.Engine as a platform-neutral .NET foundation.

The engine should not absorb UI assumptions or platform-specific behavior that belongs to applications.

## 8. Dedicated V2 Stress Program

After the architecture is sufficiently complete, execute a dedicated v2 stress/release-gate campaign covering the new temporal and evolutionary semantics.

The v1 stress arsenal remains a useful foundation but should not be treated as the final v2 release gate without reviewing its scope against the new features.

## Explicitly Deferred

### EventPkg

Not required for v2.

The current evolution objects and executor provide enough structure without introducing another orchestration wrapper.

### Whimsy / NLP

Deferred until the semantic engine is mature enough to provide a stable substrate for language-driven queries.

### Application-Specific UI

Sorophy.Engine remains UI-agnostic. Orbpad and other applications own presentation and workflow concerns.

### Broad Domain-Specific Semantics

The engine will not become a hard-coded worldbuilding, research, project-management, or game rules engine. Those domains should build vocabularies and policies on top of the core model.

## V2 Completion Philosophy

V2 should be considered complete when the engine can reliably represent and query structured information through time without requiring applications to reinvent graph state, relationship lifecycle, and historical semantics themselves.

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

These remain possibilities, not promises.
