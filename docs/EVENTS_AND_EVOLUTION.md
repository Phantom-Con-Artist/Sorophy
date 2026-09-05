<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="sorophyv2 · Krono"
     width="900"/>

<p><strong>SOROPHYV2 · KRONO</strong></p>

<h1>Events & Evolution</h1>

<p>
Krono Temporal Graph Architecture
</p>

</div>

This document defines **Krono Events & Evolution**, the explicit state-transition model implemented by Sorophy.Engine 2.


Krono introduces an explicit model for state change.

The central rule is:

> **An event can describe something that happened. An evolution operation describes a state transition. The executor is what actually mutates the graph.**

## Event Entities

Events remain ordinary `SorophyEntity` instances.

An event is semantically classified through the entity type system:

```text
SorophyEntity
Type = "Event"
```

The engine exposes `IsEvent` as a convenience for that classification.

This avoids a proliferation of hard-coded domain subclasses.

## Event Does Not Mean Automatic Execution

Creating an event entity does not execute any evolution.

That distinction is essential for safe serialization and predictable program behavior.

```text
Deserialize Event
        ↓
Reconstruct Data
        ↓
No Mutation
```

Execution must be explicit.

## Relationship Evolution

The Krono relationship evolution model contains five operation types:

```text
SorophyRelationshipCreation
SorophyRelationshipTypeChange
SorophyRelationshipPropertyModification
SorophyRelationshipValidityChange
SorophyRelationshipTermination
```

All are forms of `SorophyRelationshipEvolution`.

## Creation

Creation describes a new relationship and carries the state required to insert it into the graph.

There is no prior relationship state to record, because the relationship did not exist immediately before creation.

## Type Change

A type change preserves the relationship identity while replacing its semantic type.

Conceptually:

```text
Before:
A ──[alliance]──> B

Evolution @ T
alliance → hostility

After:
A ──[hostility]──> B
```

The previous relationship state can be recorded as a historical fact before the mutation.

## Property Modification

Property modification supports explicit set and remove semantics.

```text
Set
    add a property or replace its current value

Remove
    remove a named property
```

The historical fact captures the complete prior property set before the mutation.

## Validity Change

Validity change replaces the current relationship validity boundaries with the operation's new values.

Temporal schema compatibility is enforced where required by the model.

The engine intentionally keeps broader chronological policy separate from the simple data contract.

## Termination

Termination removes the relationship from the active graph and permanently retires its relationship identity.

Before removal, the prior relationship state is captured historically.

Conceptually:

```text
Active relationship
        ↓
Historical fact
        ↓
Remove from active graph
        ↓
Retire relationship ID
```

## Evolution Executor

`SorophyRelationshipEvolutionExecutor` is the execution boundary.

Its responsibilities include:

1. Validate the supplied graph and evolution.
2. Dispatch to the correct operation handler.
3. Retrieve the active relationship when necessary.
4. Capture prior state before mutation.
5. Record historical facts.
6. Apply the requested transition.

It does not replace `SorophyGraph` as the canonical owner of graph state.

## Failure Behavior

Evolution is explicit and defensive.

Examples of rejected operations include:

- Mutating a relationship that does not exist.
- Reusing a retired relationship identity.
- Supplying incompatible temporal schemas.
- Supplying an unsupported evolution subtype.

## Deliberately Deferred: EventPkg

Krono does not require an `EventPkg` abstraction.

The current engine already has explicit evolution objects and an executor. Introducing another package layer would add indirection without solving a current requirement.

An `EventPkg` may become useful in a later version if a higher-level event orchestration model actually needs it.

## Execution Philosophy

Evolution is deliberately separated into three layers:

```text
Event / Application intent
          ↓
Evolution operation
          ↓
Executor
          ↓
SorophyGraph current state
          +
Relationship history
```

This keeps description, intent, execution, and historical state separate.
