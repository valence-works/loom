<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- PRINCIPLE_1_NAME -> Small Core, Strong Extension
- PRINCIPLE_2_NAME -> Declarative, Predictable Execution
- PRINCIPLE_3_NAME -> Framework-Agnostic Infrastructure
- PRINCIPLE_4_NAME -> Observable and Diagnosable Runtime
- PRINCIPLE_5_NAME -> Deliberate Evolution
Added sections:
- Purpose
- Core Philosophy
- Design Principles
- Non-Goals
- Extensibility Philosophy
- Stability and Compatibility
- Observability and Diagnostics
- Contribution Philosophy
- Long-Term Vision
- Final Principle
Removed sections:
- Placeholder SECTION_2 and SECTION_3 content
Templates requiring updates:
- UPDATED .specify/templates/plan-template.md
- UPDATED .specify/templates/spec-template.md
- UPDATED .specify/templates/tasks-template.md
- NOT PRESENT .specify/templates/commands/*.md
Follow-up TODOs: None
-->
# Loom Constitution

## Purpose

Loom is a lightweight .NET recipe engine for repeatable application composition,
provisioning, and setup through declarative executable recipes. It is intended to
be embedded into applications, developer tools, deployment helpers, and local
automation without imposing a hosting model or application architecture.

Loom is foundational infrastructure software. It MUST remain independent from any
single application framework, product domain, orchestration model, or deployment
target. Loom is not tied to Elsa Workflows, Orchard Core, ASP.NET Core, or any
other framework ecosystem, even when integrations with those ecosystems exist.

## Core Philosophy

Loom exists to make application composition explicit, repeatable, and maintainable.
The core MUST stay small, understandable, and predictable. Capabilities that
belong to a specific domain, host, storage model, expression language, serializer,
or provisioning target MUST live behind explicit extension points or in separate
extensions.

The project values clear abstractions, minimal framework coupling, developer
ergonomics, and composition over complexity. New abstractions MUST solve a current
problem that cannot be addressed cleanly with existing primitives. Features MUST
justify their complexity cost before entering the core.

The project resists feature bloat, speculative generality, and hidden execution
behavior. A simple implementation that preserves extension and maintenance
options is preferred over a clever implementation that expands the conceptual
surface of the engine.

## Core Principles

### I. Small Core, Strong Extension

The core engine MUST remain intentionally minimal. It provides recipe execution
primitives, stable contracts, and deliberate extension points; it does not absorb
domain policy, deployment opinions, or framework-specific behavior. Domain-specific
behavior MUST be implemented as extensions or host application code.

Rationale: a small core keeps Loom understandable, testable, embeddable, and
durable across different application architectures.

### II. Declarative, Predictable Execution

Recipes MUST describe executable application composition in a way that is
repeatable, inspectable, and predictable. Execution semantics MUST be observable:
ordering, inputs, outputs, failures, retries, and side effects MUST be represented
clearly enough for hosts and developers to reason about them.

Rationale: recipes are infrastructure for repeatability. Hidden control flow or
implicit behavior undermines trust in the engine.

### III. Framework-Agnostic Infrastructure

Loom MUST remain framework-agnostic foundational infrastructure. Integrations with
application frameworks, hosting environments, workflow systems, provisioning
targets, or storage backends MUST be optional and replaceable. The core MUST NOT
take dependencies that make one host model privileged over others.

Rationale: Loom is designed for embedding into applications and tooling, not for
becoming an extension of any single platform.

### IV. Observable and Diagnosable Runtime

Diagnostics are a first-class part of the runtime. Execution MUST expose enough
structured information for logging, telemetry, tracing, inspection, and failure
analysis. Failures MUST be understandable without relying on undocumented internal
state.

Rationale: provisioning and setup failures are operational problems. Developers
need clear evidence, not opaque runtime behavior.

### V. Deliberate Evolution

Public APIs MUST favor simplicity, clarity, and deliberate additive evolution.
Breaking changes require clear justification, migration guidance, and maintainer
approval. Experimental APIs MUST be marked as such and kept out of stable
contracts until real usage supports standardization.

Rationale: Loom is infrastructure. Consumers need confidence that embedding Loom
will not create unnecessary churn.

## Design Principles

- The core engine MUST remain intentionally minimal.
- Domain-specific behavior MUST live in extensions or host applications.
- Public APIs MUST favor simple names, clear contracts, and predictable behavior.
- Extensibility points MUST be explicit, documented, and deliberate.
- The project SHOULD prefer additive evolution over breaking redesigns when the
  result remains coherent.
- Execution semantics MUST remain observable and predictable across supported
  execution models.
- The engine SHOULD support both in-process and external execution models without
  privileging either model in the core.
- Features MUST justify their complexity cost, maintenance cost, and impact on
  the core conceptual model.
- Implementation choices SHOULD preserve long-term maintainability and developer
  ergonomics over short-term convenience.

## Non-Goals

Loom is not intended to become:

- A workflow engine.
- A general-purpose scripting runtime.
- An infrastructure-as-code platform.
- A deployment orchestrator.
- A distributed task scheduler.
- A background job framework.

The project intentionally focuses on executable application composition through
recipes. Adjacent capabilities may be integrated through extensions, but they do
not define the core product direction.

## Extensibility Philosophy

Applications own their domain logic. Loom provides infrastructure primitives,
execution contracts, and extension surfaces; it does not define domain policy for
business processes, deployment platforms, content systems, or application
frameworks.

The engine MUST remain open for extension without requiring forks. Serialization,
expressions, recipe sources, step resolution, execution behavior, diagnostics,
and host integration points SHOULD remain extensible through stable contracts.
Extensions MUST NOT compromise core simplicity or make optional behavior appear
required.

## Stability and Compatibility

The project SHOULD favor backwards compatibility where reasonable. Public APIs
MUST evolve deliberately, and breaking changes require documented justification,
migration notes, and a versioning decision.

Experimental features MUST be clearly marked. The project MUST avoid premature
standardization: APIs SHOULD become stable only after their responsibilities,
extension needs, and compatibility burden are understood.

## Observability and Diagnostics

Execution MUST be inspectable before, during, and after recipe runs where the
execution model permits it. Diagnostics MUST be first-class, including structured
logging, telemetry hooks, meaningful errors, and traceable execution state.

The project MUST avoid hidden execution behavior. Failures SHOULD identify the
recipe step, relevant inputs, failure category, and recovery context when that
information is available without leaking sensitive data.

## Contribution Philosophy

Contributions MUST align with this constitution. Simplicity is preferred over
cleverness, and new abstractions require strong justification. Discussions SHOULD
prioritize maintainability, clarity, compatibility, diagnostics, and the
long-term direction of the project over short-term feature accumulation.

Features are evaluated against their fit with Loom as lightweight,
framework-agnostic infrastructure. Contributors SHOULD favor extensibility over
specialization and extension packages over core expansion when behavior is tied
to a domain, host, or platform.

Behavioral changes MUST include appropriate xUnit coverage in `tests/Loom.Tests`.
Public API changes MUST include tests or documentation that demonstrate intended
usage and compatibility expectations.

## Governance

Loom uses lightweight, benevolent maintainer-led governance initially. Maintainers
are responsible for architectural coherence, release discipline, compatibility
decisions, and final interpretation of this constitution.

Open design discussion is encouraged. Major architectural changes, public API
redesigns, execution model changes, or new core extension mechanisms MUST be
documented through specifications or architecture decision records before
implementation is accepted.

Architectural coherence is prioritized over rapid feature growth. The roadmap
MUST remain intentional and focused on repeatable application composition,
provisioning, extensibility, diagnostics, and long-term maintainability.

Amendments to this constitution require maintainer approval, a documented reason,
and a semantic version update:

- MAJOR: governance or principle changes that redefine project direction or
  remove established guarantees.
- MINOR: new principles, new required sections, or materially expanded guidance.
- PATCH: clarifications, wording improvements, or non-semantic corrections.

Pull requests and specifications MUST be reviewed for constitutional alignment.
When a proposal conflicts with the constitution, the proposal MUST either change
or include an approved constitutional amendment.

## Long-Term Vision

Loom aims to become a reliable foundation for repeatable application composition
in modern modular .NET applications. It SHOULD enable reusable provisioning,
setup, and configuration infrastructure while remaining lightweight enough for
developers to understand and embed with confidence.

The long-term goal is a durable ecosystem primitive, not a monolithic platform.
Loom SHOULD make it practical for applications and tools to share recipe-based
composition behavior without inheriting unnecessary framework assumptions,
runtime weight, or orchestration policy.

## Final Principle

Loom MUST remain small enough to understand, yet extensible enough to grow with
the applications built upon it.

**Version**: 1.0.0 | **Ratified**: 2026-05-06 | **Last Amended**: 2026-05-06
