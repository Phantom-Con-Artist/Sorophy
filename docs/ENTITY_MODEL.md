<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="sorophyv2 · Krono"
     width="900"/>

<p><strong>SOROPHYV2 · KRONO</strong></p>

<h1>Entity Model</h1>

<p>
Krono Temporal Graph Architecture
</p>

</div>

This document defines the **Krono Entity Model**, the entity semantics implemented by Sorophy.Engine 2.


`SorophyEntity` remains the smallest meaningful object in the Saga model.

Krono Entity Model does not replace the entity concept from v1. It expands it while preserving the core idea: an entity is an independently identifiable thing that can exist before it is placed into a graph.

## Core Identity

An entity has a stable `Guid` identity and can carry a name, type, properties, descriptive content, tags, and other structured information exposed by the current engine model.

Conceptually:

```text
SorophyEntity
├── Identity
├── Name
├── Type
├── Description
├── Properties
├── Tags
└── Embedded Documents
```

The exact API surface is defined by the implementation and its tests; this document describes the model rather than every implementation member.

## Entity Independence

`SorophyEntity` does not require an `SorophyGraph` to exist.

An application can construct, inspect, serialize, or prepare an entity before inserting it into a graph.

This keeps the entity model independent from graph storage and allows the same object model to be reused across applications.

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

## Event Entities

An event is still an `SorophyEntity`.

An entity can be classified as an event through its type:

```text
Type = "Event"
```

The engine exposes an event classification convenience through `IsEvent`.

This classification does **not** mean that constructing an event entity executes graph mutations.

That distinction is intentional:

```text
Event Entity
    ↓
semantic description of an event

Evolution Operation
    ↓
declarative description of a state transition

Evolution Executor
    ↓
explicit graph mutation
```

## Properties

Entity properties use the engine's explicit `SorophyProperty` / `SorophyValue` system.

A property is not simply an untyped string. Values retain an explicit value category, allowing applications to distinguish integers, strings, booleans, decimals, GUIDs, dates, lists, and objects.

## Tags

Tags provide lightweight categorical indexing without forcing tags to become a separate entity hierarchy.

The graph maintains tag-index behavior separately from the entity's canonical storage.

This gives the engine a useful distinction between:

```text
Entity State
    = authoritative entity data

Tag Index
    = derived lookup structure
```

## Embedded Structured Content

Krono Entity Model allows richer structured content to live with an entity without turning the entity into an application-specific document editor.

This is especially useful for applications such as Orbpad, where human-readable supporting content may need to remain attached to a structured entity.

## Invariants

Krono Entity Model preserves the core graph invariants inherited from v1:

- Identity is explicit.
- Canonical entity storage is separate from indexes.
- Duplicate identifiers are rejected.
- Removing an entity must not leave graph relationships dangling.
- Derived indexes must remain consistent with canonical entity state.

## What Krono Entity Model Does Not Try to Do

Krono Entity Model does not attempt to define every possible domain object.

A research sample, fictional kingdom, software component, project milestone, historical person, or spacecraft can all be represented as entities without requiring the engine to know the domain-specific semantics in advance.

That flexibility is a feature, not a missing taxonomy.
