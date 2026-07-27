<!--
Sync Impact Report
- Version change: 1.0.0 → 1.0.1 (PATCH: clarify quality-gate working directory)
- Modified principles: none renamed or redefined
- Modified sections:
  - Development Workflow & Quality Gates: `dotnet restore/build/test` now run from `./src`
    (where the `.sln` resides) instead of "the project root", aligning with the AGENTS.md
    decision to generate all solution code and the solution file under `./src`.
- Added sections: none
- Removed sections: none
- Templates requiring updates:
  - ✅ .specify/templates/plan-template.md (generic "Constitution Check" gate references this file; no edits required)
  - ✅ .specify/templates/spec-template.md (no structural impact; no edits required)
  - ✅ .specify/templates/tasks-template.md (no structural impact; no edits required)
  - ✅ AGENTS.md (source of the `./src` decision; already consistent — this amendment aligns to it)
  - N/A .specify/templates/commands/*.md (directory does not exist in this project)
- Follow-up TODOs: none
-->

# SsoAdmin Constitution

## Core Principles

### I. Database-Sourced Truth (No Data Fabrication)

The system MUST NEVER present, return, or persist data that does not originate from the
database (or another verified, auditable system of record explicitly sanctioned as such).
Any value shown to a user, returned by an API, written to a log, or used in a business
decision MUST be traceable to a database record, and that provenance MUST be verifiable in
code review or via query. Placeholder values, mocked/synthetic data, silently-invented
defaults, or LLM-generated data MUST NOT reach production paths. When a required value is
missing, the system MUST fail explicitly (validation error, `NotFound`, etc.) rather than
substitute a fabricated or guessed value.

**Rationale**: This is an SSO administration backend; invented identity, permission, or
credential data is a direct security and trust failure, not a cosmetic bug.

### II. No Hardcoded Secrets — Externalized Configuration

Secrets (connection strings, API keys, certificates, signing keys, credentials) MUST NEVER
be committed to source code, checked into the repository, or embedded as literals. Every
constant that represents an environment-specific concern — connection strings, file system
paths, URLs/endpoints, timeouts, feature flags, and other internal parameters — MUST be
externalized to configuration (e.g., `appsettings.json`, environment variables, or a secrets
manager) and injected via the standard .NET configuration/DI pipeline. Code MUST read such
values through configuration abstractions (`IOptions<T>`, `IConfiguration`, injected
settings objects) rather than `const`/literal values scattered through the codebase.

**Rationale**: Hardcoded secrets and environment-specific literals cause credential leaks,
block promotion across environments, and make the system impossible to reconfigure without
a rebuild.

### III. Verifiable Acceptance Criteria (NON-NEGOTIABLE)

Every acceptance criterion (each `Given/When/Then` scenario, each functional requirement,
each success criterion) defined in a feature specification MUST have at least one
corresponding automated test that fails without the implementation and passes with it. A
feature is not "done" until every acceptance criterion is covered and the mapping between
criterion and test is identifiable (e.g., via test name, comment, or traceability doc).
Untestable or unverifiable acceptance criteria MUST be rewritten until they are measurable
and testable before implementation begins.

**Rationale**: Acceptance criteria without tests are unverifiable claims; this project
requires objective, repeatable proof that behavior matches specification, not manual
assertion.

## Security & Configuration Standards

- Configuration MUST be layered per environment (Development/Staging/Production) using the
  standard .NET configuration providers; production secrets MUST come from a secrets
  manager or environment variables, never from files committed to source control.
- Domain entities MUST NOT be exposed directly in API responses; DTOs MUST be used at all
  API boundaries (per AGENTS.md).
- Logging MUST use `ILogger<T>`; `System.Console.WriteLine` MUST NOT be used for
  application logs.
- Asynchronous code MUST use `await` end-to-end; blocking calls (`.Result`, `.Wait()`) MUST
  NOT be introduced.

## Development Workflow & Quality Gates

- Every feature specification's acceptance scenarios and functional/success criteria MUST be
  mapped to test cases in `[NombreProyecto].Test` before the feature is considered complete
  (see Principle III).
- Before declaring any task complete, the following MUST be run from the `./src` directory
  (where the solution `.sln` resides, per AGENTS.md) and MUST pass: `dotnet restore`,
  `dotnet build`, `dotnet test`.
- Code review (self- or peer-) MUST explicitly check for: (a) any data path that could
  return non-database-sourced values, (b) any literal secret, path, URL, or internal
  parameter that should be externalized, and (c) any acceptance criterion lacking test
  coverage.

## Governance

This constitution supersedes conflicting ad-hoc practices for the SsoAdmin project.
AGENTS.md (and CLAUDE.md, which imports it) provides the day-to-day coding conventions and
runtime guidance that implement these principles; where AGENTS.md and this constitution
conflict, this constitution prevails and AGENTS.md MUST be updated to match.

**Amendment procedure**: Amendments are proposed via PR modifying this file, including a
Sync Impact Report as an HTML comment at the top of the file. Amendments MUST identify any
templates or guidance docs (plan/spec/tasks templates, AGENTS.md) that require updates as a
result, and those updates MUST land in the same PR or an immediately-following one.

**Versioning policy**: This constitution follows semantic versioning:
- MAJOR: Backward-incompatible governance changes or removal/redefinition of a principle.
- MINOR: A new principle or materially expanded section is added.
- PATCH: Clarifications, wording fixes, or non-semantic refinements.

**Compliance review**: All PRs/reviews MUST verify compliance with the Core Principles
above. Any deviation MUST be justified explicitly in the PR description and, if it recurs,
raised as a proposed amendment rather than repeatedly waived.

**Version**: 1.0.1 | **Ratified**: 2026-07-17 | **Last Amended**: 2026-07-27
