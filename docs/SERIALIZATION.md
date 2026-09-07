# Serialization in V2 — Krono

Serialization is one of the most important compatibility boundaries in Sorophy.Engine.

The principle carried forward from v1 is:

> **Round-trip fidelity matters more than merely producing valid JSON.**

## Existing Document Concepts

The engine continues to work with two major document concepts:

```text
.entity
    = one independently identifiable entity

.lore
    = a connected graph-oriented body of entities and relationships
```

## `.lore` V2

Krono v2 persists the temporal graph model explicitly.

The top-level representation includes:

```text
formatVersion
entities
relationships
relationshipHistories
retiredRelationshipIds
```

The persisted graph therefore contains both current structural state and explicitly recorded historical state.

The serializer does not manufacture historical facts from the absence of history or from ordinary deletion.

## V2 Model Expansion

Krono expands the serialized semantic model with:

- Entity 2.0 data
- typed `SorophyValue` data
- Event Entity temporal anchoring
- relationship validity
- semantic temporal values
- relationship Evolutions where represented by the persistence contract
- historical facts
- relationship histories
- Event provenance
- retired relationship identities

These features increase the responsibility of serialization because a serialized document must preserve semantic structure rather than silently collapsing it into unrelated primitive values.

## Canonical Value Model

`SorophyValue` remains the engine's typed-value boundary.

Examples include:

```text
Null
Boolean
Integer
Decimal
String
Guid
DateTime
List
Object
```

Nested lists and objects remain structured values rather than arbitrary blobs.

## Temporal Serialization

Temporal values retain their semantic information, including the schema relationship required by the temporal model.

A meaningful `SorophyTime` must not be reduced to an unexplained string and treated as a successful semantic round trip.

Event Entities likewise preserve their temporal anchor through `OccurredAt`.

## History Serialization

Historical facts preserve their semantic shape:

```text
At
RelationshipId
SourceId
TargetId
Type
Properties
ValidFrom
ValidTill
EventEntityId?
```

Relationship histories and their recorded facts remain distinguishable from active relationship state.

## Relationship Retirement Serialization

Retired relationship identities are persisted explicitly.

This prevents deserialization from accidentally allowing an identity that was previously retired to be reused.

```text
retiredRelationshipIds
```

is part of the v2 persistence model.

## Evolution and Execution Boundary

Evolution objects are descriptions of structural transitions.

If an Evolution representation is persisted or transported as data, deserialization reconstructs data only.

```text
Deserialize
    ≠
Execute
```

Loading an Event Entity or Evolution description must never silently mutate the graph.

Execution remains explicit through `SorophyRelationshipEvolutionExecutor`.

## Event Provenance

Historical facts may preserve:

```text
eventEntityId
```

When present, this identifies the Event Entity that anchored the Evolution.

Serialization preserves the provenance relationship as data. It does not execute or reinterpret the Event.

## Validation on Read

Malformed, incomplete, truncated, or semantically invalid documents should fail explicitly.

Validation includes relevant graph, temporal, history, retirement, referential-integrity, and Event-provenance invariants.

Silent repair is deliberately avoided because a structured-information engine must not continue operating on data whose intended meaning may have been changed during loading.

## Deterministic Output

Where deterministic serialization is part of the engine contract, semantically equivalent state should produce stable output.

Determinism makes:

- version control useful;
- diffs useful;
- tests reproducible;
- corruption easier to diagnose.

Deterministic serialization is therefore a verification property, not merely a cosmetic formatting preference.

## Round-Trip Verification

Krono persistence is tested through serialization/deserialization round trips and cross-platform portability workflows.

The verification goal is preservation of semantic graph state across:

```text
Serialize
    ↓
Persist
    ↓
Deserialize
    ↓
Validate
    ↓
Re-serialize
```

Cross-platform persistence testing further exercises this path across supported operating systems.

## Compatibility Boundary

Compatibility must distinguish between:

```text
Old file understood by new engine

and

New file understood by old engine
```

These are different guarantees.

The `.lore` v2 format identifies its format version explicitly so compatibility policy can evolve without pretending that all versions are mutually interchangeable.

## Design Principle

Serialization is a representation boundary, not an execution boundary.

```text
Canonical Graph State
        │
        ▼
   Serializer
        │
        ▼
    .lore v2
        │
        ▼
   Deserializer
        │
        ▼
Validated Graph State
```

The persistence layer preserves explicit state and semantic structure. It does not invent history, infer application meaning, or execute declarative objects while loading them.
