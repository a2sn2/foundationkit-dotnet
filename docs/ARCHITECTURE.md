# FoundationKit Technical Architecture

## Purpose

FoundationKit is a composable .NET 10 system-building foundation. The active repository consists of 17 reusable packages, Composer tooling, catalog generation, tests, and one executable Workbench reference.

## Base layers

```text
Domain <- Application <- Infrastructure
             ^
             |
           WebApi

Blazor is client-oriented and does not depend on server persistence.
```

- **Domain**: entities, aggregate roots, value objects, domain events/exceptions.
- **Application**: results, validation, use-case ports, repositories/specifications, UoW, pagination, project/module/CRUD contracts.
- **Infrastructure**: provider-neutral EF adapters, domain-event dispatch, module registration helpers.
- **WebApi**: HTTP/Problem Details/correlation/security-header conventions and generic CRUD endpoint mapping.
- **Blazor**: typed API/error/state/ViewModel primitives.

Optional packages compose around those boundaries. Lower layers do not gain upper-layer dependencies for convenience.

## Project isolation

FoundationKit code is shared; application runtime state is not. Each host registers one immutable project identity and owns its DI container, configuration, policies, managers, database/provider, schema, migrations, credentials, and deployment. Shared provider resources must include the project namespace.

## Module/Service Engine

A module definition describes an entity/resource and selected reusable behaviors. CRUD v1 is the first executable module capability. The generic application service orchestrates validation, semantic authorization, mapping, repository/UoW persistence, concurrency policy, and post-success observers while business rules remain host-specific managers/policies.

```text
Request
  ↓
Generic CRUD service
  ├─ validation
  ├─ semantic authorization
  ├─ business manager hook
  ├─ mapper
  ├─ repository / UoW
  ├─ concurrency
  └─ success observers
  ↓
Result / Error
  ↓
Generic Web API mapper
  ↓
HTTP / Problem Details / OpenAPI
```

## Database ownership

Reusable packages do not own a product schema or migration. Workbench selects SQL Server and owns `WorkbenchDbContext` plus migrations. `FoundationKit.Infrastructure` may use provider-neutral EF Core APIs but contains no SQL Server provider dependency.

## Events and reliability

Current domain-event dispatch is in-process after successful persistence. It is not durable integration messaging, outbox/inbox, retry/dead-letter infrastructure, or a broker guarantee. Those remain separate roadmap capabilities.

## Composition model

The canonical capability graph, dependency resolver, profiles, contract versions, and maturity evidence live in Application. Generated catalog JSON is checked for drift. Composer consumes the same model for validation, explanation, compatibility, deterministic generation, and interactive generation.

## Workbench

Workbench proves database → domain/application → API → client/reference behavior against a real SQL Server path. It is an executable reference, not a universal production deployment.

## Production boundary

Repository gates prove code/package/reference behavior. Production approval remains deployment-specific and requires external operational, security, governance, recovery, and compliance controls.
