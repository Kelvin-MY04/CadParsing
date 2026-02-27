<!--
  Sync Impact Report
  ==================
  Version change: [PLACEHOLDER] → 1.0.0 (initial ratification — all tokens were blank)

  Modified Principles:
  - All principles: newly authored (template was fully un-filled)

  Added Sections:
  - I.   Meaningful Naming
  - II.  Single Responsibility
  - III. SOLID & Object-Oriented Design
  - IV.  DRY — Don't Repeat Yourself
  - V.   Defensive Error & Exception Handling
  - VI.  Test-Driven Development (NON-NEGOTIABLE)
  - Code Quality Standards
  - Development Workflow
  - Governance

  Removed Sections:
  - None (initial ratification)

  Templates Requiring Updates:
  ✅ .specify/templates/plan-template.md — "Constitution Check" gate is a generic runtime
     placeholder; no hardcoded principle names to update.
  ✅ .specify/templates/spec-template.md — "User Scenarios & Testing" section already mandates
     independent testability per story; fully compatible with Principle VI (TDD).
  ✅ .specify/templates/tasks-template.md — Already includes test-first ordering note
     ("Write these tests FIRST, ensure they FAIL before implementation"); compatible with
     Principle VI. Task categories (contract, integration, unit) match the three test layers.
  ✅ .specify/templates/agent-file-template.md — Generic template; no principle-specific
     references that conflict.
  ✅ .specify/templates/constitution-template.md — Source template; read-only reference.
  ⚠  .specify/templates/commands/ — No command files found; no updates needed.

  Deferred TODOs:
  - None. All fields resolved on initial ratification.
-->

# CadParsing Constitution

## Core Principles

### I. Meaningful Naming

Every identifier — variable, parameter, method, class, property, and file — MUST use a
descriptive, intention-revealing name. Abbreviations and cryptic shorthand are prohibited.

**Rules:**

- Variable and property names MUST describe the value they hold (e.g., `layerCount`, not `lc`).
- Method names MUST begin with a verb that conveys their single action
  (e.g., `ParseLayerData`, `ValidateInputFormat`).
- Class names MUST be nouns or noun phrases describing the entity or concept they represent.
- Boolean identifiers MUST read as a predicate (e.g., `isVisible`, `hasParseError`).
- Names MUST NOT require an accompanying comment to explain their meaning.
- Single-letter names are prohibited except for universally accepted short-scope loop counters
  (`i`, `j`, `k`).

**Rationale:** Readable code is maintainable code. Names that reveal intent eliminate the need
to reverse-engineer logic and reduce cognitive load during review and debugging.

### II. Single Responsibility

Every method MUST perform exactly one well-defined action. Every class MUST encapsulate a
single, coherent responsibility. Methods that grow beyond approximately 20 lines of meaningful
logic MUST be decomposed into smaller, named helpers.

**Rules:**

- A method MUST do one thing — and only that thing.
- Method bodies SHOULD NOT exceed 20 lines of logic (blank lines and comments excluded).
- If a method name requires a conjunction (e.g., `ParseAndValidate`), it MUST be split into
  two separate methods.
- Classes MUST NOT serve dual purposes without explicit justification documented in the
  Complexity Tracking table of the relevant plan.
- Large switch or if-else chains that delegate to fundamentally different behaviors MUST be
  refactored using polymorphism or strategy patterns.

**Rationale:** Short, focused methods are easier to test in isolation, easier to understand at a
glance, and simpler to change without cascading side-effects elsewhere in the system.

### III. SOLID & Object-Oriented Design

All code MUST adhere to the five SOLID principles and the foundational Object-Oriented Design
(OOD) tenets: encapsulation, abstraction, inheritance (where appropriate), and polymorphism.

**Rules:**

- **S — Single Responsibility**: Each class has exactly one reason to change (see Principle II).
- **O — Open/Closed**: Classes MUST be open for extension and closed for modification; use
  interfaces, abstract base types, and composition to add behavior without editing existing code.
- **L — Liskov Substitution**: Subtypes MUST be fully substitutable for their base types without
  altering the correctness of the program.
- **I — Interface Segregation**: Interfaces MUST be narrow and role-specific; no client MUST be
  forced to depend on methods it does not use. Prefer many small interfaces over one large one.
- **D — Dependency Inversion**: High-level modules MUST NOT depend on low-level modules; both
  MUST depend on abstractions. Dependencies MUST be injected rather than constructed internally.
- Favor composition over inheritance wherever practicable.
- Internal state MUST be encapsulated; direct field access across class boundaries is prohibited.

**Rationale:** SOLID principles produce loosely coupled, highly cohesive systems that resist
regression and accommodate change without costly rewrites or broad ripple effects.

### IV. DRY — Don't Repeat Yourself

Every piece of knowledge or logic MUST have a single, authoritative representation in the
codebase. Duplication of logic — not merely text — is prohibited.

**Rules:**

- Identical logic appearing in two or more places MUST be extracted into a shared method, class,
  or module upon first discovery.
- All numeric constants and string literals used in logic MUST be declared as named constants;
  magic values are prohibited.
- Copy-pasted code blocks MUST NOT be committed; reviewers MUST reject them.
- Configuration values used in more than one place MUST be centralized in a single source.
- Structural boilerplate (e.g., generated scaffolding) is exempt only when no practical
  abstraction exists; this exemption MUST be documented inline.

**Rationale:** Duplication multiplies the cost of every future change and is a leading cause of
inconsistency bugs. DRY ensures a single point of truth for each decision in the system.

### V. Defensive Error & Exception Handling

Every method that performs I/O, parses external input, calls external services, or executes any
operation that may fail MUST handle exceptions explicitly. Silent failures are strictly
prohibited.

**Rules:**

- Every public method MUST document the exceptions it may throw (via XML doc, docstring, or
  equivalent language convention).
- Exceptions MUST be caught at the layer that has sufficient context to recover or to add
  meaningful diagnostic information.
- Catch blocks MUST either recover meaningfully, rethrow with enriched context, or log and
  rethrow; empty catch blocks are prohibited without an explicit, commented justification.
- Error messages MUST be descriptive and actionable — they MUST state what failed, in what
  context, and where possible, what the caller can do about it.
- Resource cleanup (file handles, database connections, network sockets) MUST use `finally`
  blocks or language-idiomatic resource management (e.g., `using`, RAII).
- Domain-specific exception types MUST be defined and used where the error represents a known,
  named failure mode; generic exceptions are a last resort.

**Rationale:** Unhandled exceptions cause data corruption and silent data loss. Explicit handling
ensures failures surface quickly, are diagnosed accurately, and degrade gracefully.

### VI. Test-Driven Development (NON-NEGOTIABLE)

TDD is mandatory. Tests MUST be written before implementation code. The Red-Green-Refactor
cycle MUST be followed without exception. All three test layers — unit, integration, and
end-to-end — are required for every feature.

**Rules:**

- **Red**: Write a failing test capturing the desired behavior before writing any implementation.
- **Green**: Write the minimum code necessary to make the failing test pass — no more.
- **Refactor**: Improve clarity, remove duplication, and verify naming compliance while keeping
  all tests green.
- **Unit Tests**: MUST cover every public method in isolation; external dependencies MUST be
  replaced with mocks or stubs.
- **Integration Tests**: MUST verify correct interaction across component boundaries (e.g.,
  service-to-repository, parser-to-model, CLI-to-service).
- **End-to-End Tests**: MUST validate complete user workflows against real or near-real
  inputs and outputs without mocking infrastructure.
- Test coverage MUST remain at or above **80%** for all modules.
- Tests MUST be deterministic, isolated, and independent of execution order.
- No implementation code MUST be merged without corresponding passing tests for all three layers.
- Reviewers MUST verify that tests were authored before implementation (via commit history).

**Rationale:** TDD produces inherently testable designs, catches regressions at the moment they
are introduced, and documents intended behavior through executable specifications rather than
prose that can fall out of date.

## Code Quality Standards

Measurable quality gates enforced at every code review and in the CI/CD pipeline.

- **Method Length**: No method body MUST exceed 20 lines of logic. Methods exceeding this limit
  MUST include a documented justification in the pull request description.
- **Cyclomatic Complexity**: No method MUST have a cyclomatic complexity greater than 10.
  Violations require decomposition, not suppression.
- **Test Coverage**: Code coverage MUST be ≥ 80% per module. New code MUST NOT reduce the
  overall coverage percentage.
- **Naming Compliance**: Automated linting MUST enforce naming conventions as defined in
  Principle I. CI MUST fail on violations.
- **No Dead Code**: Unreachable code, unused variables, unused imports, and commented-out code
  blocks MUST NOT be committed.
- **No Magic Values**: All numeric constants and string literals used in logic MUST be declared
  as named constants (see Principle IV).
- **No Silent Exceptions**: Linting or static analysis MUST flag empty catch blocks. CI MUST
  fail on any suppressed exception without a justifying comment.

## Development Workflow

The mandatory sequence for every feature, bug fix, or refactoring task.

1. **Understand**: Read the spec and acceptance criteria before writing any code or tests.
2. **Design**: Identify classes, interfaces, and method signatures; validate against SOLID
   principles and Principle II (Single Responsibility) before proceeding.
3. **Test First (Red)**: Write failing unit tests for each intended method; write failing
   integration tests for each component boundary; define end-to-end test scenarios with
   expected inputs and outputs.
4. **Implement (Green)**: Write the minimum code necessary to satisfy each failing test.
5. **Refactor**: Improve clarity, remove duplication, verify DRY and naming compliance, and
   confirm all tests remain green.
6. **Self-Review**: Check the implementation against all six Core Principles before submitting.
7. **Code Review**: Submit for peer review; reviewer checks constitution compliance using the
   Code Quality Standards gate list above.
8. **CI Gate**: All tests MUST pass; coverage MUST meet the 80% threshold; linter MUST report
   zero violations.
9. **Merge**: Only after all CI gates pass and reviewer approval is obtained.

## Governance

This constitution supersedes all other coding practices, verbal agreements, and legacy
conventions in the CadParsing project.

**Amendment Procedure:**

1. Propose the amendment in writing, documenting: the change, the rationale, and the migration
   impact on existing code and dependent templates.
2. Obtain review and approval from at least one other contributor.
3. Bump the version following the semantic versioning policy below.
4. Update all dependent template files identified in the Sync Impact Report.
5. Update this document's `Last Amended` date to the date of ratification.

**Compliance:** All pull requests MUST be reviewed for constitution compliance. Reviewers are
empowered — and expected — to block merges for violations. Violations discovered post-merge
MUST be filed as follow-up issues and resolved within the next sprint.

**Versioning Policy:**

- **MAJOR**: Removal or fundamental redefinition of an existing principle.
- **MINOR**: Addition of a new principle or material expansion of guidance within an existing
  section.
- **PATCH**: Clarifications, wording improvements, typo fixes, or non-semantic refinements.

**Version**: 1.0.0 | **Ratified**: 2026-02-26 | **Last Amended**: 2026-02-26
