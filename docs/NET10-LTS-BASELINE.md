# FoundationKit .NET 10 LTS Baseline

Status date: **2026-08-09**.

## Decision

FoundationKit's active repository baseline targets **`net10.0`** and uses the .NET 10 LTS SDK/runtime line.

This is a coordinated support-lifecycle migration of the existing FoundationKit Core v0.1 baseline. It does not reopen the reusable capability-extraction cycle and does not add a runtime package.

## Coordinated scope

The migration keeps these surfaces aligned as one baseline:

- repository-wide `TargetFramework` = `net10.0`;
- `global.json` on the .NET 10 SDK line with feature-band roll-forward;
- ASP.NET Core, Identity, EF Core, and Microsoft.Extensions.DependencyInjection on the .NET 10 servicing line;
- Microsoft.Data.SqlClient on a version compatible with the EF Core 10 SQL Server provider;
- Workbench, Athar, and Madar build/publish/runtime targets;
- Workbench, Athar, and Madar Docker SDK/runtime images;
- Composer-generated projects;
- CI, CodeQL, Composer golden-generation, experimental-product, and Windows launcher SDK setup;
- .NET 10 API/analyzer compatibility fixes verified by the repository gates.

Unrelated major dependency upgrades are intentionally not coupled to this migration unless exact-head compatibility evidence requires them. UI, test-framework, and OpenAPI library majors therefore remain separate dependency decisions.

## Compatibility consequence

The reusable packages are now built for `net10.0`. A consumer that can only target .NET 8 cannot reference this active baseline directly.

This is a framework-target compatibility change in an evolving pre-1.0 repository. The NuGet package version remains `0.1.0` for the current repository baseline; package version, target framework, capability contract version, and capability maturity remain distinct signals.

## Preserved invariants

The migration does not change:

- **17 `.nupkg` + 17 `.snupkg`** reusable output;
- capability IDs or dependency graph;
- capability contract versions;
- capability maturity declarations;
- product-owned database schemas/migrations;
- Madar organization/routing/SLA/attachments/search/reporting ownership boundaries;
- the experimental/pre-production governance boundary.

## Verification requirement

A .NET 10 baseline change is accepted only when the exact final PR head passes the same repository gates used for the previous baseline, including:

- repository verification and tracked-source secret scanning;
- NuGet vulnerability audit and SBOM generation;
- Release build with analyzers;
- all FoundationKit, Workbench, Athar, and Madar tests;
- Composer deterministic generated-project restore/build/test;
- Workbench/Athar/Madar publish;
- Workbench/Athar/Madar SQL integration and product E2E paths;
- Security Scan, Trivy/negative-security checks, and CodeQL;
- Windows PowerShell launcher checks;
- exact 17+17 package artifact inspection.

## Governance boundary

Targeting a supported LTS framework improves the technical support baseline. It does **not** by itself grant Production Approval, ISO/IEC 27001 certification, independent Segregation-of-Duties evidence, production KMS/Vault/SIEM/SMTP/backup configuration, or legal/privacy approval.

Issue #35 remains the production-governance gate before real go-live.
