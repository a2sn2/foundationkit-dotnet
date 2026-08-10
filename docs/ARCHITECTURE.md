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
- **Application**: results, validation, use-case ports, repositories/specifications, UoW, pagination, project/module/CRUD contracts, capability composition, and provider-neutral durable-idempotency contracts.
- **Infrastructure**: provider-neutral EF adapters, explicit relational EF adapters where relational semantics are required, domain-event dispatch, and module registration helpers.
- **WebApi**: HTTP/Problem Details/correlation/security-header conventions, generic API/CRUD endpoint mapping, concurrency/idempotency HTTP orchestration, and runtime contract metadata.
- **Blazor**: typed API/error/state/ViewModel primitives.

Optional packages compose around those boundaries. Lower layers do not gain upper-layer dependencies for convenience.

## Project isolation

FoundationKit code is shared; application runtime state is not. Each host registers one immutable project identity and owns its DI container, configuration, policies, managers, database/provider, schema, migrations, credentials, and deployment. Shared provider resources must include the project namespace.

Durable idempotency follows the same rule: its database identity begins with `ProjectId`, so shared relational infrastructure does not collapse independent project key spaces.

## Module/Service Engine

A module definition describes an entity/resource and selected reusable behaviors. CRUD v1 is the first executable module capability. The generic application service orchestrates validation, semantic authorization, mapping, repository/UoW persistence, concurrency policy, and post-success observers while business rules remain host-specific managers/policies.

Module composition distinguishes declared capability intent from deterministic dependency-expanded effective intent. That effective set is composition metadata, not proof that an environment-specific provider/store/transport has been provisioned.

```text
Request
  ↓
API Engine
  ├─ bounded transport parsing
  ├─ idempotency / preconditions
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
HTTP / Problem Details / OpenAPI
```

## Database ownership

Reusable packages do not own a product schema or migration. Workbench selects SQL Server and owns `WorkbenchDbContext` plus migrations.

`FoundationKit.Infrastructure` has no SQL Server/PostgreSQL provider dependency. It may use EF Core Relational when a reusable adapter genuinely depends on relational semantics. The Phase 10 durable-idempotency adapter is one such boundary: FoundationKit provides the model-builder/store implementation, while the consumer decides the concrete relational provider and owns the migration/table in its schema.

## API contract source of truth

Runtime C# DTOs, module/API configuration, endpoint metadata, and ApiExplorer metadata produce the running OpenAPI contract. That OpenAPI document is the canonical serialized transport contract.

Derived artifacts flow one way:

```text
C# contracts/config/metadata
        ↓
runtime OpenAPI
        ↓
Postman (deterministic generated artifact)
        ↓
future typed clients
```

CI rejects drift between runtime OpenAPI and the committed generated Postman collection.

## Reliability

Current proven reliability surfaces are intentionally separated:

- domain-event dispatch remains in-process after successful persistence; it is not durable integration messaging;
- HTTP durable idempotency provides project-scoped relational acquisition and bounded completed-response replay for opted-in API operations;
- failed/indeterminate idempotent operations are fail-closed rather than automatically executed again under the same key.

FoundationKit does **not** infer from durable HTTP replay that it has implemented outbox/inbox, broker delivery, retry/dead-letter infrastructure, distributed transactions, or distributed exactly-once execution. Those remain separate evidence-driven roadmap boundaries.

## Composition model

The canonical capability graph, dependency resolver, profiles, contract versions, maturity evidence, project isolation contract, and module capability rules live in Application. Generated catalog JSON is checked for drift. Composer consumes the same capability truth for validation, explanation, compatibility, deterministic generation, and interactive generation.

The next Composer model expands from project/profile/capability selection toward `Project → Modules → Resources → Capabilities → Providers → Overrides → API` without creating a second parallel dependency graph.

## Workbench

Workbench proves database → domain/application → API → client/reference behavior against a real SQL Server path. It proves module composition, API Engine behavior, OpenAPI/Postman contract derivation, and durable idempotency replay through consumer-owned migrations. It is an executable reference, not a universal production deployment.

## Production boundary

Repository gates prove code/package/reference behavior. Production approval remains deployment-specific and requires external operational, security, governance, recovery, and compliance controls.
