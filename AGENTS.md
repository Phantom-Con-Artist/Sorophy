# Sorophy™ — Agent Development Instructions

## Project Identity

Project: Sorophy™
Architecture: The Saga Architecture
Current Major Version: 2.0.0
Codename: Krono
Current Development Branch: v2.0.0-beta

Sorophy™ is the reference implementation of The Saga Architecture, a temporal graph evolution core for structured, connected, and evolving information.

---

## Current Objective

The current objective is to develop Sorophy™ v2.0.0 "Krono" toward a stable release.

This is an active architectural development phase.

Prioritize:

- Correctness
- Architectural integrity
- Deterministic behavior
- Temporal correctness
- Graph integrity
- Evolution and history correctness
- Serialization correctness
- Performance
- Test coverage
- API stability
- Backward compatibility where intentionally supported

Do not prioritize cosmetic changes over correctness.

---

## Core Principle

The Saga Architecture is based on:

> Information should be structured with Time.

Sorophy should understand structure, state, relationships, history, transitions, and evolution.

The engine should understand structure, not application-specific semantics.

Do not introduce domain-specific semantic assumptions into the core engine unless explicitly requested.

---

## Terminology

Use these names consistently:

- The Saga
- The Saga Architecture
- Sorophy™
- Sorophy Engine
- Krono
- Myriad Ecosystem
- WYRD Architecture
- KRONO Architecture

Do not rename established project terminology.

Do not reintroduce:

- Orb Engine
- Orb
- Orbis
- Worldscaper

unless explicitly discussing historical lineage or migration documentation.

---

## Architectural Rules

Do not perform broad architectural changes without explicit approval.

Do not:

- Rewrite major subsystems without approval
- Introduce unnecessary abstractions
- Replace working implementations merely for stylistic reasons
- Rename public APIs without approval
- Change public contracts without approval
- Change serialization formats without approval
- Change temporal semantics without approval
- Change graph invariants without approval
- Modify licensing or trademark language
- Modify repository branding

Prefer small, focused, testable changes.

Before proposing a large refactor, explain:

1. Why it is necessary
2. What it changes
3. What existing behavior may be affected
4. How it will be tested

---

## Code Changes

Before modifying code:

1. Inspect the relevant implementation.
2. Inspect related tests.
3. Understand existing invariants and contracts.
4. Identify the smallest appropriate change.

After meaningful code changes:

1. Build the solution.
2. Run relevant tests.
3. Run broader tests when appropriate.
4. Report failures clearly.
5. Do not claim success without actually running validation.

Never hide or ignore test failures.

---

## Tests

Existing tests are part of the project's behavioral contract.

Do not delete, weaken, disable, or bypass tests merely to make a change pass.

When behavior changes intentionally:

- Update existing tests where appropriate.
- Add regression tests for newly introduced behavior.
- Preserve unrelated test coverage.

Stress tests are also considered important validation infrastructure.

---

## Git Safety

Do not:

- Create branches
- Delete branches
- Rename branches
- Create tags
- Delete tags
- Push commits
- Force-push
- Reset history
- Rewrite history

unless explicitly instructed.

Do not commit changes unless explicitly instructed.

Before any destructive Git operation, ask for confirmation.

---

## File Safety

Only modify files relevant to the requested task.

Do not modify unrelated repositories.

Do not modify files outside the Sorophy workspace.

Do not delete files unless the task explicitly requires deletion.

Do not modify generated artifacts unless explicitly requested.

---

## Dependencies

Do not add a dependency simply because it makes implementation easier.

Before adding a new dependency, explain:

- Why it is needed
- What problem it solves
- Whether the existing framework can solve the problem
- Its impact on the project

Wait for approval before adding significant dependencies.

---

## Documentation

Keep documentation consistent with the actual implementation.

Do not invent features that do not exist.

Do not claim stability for features that are still experimental.

The current development state is:

Sorophy™ v2.0.0 "Krono" — Beta.

---

## Agent Behavior

You are an engineering assistant, not the project owner.

You may:

- Inspect
- Analyze
- Explain
- Propose
- Implement explicitly requested changes
- Run validation

You must not independently redefine the project's architecture, roadmap, branding, licensing, or scope.

When uncertain between multiple architectural approaches, stop and explain the options rather than silently choosing a major direction.

When a requested change conflicts with existing architecture, identify the conflict before modifying code.

---

## Golden Rule

Prefer:

> Understand → Propose → Implement → Test

over:

> Guess → Rewrite → Hope