# FoundationKit Platform Leverage Audit

## Status

This document governs post-baseline platform leverage after `v0.1.0-consumer-baseline.1`.

It is **not** a new numbered Core vNext roadmap phase. The approved Core vNext roadmap remains closed at Phase 12, and the frozen consumer baseline remains unchanged.

## Decision rule

FoundationKit should minimize commodity infrastructure ownership:

1. **Use native .NET / ASP.NET Core first** when the platform already provides a suitable stable primitive.
2. **Use ABP OSS selectively as an optional provider** when it provides mature infrastructure that is materially better than maintaining a duplicate FoundationKit engine.
3. **Keep FoundationKit-owned code** where the capability is part of FoundationKit's differentiating contract, generation model, architecture evidence, or portability boundary.
4. Do not adopt packages merely because they exist. New dependencies must reduce owned complexity, preserve the 17-package boundary, and pass the complete existing proof suite.
5. ABP Commercial is outside this decision and requires an explicit separate approval.

## Architecture direction

```text
.NET 10 / ASP.NET Core primitives
              |
              v
optional ABP OSS provider integrations
              |
              v
FoundationKit bounded contracts + differentiating tooling
              |
              +--> Composer / Core Studio
              +--> SQL-first Read Engine
              +--> runtime-contract deterministic tooling
              +--> Linked / Standalone generation
              +--> FoundationKit.Blazor / Soft Orbit
              |
              v
consumer applications
```

ABP is not a mandatory FoundationKit host lifecycle. The adapters introduced here implement existing FoundationKit-facing contracts or additive provider contracts. A consumer that does not choose ABP must still be able to consume the relevant FoundationKit packages without adopting an ABP application architecture.

## Implemented native .NET leverage

### First-party OpenAPI

`FoundationKit.WebApi` registers ASP.NET Core first-party OpenAPI with `AddOpenApi`, and Workbench maps the native document with `MapOpenApi`.

The existing Swagger document remains the canonical serialized transport source for deterministic Postman and typed C# client generation **until exact parity is independently proven**. This avoids breaking a proven contract pipeline merely to change generators.

### HybridCache

`FoundationKit.Caching` now exposes `IValueCache` backed by .NET `HybridCache` and `AddFoundationHybridCache()`.

This delegates typed serialization/caching behavior, stampede protection, L1/L2 integration and tag invalidation mechanics to the .NET implementation instead of rebuilding them in FoundationKit. The original bounded byte-oriented `ICacheStore` remains for compatibility/reference evidence.

### HTTP resilience

`FoundationKit.Infrastructure` exposes `AddFoundationResilientHttpClient()` over the standard `Microsoft.Extensions.Http.Resilience` pipeline. FoundationKit does not maintain a competing retry/circuit-breaker implementation.

### TimeProvider

Workbench `IClock` is now backed by .NET `TimeProvider` rather than directly reading `DateTimeOffset.UtcNow`, preserving FoundationKit's small application-facing clock contract while using the native time abstraction.

## Implemented ABP OSS provider bridges

ABP provider integration is currently additive and optional:

- `FoundationKit.Identity`: `AbpCurrentUserAdapter` maps `Volo.Abp.Users.ICurrentUser` into FoundationKit's minimal current-user contract.
- `FoundationKit.Authorization`: `AbpPermissionAuthorizationEvaluator` maps ABP `IPermissionChecker` into an async FoundationKit authorization surface while preserving FoundationKit ownership short-circuit semantics.
- `FoundationKit.Settings`: `AbpSettingReader` maps ABP current-context settings into `ISettingReader`.
- `FoundationKit.FeatureManagement`: `AbpFeatureEvaluator` maps ABP `IFeatureChecker` into FoundationKit feature decisions.

These bridges deliberately avoid forcing ABP's application/module lifecycle into Domain, Application, Composer, generated products, or standalone generation.

## 17-package audit

| Package | Direction | Decision |
|---|---|---|
| Domain | FoundationKit | Keep: core domain primitives are part of the stable architecture boundary. |
| Application | FoundationKit + native BCL | Keep: result/CQRS/persistence/module contracts are differentiating orchestration seams. |
| Infrastructure | .NET-first | Adapt: EF Core remains native; standard HTTP resilience is now delegated to .NET. |
| WebApi | ASP.NET Core-first | Adapt: native ProblemDetails/security pipeline retained; first-party OpenAPI added in parallel to canonical Swagger. |
| Blazor | FoundationKit | Keep: reusable Soft Orbit/client/application foundation is differentiated. |
| Security | ASP.NET Core-first | Keep bounded policy vocabulary; prefer native host controls where available. |
| Identity | FoundationKit contract + optional ABP | Adapt: current-user bridge added; user store/OIDC remains consumer/provider-owned. |
| Authorization | FoundationKit semantics + optional ABP | Adapt: ABP permission checker bridge added without deleting ownership semantics. |
| Auditing | FoundationKit | Keep for now: existing audit contract/evidence is proven; benchmark ABP only when a consumer requires broader auditing infrastructure. |
| Workflow | FoundationKit | Keep for now: do not replace a proven business-process boundary without consumer evidence. |
| Approvals | FoundationKit | Keep: approval semantics are a bounded product/process capability. |
| Notifications | FoundationKit | Keep provider-neutral contract. |
| Notifications.Smtp | FoundationKit/provider | Keep current transport seam; no reason to force ABP into every notification consumer. |
| Settings | FoundationKit contract + optional ABP | Adapt: ABP provider bridge added. |
| FeatureManagement | FoundationKit contract + optional ABP | Adapt: ABP feature bridge added. |
| Localization | .NET/FoundationKit | Keep current bounded model; evaluate ABP localization when a real consumer needs its richer resource model. |
| Caching | .NET-first | Adapt: native HybridCache provider added; legacy bounded store retained for compatibility. |

Package count remains **17**. Provider leverage is implemented inside existing package ownership boundaries rather than creating package #18.

## Deliberately not adopted yet

The following are useful ABP/.NET capabilities but are not automatically part of this change:

- multi-tenancy;
- background jobs/workers beyond native host requirements;
- distributed event bus;
- BLOB storage;
- distributed locking;
- advanced data filters;
- organization/tenant administration;
- broad replacement of the proven auditing/workflow/notification engines;
- .NET Aspire as a mandatory runtime architecture.

They are candidates for **consumer-driven adoption**, not missing blockers. A real consumer must establish the requirement, provider fit, migration cost and portability impact before FoundationKit owns an integration.

## Effectiveness gates

This leverage work is considered effective only when all of the following remain true:

- exactly 17 reusable NuGet packages and 17 symbol packages;
- full repository restore passes vulnerability audit;
- build and all standard tests are green;
- Workbench proves the native OpenAPI document is reachable and non-empty;
- native HybridCache behavior is unit-proven;
- resilient HttpClient registration is unit-proven;
- ABP settings/features/permissions/current-user adapters are unit-proven;
- canonical runtime Swagger → Postman → typed-client deterministic flow remains unchanged and green;
- Composer Linked and Standalone proofs remain green;
- no ABP dependency is introduced into Composer or the generated standalone foundation unless explicitly selected by a future consumer contract.

## Outcome

The objective is not to make FoundationKit an ABP wrapper. The objective is to make FoundationKit **smaller in commodity responsibilities and stronger in its unique responsibilities** by delegating proven infrastructure to .NET first and ABP OSS where it provides real value.
