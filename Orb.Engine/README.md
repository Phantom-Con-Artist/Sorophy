<div align="center">

<p>A project under</p>

<img src="docs/orbisprojectcover.png" alt="Orb Project" width="900"/>

<p>introduces</p>

<img src="docs/orbenginecover.png" alt="The Orb Engine" width="900"/>

<h1>The Orb Engine</h1>

<p>

<strong>A structured data and graph engine for building interconnected information.</strong>

</p>

<p>

Entities · Relationships · Typed Values · Graphs · Serialization · Storage

</p>

<br/>

<p align="center"><a href="https://github.com/Phantom-Con-Artist/Orb"><img src="https://img.shields.io/badge/GitHub-Orb-181717?style=for-the-badge&logo=github" alt="GitHub"/></a>&nbsp;<a href="https://github.com/Phantom-Con-Artist/Orb/releases"><img src="https://img.shields.io/badge/Version-1.0.0--Stable--Grade--A-7C3AED?style=for-the-badge" alt="Version"/></a>&nbsp;<a href="LICENSE"><img src="https://img.shields.io/badge/License-AGPL--3.0--or--later-2E7D32?style=for-the-badge" alt="License: AGPL-3.0-or-later"/></a></p>

<p align="center"><a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10"/></a>&nbsp;<a href="https://learn.microsoft.com/en-us/dotnet/csharp/"><img src="https://img.shields.io/badge/C%23-Language-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/></a>&nbsp;<a href="https://github.com/Phantom-Con-Artist/Orb/blob/main/CHANGELOG.md"><img src="https://img.shields.io/badge/Changelog-Keep%20a%20Changelog-E05735?style=for-the-badge" alt="Changelog"/></a></p>

<p align="center"><a href="https://discord.com/invite/Em2ur4J8PF"><img src="https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Discord"/></a>&nbsp;<a href="mailto:personalsarkar345@gmail.com"><img src="https://img.shields.io/badge/Email-personalsarkar345%40gmail.com-EA4335?style=for-the-badge&logo=gmail&logoColor=white" alt="Email"/></a></p>

<p>

<strong>✅ 1.0.0 · Stable · Grade A — Silver Standard</strong><br/>

Stable enough for the documented capabilities and workload classes covered by the release verification program.

</p>

</div>

---

<div *align*="center">

### Build information into structure.

</div>

Orb.Engine is the foundational engine behind **Orbis**, designed to represent structured information as interconnected entities, properties, and relationships.

Rather than treating information as a collection of disconnected documents, Orbis provides a graph-oriented model where objects can exist independently, connect to one another, carry strongly defined values, and be persisted without losing their structure.

The engine is designed to sit underneath higher-level applications such as editors, knowledge-management tools, worldbuilding systems, structured-data applications, and other domain-specific software.

---

## Table of Contents

- [The Orbis Vision](#the-orbis-vision)

- [Core Concepts](#core-concepts)

- [The OrbValue System](#the-orbvalue-system)

- [Nested Values](#nested-values)

- [Graph Operations](#graph-operations)

- [Relationship Cascade](#relationship-cascade)

- [Traversal](#traversal)

- [Reachability](#reachability)

- [Serialization](#serialization)

- [Round-Trip Fidelity](#round-trip-fidelity)

- [Storage](#storage)

- [Architecture](#architecture)

- [Public API](#public-api)

- [Validation & Reliability](#validation--reliability)
- [Test Arsenal & 1.0.0 Verification](#test-arsenal--100-verification)

- [Stable Release Grade](#stable-release-grade)

- [Release Stability Grades](#release-stability-grades)

- [What Orb.Engine Is Not](#what-orbengine-is-not)

- [Intended Ecosystem](#intended-ecosystem)

- [Installation](#installation)

- [Quick Example](#quick-example)

- [Project Structure](#project-structure)

- [Development](#development)

- [Design Principles](#design-principles)

- [Roadmap](#roadmap)

- [Contributing](#contributing)

- [Community](#community)

- [License](#license)

- [The Philosophy](#the-philosophy)

- [Author](#author)

---

# ✦ The Orbis Vision

Orbis is not intended to be just a graph library.

**Orbis is an attempt to build a general-purpose structured information ecosystem.**

The fundamental idea is simple:

> **Information should be represented as structured, interconnected objects rather than being trapped inside isolated documents.**

Modern software gives us countless ways to create information, but much of that information remains fragmented across files, notes, databases, applications, and proprietary formats.

A character may exist in one document. A location may exist in another. The relationship between them may exist only as a sentence buried inside a third document. One application may understand a given representation while another understands something completely different.

Orbis approaches the problem from another direction. Instead of beginning with documents, Orbis begins with **structure**.

### The Core Idea

At the foundation of Orbis is the concept of an **entity** — something that exists within an information system. It could be:

```text

Person · Location · Organization · Project · Concept

Historical event · Scientific object · Fictional character

Machine · Document · Dataset · or virtually anything else

an application needs to represent

```

Entities can carry structured properties and connect to one another through relationships, producing a model such as:

```text

┌───────────────┐

│    Person     │

│  Subhradeep   │

└───────┬───────┘

        │

     created

        │

        ▼

┌───────────────┐

│    Project    │

│    Orbis      │

└───────┬───────┘

        │

     contains

        │

        ▼

┌───────────────┐

│  Orb.Engine   │

└───────────────┘

```

The important part is not merely that these objects exist — it's that **their relationships are data too.**

### From Documents to Structured Information

Traditional documents are excellent for human-readable content, but they often provide little semantic structure beyond what the author establishes through formatting and language. Orbis attempts to separate the **information model** from the application used to view it:

```text

                     ORBIS

                       │

               ┌────────┴────────┐

               │                 │

         Structured Data      Applications

               │                 │

               │        ┌────────┼────────┐

               │        │        │        │

               ▼        ▼        ▼        ▼

           Entities   Orbpad   Editors   Other Tools

               │

               ▼

         Relationships

               │

               ▼

              Graph

```

The engine becomes the foundation. Applications become interfaces and specialized experiences built on top of it.

### A General-Purpose Foundation

Although Orbis can be used for worldbuilding and knowledge management, the underlying model is intentionally broader. The same engine can represent:

```text

Worldbuilding · Knowledge bases · Research data

Project structures · Organizations · Historical information

Game data · Scientific information · Documentation

Personal knowledge · Structured notes

```

The domain changes. The underlying need does not: **represent things, describe them, connect them, and preserve those connections.** That is the problem Orbis is designed to address.

### The Orbis Document Model

The ecosystem is built around structured document concepts. The engine currently focuses on two fundamental representations:

```text

.entity                          .lore

   │                                │

   ├── Identity                     ├── Entities

   ├── Type                         │    ├── Entity A

   └── Properties                   │    ├── Entity B

                                    │    └── Entity C

   a single, independently           │

   identifiable object              └── Relationships

                                         ├── A → B

                                         ├── B → C

                                         └── A → C

                                    a connected body of things

```

An entity is a thing. A lore document is a connected body of things.

### Applications Should Not Own the Data Model

A central architectural goal of Orbis is to avoid coupling the information model to a particular application. A worldbuilding tool, for example, shouldn't need to invent its own character model, location model, relationship model, serialization format, graph implementation, and storage mechanism — when those concepts can be provided by the underlying ecosystem instead:

```text

                 Application

                     │

                     ▼

              ┌──────────────┐

              │  Orb.Engine  │

              └──────┬───────┘

                     │

          ┌──────────┼──────────┐

          ▼          ▼          ▼

       Entities   Graphs     Documents

```

The application determines **how users interact with information.**

The engine determines **how that information exists and behaves.**

### Orbpad and the Orbis Ecosystem

Orbpad is envisioned as the first application built on the Orbis foundation. Rather than embedding the entire data engine directly into an application, Orbpad consumes the engine as a dedicated underlying system:

```text

Orbis Engine

     │

     │ structured information

     ▼

Orbpad

     │

     │ user experience

     ▼

Human

```

This separation also allows future applications to use the same underlying data model without becoming clones of Orbpad. The long-term goal isn't simply to build one application — it's to build a **foundation that multiple applications can understand.**

### Interoperability as a Principle

Orbis is designed around the belief that structured information should not be permanently trapped inside the application that created it. The engine therefore treats serialization, explicit types, stable identifiers, graph relationships, document boundaries, and storage as fundamental concerns — not implementation afterthoughts. A future application should ideally be able to consume an Orbis document without needing to understand the application that originally created it.

### The Long-Term Goal

```text

                 ┌───────────────┐

                 │   Application │

                 └───────┬───────┘

                         │

                         ▼

                 ┌───────────────┐

                 │ Orbis Engine  │

                 └───────┬───────┘

                         │

                    Structured

                   Information

                         │

              ┌───────────┼───────────┐

              ▼           ▼           ▼

           Orbpad      Tool A       Tool B

```

The application is replaceable. The information is not. That is the direction Orbis is being designed toward.

### Why Orb.Engine Exists

Orb.Engine is the first major foundation for this vision, providing the primitive building blocks the larger ecosystem is built on:

```text

OrbValue → OrbProperty → OrbEntity → OrbRelationship → OrbGraph

   → Serialization → Storage → Applications

```

The engine is deliberately smaller than the ecosystem it's intended to support. That's by design — a foundation should provide strong primitives without attempting to become every application built upon it.

---

# ✦ Core Concepts

Orbis Engine currently revolves around a small number of deliberately defined primitives.

## `OrbGraph`

The graph is the primary container for interconnected information. It maintains:

* Entities

* Relationships

* Graph connectivity

* Mutation invariants

* Traversal operations

* Neighbor discovery

* Reachability

Conceptually:

```text

OrbGraph

│

├── Entities

│   ├── Entity A

│   ├── Entity B

│   └── Entity C

│

└── Relationships

    ├── A → B

    ├── B → C

    └── A → C

```

The graph exposes read-only views of its underlying collections while retaining control over graph mutation internally.

## `OrbEntity`

An entity represents a single identifiable object within the Orbis model. An entity has:

* A unique `Guid` identifier

* A name

* An optional type

* A collection of properties

```csharp

var character = new OrbEntity(

    Guid.NewGuid(),

    "Arannis");

character.Type = "Character";

```

Entities are intentionally independent from the graph itself. This allows them to be created, serialized, stored, and manipulated before being inserted into a graph.

## `OrbRelationship`

A relationship connects two entities.

```text

Source

   │

   │ relationship

   ▼

Target

```

For example:

```text

King

 │

 └── rules → Kingdom

```

Relationships are first-class graph objects rather than strings embedded inside entity data. The graph enforces relationship validity, including the requirement that referenced source and target entities exist.

## `OrbProperty`

Properties attach structured information to entities and relationships. A property consists of:

```text

Name

Value

```

The value is represented through `OrbValue`, allowing the engine to maintain an explicit value type rather than treating everything as an untyped string.

```csharp

var age = new OrbProperty

{

    Name = "Age",

    Value = new OrbValue(

        OrbValueType.Integer,

        42L)

};

```

---

# ✦ The OrbValue System

`OrbValue` provides the engine's canonical typed-value model.

Supported `OrbValueType` categories include:

```text

Null · Boolean · Integer · Decimal

String · Guid · DateTime · List · Object

```

The codec also accepts CLR `float` and `double` inputs. These floating-point inputs are represented through the engine's `Decimal` value category rather than through a separate `OrbValueType.Double` category.

The purpose of this system is to prevent the graph from degenerating into an untyped collection of arbitrary CLR objects. For example:

```csharp

new OrbValue(OrbValueType.Integer, 42L);

```

is fundamentally different from:

```csharp

new OrbValue(OrbValueType.String, "42");

```

The engine therefore knows that the first value is an integer and the second is textual data.

---

# ✦ Nested Values

Orbis supports structured values inside lists and objects.

```csharp

var value = new OrbValue(

    OrbValueType.Object,

    new Dictionary<string, object?>

    {

        ["name"] = "Arannis",

        ["age"] = 42,

        ["active"] = true

    });

```

Nested values are processed recursively by the serialization system. This includes preservation of important CLR types such as:

* `Guid`

* `DateTime`

* Integer primitive types

* Floating-point CLR inputs (`float` and `double`)

* Decimal values

* Nested lists

* Nested objects

The engine's round-trip tests specifically exercise these cases.

---

# ✦ Graph Operations

`OrbGraph` provides operations for manipulating and querying graph structure.

**Entity operations**

```csharp

graph.AddEntity(entity);

graph.RemoveEntity(entity.Id);

graph.ContainsEntity(entity.Id);

graph.TryGetEntity(entity.Id, out var result);

```

**Relationship operations**

```csharp

graph.AddRelationship(relationship);

graph.RemoveRelationship(relationship.Id);

graph.ContainsRelationship(relationship.Id);

```

Relationships cannot be inserted against nonexistent entities. This prevents the graph from entering an invalid state such as:

```text

Relationship

     │

     ├── Source → DOES NOT EXIST

     │

     └── Target → Entity B

```

---

# ✦ Relationship Cascade

Removing an entity also removes relationships connected to that entity.

```text

A ─────→ B

│

└──────→ C

```

Removing `A` results in:

```text

B

C

```

with both relationships removed, maintaining the graph's referential integrity.

---

# ✦ Traversal

Orbis Engine provides graph traversal functionality for exploring connected entities.

```text

A

│

├── B

│   ├── D

│   └── E

│

└── C

    └── F

```

This forms the basis for higher-level operations such as:

* Dependency exploration

* Relationship navigation

* Connected-component analysis

* Knowledge exploration

* Worldbuilding relationship queries

* Graph-based application logic

Traversal semantics are explicitly tested to ensure deterministic and predictable behavior.

---

# ✦ Reachability

The graph can determine whether one entity can reach another through graph relationships.

```text

A → B → C → D

A ───────────────→ D

       reachable

```

This allows applications to reason about graph connectivity without manually implementing their own traversal logic.

---

# ✦ Serialization

Orbis Engine provides dedicated serializers for its two primary document concepts.

## `.entity`

An `.entity` document represents an individual entity.

```text

OrbEntity → EntitySerializer → .entity representation

```

```csharp

string json = EntitySerializer.Serialize(entity);

OrbEntity entity =

    EntitySerializer.Deserialize(json);

```

## `.lore`

A `.lore` document represents an entire graph.

```text

OrbGraph → LoreSerializer → .lore representation

```

```csharp

string json = LoreSerializer.Serialize(graph);

OrbGraph graph =

    LoreSerializer.Deserialize(json);

```

The `.lore` representation provides a natural way to persist interconnected collections of entities and relationships.

---

# ✦ Round-Trip Fidelity

Serialization is not merely intended to produce readable JSON. The engine explicitly tests:

```text

Object → Serialize → JSON → Deserialize → Object

```

with the goal of preserving the semantic structure of the original data — including nested `Guid`, `DateTime`, primitive CLR types, lists, objects, and `OrbValue` types.

A serializer that quietly turns a `DateTime` into an arbitrary string during a round trip may technically produce valid JSON, but it has destroyed information about the original value. Orbis treats that as a fidelity problem.

---

# ✦ Storage

The engine also provides storage abstractions for persisted entity and lore documents.

```csharp

// Entity storage

EntityStorage.Save(...);

EntityStorage.Load(...);

// Lore storage

LoreStorage.Save(...);

LoreStorage.Load(...);

```

The storage layer is intentionally kept separate from the graph and serialization models. This separation allows applications to change how documents are stored without changing the underlying graph model.

---

# ✦ Architecture

```text

┌──────────────────────────────────────────┐

│              Applications                 │

│                                           │

│  Orbpad · Worldbuilding · Other Tools     │

└─────────────────────┬──────────────────────┘

                       │

                       ▼

┌──────────────────────────────────────────┐

│              Orbis Engine                  │

│                                           │

│  Graph · Entities · Relationships          │

│  Properties · Typed Values                 │

└─────────────────────┬──────────────────────┘

                       │

          ┌────────────┴────────────┐

          ▼                         ▼

┌───────────────────┐    ┌───────────────────┐

│    Serialization   │    │      Storage       │

│                    │    │                   │

│  EntitySerializer  │    │  EntityStorage    │

│  LoreSerializer    │    │  LoreStorage      │

└──────────┬─────────┘    └──────────┬────────┘

           │                         │

           └────────────┬────────────┘

                        ▼

                 .entity / .lore

```

The engine is deliberately separated from the applications built on top of it. This means Orbpad, for example, doesn't need to become the place where graph logic, serialization rules, storage rules, and UI logic are all tangled together.

The engine owns the data model. Applications own the experience.

---

# ✦ Public API

The intended public API of Orb.Engine consists primarily of:

```text

OrbGraph          OrbValue

OrbEntity         OrbValueType

OrbRelationship

OrbProperty       EntitySerializer

                  LoreSerializer

                  EntityStorage

                  LoreStorage

```

Implementation details such as the recursive value codec and serialization document models are kept outside the intended public API surface. This boundary is tested as part of the project's API-surface test suite.

---

# ✦ Validation & Reliability

Orb.Engine has been developed around an adversarial testing philosophy rather than relying only on happy-path examples. The test system is divided into two complementary layers:

1. **Deterministic unit tests** establish the behavioral contract of individual engine features and invariants.
2. **Stress and torture campaigns** exercise the same engine under sustained mutation, scale, corruption, persistence, allocation pressure, and randomized workloads.

The current unit suite reports:

<div align="center">

### 🧪 251 / 251 tests passing

**0 failed · 0 skipped**

<img src="https://img.shields.io/badge/tests-251%20passing-2E7D32?style=for-the-badge&logo=checkmarx&logoColor=white" alt="251 tests passing"/>
&nbsp;
<img src="https://img.shields.io/badge/failed-0-2E7D32?style=for-the-badge" alt="0 failed"/>
&nbsp;
<img src="https://img.shields.io/badge/skipped-0-2E7D32?style=for-the-badge" alt="0 skipped"/>

</div>

The 251 unit tests cover the following aspects.

### Graph Core & Invariants

The graph tests cover entity insertion/removal, relationship insertion/removal, duplicate and invalid operations, referential integrity, relationship validity, entity-removal cascades, self-relationships, parallel relationships, removal idempotence, and protection against leaving stale adjacency state behind.

### Typed Values & Property Contracts

`OrbValue` is tested as a real typed-value system rather than an object bag. The suite exercises integer, boolean, decimal, floating-point CLR inputs (`float` and `double`), string, `Guid`, `DateTime`, null, list, and object values, including nested values and CLR-type preservation. Floating-point CLR inputs are represented through the engine's decimal value category; `OrbValueType` itself does not define a separate `Double` category. Integer contracts are deliberately strict: an `OrbValueType.Integer` value must contain an `Int64` (`long`) value.

### Serialization & Round-Trip Fidelity

Entity and lore serialization are tested through object → JSON → object round trips. Tests cover nested lists and objects, `Guid`, `DateTime`, primitive CLR types, property preservation, document boundaries, malformed input, and serializer failure behavior. The goal is semantic fidelity, not merely syntactically valid JSON.

### Storage Tests

Entity and lore storage tests exercise save/load behavior, document boundaries, missing files, invalid documents, and persistence reconstruction. Storage remains separated from the graph and serializer layers so the data model is not coupled to a particular persistence mechanism.

### Traversal & Reachability

Traversal tests cover deterministic traversal semantics, outgoing and incoming relationships, neighbor discovery, reachability, connected structures, and the behavior of graph traversal after mutation.

### Public API Boundary

The public API surface is tested so internal implementation details do not accidentally become part of the intended contract. The documented public surface centers on `OrbGraph`, `OrbEntity`, `OrbRelationship`, `OrbProperty`, `OrbValue`, `OrbValueType`, the serializers, and the storage APIs.

### Adversarial & Failure Behavior

The suite deliberately attacks the ugly cases: missing entities, invalid relationships, failed mutations, duplicate operations, stale references, malformed documents, unexpected values, and state changes around relationship deletion. A graph engine that only survives `A → B` is not a graph engine I would trust.

---

# ✦ Test Arsenal & 1.0.0 Verification

Orb Engine `1.0.0` was subjected to the full **Orb Engine Test Arsenal** using the following release-gate configuration:

- **Version:** `1.0.0`
- **Release grade:** **Grade A — Silver Standard**
- **Profile:** `full`
- **Seed:** `12345`
- **Configured operations:** `1,000,000`
- **Audit interval:** `10,000`
- **Target framework:** `.NET 10`

The release verification program was designed to test correctness, graph invariants, mutation integrity, deterministic behavior, serialization fidelity, persistence and recovery, memory behavior, allocation reuse, scalability, performance, fuzz resilience, and long-duration endurance.

The `1,000,000` operation setting is the configured workload supplied to each campaign. Individual campaigns interpret that workload according to their test design. Some execute one-million-operation workloads directly, some execute multiple independent workloads, and others distribute the configured workload across graph sizes or specialized benchmark stages.

## Benchmark Environment

| Component | Detail |
|---|---|
| **CPU** | AMD Ryzen 5 3450U |
| **GPU** | None / Not used |
| **RAM** | 16 GB |
| **OS** | Windows 10, version 22H2 (OS Build 19045.3803) |
| **Architecture** | x64 |
| **.NET SDK** | 10.0.400 |
| **Target framework** | net10.0 |
| **Build configuration** | Release |
| **Orb Engine version** | 1.0.0 |
| **Release grade** | Grade A — Silver Standard |
| **Profile** | full |
| **Seed** | 12345 |
| **Operations** | 1,000,000 |
| **Audit interval** | 10,000 |

All throughput, memory, and timing figures reported in this section were measured on the environment above. Absolute numbers will vary on different hardware; the release-gate pass/fail results are configuration-driven (profile, seed, operation count, audit interval) and are reproducible independent of the host machine.

## Verification Summary

| Campaign | Verification Purpose | Result |
|---|---|---|
| **Mutation Chaos** | Exercises randomized high-volume graph mutations to detect invariant violations, failed-mutation corruption, relationship inconsistencies, invalid state transitions, and query failures under sustained mutation pressure. | **PASS — 1,000,000-operation configuration.** The workload reached approximately **100,000 entities** and tens of thousands of relationships while maintaining graph integrity and query correctness. |
| **Deterministic Replay** | Verifies that deterministic operation journals produce reproducible execution and equivalent graph state when independently replayed. | **PASS — two independent 1,000,000-operation journals.** Journal determinism, reference-model agreement, replay equivalence, repeated audits, and graph invariants all passed. |
| **Mutation Performance** | Measures isolated entity and relationship mutation throughput and allocation behavior without including graph preparation, reference-model execution, or full validation in the timed mutation paths. | **PASS — 1,000,000-operation configuration.** The campaign completed successfully under the full release workload and established mutation-performance evidence for the stable release. |
| **Pool Reuse** | Verifies that released adjacency storage can be reused during repeated relationship allocation/removal cycles instead of causing continuous allocation growth. | **PASS — 1,000,000-operation configuration.** Reuse behavior was observed, including an effectively **0 B second-wave allocation measurement** in the dedicated reuse test. |
| **High-Degree Topology** | Exercises a pathological high-degree hub with large incident degree, parallel relationships, self-links, query operations, mass removal, and final graph validation. | **PASS — 1,000,000-operation configuration.** Verified a **100,000-edge hub** with approximately **50,000 outgoing**, **50,000 incoming**, and approximately **99,900 distinct neighbors**. |
| **Performance Benchmark** | Establishes representative throughput baselines across multiple graph sizes and identifies scaling characteristics for common engine operations. | **PASS — 1,000,000-operation configuration.** Representative graph operations were benchmarked through the configured scale points. The fastest recorded operation in the full run was **Entity / Contains (10,000)** at approximately **21.2 million operations/s**. |
| **Relationship Scaling** | Measures relationship-query behavior from small to large sparse graphs and identifies scaling characteristics that could indicate unintended dependence on total graph size. | **PASS — 1,000,000-operation configuration.** Tested graph sizes from **1,000 through 100,000 entities**. At 100,000 entities, representative throughput reached approximately **3.32M outgoing queries/s**, **3.10M incoming queries/s**, **1.21M all-relationship queries/s**, and **868K neighbor queries/s**. |
| **Memory Benchmark** | Measures managed-memory usage and working-set behavior across entity, relationship, property, and relationship-churn workloads. | **PASS — 1,000,000-operation configuration.** Memory behavior was measured through **100,000 entities**. Entity footprint stabilized near **271 B/entity**, while relationship/index footprint decreased toward approximately **418 B/relationship** at 100,000 entities. |
| **Differential Fuzzing** | Compares Orb Engine behavior with an independent reference model under deterministic randomized graph mutation workloads. | **PASS — 5,000,000 differential-fuzz operations.** Five independent seed families completed **1,000,000 operations each**, with the engine remaining equivalent to the reference model. |
| **Serialization Torture** | Exercises serialization and deserialization using large graphs, filesystem I/O, deterministic output checks, round-trip reconstruction, and malformed-input cases. | **PASS — 1,000,000-operation configuration.** Large-scale serialization testing reached **1,000,000 entities** and **1,000,049 relationships**, producing a lore document of approximately **374.97 MB**. |
| **Crash / Recovery Torture** | Verifies rejection of missing, empty, truncated, interrupted, corrupted, and structurally invalid documents and verifies restoration from known-good state. | **PASS — 1,000,000 recovery operations.** The workload included **549,088 injected fault conditions** and **49,923 disk-backed recovery cycles**. |
| **Soak / Endurance** | Detects cumulative state drift, memory retention, allocator degradation, persistence instability, validation failures, and throughput collapse under repeated engine lifecycle activity. | **PASS — 1,000,000 cycles.** Executed **700,476 mutation cycles** and **299,524 query-heavy cycles**, with **1,400,952 relationship additions**, **1,400,952 relationship removals**, **700,476 entity additions**, **700,476 entity removals**, **10,000 in-memory persistence round trips**, **1,000 disk persistence round trips**, and **100 full audits**. The canonical serialized state remained unchanged, final state remained **16 entities / 19 relationships**, and final validation passed. Retained managed-memory delta was **+255.77 KB**, with **5.49 MB peak managed heap** and **41.93 MB peak working set**. Final throughput was approximately **56,923 cycles/s**. |

## Release-Gate Result

The complete release verification program consisted of **12 campaigns**.

<div align="center">

<img src="https://img.shields.io/badge/Campaigns-12%2F12-2E7D32?style=for-the-badge" alt="12/12 campaigns"/>&nbsp;<img src="https://img.shields.io/badge/Checks-49%2F49-2E7D32?style=for-the-badge" alt="49/49 checks"/>&nbsp;<img src="https://img.shields.io/badge/Failures-0-2E7D32?style=for-the-badge" alt="0 failures"/>&nbsp;<img src="https://img.shields.io/badge/Profile-full-512BD4?style=for-the-badge" alt="Profile: full"/>&nbsp;<img src="https://img.shields.io/badge/Grade-A%20--%20Silver%20Standard-C0C0C0?style=for-the-badge&logo=shield&logoColor=white" alt="Grade A - Silver Standard"/>

</div>

The first ten campaigns — Mutation Chaos, Deterministic Replay, Mutation Performance, Pool Reuse, High-Degree Topology, Performance Benchmark, Relationship Scaling, Memory Benchmark, Serialization Torture, and Crash / Recovery Torture — completed as a single shared release-gate run:

```text
Campaigns: 10/10
Checks:    47/47
Status:    PASS
```

The remaining two campaigns, **Differential Fuzzing** (5,000,000 fuzz operations across five independent seed families) and **Soak / Endurance** (1,000,000 cycles), are independent long-form workloads and are tracked and reported separately from the shared release-gate run above. Combined across all twelve campaigns, the full release verification program reports:

```text
Campaigns: 12/12
Checks:    49/49
Status:    PASS
Failures:  0
```

**12 campaigns · 49 checks · 0 failures · full profile · 1,000,000 configured operations · Grade A — Silver Standard.**

The distinction around "configured operations" is intentional and worth preserving: it keeps this documentation auditable rather than implying that every campaign literally performed one million iterations of the same operation. Each campaign's actual workload — direct, distributed across graph sizes, or benchmark-staged — is documented per-campaign in the Verification Summary table above.

## What Each Stress Layer Proves

### Mutation Chaos — “Can the graph survive violence?”

Mutation Chaos exists to expose invariant bugs that ordinary examples never touch. Random additions, removals, relationship churn, and changing graph shapes are useful for finding stale indexes, incorrect counts, invalid references, and mutation-order bugs. The million-operation run passed.

### Deterministic Replay — “Can I reproduce the same universe twice?”

The same generated journal is replayed against independent graphs. This is valuable because deterministic state transitions make future bugs reproducible rather than mystical. A failure at operation 713,492 should be replayable from the same seed and journal instead of disappearing when the developer looks at it.

### Mutation Performance — “Is the core fast without benchmark contamination?”

This campaign isolates actual mutation calls from graph setup, reference-model work, and full validation. It measures entity mutation, relationship mutation, entity removal with edges, slab reuse churn, and mixed mutation while also reporting allocation per operation.

### Relationship Scaling — “Does query cost scale with the question, or with the whole universe?”

Sparse graphs are deliberately used here because they make pathological full-graph scanning easier to spot. Query throughput remains in the high hundreds of thousands to millions of operations per second at 100K nodes for the tested operations, while validation was separately exercised on large graphs.

### Memory Benchmark — “Does the data structure stay sane as it grows?”

The memory campaign measures entities, relationships plus indexes, sparse properties, and post-deletion retention. The important result is the consistency of the footprint at larger sizes rather than one magic memory number.

### Pool Reuse — “Does churn keep allocating forever?”

Allocator reuse is specifically tested because graph workloads commonly add/remove relationships repeatedly. The second allocation wave dropping to effectively zero is strong evidence that reusable capacity is doing real work rather than existing only as an architectural idea.

### High-Degree Topology — “What happens when one node becomes a monster?”

A sparse graph can make everything look beautiful. A high-degree hub is where adjacency handling, traversal, deletion, and validation can become pathological. The 100K-edge hub survived construction, inspection, and removal successfully.

### Differential Fuzzing — “Does the implementation agree with an independent model?”

This is one of the strongest correctness-oriented stress tests because it does not merely assert internal expectations. It compares Orb.Engine behavior against a separate reference model over deterministic randomized workloads. Five million randomized operations passed across five seeds.

### Serialization Torture — “Can the file representation carry a civilization?”

The serializer was taken beyond toy graphs into a one-million-entity dataset with more than one million relationships. Large-file write, read, deserialize, and re-serialize paths completed successfully, making this a meaningful persistence-scale test rather than a formatting check.

### Crash / Recovery — “What happens when persistence goes bad?”

The campaign deliberately generates empty, truncated, corrupted, and semantically invalid documents, then verifies that Orb.Engine rejects them instead of silently creating a questionable graph. It also exercises last-known-good recovery and repeated corruption/recovery. The million-operation endurance layer then repeats those scenarios at scale.

### Soak / Endurance — “Does the engine slowly become haunted?”

Soak testing is designed to find cumulative failures that do not appear in a single large burst: state drift, memory retention, pool degradation, slow performance collapse, and gradual corruption. The million-cycle run returned to the same canonical serialized state and finished with a final valid graph.

## Aggregate Assessment

The combined evidence is strong for a stable foundational engine release. The testing does not rely on a single score: correctness, determinism, graph invariants, scale, memory behavior, allocation reuse, serialization, persistence failure handling, randomized differential testing, and long-duration state stability were all exercised independently.

The most significant signal is **cross-test agreement**. The same internal structures survived very different forms of pressure: randomized mutation, deterministic replay, high-degree topology, allocator churn, massive serialization, corruption/recovery, and one-million-cycle endurance. That reduces the likelihood that the green results are an artifact of one narrow benchmark shape.

### 1.0.0 Stable Grade A Recommendation

<p>

<img src="https://img.shields.io/badge/Release-1.0.0-7C3AED?style=for-the-badge" alt="Release 1.0.0"/>&nbsp;<img src="https://img.shields.io/badge/Grade-A%20--%20Silver%20Standard-C0C0C0?style=for-the-badge&logo=shield&logoColor=white" alt="Grade A - Silver Standard"/>&nbsp;<img src="https://img.shields.io/badge/Campaigns-12%2F12-2E7D32?style=for-the-badge" alt="12/12 campaigns"/>&nbsp;<img src="https://img.shields.io/badge/Checks-49%2F49-2E7D32?style=for-the-badge" alt="49/49 checks"/>&nbsp;<img src="https://img.shields.io/badge/Status-Stable-2E7D32?style=for-the-badge" alt="Status: Stable"/>

</p>

**Recommendation: Version 1.0.0 qualifies as a Grade A — Silver Standard stable release and is suitable as the foundational core of the Orbis ecosystem within the tested and documented scope.**

The recommendation is based on the combination of the 251-test unit suite and the independent stress campaigns described above: 12 campaigns, 49 checks, 0 failures, run under the `full` profile with seed `12345` and 1,000,000 configured operations. Grade A does not mean that every possible workload, platform, failure mode, or future compatibility requirement has been proven. It means the current release has met the defined stable-core verification requirements for the capabilities and workload classes that were actually tested, and no unresolved correctness failure was observed in those release-gate paths.

In practical terms, 1.0.0 Grade A means:

- Core graph invariants and mutation behavior passed the full unit and adversarial test suite.
- Deterministic replay and differential testing found no divergence in the tested million-operation workloads.
- Large sparse graphs and high-degree relationship topologies passed their dedicated stress campaigns.
- Memory footprint, allocation reuse, and mutation performance were measured under scale and churn without a detected runaway-retention failure.
- Serialization and persistence survived million-entity, corruption, recovery, and repeated round-trip workloads.
- One-million-cycle soak testing completed without canonical-state drift, final validation failure, or retained-memory growth beyond the defined tolerance.

The Grade A boundary is explicit: the release is recommended for applications and workloads covered by these verification areas. It is not a blanket claim that every conceivable use of Orb.Engine has been tested.

---

# ✦ Stable Release Grade

<div align="center">

## ✅ `1.0.0` · Stable · **Grade A — Silver Standard**

<img src="https://img.shields.io/badge/Grade-A-C0C0C0?style=for-the-badge&logo=shield&logoColor=white" alt="Grade A"/>
&nbsp;
<img src="https://img.shields.io/badge/Standard-Silver-C0C0C0?style=for-the-badge" alt="Silver Standard"/>
&nbsp;
<img src="https://img.shields.io/badge/Unit%20Tests-251%2F251-2E7D32?style=for-the-badge" alt="251/251 unit tests"/>
&nbsp;
<img src="https://img.shields.io/badge/Fuzz%20Ops-5M%20passing-512BD4?style=for-the-badge" alt="5,000,000 fuzz ops passing"/>

</div>

Version 1.0.0 is a **stable Grade A release**. The engine has completed the current correctness, mutation, scale, memory, serialization, persistence, recovery, fuzzing, and endurance verification program.

The public API and serialized document formats are treated as the defined 1.0.0 release surface. Changes after 1.0.0 should be evaluated under the Orbis release-grade system and the compatibility policy declared for the affected release.

### 1.0.0 Verification Snapshot

- **251 / 251 unit tests passing**
- **0 failed · 0 skipped**
- **5,000,000 differential-fuzz operations passing**
- **1,000,000 crash/recovery operations passing**
- **1,000,000 soak/endurance cycles passing**
- **1,000,000-entity serialization torture passing**
- **100,000-edge high-degree topology passing**
- **100K-scale memory, relationship-scaling, pool-reuse, and mutation-performance campaigns passing**

These results justify the **Grade A** release classification because they establish stable behavior across the engine's tested foundational responsibilities.

---

# ✦ Release Stability Grades

The Orbis release-grade system applies **only to stable releases**. It is deliberately limited to two grades. A grade describes the level of verification completed for a specific release; it does not replace the version number and it does not claim that software can never fail.

## 🥈 Grade A — Silver Standard

<p>
<img src="https://img.shields.io/badge/Grade-A-C0C0C0?style=for-the-badge&logo=shield&logoColor=white" alt="Grade A"/>
&nbsp;
<img src="https://img.shields.io/badge/Coverage-Tested%20Scope-2E7D32?style=for-the-badge" alt="Coverage: Tested Scope"/>
</p>

**Definition:** A stable release that has passed every mandatory release-gate test for its declared engine capabilities and tested workload classes, with no unresolved correctness failure observed in those release-gate paths.

A Grade A release must satisfy all of the following:

- The complete mandatory unit-test suite passes with **zero failures and zero skipped release-gate tests**.
- All stress campaigns designated as release gates for the declared scope pass.
- Core graph mutation, relationship integrity, serialization, persistence, recovery, memory, and performance behavior have been exercised at the release's documented test scales.
- No known unresolved defect remains that prevents a documented capability from functioning correctly within its tested scope.
- The release record identifies the workload classes and test limits that were actually verified.

**Use recommendation:** Developers may use Grade A releases for the specific purposes, capabilities, and workload classes explicitly covered by the release verification record. A Grade A designation does not approve deployment scenarios outside that verified scope.

## 🥇 Grade S — Gold Standard

<p>
<img src="https://img.shields.io/badge/Grade-S-FFD700?style=for-the-badge&logo=shield&logoColor=white" alt="Grade S"/>
&nbsp;
<img src="https://img.shields.io/badge/Coverage-Full%20Supported%20Scope-B8860B?style=for-the-badge" alt="Coverage: Full Supported Scope"/>
&nbsp;
<img src="https://img.shields.io/badge/Status-Not%20Yet%20Awarded-lightgrey?style=for-the-badge" alt="Status: Not Yet Awarded"/>
</p>

**Definition:** A stable release that satisfies every Grade A requirement and has additionally passed verification for **every documented supported capability, every declared supported workload class, and every officially supported runtime environment** included in the release contract.

A Grade S release must satisfy all of the following:

- Every Grade A requirement passes.
- Every documented public capability has a corresponding passing verification path.
- Required scale, memory, mutation, serialization, persistence, corruption/recovery, and long-duration endurance tests pass at the release's defined limits.
- The same release-gate suite passes on every officially supported platform/runtime configuration.
- Compatibility and migration tests for the release's declared API and file-format guarantees pass.
- No known unresolved critical or high-severity correctness defect exists in any documented supported capability.

**Use recommendation:** Developers may treat Grade S as the **Gold Standard for general-purpose development within the complete documented and officially supported scope of the engine**.

### Grade Relationship

```text
Grade A — Silver Standard
    Stable within the tested and documented scope.

Grade S — Gold Standard
    Grade A + complete supported-scope verification across
    capabilities, workload classes, environments, and compatibility
    guarantees defined by the release contract.
```

A Grade can move **upward** between stable releases when additional verification is completed. A release does not receive Grade S merely because it is older, widely used, or has accumulated a large test count; the grade is awarded from the verification evidence for that specific release.

---

# ✦ What Orb.Engine Is Not

Orb.Engine is intentionally **not**:

* A graphical editor

* A complete worldbuilding application

* A database server

* A cloud platform

* A UI framework

* A game engine

* A replacement for a relational database

* A general-purpose ORM

It is a **core structured-information and graph engine**. Higher-level applications can build on top of it.

---

# ✦ Intended Ecosystem

Orbis is being developed as an ecosystem rather than a single monolithic application.

```text

                    ORBIS

                      │

          ┌───────────┴───────────┐

          │                       │

          ▼                       ▼

     Orb.Engine                Applications

          │                       │

          │              ┌────────┼────────┐

          │              │        │        │

          ▼              ▼        ▼        ▼

     Graph/Data        Orbpad   Tools   Future Apps

     Foundation

```

The engine provides the underlying structured information model. Applications provide domain-specific interfaces and workflows — the same underlying graph engine can potentially support very different applications without forcing those applications to reinvent the data layer.

---

# ✦ Installation

Once published, Orb.Engine can be consumed as a .NET package.

```bash

dotnet add package Orb.Engine --version 1.0.0

```

Or from a project file:

```xml

<ItemGroup>

  <PackageReference Include="Orb.Engine" Version="1.0.0" />

</ItemGroup>

```

> **Release status:** `1.0.0` is a stable Grade A release. Future stable releases are evaluated under the Orbis release-grade system.

---

# ✦ Quick Example

A minimal graph can be created with entities and a relationship:

```csharp

using Orb.Engine.Graph;

using Orb.Engine.Types;

var graph = new OrbGraph();

var author = new OrbEntity(

    Guid.NewGuid(),

    "Subhradeep Sarkar");

author.Type = "Person";

var project = new OrbEntity(

    Guid.NewGuid(),

    "Orbis");

project.Type = "Project";

graph.AddEntity(author);

graph.AddEntity(project);

var relationship = new OrbRelationship(

    Guid.NewGuid(),

    author.Id,

    project.Id,

    "created");

graph.AddRelationship(relationship);

```

The resulting conceptual graph is:

```text

Subhradeep Sarkar

        │

        │ created

        ▼

      Orbis

```

The exact API is defined by the `1.0.0` stable release surface; future changes should follow the applicable release policy.

---

# ✦ Project Structure

```text

Orb/

│

├── Orb.Engine/

│   │

│   ├── Graph/

│   │   ├── OrbGraph

│   │   ├── OrbEntity

│   │   ├── OrbRelationship

│   │   └── OrbProperty

│   │

│   ├── Types/

│   │   ├── OrbValue

│   │   └── OrbValueType

│   │

│   ├── Serialization/

│   │   ├── EntitySerializer

│   │   ├── LoreSerializer

│   │   └── internal serialization infrastructure

│   │

│   ├── Storage/

│   │   ├── EntityStorage

│   │   └── LoreStorage

│   │

│   └── Orb.Engine.csproj

│

├── Orb.Engine.Tests/

│   ├── Graph/

│   ├── Serialization/

│   └── PublicApiSurfaceTests

│

├── docs/

│   └── orbis.logo

│

├── README.md

├── CHANGELOG.md

└── LICENSE

```

The exact internal directory structure may evolve as the engine develops.

---

# ✦ Development

Clone the repository:

```bash

git clone https://github.com/Phantom-Con-Artist/Orb.git

cd Orb

```

Build:

```bash

dotnet build

```

Run the complete unit test suite:

```bash

dotnet test

```

Run the stress-test arsenal:

```bash

dotnet run --project Orb.Engine.StressTests -- list

```

Build a release configuration:

```bash

dotnet build -c Release

```

Create the NuGet package:

```bash

dotnet pack -c Release

```

---

# ✦ Design Principles

### 1. Structure over convenience

Data should have a defined meaning rather than being converted into strings simply because strings are convenient.

### 2. Explicit invariants

Invalid graph states should be prevented rather than silently tolerated.

### 3. Round-trip fidelity

Serialization should preserve semantic information.

### 4. Small public API

Implementation details should remain implementation details.

### 5. Separation of concerns

Graph logic, serialization, storage, and applications should remain distinct.

### 6. Test the ugly cases

A graph engine isn't trustworthy merely because `A → B` works. It needs to survive:

```text

missing entities · invalid relationships · failed mutations

nested objects · nested lists · type conversions

serialization failures · round trips · unexpected values

```

without quietly corrupting state.

---

# ✦ Roadmap

The `1.0.0` Grade A release establishes the stable foundation for the next stages of Orbis.

* API stabilization

* Expanded documentation

* Additional storage capabilities

* More serialization tooling

* Performance benchmarking

* Expanded graph algorithms

* Application-layer integrations

* Orbpad integration

* Additional Orbis ecosystem components

* Grade S qualification through broader compatibility and supported-environment verification

The roadmap may change as real-world usage exposes new requirements.

---

# ✦ Contributing

Contributions, ideas, bug reports, and technical discussion are welcome.

Before contributing major architectural changes, please consider opening an issue or discussion so that the intended design can be established before implementation begins.

For bug reports, include:

* Orbis Engine version

* .NET version

* Operating system

* Minimal reproduction

* Expected behavior

* Actual behavior

* Relevant exception or test output

---

# ✦ Community

<div align="center">

<a href="https://discord.com/invite/Em2ur4J8PF">

<img src="https://img.shields.io/badge/Join%20us%20on-Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Join us on Discord"/>

</a>

</div>

Questions, design discussion, and community support happen on Discord. It's the fastest way to reach the maintainer and other developers building on Orb.Engine outside of formal GitHub issues.

---

# ✦ License

Orb.Engine is released under the **GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later)**.

See [`LICENSE`](LICENSE) for the authoritative license text. This README does not grant a separate or alternative software license.

Repository branding, logos, badges, and other visual assets may be subject to separate terms where explicitly stated; their presence does not change the software license of Orb.Engine.

---

# ✦ The Philosophy

Orbis ultimately follows one principle:

<div *align*="center">

### **Don't build another place to store information.**

### **Build a system that understands what the information is.**

</div>

The project will continue to evolve, but `1.0.0` marks the first stable Grade A foundation of the engine. It establishes a verified stable core without claiming that the larger Orbis ecosystem is complete.

The ecosystem comes later. The foundation comes first.

---

# ✦ Author

<div *align*="center">

<h2>Subhradeep Sarkar</h2>

<p>Creator and maintainer of Orbis Engine</p>

<p>

  <a href="mailto:personalsarkar345@gmail.com">

    <img src="https://img.shields.io/badge/Email-personalsarkar345%40gmail.com-EA4335?style=for-the-badge&logo=gmail&logoColor=white" alt="Email"/>

  </a>

  <a href="https://www.linkedin.com/in/subhradeepcs">

    <img src="https://img.shields.io/badge/LinkedIn-Subhradeep%20Sarkar-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white" alt="LinkedIn"/>

  </a>

  <a href="https://subhradeepsarkarportfolio.pages.dev/">

    <img src="https://img.shields.io/badge/Portfolio-Subhradeep%20Sarkar-7C3AED?style=for-the-badge&logo=google-chrome&logoColor=white" alt="Portfolio"/>

  </a>

  <a href="https://discord.com/invite/Em2ur4J8PF">

    <img src="https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Discord"/>

  </a>

</p>

</div>

### Contact

**Email:** [personalsarkar345@gmail.com](mailto:personalsarkar345@gmail.com)

**LinkedIn:** https://www.linkedin.com/in/subhradeepcs

**Portfolio:** https://subhradeepsarkarportfolio.pages.dev/

**Discord:** https://discord.com/invite/Em2ur4J8PF

---

<div *align*="center">

<br/>

<strong>Orbis Engine</strong>

<br/>

Structured information. Connected by design.

<br/><br/>

© 2026 <strong>Subhradeep Sarkar</strong>. Orb.Engine is licensed under the GNU Affero General Public License v3.0 or later. See `LICENSE` for the authoritative license text.

</div>