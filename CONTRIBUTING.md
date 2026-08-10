# Contributing to FoundationKit

FoundationKit is a reusable system-building foundation. Changes must preserve small provider-neutral boundaries and must not turn one host's business rules into universal defaults.

## Required design note

Before a reusable increment, define:

`Problem → Evidence → Scope → API impact → Compatibility → Contract versioning → Tests → Migration → CI → Acceptance Criteria`

## Core rules

- Reusable code belongs under `src/FoundationKit.*` only when the boundary is independently useful.
- Product/provider decisions stay outside lower layers unless the package is explicitly a provider adapter.
- Reusable packages do not own host EF migrations or select SQL Server globally.
- No mutable global runtime state.
- New shared-resource providers must preserve `FoundationProjectId` namespacing.
- Business-specific validation/policy/manager logic stays in the consuming host.
- Transport contracts are not EF entities.
- A package is not created merely to complete a roadmap checkbox.

## Generated metadata

Capability and maturity JSON are generated views of the canonical C# model. Change the canonical model first and run the catalog generator; do not create an independent second truth.

## Verification

Before merge:

```bash
bash scripts/verify-repository.sh
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln -c Release
dotnet test FoundationKit.sln -c Release
dotnet run --project tools/FoundationKit.CatalogGenerator -c Release -- --check
bash scripts/pack.sh Release artifacts/packages
```

Runtime/Core integration changes also require the Workbench SQL smoke path.

## Compatibility

Follow `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`. Additive changes are preferred. Breaking public changes require an explicit major-version decision and migration guidance.
