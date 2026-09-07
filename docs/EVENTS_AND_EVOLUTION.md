<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="Sorophy v2 · Krono"
     width="900"/>

<p><strong>SOROPHY V2 · KRONO</strong></p>

<h1>Events & Evolution</h1>

<p>
Krono Temporal Graph Evolution Core
</p>

</div>

This document defines **Krono Events & Evolution**, the explicit state-transition model implemented by **Sorophy.Engine 2.0.0**.

## Core Principle

Krono separates temporal facts from structural mutation:

> **An Event describes something that occurred. An Evolution describes a structural state transition. The executor performs the mutation.**

This separation is fundamental to Krono.

```text
Event Entity
     │
     │ EventEntityId
     ▼
Evolution Operation
     │
     ▼
SorophyRelationshipEvolutionExecutor
     │
     ├── mutate canonical graph
     └── record historical fact
```

The Event itself never executes the transition.

## Event Entities

Events remain ordinary `SorophyEntity` instances.

An Event is identified structurally through its entity type:

```text
SorophyEntity
    │
    ├── Type = "Event"
    ├── IsEvent = true
    └── OccurredAt = temporal coordinate
```

An Event Entity is a **first-class passive temporal anchor**.

It is:

- declarative;
- temporal through `OccurredAt`;
- independently identifiable;
- usable as Evolution provenance;
- not an executable workflow object.

### No `event.Execute()`

Krono intentionally does not define an Event execution lifecycle.

Creating, loading, or inspecting an Event Entity does not mutate the graph.

```text
Deserialize Event
       ↓
Reconstruct Event Data
       ↓
No Graph Mutation
```

Application semantics determine what, if anything, an Event causes.

## Event Temporal Coordinate

`Event.OccurredAt` is the authoritative temporal coordinate for an Event.

When an Evolution is anchored to an Event:

```text
Event.OccurredAt
       │
       ▼
Evolution temporal coordinate
       │
       ▼
Historical Fact.At
```

An Event-anchored Evolution does not introduce a conflicting independent `At` value.

Events occurring at the same temporal coordinate are temporally equivalent. Krono does not infer deterministic causal ordering between same-time Events.

## Events and Graph Topology

Event Entities are not special relationship endpoints.

Normal relationships remain:

```text
Entity → Relationship → Entity
```

The Event participates in temporal structure through its identity and `OccurredAt`, not by becoming a special graph-topology node.

## Relationship Evolution

Krono defines five relationship evolution operations:

```text
SorophyRelationshipCreation
SorophyRelationshipPropertyModification
SorophyRelationshipTypeChange
SorophyRelationshipValidityChange
SorophyRelationshipTermination
```

All derive from `SorophyRelationshipEvolution`.

Each Evolution describes a structural transition. The executor is responsible for applying it.

An Evolution may optionally carry:

```text
EventEntityId
```

This provides persistent provenance linking the structural transition to its Event Entity.

## Creation

Creation establishes a new relationship through the evolution model.

A Creation Evolution preserves the initial historical fact for that relationship.

This differs from direct:

```text
graph.AddRelationship(...)
```

A direct graph insertion is a baseline structural operation and does not fabricate temporal history.

## Property Modification

Property Modification changes relationship properties while preserving relationship identity.

It supports explicit set/remove semantics:

```text
Set
    add or replace a property

Remove
    remove a named property
```

The executor records the relevant prior relationship state before applying the mutation.

## Type Change

Type Change preserves relationship identity while replacing its structural type.

```text
Before:
A ──[alliance]──> B

Evolution
alliance → hostility

After:
A ──[hostility]──> B
```

The historical fact preserves the prior state.

## Validity Change

Validity Change replaces the relationship's current validity boundaries with the values specified by the Evolution.

```text
ValidFrom
ValidTill
```

Temporal compatibility is enforced by the temporal model.

`ValidFrom` / `ValidTill` describe semantic validity. They are distinct from the historical fact coordinate `At`.

## Termination

Termination explicitly removes a relationship from the active graph and retires its relationship identity.

```text
Active Relationship
       ↓
Historical Fact
       ↓
Remove from Active Graph
       ↓
Retire Relationship ID
```

The previous relationship state is preserved historically.

Ordinary relationship deletion is not automatically interpreted as temporal termination.

## Evolution Executor

`SorophyRelationshipEvolutionExecutor` is the explicit execution boundary.

Its responsibilities include:

1. Validate the graph and Evolution.
2. Dispatch to the correct Evolution operation.
3. Retrieve active relationship state where required.
4. Validate Event provenance where supplied.
5. Capture prior state where required.
6. Record historical facts.
7. Apply the requested structural transition.
8. Retire relationship identity when termination requires it.

The executor does not replace `SorophyGraph` as the canonical owner of state.

## Event Provenance

When an Evolution carries `EventEntityId`, the executor validates that the referenced ID:

1. exists;
2. identifies an entity;
3. identifies an Event Entity.

Invalid, dangling, or non-Event provenance is rejected.

Provenance is persistent structural information. It is not an audit log and does not turn Krono into event sourcing.

## Multiple Evolutions Per Event

One Event may anchor multiple Evolutions.

```text
             Event
               │
        ┌──────┼──────┐
        ▼      ▼      ▼
     Evolution Evolution Evolution
        │      │      │
        └──────┴──────┘
               ▼
        Graph State Changes
```

This is intentional.

The Event represents the temporal occurrence. The application determines which independent structural consequences should be represented as Evolutions.

## Failure Behavior

Evolution execution is explicit and defensive.

Examples of rejected operations include:

- mutating a relationship that does not exist;
- reusing a retired relationship identity;
- supplying invalid Event provenance;
- supplying an Event ID that is not an Event Entity;
- supplying incompatible temporal schemas;
- supplying an unsupported Evolution subtype;
- violating graph referential integrity.

Krono rejects invalid structural transitions rather than silently repairing them.

## EventPkg Boundary

Krono does not require an `EventPkg` abstraction.

The current engine already provides:

```text
Event Entity
+
Evolution Object
+
Evolution Executor
```

Introducing another package abstraction would add indirection without solving a current core requirement.

An EventPkg-style orchestration layer may exist above Sorophy in a future ecosystem component, but it is not part of the current engine contract.

## Execution Philosophy

The complete model is:

```text
Application Semantics
        │
        ▼
   Event Entity
        │
        │ EventEntityId
        ▼
Evolution Operation
        │
        ▼
Evolution Executor
        │
        ├───────────────┐
        ▼               ▼
Canonical Graph    Historical Facts
```

This keeps **description, temporal anchoring, structural intent, execution, and historical state** separate.

That separation is what allows Krono to remain a reusable temporal graph core rather than an application-specific workflow engine.
