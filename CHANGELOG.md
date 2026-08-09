# Changelog

All notable repository and package changes are documented here.

## [Unreleased]

### FoundationKit Core v0.1

- Expanded the reusable output from the original five architectural packages to the current **17 NuGet packages + 17 symbol packages** through evidence-driven optional/reference capabilities.
- Added Auditing, Security, Identity, Authorization, Workflow, Approvals, Notifications, SMTP, Settings, Feature Management, Localization, and Caching without moving product schemas/policies into the reusable foundation.
- Added Capability Model v1, seven composition profiles, strict project manifests, dependency resolution, exact capability contract-version compatibility, and machine-readable maturity evidence.
- Added `FoundationKit.Composer` commands for `capabilities`, `profiles`, `validate`, `validate --require-stable`, and `explain`.
- Closed **FoundationKit Core v0.1 Composable Baseline** without creating a speculative eighteenth runtime package.

### Workbench

- Established `FoundationKit.Workbench` as the executable architecture/reference consumer with connected User/Admin SQL-backed vertical slices.
- Added Blazor/MudBlazor, typed contracts/clients, Swagger/Postman, SQL Server migrations, Docker integration, and GitHub Pages reference surfaces.
- Added runtime consumer evidence for Settings, Feature Management, Localization, and Caching.

### Athar

- Added the complete Arabic reference product under `examples/Athar` with Domain/Application/Infrastructure/Contracts/API/Blazor projects.
- Added ASP.NET Core Identity, secure cookie authentication, account lifecycle, MFA, authorization, maker-checker, anti-CSRF, rate limiting, idempotency, optimistic concurrency, auditing, notifications/SMTP, SQL Server migrations, readiness, Docker, E2E, and backup/restore verification.
- Adopted reusable Security, Identity, Authorization, Workflow, Approvals, Auditing, Notifications, and SMTP boundaries while keeping product policy/schema/copy in Athar.

### Madar

- Added the operational case-management product under `apps/Madar` and integrated it into the solution, repository manager, Atlas, CI, security scanning, and SQL integration.
- v0.1 — authentication/authorization, SQL persistence, case lifecycle, audit, Arabic API/Blazor.
- v0.1.1 — readiness, bounded startup retry, operational launch integration.
- v0.2 — SLA target/breach/escalation evidence.
- v0.3 — append-only case comments.
- v0.4 — sensitive-case maker-checker approval gate using `FoundationKit.Approvals`.
- v0.5 — bounded operational notifications using `FoundationKit.Notifications` and optional SMTP.
- v0.6 — department queues/routing and Operator claim.
- v0.7 — department and membership administration.
- v0.8 — controlled transfer and reassignment.
- v0.9 — private append-only case attachments/documents.
- v0.10 — authorization-preserving SQL case search and same-scope operational reporting.
- Kept departments/routing, SLA, attachments, search, and reporting product-owned rather than creating speculative generic packages.

### Security, supply chain, and operations

- Added tracked-source secret scanning, repository hygiene/boundary checks, NuGet vulnerability audit, CycloneDX dependency inventory, package/publish SHA-256 evidence, CodeQL, Trivy, negative-security testing, container-hardening checks, and SQL-backed product regressions.
- Added production-oriented Athar configuration checks, trusted proxy handling, MFA step-up, production SQL transport/principal rules, protected Data Protection key support, and real backup/restore verification.
- Added repository security/risk/threat/production-governance documentation while explicitly retaining the experimental/pre-production boundary and Issue #35 before real Production.
- Refreshed .NET 8 servicing dependencies to the current 8.0.29 line where applicable, replaced deprecated `Azure.Identity 1.13.2` with 1.17.2, raised the SQL client floor to 5.1.9, and added Madar Docker Dependabot monitoring (PR #103).
- Tracked the required migration to .NET 10 LTS before .NET 8 end of support in Issue #104.

### Repository truth cleanup

- Reclassified repository roles consistently: Workbench = executable architecture/reference consumer, Athar = complete Arabic reference product, Madar = operational product through v0.10.
- Removed superseded Athar implementation/native-run documentation and legacy Athar run/stop wrappers replaced by `foundationkit.ps1` / `scripts/athar-product.ps1`.
- Synchronized architecture, package, capability, product, security, production-readiness, and new-product guidance with the current repository state.
- Preserved historical security/release evidence as audit history rather than deleting it as “unused”.

## [0.1.0] - 2026-08-06

### Added

- Five reusable FoundationKit packages for Domain, Application, Infrastructure, WebApi, and Blazor.
- Provider-neutral EF Core persistence adapters and in-process domain-event dispatch.
- Result mapping, correlation IDs, security headers, typed API results, and asynchronous UI state.
- Package, symbol-package, architecture-test, documentation, and CI foundations.
- Local SQL Server Workbench consumer, canonical capability catalog, generated capability documentation, Docker launchers, and persistence smoke testing.
- Blazor WebAssembly + MudBlazor client, shared API contracts, Swagger, Postman, and GitHub Pages deployment.
- Explicit User Full Stack and Admin Full Stack reference paths connected through a SQL-backed review workflow.

### Fixed

- Synchronous and asynchronous post-save domain-event interception.
- Event clearing before handler dispatch to prevent accidental redispatch after handler failure.
- Invalid JSON handling for successful typed HTTP responses.
