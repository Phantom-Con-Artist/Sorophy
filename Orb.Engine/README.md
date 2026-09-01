<div align="center">

  <p>
    <img src="docs/orbenginelogo.png"
         alt="The Orb Engine"
         width="90"/>
    &nbsp;&nbsp;&nbsp;
    <img src="docs/orbisprojectlogo.png"
         alt="Orb Project"
         width="90"/>
  </p>

  <h1>The Orb Engine</h1>

  <p>
    <strong>
      A structured data and graph engine for building interconnected information.
    </strong>
  </p>

  <p>
    Entities · Relationships · Typed Values · Graphs · Serialization · Storage
  </p>

  <br/>

  <img src="docs/orbisprojectcover.png"
       alt="Orb Project"
       width="900"/>

  <br/><br/>

  <img src="docs/orbenginecover.png"
       alt="The Orb Engine"
       width="900"/>

  <br/><br/>

<div align="center">

[![GitHub](https://img.shields.io/badge/GitHub-Orb-181717?style=for-the-badge&logo=github)](https://github.com/Phantom-Con-Artist/Orb)
[![Version](https://img.shields.io/badge/Version-1.0.0--beta.1-7C3AED?style=for-the-badge)](https://github.com/Phantom-Con-Artist/Orb/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-GPL--3.0-2E7D32?style=for-the-badge)](LICENSE)

</div>

  <p>
    <strong>🚧 1.0.0-beta.1</strong>
    <br/>
    The API and file formats may change before stable release.
  </p>

</div>

---

<div align="center">

### Build information into structure.

</div>

Orbis Engine is the foundational engine behind **Orbis**, designed to represent structured information as interconnected entities, properties, and relationships.

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
- [Beta Status](#beta-status)
- [What Orb.Engine Is Not](#what-orbengine-is-not)
- [Intended Ecosystem](#intended-ecosystem)
- [Installation](#installation)
- [Quick Example](#quick-example)
- [Project Structure](#project-structure)
- [Development](#development)
- [Design Principles](#design-principles)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
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
  identifiable object               └── Relationships
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

Supported value categories include:

```text
Null · Boolean · Integer · Decimal · Double
String · Guid · DateTime · List · Object
```

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
* Floating-point values
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
│                                            │
│  Orbpad · Worldbuilding · Other Tools     │
└─────────────────────┬──────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────┐
│              Orbis Engine                  │
│                                            │
│  Graph · Entities · Relationships          │
│  Properties · Typed Values                 │
└─────────────────────┬──────────────────────┘
                       │
          ┌────────────┴────────────┐
          ▼                         ▼
┌───────────────────┐    ┌───────────────────┐
│    Serialization   │    │      Storage       │
│                     │    │                    │
│  EntitySerializer   │    │  EntityStorage     │
│  LoreSerializer     │    │  LoreStorage       │
└──────────┬──────────┘    └──────────┬─────────┘
           │                          │
           └────────────┬─────────────┘
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

Orb.Engine has been developed with adversarial testing rather than relying exclusively on happy-path examples. The test suite currently covers:

* Integer contracts
* List and object contracts
* Property identity
* Entity serialization
* Lore serialization
* Document boundaries
* Graph mutation invariants
* Relationship validation
* Entity removal cascades
* Graph traversal
* Reachability
* Round-trip fidelity
* Nested `Guid` preservation
* Nested `DateTime` preservation
* Nested primitive CLR type preservation
* Serializer failure behavior
* Public API surface

<div align="center">

### 🧪 251 / 251 tests passing

**0 failed · 0 skipped**

</div>

The test suite is treated as part of the engine's V1 contract rather than merely as development tooling.

---

# ✦ Beta Status

<div align="center">

## 🚧 `1.0.0-beta.1`

</div>

Orbis Engine is currently in **beta**. The engine has completed its initial V1 architectural and behavioral audit, but the API and serialized formats should not yet be considered permanently frozen.

During the beta period:

* APIs may change.
* Serialization formats may change.
* Internal architecture may change.
* Additional invariants may be introduced.
* Breaking changes may occur between beta releases.

Applications integrating Orb.Engine should pin their dependency version rather than assuming compatibility across future beta releases.

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
     Orb.Engine               Applications
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
dotnet add package Orb.Engine --version 1.0.0-beta.1
```

Or from a project file:

```xml
<ItemGroup>
  <PackageReference Include="Orb.Engine" Version="1.0.0-beta.1" />
</ItemGroup>
```

> **Note:** During the beta period, package availability and distribution channels may change.

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

The exact API may evolve during beta development.

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

Run the complete test suite:

```bash
dotnet test
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

The current beta establishes the foundation for the next stages of Orbis.

* API stabilization
* Expanded documentation
* Additional storage capabilities
* More serialization tooling
* Performance benchmarking
* Expanded graph algorithms
* Application-layer integrations
* Orbpad integration
* Additional Orbis ecosystem components
* Stable V1 release

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

# ✦ License

Orbis Engine is released under the **GNU General Public License v3.0**.

See [`LICENSE`](LICENSE) for the complete license text. The intention of using the GPL is to ensure that the engine and derivative versions remain available under the same fundamental freedoms provided by the license.

---

# ✦ The Philosophy

Orbis ultimately follows one principle:

<div align="center">

### **Don't build another place to store information.**
### **Build a system that understands what the information is.**

</div>

The project is still young, and the architecture will continue to evolve. `1.0.0-beta.1` represents the first public beta of the engine foundation — not the completion of the larger Orbis vision.

The ecosystem comes later. The foundation comes first.

---

# ✦ Author

<div align="center">

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
</p>

</div>

### Contact

**Email:** [personalsarkar345@gmail.com](mailto:personalsarkar345@gmail.com)
**LinkedIn:** https://www.linkedin.com/in/subhradeepcs
**Portfolio:** https://subhradeepsarkarportfolio.pages.dev/

---

<div align="center">

<br/>

<strong>Orbis Engine</strong>

<br/>

Structured information. Connected by design.

<br/><br/>

© 2026 <strong>Subhradeep Sarkar</strong>. All rights reserved except where otherwise specified by the GNU General Public License v3.0.

</div>