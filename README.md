# FoundationKit

FoundationKit is a composable .NET 10 full-stack system-building foundation. The repository focuses on reusable Core packages, deterministic composition tooling, and one executable Workbench that proves the architecture against a real SQL Server path.

## Current reusable surface

FoundationKit ships exactly **17 NuGet packages + 17 symbol packages**:

- base architecture: Domain, Application, Infrastructure, WebApi, Blazor;
- governance/security: Auditing, Security, Identity, Authorization;
- process/communication: Workflow, Approvals, Notifications, Notifications.Smtp;
- platform: Settings, FeatureManagement, Localization, Caching.

Package existence and capability maturity are separate concepts. Production approval is also separate from repository quality evidence.

## Core vNext direction

Core vNext adds a configuration-first application experience without abandoning Clean Architecture:

```csharp
services.AddFoundationProject("my-project");

services.AddFoundationEfCrudModule<
    Customer, Guid,
    CreateCustomerRequest,
    UpdateCustomerRequest,
    CustomerResponse,
    CustomerMapper,
    AppDbContext>(module => module
        .Named("Customers", "customers")
        .Crud()
        .Auditing()
        .Authorization()
        .Concurrency()
        .UseManager<CustomerManager>());
```

FoundationKit owns the repeatable orchestration; the application owns its entities, transport contracts, mapper, validators, authorization policy, business manager, database/schema, and provider configuration.

The first vertical proof covers database persistence, generic application CRUD, validation, authorization, concurrency, audit extension behavior, standard HTTP results/Problem Details, generic endpoints, OpenAPI, and SQL integration.

## Project isolation

Every host registers an immutable project identity. DI, configuration, business policies, database contexts, and module definitions remain host-local. Shared external-resource keys use the canonical project namespace. See `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`.

## Workbench

Workbench is the executable architecture/reference surface. It owns its SQL Server schema/migrations and proves Core behavior without moving its schema or business demonstration data into reusable packages.

Run on Windows:

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

Or run the API directly with a configured `ConnectionStrings:Workbench`:

```bash
dotnet run --project samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj
```

## Composer

`FoundationKit.Composer` uses the canonical capability graph for profile/capability discovery, strict manifest validation, dependency explanation, capability-contract compatibility, deterministic project generation, and an interactive questionnaire. Generated projects use only reusable runtime packages that actually exist.

## Quality gates

Pull requests verify tracked-source hygiene, architecture boundaries, generated metadata, Release build, tests, package count/integrity, dependency/SBOM evidence, security scans, Composer generation, Workbench publish, and SQL integration.

## Key documentation

- `docs/ARCHITECTURE.md`
- `docs/PACKAGES.md`
- `docs/WORKBENCH.md`
- `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`
- `docs/CRUD-MODULE-ENGINE.md`
- `docs/CORE-VNEXT-119-DECISION.md`
- `docs/CAPABILITY-ROADMAP-V1.md`
- `docs/PRODUCTION-READINESS-AR.md`

License: MIT.
