# Changelog

All notable repository and package changes are documented here.

## [Unreleased]

### Added

- Complete Arabic `Athar` production-reference product under `examples/Athar`.
- Six explicit Athar projects: Domain, Application, Infrastructure, Contracts, API, and Blazor Client.
- Arabic user experience for registration, login, initiative creation, status tracking, and administration.
- ASP.NET Core Identity, User/Administrator roles, secure cookie authentication, password policy, and lockout.
- Anti-CSRF endpoint and validation filter for all write operations.
- Authentication and write rate-limiting policies.
- Idempotent initiative creation using a unique `ClientRequestId` per owner.
- Optimistic concurrency through SQL Server `rowversion`.
- Initiative review records and audit entries.
- SQL Server Identity and product schemas with a complete initial migration and model snapshot.
- Live and ready health endpoints, startup migration retry, Swagger, Postman, Docker, and end-to-end CI smoke testing.
- Generic `EntityDto<TId>` and `AuditedEntityDto<TId>` application models.
- Blazor-oriented `ViewModelBase` and `ListViewModel<T>` for MVVM-style state ownership.
- Arabic production-readiness gate and new-project guide.
- Reserved `apps/` boundary for future real products.
- Madar product foundation under `apps/Madar` as the first real application consumer, with Domain, Application, Infrastructure, Contracts, API, Blazor Client, and `tests/Madar.Tests` projects.
- Madar's product-owned `Case` aggregate with bounded case types/priorities, assignment events, SQL Server `rowversion`, and the deterministic `new -> assigned -> in-progress -> resolved -> closed` lifecycle using `FoundationKit.Workflow`.
- Madar v0.1 runtime composition with ASP.NET Core Identity cookies, product roles/permissions, anti-CSRF write protection, FoundationKit rate-limit partitions, `CaseManager` orchestration, SQL Server persistence/migrations, and FoundationKit repository/unit-of-work reuse.
- Madar SQL-backed audit sink and authorized case timeline using `FoundationKit.Auditing`, preserving actor/correlation/action/attributes without moving product persistence into the reusable capability.
- Madar API surface for authentication, current user, operator discovery, case create/list/view/assign/transition, and case audit timeline, plus an Arabic Blazor login/case-list/create/details/lifecycle/timeline experience.
- Madar non-root Docker topology and SQL/auth/case/audit smoke workflow; pull-request CI now publishes Madar and verifies the complete first vertical slice against a real SQL Server container.
- Madar Application authorization tests covering create/audit, assignment permission and operator eligibility, assignee-owned progression, denied cross-operator progression, and scoped versus privileged case listing.
- Madar v0.1.1 database-backed `/health/ready` endpoint that verifies SQL connectivity and pending EF migrations without exposing connection strings or infrastructure details.
- Madar bounded database-startup policy and retry implementation, including migration-or-schema-validation modes plus direct retry/cancellation tests.
- Madar protected local Docker launcher with generated development credentials, fixed Compose project identity, bounded readiness wait, environment restoration, status/log/stop commands, and fail-closed Windows ACL protection.
- Madar as a first-class target in the unified Windows repository manager for start/status/logs/stop/open/LAN/doctor behavior while preserving the established Athar/Workbench Native `All` path.
- Madar Atlas/Pages group documenting all four current Blazor routes plus Swagger, anti-CSRF, liveness, readiness, product documentation, and operations guidance; route verification now reads actual Madar Razor `@page` declarations.
- Explicit Madar container Trivy gate for fixable HIGH/CRITICAL findings plus complete SARIF evidence uploaded to GitHub code scanning.
- Madar v0.2 product-owned SLA policy resolution keyed by case priority, with case-creation target snapshots, explicit `not-applicable` / `active` / `met` / `breached` state semantics, exact-target boundary tests, and persistent first-breach/escalation timestamps.
- Authorized bounded `POST /api/cases/sla/evaluate` orchestration for Supervisor/Administrator roles, including anti-CSRF/write-rate-limit protection, idempotent first-breach audit evidence, bounded batch processing, and `hasMore` continuation evidence without introducing a reusable jobs/scheduler package.
- Madar SLA SQL migration `20260808110000_AddMadarSla` with nullable target/breach/escalation columns and a due-case query index, plus API/Arabic Blazor surfaces for SLA target/state/breach/escalation evidence.
- Madar SQL integration smoke coverage for a short CI-only critical SLA target that proves target snapshot, elapsed breach, persisted escalation/audit, duplicate-safe re-evaluation, and a separate normal case that resolves within SLA.
- `FoundationKit Atlas`, a creative Arabic GitHub Pages portal that documents every Workbench and Athar Blazor route, core package, API surface, document, and operational proof.
- Pages manifest validation that compares documented UI routes with the actual Razor `@page` declarations.
- Detailed Arabic Visual Studio 2026 guide for SQL Server, User Secrets, startup projects, user/admin workflows, and troubleshooting.
- Capability Model v1 with dependency resolution, reusable profiles, project manifests, and a machine-readable capability graph protected by drift checks.
- `FoundationKit.Auditing` as the first extracted opt-in capability package with bounded provider-neutral audit contracts and sensitive-field rejection.
- FoundationKit Composer CLI v1 with strict manifest parsing, capability/profile discovery, dependency explanation, and fail-closed maturity validation.
- Capability contract compatibility v1 with machine-readable contract versions for every capability identity, optional exact `capabilityContracts` requirements in schema-v1 manifests, fail-closed compatibility enforcement, Composer diagnostics, and generated catalog drift protection without adding a new reusable package.
- Capability Maturity Evidence v1 with canonical evidence assessments, fail-closed promotion requirements for `ReferenceOnly`/`Preview`/`Stable`, generated machine metadata, and catalog/CI enforcement without adding a new reusable package.
- Capability Roadmap v1 and a shared Definition of Done for future reusable capabilities.
- `FoundationKit.Security` preview package with explicit trusted-proxy forwarding, reusable rate-limit partition keys, and shared `amr=mfa` authentication-assurance conventions.
- Athar adoption of `FoundationKit.Security` for trusted proxy handling, authentication/write partitioning, and administrator MFA authorization policy.
- `FoundationKit.Identity` reference capability package with reusable account policy, notification ports, security-event vocabulary, and explicit step-up requirements for sensitive account operations.
- Athar adoption of `FoundationKit.Identity` account policy and notification contracts while keeping ASP.NET Core Identity, Arabic product copy, token handling, and EF persistence in the product/adapters.
- `FoundationKit.Authorization` reference capability package with immutable permission descriptors, role-to-permission grants, authorization subjects, permission evaluation, and owner-or-privileged resource access.
- Athar adoption of semantic product permissions in `InitiativeManager`, replacing embedded administrator-role checks in business logic while retaining the existing coarse ASP.NET Core administrator policy.
- `FoundationKit.Workflow` first extraction with deterministic state/trigger transition definitions, fail-closed resolution, immutable transition records, and bounded Auditing integration.
- Athar adoption of a product-owned initiative review workflow for `submitted + approve/reject -> approved/rejected` while retaining aggregate validation, domain events, persistence, and concurrency.
- `FoundationKit.Approvals` reference capability with strict approve/reject decisions, permission-first maker-checker eligibility, Workflow resolution, and bounded approval audit intent.
- Athar adoption of `FoundationKit.Approvals` in the initiative review orchestration while retaining the aggregate self-review invariant, existing product persistence, audit entries, domain events, routes, DTOs, and concurrency behavior.
- `FoundationKit.Notifications` reference capability with bounded channel-neutral message/delivery contracts and sensitive-safe diagnostics.
- Athar account-security delivery split into an Identity/account formatting adapter and a provider-neutral notification boundary, keeping one-time tokens and Arabic product copy in Athar.
- `FoundationKit.Notifications.Smtp` reference provider package with validated SMTP transport options, provider-neutral delivery result mapping, caller-cancellation preservation, and a bounded observer that never receives recipient/body/token/credential/exception-object data.
- Athar adoption of the reusable SMTP provider while retaining product configuration keys, fail-closed production SMTP/TLS validation, secret ownership, and logging policy.
- `FoundationKit.Settings` reference capability with bounded keys/values, caller-defined opaque scopes, deterministic most-specific-first resolution, deterministic source precedence, and an in-memory reference source that rejects duplicate addresses.
- `FoundationKit.FeatureManagement` reference capability with bounded feature IDs, settings-backed Boolean enablement, explicit defaults, and fail-closed handling for invalid explicit configuration.
- Workbench runtime adoption of Settings and Feature Management through `GET /api/platform-reference`, covered by the SQL Server integration smoke workflow.
- `FoundationKit.Localization` reference capability with canonical culture metadata, BCL-derived RTL/LTR directionality, deterministic exact/parent/default fallback, explicit invalid-request provenance, and bounded provider-neutral time-zone identifiers.
- Workbench runtime adoption of Localization through the same platform-reference endpoint, proving `ar-YE` as `RightToLeft` and `UTC` as the configured time-zone identity in the SQL integration smoke workflow.
- `FoundationKit.Caching` reference capability with bounded byte-cache contracts, explicit TTL/hit/miss/remove semantics, caller cancellation, defensive snapshots, and a BCL-only bounded in-memory provider.
- Workbench adoption of Caching on the existing embedded capability-catalog read path, with direct consumer tests and repeated `/api/catalog` SQL-smoke coverage proving miss/fill then hit behavior.
- Repository consistency verification that derives the reusable package set from `src/FoundationKit.*` and fails if the human catalog or Atlas package cards drift from the actual projects.
- Tracked-repository hygiene gate that rejects committed build output, IDE state, local settings/secrets, logs, packages, backups, local databases, and private-key material independently of `.gitignore`.
- Canonical Arabic Windows first-run guide covering Native/Docker/Visual Studio paths, SQL Server instance overrides, port mapping, diagnostic commands, and safe troubleshooting evidence.

### Changed

- The repository now distinguishes reusable core, architecture Workbench, the Athar reference product, real applications beginning with Madar, and a dedicated static documentation portal.
- `FoundationKit.sln` and the normal solution build/test surface now include Madar while reusable package output remains seventeen NuGet packages plus seventeen symbol packages.
- Madar's first slice now uses FoundationKit `IRepository<Case, Guid>`, `EfRepository`, `IUnitOfWork`, `EfUnitOfWork`, authorization evaluation, auditing, and workflow boundaries instead of introducing duplicate product-level infrastructure abstractions.
- Madar case visibility is decided in the Application layer: ordinary users query created/assigned cases while privileged roles may select the all-cases query; Infrastructure does not infer authorization from a user identifier.
- Madar SLA duration is now resolved from Madar runtime configuration and snapshotted as an absolute case target at creation; changing a later configuration value therefore does not rewrite historical case expectations.
- Madar breach semantics now distinguish the deterministic contract-crossing instant (`SlaBreachedUtc`) from the first materialized escalation/evaluation time (`EscalatedUtc`), while late resolution can materialize the breach without waiting for a scheduler.
- Madar local SLA remains disabled by default; development placeholder durations and the intentionally short CI-only critical duration are not represented as production policy.
- CI publishes and tests Workbench, Athar, and Madar, with the SQL integration job now exercising Madar authentication, migration, readiness, SLA target/breach/idempotency, case assignment/lifecycle, Blazor/API surface, and persisted audit timeline in addition to existing Workbench/Athar checks.
- The Windows `doctor` surface and port diagnostics now include Madar readiness on `8100` and its SQL container host port `14335`; `All` retains backwards-compatible Native semantics while including Madar when the Docker operational path is available.
- FoundationKit Atlas now distinguishes Madar as the first real `apps/` product and verifies its live Razor routes alongside Workbench and Athar.
- `FoundationKit.sln`, repository verification, CI, documentation, and package versions now include Athar.
- GitHub Pages now deploys the standalone Arabic repository atlas instead of presenting one product client as the entire repository.
- Reusable package output increases to seventeen NuGet packages plus seventeen symbol packages after adding Caching to the Localization, Settings/Feature Management, and earlier reusable capability family.
- Capability extraction guidance requires a concrete consumer and a reusable independent boundary before creating a new package; Files/Documents, Jobs, Messaging, Idempotency, Concurrency, Organization, Multi-Tenancy, Search, Reporting, Privacy, Retention, Money, and Numbering remain Planned/ReferenceOnly where current evidence is product-specific or incomplete.
- `tooling-cli` maturity now reflects the implemented Composer validation/discovery/explanation surface as `ReferenceOnly`; interactive project generation remains explicitly Planned.
- The unified `foundationkit.ps1 pack` path delegates to canonical `scripts/pack.ps1` instead of maintaining a stale five-package list.
- The human package catalog, generated `FEATURES.md`, Atlas package cards, and root README now describe the same seventeen reusable projects.
- CI PowerShell syntax validation now includes the unified manager, Madar product launcher, and canonical pack script and aggregates parser errors across all checked scripts.
- `.gitignore` now covers a broader set of generated, local-data, certificate/private-key, backup, package, test, and tool artifacts while preserving explicit environment-template exceptions.
- The Windows `doctor` command now verifies that a .NET 8 SDK exists and reports visible SQL Server services, local port listeners, Git state, Docker readiness, and application health to make first-run failures diagnosable.
- Visual Studio guidance now distinguishes its launch-profile ports from root-manager ports and uses a password placeholder instead of publishing a reusable example administrator credential.
- GitHub workflow dependencies now use the Dependabot-proposed immutable revisions for `actions/checkout` v7.0.1, `actions/setup-dotnet` v6.0.0, and `actions/upload-artifact` v7.0.1 instead of revisions that trigger the hosted-runner Node.js 20 deprecation path.

### Fixed

- Workbench local `.local/workbench-product.env` credentials are now ACL-restricted to the current Windows account, matching the existing Athar local-secret posture.
- Athar Native startup now restores every process-level environment variable used for child launch after the application process is spawned, preventing the calling PowerShell session from retaining the local connection string or administrator password.
- `foundationkit.ps1 doctor` now probes explicit IPv4 loopback health endpoints with a bounded retry and distinguishes tracked/listening-but-unhealthy runtimes from truly stopped applications, preventing false `[STOPPED]` diagnostics observed during local dual-runtime testing.
- Madar local startup no longer reports a successful handoff when readiness never arrives; it fails after a bounded wait and prints Compose diagnostics while preserving secrets outside the process environment after invocation.
- Madar local log/stop operations now provide the required Compose substitutions even when only inspecting or shutting down an existing stack, avoiding configuration-parse failures caused by required development variables.

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