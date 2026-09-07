<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="Sorophy v2 · Krono"
     width="900"/>

<p><strong>SOROPHY V2 · KRONO</strong></p>

<h1>Historical State Model</h1>

<p>
Krono Temporal Graph Evolution Core
</p>

</div>

This document defines the **Krono Historical State Model**, the relationship-history semantics implemented by **Sorophy.Engine 2.0.0**.

## Purpose

Krono treats relationship history as explicit engine state.

The purpose of history is to preserve recorded temporal facts about relationship state. It is not an undo stack, automatic edit log, or event-sourcing system.

```text
Current Graph State
       +
Recorded Historical Facts
       │
       ▼
Temporal State Model
```

## SorophyRelationshipFact

A historical fact records an immutable relationship state at a temporal point.

Conceptually:

```text
SorophyRelationshipFact
├── At
├── RelationshipId
├── SourceId
├── TargetId
├── Type
├── Properties
├── ValidFrom
├── ValidTill
└── EventEntityId?
```

### `At`

`At` identifies the temporal coordinate at which the historical fact was recorded.

It is intentionally separate from relationship validity:

```text
At
    = temporal coordinate of the recorded fact

ValidFrom / ValidTill
    = semantic validity interval of the relationship
```

Krono does not assume these values are identical.

For Event-anchored Evolutions, `At` derives from `Event.OccurredAt`.

## Historical Fact Immutability

Historical facts are immutable after construction.

Consumers can inspect them, but the recorded fact is not a mutable archive that can be rewritten through the public history model.

This protects historical state from later mutation.

## Relationship History

`SorophyRelationshipHistory` belongs to one relationship identity.

It is append-only:

```text
Fact 1
  ↓
Fact 2
  ↓
Fact 3
  ↓
...
```

Facts retain their recorded order.

The public history surface is read-only while internal graph execution controls appending.

## Graph-Level History Store

`SorophyGraph` maintains the relationship-to-history mapping.

A history collection is created when the first historical fact for that relationship is recorded. Empty history stores are not required for every active relationship.

The history store is separate from active relationship storage.

```text
Active Relationships
        │
        │ current state
        ▼
   SorophyGraph

Relationship Histories
        │
        │ recorded temporal facts
        ▼
 Historical State Store
```

## Relationship History and Evolution

For an existing relationship mutation, the executor captures the relevant prior state before applying the new state.

```text
Current Relationship
        │
        ├── capture ──> Historical Fact
        │                    │
        │                    └── append
        │
        └── mutate ───> Current Relationship
```

The exact transition is determined by the Evolution operation.

For termination, the prior relationship state is recorded before the relationship is removed from active graph state and its identity is retired.

## Creation History

Creation Evolutions preserve the initial historical fact for the newly created relationship.

This provides a temporal anchor for relationship creation without implying that every direct `AddRelationship(...)` call is a temporal Evolution.

Direct graph insertion establishes baseline state and does not fabricate historical records.

## Removal Does Not Erase History

When a relationship is terminated, the active relationship disappears from the canonical active graph, but its historical facts remain retained.

```text
Active Graph
    = current structural state

History Store
    = explicitly recorded historical state
```

An ordinary relationship deletion does not automatically create a termination fact.

## Relationship Identity Retirement

Relationship identity is permanent once retired.

A terminated relationship ID cannot simply be reused for a different active relationship.

```text
Created → Active → Evolved → Terminated → Retired
```

Retirement prevents ambiguous identity histories in which one relationship ID appears to represent unrelated structural relationships at different times.

The graph persists retired relationship identities separately from active relationships and historical facts.

## Event Provenance in History

When an Evolution is anchored to an Event Entity, its historical fact may preserve:

```text
EventEntityId
```

This establishes which Event Entity anchored the structural transition.

The provenance is persistent structural information. It is not an audit record and does not imply event sourcing.

## Temporal Reconstruction

Historical facts provide the temporal information required by higher-level read-only projections.

Krono uses this recorded history together with canonical graph state and the temporal model to support:

```text
Temporal Snapshot
Temporal Query
Graph Diff
```

These are derived views. They do not replace the canonical graph or history store.

## History and Snapshot Semantics

Snapshots reconstruct graph state at a requested temporal coordinate.

At the requested coordinate, Krono uses its temporal and historical semantics to determine which recorded relationship state belongs in the projection.

Same-time temporal coordinates are equivalent; the engine does not invent an ordering between separate facts merely because they were recorded in a particular sequence.

## What History Is Not

Krono relationship history is not:

- a mutable cache;
- a UI undo stack;
- a second active graph;
- an implicit Event executor;
- an automatic audit log;
- an event-sourcing stream;
- a mechanism for reconstructing state by guesswork.

It is an explicit append-only record of historical relationship facts.

## Design Boundary

History deliberately stays below higher-level analysis.

```text
Evolution Executor
        │
        ├── Current Graph State
        │
        └── Historical Facts
                 │
                 ▼
        Temporal Projection Layer
          ├── Snapshot
          ├── TQD
          └── Graph Diff
```

This keeps historical state authoritative as recorded data while keeping temporal reconstruction and analysis in their appropriate read-only layers.
