<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="sorophyv2 · Krono"
     width="900"/>

<p><strong>SOROPHYV2 · KRONO</strong></p>

<h1>Historical State Model</h1>

<p>
Krono Temporal Graph Architecture
</p>

</div>

This document defines the **Krono Historical State Model**, the relationship-history semantics implemented by Sorophy.Engine 2.


Krono treats relationship history as first-class engine state.

The goal is not to create an undo stack. It is to preserve historical facts about relationship state without allowing those facts to be rewritten through the normal history API.

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
└── ValidTill
```

### `At`

`At` identifies the temporal point at which the historical fact belongs in the relationship history.

It is intentionally separate from relationship validity.

### Relationship State

The fact captures the relationship identity, endpoints, semantic type, properties, and validity boundaries represented at that point.

## Immutability

Facts are immutable after construction.

A consumer can read them, but the historical record is not a mutable archive that can be rewritten through the public fact model.

This matters because historical truth loses its value if later mutations silently modify the old record.

## SorophyRelationshipHistory

`SorophyRelationshipHistory` belongs to exactly one relationship identity.

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

Facts remain in insertion order.

The history exposes read-only collection behavior while retaining internal append control.

## Graph-Level History Store

`SorophyGraph` maintains a relationship-to-history mapping.

A history is created when the first historical fact for that relationship is recorded.

This avoids creating empty historical stores for every active relationship.

## Removal Does Not Erase History

When a relationship is terminated, the active relationship disappears from the canonical active graph, but its history remains retained.

That gives us two distinct concepts:

```text
Active Graph
    = what exists now

History Store
    = what the engine recorded about previous states
```

## Relationship Identity Retirement

Relationship identity is permanent.

When a relationship is terminated through the graph lifecycle, its identifier is retired.

A later creation attempt using the same identity is rejected.

This avoids ambiguous historical records such as:

```text
Relationship ID X
    existed as A → B
    terminated
    mysteriously reappeared as C → D
```

The engine instead preserves identity continuity and historical clarity.

## Snapshot Semantics During Evolution

For an existing relationship mutation, the executor captures the previous state before applying the new state.

A simplified transition looks like:

```text
Current relationship
        │
        ├── capture → SorophyRelationshipFact
        │                 │
        │                 └── record in history
        │
        └── mutate → current relationship
```

For termination, the historical validity ends at the termination effective time.

## What History Is Not

History is not:

- A mutable cache
- A UI undo stack
- A second active graph
- An implicit event executor
- A license to reconstruct state by guesswork

It is an append-only record of historical relationship facts.

## Future Work

The current history foundation is ready for higher-level features such as:

```text
Relationship history queries
Temporal snapshots
State reconstruction
Diffs between temporal points
Replay / auditing
```

Those belong to later Krono milestones rather than being hidden inside the primitive history classes.
