# Changelog

All notable FoundationKit Core changes are recorded here. Repository history before the Core-only refocus remains available through Git, but removed application-specific release notes are not part of the active product documentation.

## Unreleased — Core vNext

- Refocused the repository on FoundationKit Core, Composer, and Workbench.
- Added immutable `FoundationProjectId`, host-local project context, and canonical project-scoped resource namespacing.
- Added project-isolation, SemVer, capability-contract, migration, and deprecation rules.
- Added Module/Service Engine v1 definitions and host-local registry.
- Added generic CRUD application orchestration over existing repository/UoW/specification/validation/result contracts.
- Added explicit mapper, manager, authorization, concurrency, and operation-observer extension points.
- Added fail-closed CRUD authorization when authorization is declared without an explicit semantic policy.
- Added EF concurrency translation and generic CRUD endpoint mapping.
- Added a Workbench SQL vertical proof for CRUD create/read/list/update/delete, concurrency conflict, manager override, auditing seam, OpenAPI, and Problem Details.
- Strengthened Core-only repository verification and CI.

## 0.1.0

- Closed the 17-package composable baseline on .NET 10.
- Added capability graph, profiles, contract compatibility, maturity evidence, deterministic Composer generation, interactive Composer questionnaire, packaging/security gates, and Workbench reference execution.
