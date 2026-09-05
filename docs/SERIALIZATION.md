# Serialization in V2

Serialization is one of the most important compatibility boundaries in Sorophy.Engine.

The principle carried forward from v1 is:

> **Round-trip fidelity matters more than merely producing valid JSON.**

## Existing Document Concepts

The engine continues to work with two major document concepts:

```text
.entity
    one independently identifiable entity

.lore
    a connected graph-oriented body of entities and relationships
```

## V2 Model Expansion

Krono expands the in-memory semantic model with:

- Entity 2.0 data
- Relationship validity
- Semantic temporal values
- Relationship evolution
- Historical facts
- Relationship histories

These features increase the responsibility of serialization because a serialized document must not silently collapse richer semantic state into unrelated primitive values.

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

Temporal values must retain their semantic information, including the temporal schema relationship required by the model.

A serializer should not turn a meaningful `SorophyTime` into an unexplained string and call that a successful round trip.

## History Serialization

History persistence must preserve the immutable semantic shape of historical facts when history becomes part of a persisted document contract.

The required conceptual fields are:

```text
At
RelationshipId
SourceId
TargetId
Type
Properties
ValidFrom
ValidTill
```

Any future on-disk representation should preserve these semantics rather than storing only an opaque implementation dump.

## Evolution Serialization

Evolution objects are declarative descriptions of requested transitions.

If persisted, they must deserialize as data and must **not execute automatically**.

That means:

```text
Deserialize
    ≠
Execute
```

Execution must remain explicit through the evolution executor.

## Backward Compatibility

V2 should be developed with a clear distinction between:

```text
Old file understood by new engine

and

New file understood by old engine
```

Those are different compatibility guarantees.

A future release policy should declare which direction is supported for each document format.

## Deterministic Output

Where deterministic serialization is part of the engine's test contract, the output should remain stable for semantically equivalent state.

Determinism matters because it makes:

- Version control useful
- Diffing useful
- Tests reproducible
- Corruption easier to diagnose

## Failure Philosophy

Malformed, incomplete, truncated, or semantically invalid documents should fail explicitly.

Silent repair is dangerous for a structured information engine because the application may continue operating on data that no longer means what the author intended.

## V2 Serialization Roadmap

The long-term serialization work includes:

```text
Entity 2.0 persistence
Temporal values
Relationship validity
History persistence
Evolution persistence
Migration / compatibility rules
Large-document hardening
Deterministic round-trip verification
```

The exact persistence contract for each new v2 feature should be considered complete only when the corresponding serializer behavior is explicitly implemented and tested.
