# Sorophy™ v2.0.0 "Krono" — Temporal Query Domain (TQD)

## Overview

The **Temporal Query Domain (TQD)** is Sorophy Krono's read-only temporal query surface.

TQD answers structural and historical questions across temporal coordinates without mutating the graph, executing Event Entities, changing provenance, or becoming a semantic query language.

```text
Canonical Graph + Relationship History
                    │
                    ▼
             Temporal Query Domain
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
     Point       Interval     History
     Queries     Queries      Queries
                    │
                    ▼
               Provenance
```

TQD is an analysis surface over authoritative state. It is not a second graph engine.

---

## Architectural Boundary

TQD sits above the canonical graph and temporal history layers.

```text
SorophyGraph
     +
Relationship Histories
     +
SorophyTime
     +
Event Provenance
     │
     ▼
    TQD
     │
     ├── Point-in-time state
     ├── Temporal intervals
     ├── Relationship histories
     └── Event provenance
```

The underlying graph remains authoritative.

TQD never becomes the owner of graph state.

---

## Governing Question

A useful distinction in Krono is:

> **TQD asks: "What happened, or what was the structural state at a given time?"**

Graph Diff asks a different question:

> **"What is structurally different between these two states?"**

The two systems are intentionally complementary.

---

## Public Contract

The query subsystem is centered around:

```text
ITemporalQueryDomain
SorophyTemporalQueryDomain
SorophyGraph.Query
```

The API is deliberately read-only and focused on temporal inspection.

---

## Point-in-Time Queries

Point-in-time queries inspect graph state at a `SorophyTime`.

Conceptually:

```text
T
│
▼
Snapshot Semantics
│
├── Entity state
├── Relationship state
├── Outbound relationships
└── Inbound relationships
```

Point-in-time queries delegate to the same temporal reconstruction semantics used by Graph Snapshot.

This prevents the engine from maintaining two competing definitions of historical graph state.

Typical capabilities include:

- querying an entity at `T`;
- checking entity existence at `T`;
- querying a relationship at `T`;
- checking relationship existence at `T`;
- retrieving outbound relationships at `T`;
- retrieving inbound relationships at `T`.

---

## Temporal Interval Queries

TQD can inspect historical facts across a temporal interval:

```text
[T1 ---------------- T2]
        │
        ▼
Historical Facts
```

Interval queries operate over recorded relationship history.

They can answer questions such as:

- which historical facts occurred in an interval;
- which relationships were modified in an interval;
- which relationship IDs were affected;
- which facts belong to a temporal range.

Interval querying does not invent transitions that are absent from history.

---

## Relationship History

TQD exposes relationship-centric historical inspection.

A relationship's history is an append-only sequence of recorded facts.

```text
Relationship R
      │
      ▼
┌─────────────┐
│ Fact @ T1   │
│ Fact @ T2   │
│ Fact @ T3   │
└─────────────┘
```

Historical facts preserve the structural state relevant to each recorded evolution.

A history query therefore remains faithful to what Krono actually recorded.

---

## `At` Versus Validity

TQD preserves the distinction between structural evolution time and semantic validity.

```text
Fact.At
    = temporal coordinate of the recorded evolution

ValidFrom / ValidTill
    = semantic validity interval carried by relationship state
```

Structural existence queries use evolution time `At`.

`ValidFrom` and `ValidTill` are returned as relationship/fact data rather than being silently substituted for structural existence.

---

## Event Provenance Queries

TQD can query historical facts associated with Event Entities.

The key provenance field is:

```text
EventEntityId
```

This provides persistent attribution from an Evolution to its Event Entity.

TQD can therefore answer:

- which facts are associated with a given Event Entity;
- which relationships were evolved by an Event Entity.

The provenance relationship is structural metadata.

It is **not** an audit log and does not make Krono an event-sourcing system.

---

## Event Entities Remain Passive

TQD does not execute Event Entities.

```text
Event Entity
     │
     └── provenance / temporal anchor
                  │
                  ▼
                TQD
                  │
                  ▼
             Read-only result
```

Application semantics determine what an Event means and which Evolutions it causes.

TQD only inspects the resulting recorded structure.

---

## Co-Temporal Events

Multiple Event Entities may have the same `OccurredAt`.

TQD must not manufacture a causal ordering between them.

Likewise, the fact that a query returns a deterministic sequence does not mean that sequence represents temporal causality.

### Result Ordering

For deterministic multi-relationship query results, ordering may use:

1. `Fact.At`;
2. `RelationshipId`;
3. the fact's insertion index within the relationship history.

The `RelationshipId` tie-breaker exists only for repeatable presentation.

It does **not** mean:

- relationship A happened before relationship B;
- one Event caused another;
- the engine has discovered execution order.

Within a single relationship history, same-coordinate insertion order remains preserved as stored data.

---

## Read-Only and Isolation Model

TQD never returns mutable canonical graph objects directly.

Point-in-time results use isolated snapshot abstractions.

Historical results use immutable historical facts.

```text
Caller
  │
  ▼
 TQD
  │
  ├──── read ────► Graph
  │
  └──── read ────► History
  │
  ▼
Immutable / isolated results
```

Subsequent graph mutations do not alter already returned query results.

TQD itself performs no graph mutation.

---

## Determinism

Krono requires:

```text
Canonical State + Query Parameters
            │
            ▼
      Identical Result
```

Deterministic presentation is required across supported execution environments.

This is distinct from temporal causality.

The query engine may impose stable ordering so that identical state and parameters produce repeatable results without claiming that the ordering represents historical execution.

---

## Invalid and Unknown Input

TQD follows the broader Krono validation philosophy.

Examples:

- unknown entity IDs return no matching state;
- unknown relationship IDs return no matching history;
- unknown Event Entity IDs return empty provenance results;
- incompatible temporal coordinates fail explicitly rather than being guessed;
- invalid provenance is rejected by the evolution/provenance layer.

The query layer does not silently repair invalid temporal data.

---

## TQD and Snapshot

The boundary is intentionally clean:

| Question | Primary capability |
|---|---|
| What did the graph look like at `T`? | Snapshot / TQD point-in-time |
| What relationships existed at `T`? | TQD / Snapshot |
| What facts occurred in `[T1,T2]`? | TQD |
| What is the history of relationship `R`? | TQD |
| Which facts came from Event `E`? | TQD |
| What is structurally different between two states? | Graph Diff |

Point-in-time TQD queries use Snapshot semantics rather than maintaining an independent reconstruction algorithm.

---

## TQD and Graph Diff

TQD and Graph Diff intentionally stop at different boundaries.

```text
                 TQD
                  │
        "What happened?"
                  │
                  ▼
       Facts / History / State
                  │
                  │
                  ▼
             Snapshots
                  │
             ┌────┴────┐
             ▼         ▼
          State A    State B
             └────┬────┘
                  ▼
             Graph Diff
                  │
      "What is different?"
```

Graph Diff does not replace TQD.

TQD can provide the temporal context needed by an application to decide which snapshots it wants to compare.

---

## What TQD Does Not Do

TQD intentionally does not:

- mutate the graph;
- execute Event Entities;
- infer application semantics;
- infer causality;
- create synthetic history;
- invent temporal coordinates;
- provide an NLP query language;
- introduce LINQ expression trees or predicate frameworks;
- perform arbitrary multi-hop temporal path analysis;
- replace Graph Diff;
- create patches, merges, or undo operations;
- become an event-sourcing subsystem.

These boundaries keep TQD a focused temporal inspection layer.

---

## Verification

The TQD implementation added **26 tests** on top of the 560-test Snapshot baseline.

Milestone verification:

- **586/586 unit tests passed**;
- **0 failed**;
- **0 skipped**;
- build succeeded with **0 warnings and 0 errors** in the Engine;
- **13/13 stress campaigns passed**;
- **53/53 stress checks passed**;
- no serialization, temporal core, graph core, evolution semantics, provenance, or dependency changes were required for the TQD implementation.

Later Krono hardening expanded verification beyond this milestone baseline.

---

## Design Principle

TQD exists to make Krono's temporal model queryable without making it behavioral.

> **Krono records structure and evolution. TQD lets callers inspect that structure and evolution.**

The engine remains responsible for structural truth; applications remain responsible for semantics.
