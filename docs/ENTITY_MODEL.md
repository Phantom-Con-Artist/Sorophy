<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="Sorophy v2 · Krono"
     width="900"/>

<p><strong>SOROPHY V2 · KRONO</strong></p>

<h1>Entity Model</h1>

<p>
Krono Temporal Graph Evolution Core
</p>

</div>

This document defines the **Krono Entity Model**, the entity semantics implemented by **Sorophy.Engine 2.0.0**.

The entity model is deliberately generic: Sorophy provides structural identity and data representation, while applications provide domain-specific meaning.

## Core Identity

`SorophyEntity` remains the smallest meaningful object in the Saga model.

Krono expands the v1 entity concept while preserving a fundamental rule:

> An entity is an independently identifiable object that can exist before it is placed into a graph.

Conceptually:

```text
SorophyEntity
├── Identity
├── Name
├── Type
├── Description
├── Properties
├── Tags
├── Embedded Documents
└── OccurredAt?          ← Event temporal anchor
```

The entity's identity is stable and explicit through its `Guid` identifier.

## Entity Independence

`SorophyEntity` does not require an `SorophyGraph` to exist.

An application can construct, inspect, serialize, or prepare an entity before inserting it into a graph.

This keeps entity construction independent from graph storage and allows the same structural model to be reused across applications.

## Types Are Semantic Classifiers

The `Type` field is a semantic classifier rather than a rigid inheritance hierarchy.

Examples include:

```text
Character
Location
Organization
Project
Document
Dataset
Event
```

Applications can establish their own domain vocabulary without requiring a new engine subclass for every domain object.

Sorophy therefore does not need to understand the semantics of every possible domain type.

## Event Entities

An Event is a normal `SorophyEntity` whose type identifies it as an Event.

```text
SorophyEntity
    │
    └── Type = "Event"
          │
          └── OccurredAt = temporal coordinate
```

The engine exposes `IsEvent` as a classification convenience.

An Event Entity is a **first-class passive temporal anchor**.

It is:

- an entity;
- declarative;
- temporally anchored through `OccurredAt`;
- usable as Evolution provenance;
- not an executable object.

### Events Do Not Execute

Event classification does not mean that constructing or invoking an Event Entity performs graph mutations.

Krono intentionally has no `event.Execute()` lifecycle.

The structural lifecycle is:

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
     ▼
Graph Mutation + Historical Fact
```

The application determines what structural consequences an Event should cause.

This preserves the boundary between:

```text
Event
= declarative temporal fact

Evolution
= declarative structural transition

Executor
= explicit structural mutation
```

One Event Entity may anchor multiple Evolutions.

## Event Temporal Semantics

`OccurredAt` is the authoritative temporal coordinate of an Event Entity.

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

An Event-anchored Evolution does not define a conflicting independent temporal coordinate.

Events occurring at the same temporal coordinate are temporally equivalent. The engine does not infer a deterministic ordering or causality between them.

## Event Entities and Relationships

Event Entities are not special relationship endpoints.

Normal relationships remain strictly:

```text
Entity → Relationship → Entity
```

An Event Entity participates in the temporal evolution model through its identity and `OccurredAt`, not by becoming a special graph edge endpoint.

This keeps temporal anchoring separate from normal graph topology.

## Entity Lifecycle

Krono keeps the core entity lifecycle intentionally small:

```text
Creation
    ↓
Property Mutation
    ↓
Termination
```

The engine does not automatically turn every direct entity mutation into temporal history.

Direct `AddEntity(...)` establishes baseline graph state. Historical lifecycle semantics require the explicit temporal model rather than fabricated timestamps.

## Properties

Entity properties use the engine's explicit `SorophyProperty` / `SorophyValue` system.

A property is not simply an untyped string.

Values retain explicit categories so applications and the engine can distinguish structured values such as:

```text
Integer
String
Boolean
Decimal
Guid
Date / Temporal Value
List
Object
```

The typed-value system allows structured information to remain structurally represented without forcing domain semantics into the engine.

## Tags

Tags provide lightweight categorical classification and lookup.

The entity's tags are part of canonical entity state, while graph-maintained tag indexes are derived supporting structures.

```text
Entity State
    = authoritative tag data

Tag Index
    = derived lookup structure
```

The index exists for efficient access and must remain consistent with canonical entity state.

It is never the source of truth.

## Embedded Structured Content

Krono allows structured supporting content to live with an entity without turning `SorophyEntity` into an application-specific document editor.

This is useful for applications such as Orbpad, where human-readable material may remain associated with a structured entity.

The engine stores the structured content; applications decide how that content is presented and interpreted.

## Entity Removal and Graph Integrity

Entity identity is independent of graph membership, but removing an entity must preserve graph integrity.

When an entity is removed from a graph, relationships that depend on that entity cannot remain as dangling active graph edges.

This is a structural integrity rule, not a claim that entity removal automatically represents a temporal termination event.

Temporal lifecycle semantics and direct graph operations remain distinct.

## Canonical Entity State

The canonical entity collection is authoritative.

Derived structures may accelerate entity access, but they do not define entity existence.

```text
Canonical
└── Entities

Derived
└── Indexes
```

This distinction mirrors the wider Krono architecture: authoritative state is kept separate from derived accelerators and projections.

## Persistence

Entity state participates in `.lore` v2 persistence as part of the graph's canonical state.

Persistence preserves the structural entity model rather than inventing temporal history.

Event-specific temporal information, including `OccurredAt`, is persisted as part of the entity model where present.

## Validation and Invariants

Krono Entity Model preserves the core graph invariants inherited from v1:

- Identity is explicit.
- Duplicate identifiers are rejected.
- Canonical entity storage is authoritative.
- Derived indexes must remain consistent with canonical entity state.
- Removing an entity must not leave active graph relationships dangling.
- Event classification is structural and does not execute behavior.
- Event provenance must reference an existing Event Entity.
- Event temporal anchoring is represented explicitly through `OccurredAt`.

Corrupted or inconsistent graph state is rejected by the validation layer rather than silently repaired.

## What the Entity Model Does Not Do

The Krono Entity Model does not attempt to define every possible domain object or semantic taxonomy.

A research sample, fictional kingdom, software component, project milestone, historical person, spacecraft, or other domain object can all be represented as an entity without requiring Sorophy to understand the domain-specific semantics in advance.

It also does not turn entities into executable workflow objects.

In particular:

```text
Entity ≠ Workflow
Event Entity ≠ Executable Event
Type ≠ Inheritance Hierarchy
Tag Index ≠ Canonical State
```

That separation is intentional.

## Design Principle

The Entity Model provides the stable structural vocabulary on which the rest of Krono is built:

```text
Entity
   │
   ├── Identity
   ├── Structured Data
   └── Optional Event Temporal Anchor
          │
          ▼
     Evolution Model
          │
          ▼
      Graph State
```

The entity remains simple enough to be reusable, while Krono's temporal and evolutionary layers provide the additional machinery required to represent change over time.
