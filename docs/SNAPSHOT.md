# Sorophy™ v2.0.0 "Krono" — Graph Snapshot

## Overview

Graph Snapshot is Sorophy Krono's **point-in-time temporal projection** capability.

A snapshot represents the observable structural graph reconstructed at a requested `SorophyTime` coordinate. It is a read-only, isolated representation derived from the canonical graph state and recorded relationship history.

```text
Canonical Graph
      +
Relationship History
      +
SorophyTime
      │
      ▼
Snapshot Materialization
      │
      ▼
ISorophySnapshot
```

Snapshots are projections, not a second source of truth.

---

## Architectural Role

Snapshot sits at the boundary between Krono's authoritative temporal graph and its read-only temporal analysis capabilities.

```text
                    Sorophy.Engine
                         │
        ┌────────────────┴────────────────┐
        │                                 │
 Canonical State                    Historical State
        │                                 │
        └────────────────┬────────────────┘
                         ▼
              Snapshot Materialization
                         │
                         ▼
                  ISorophySnapshot
                    │          │
                    ▼          ▼
                   TQD      Graph Diff
```

The canonical graph and historical fact store remain authoritative. Snapshot only materializes a view of that state at a temporal coordinate.

---

## Snapshot Semantics

### Point-in-Time

The primary API is:

```csharp
SorophyGraph.CreateSnapshot(SorophyTime time)
```

The resulting snapshot represents the graph as reconstructed at `time`.

Snapshot membership follows Krono's temporal evolution semantics:

- structures that exist at the requested coordinate are present;
- future relationship creations are absent;
- relationship termination at or before the requested coordinate removes the relationship;
- a relationship terminated after the requested coordinate remains present;
- baseline structures created directly without temporal history are treated as pre-existing and remain available across coordinates unless explicitly terminated;
- dangling relationships are pruned when their endpoints are not present.

The snapshot is **post-transition / inclusive** at the requested coordinate.

---

## Event-Based Snapshots

Krono also supports:

```csharp
SorophyGraph.CreateSnapshot(SorophyEntity eventEntity)
```

The supplied entity must be a valid Event Entity with an `OccurredAt` coordinate.

The Event's `OccurredAt` is the authoritative temporal coordinate:

```text
Event Entity
    │
    └── OccurredAt
           │
           ▼
     CreateSnapshot(...)
           │
           ▼
     Snapshot at T
```

Events do not execute themselves.

Two different Event Entities with the same `OccurredAt` therefore resolve to equivalent temporal snapshots. Krono does not invent an ordering between co-temporal events.

---

## Snapshot Contents

The public snapshot model exposes isolated representations of:

### Snapshot metadata

- requested temporal coordinate.

### Entities

Snapshot entities expose the observable entity state required for point-in-time graph inspection, including:

- identity;
- name;
- type;
- description;
- tags;
- documents;
- properties;
- Event classification and temporal occurrence data where applicable.

### Relationships

Snapshot relationships expose the observable structural relationship state, including:

- relationship identity;
- source and target entity identities;
- relationship type;
- properties;
- temporal validity data.

Adjacency inspection is available through the snapshot contract for outbound and inbound relationships.

---

## Isolation Model

Snapshots must never expose mutable canonical objects.

Materialization creates independent snapshot representations and deep-clones mutable nested values.

This includes nested structures such as:

```text
List<object?>
Dictionary<string, object?>
Tags
Documents
Property values
```

Therefore:

```text
Live Graph Mutation
       │
       X
       │
Existing Snapshot
```

Subsequent mutations to the live graph cannot alter a previously materialized snapshot.

Likewise, callers cannot use snapshot objects to mutate canonical graph state.

---

## Canonical State Remains Authoritative

A snapshot is not persisted as an alternate canonical graph.

```text
Canonical State
      │
      ├── current entities
      ├── current relationships
      ├── relationship histories
      └── retired relationship identities
             │
             ▼
       Snapshot Projection
```

The snapshot can be discarded and recreated from authoritative state.

This preserves the core Krono rule:

> Derived projections must never become the owner of structural truth.

---

## Relationship Reconstruction

Relationship state at `T` is reconstructed from canonical state together with applicable historical facts.

The reconstruction process accounts for the five relationship evolution operations:

1. Creation
2. Property Modification
3. Type Change
4. Validity Change
5. Termination

Historical facts provide the temporal transition information required to reconstruct the relationship's observable state.

Creation evolutions preserve an initial historical fact.

Direct graph insertion does not fabricate historical creation records.

---

## `At` and Semantic Validity

Snapshot reconstruction is based on the evolution coordinate `At`.

`ValidFrom` and `ValidTill` remain semantic validity attributes of the relationship state.

These concepts are intentionally separate:

```text
At
│
└── When the structural transition was recorded

ValidFrom / ValidTill
│
└── The relationship's semantic validity interval
```

A snapshot therefore does not silently reinterpret semantic validity as structural existence.

---

## Temporal Boundaries

Krono uses strict temporal comparison.

A snapshot request must use a compatible temporal coordinate according to the rules of `SorophyTime`.

The engine does not guess conversions between incompatible schemas, units, or numeric representations.

Invalid temporal comparisons fail explicitly.

---

## Determinism

Given identical canonical state, historical state, and temporal coordinate:

```text
Same State + Same T
        │
        ▼
Same Snapshot
```

Snapshot materialization is deterministic.

Co-temporal Event Entities do not introduce hidden ordering.

Deterministic result ordering is a presentation concern and does not imply causality.

---

## Public Contract

The snapshot subsystem is centered around:

```text
ISorophySnapshot
ISorophySnapshotEntity
ISorophySnapshotRelationship
SorophyGraph.CreateSnapshot(SorophyTime)
SorophyGraph.CreateSnapshot(SorophyEntity)
```

The implementation also uses a dedicated snapshot materialization boundary rather than exposing canonical graph internals directly.

---

## Relationship to TQD

TQD uses snapshot semantics for point-in-time queries.

```text
TQD
 │
 └── Point-in-Time Query
          │
          ▼
     Snapshot Semantics
```

TQD may also query relationship history, intervals, and Event provenance directly.

Snapshot answers:

> What structural graph state exists at this temporal coordinate?

TQD answers broader temporal questions around that state and its recorded transitions.

---

## Relationship to Graph Diff

Graph Diff consumes materialized snapshots:

```text
Snapshot A ──┐
             ├──► SorophyGraphDiff ──► Change Set
Snapshot B ──┘
```

This keeps Graph Diff independent of canonical graph internals and historical transition storage.

Graph Diff compares endpoint states; it does not reconstruct the transition history between them.

---

## What Snapshot Does Not Do

Graph Snapshot intentionally does not:

- execute Event Entities;
- infer application semantics;
- invent timestamps;
- fabricate history;
- provide causal ordering;
- create `SnapshotBefore` semantics;
- mutate the canonical graph;
- become an alternate source of truth;
- provide a query DSL;
- perform Graph Diff itself;
- infer domain meaning from graph structure.

---

## Verification

The original Snapshot implementation milestone was verified with:

- **23 targeted snapshot tests**;
- **560/560 total unit tests** at that milestone;
- **13/13 stress campaigns**;
- **53/53 stress checks**;
- build with **0 warnings and 0 errors**.

The Snapshot subsystem was subsequently exercised by later TQD, Graph Diff, and full Krono hardening suites.

The implementation reconciliation also removed design-draft fields that did not exist in the actual relationship model and omitted unnecessary `MaterializedAtUtc` metadata in favor of the smallest coherent public API.

---

## Design Principle

Graph Snapshot is deliberately simple in one important respect:

> **It materializes temporal state; it does not own temporal truth.**

That distinction keeps the projection layer composable with TQD, Graph Diff, persistence, and future Saga ecosystem capabilities without turning the engine into a monolithic temporal runtime.
