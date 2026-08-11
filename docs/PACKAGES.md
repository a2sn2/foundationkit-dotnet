# FoundationKit Package Contracts

FoundationKit currently ships 17 reusable packages. Package version, capability maturity, and capability contract version are separate concepts.

## Base packages

- `FoundationKit.Domain` — entity/aggregate/value-object/event primitives with no framework dependency.
- `FoundationKit.Application` — results/errors, validation, pagination, repositories/specifications, UoW/clock/current-user/event ports, capability graph, project isolation contracts, module definitions, generic CRUD application orchestration, and provider-neutral durable-idempotency acquisition/replay contracts.
- `FoundationKit.Infrastructure` — provider-neutral EF repository/UoW/event adapters, relational EF idempotency adapter/model composition, and Core module/EF composition helpers; no SQL Server/PostgreSQL provider selection and no reusable product migrations.
- `FoundationKit.WebApi` — HTTP result/Problem Details/correlation/header helpers, generic CRUD endpoint mapping, and opt-in durable idempotency orchestration over endpoint metadata plus `IIdempotencyStore`.
- `FoundationKit.Blazor` — typed API results/errors and metadata, resilient response parsing, async/presentation/query state, semantic Soft Orbit design tokens/static assets, and product-neutral first-party Razor primitives for theme, app shell/navigation, buttons, cards, badges, page headers and loading/empty states. The reusable package does not depend on MudBlazor or own product authorization/business semantics.

## Optional/reference packages

- `FoundationKit.Auditing` — bounded audit event/context/sink/recorder contracts and CRUD audit observer.
- `FoundationKit.Security` — reverse-proxy, rate partition, and MFA-assurance conventions.
- `FoundationKit.Identity` — account-policy, account-notification, security-event, and step-up contracts; no user store or identity schema.
- `FoundationKit.Authorization` — permission/role grant/evaluator/ownership primitives; product role names and persistence remain host-owned.
- `FoundationKit.Workflow` — deterministic transition definitions/resolution and bounded audit intent.
- `FoundationKit.Approvals` — approve/reject, permission gate, maker-checker, workflow resolution, audit intent.
- `FoundationKit.Notifications` — channel-neutral bounded message/sender/delivery-result contracts.
- `FoundationKit.Notifications.Smtp` — validated narrow SMTP transport adapter.
- `FoundationKit.Settings` — bounded setting keys/scopes/values and deterministic source precedence.
- `FoundationKit.FeatureManagement` — settings-backed Boolean feature evaluation with explicit defaults and fail-closed invalid configuration.
- `FoundationKit.Localization` — culture metadata, RTL/LTR directionality, fallback and bounded time-zone identity.
- `FoundationKit.Caching` — byte-cache contracts, TTL/hit/miss/remove semantics, in-memory reference provider.

## Provider/schema rule

The consuming host selects relational/storage providers and owns its schema/migrations. A provider-neutral contract must not silently turn into a global vendor choice.

`FoundationKit.Infrastructure` may depend on EF Core Relational for reusable relational adapters while remaining vendor-neutral. A consuming application that enables the durable idempotency EF adapter must call `AddFoundationIdempotencyStore()` from its own model and own the resulting migration/table. FoundationKit does not ship a reusable SQL Server or PostgreSQL migration for that table.

## Frontend dependency rule

`FoundationKit.Blazor` is the single reusable UI/design-system package. A product or Workbench sample may use an additional component library, but that dependency must not be promoted into reusable Core solely to style the sample. Generated applications should consume the first-party semantic tokens/components and override product branding at the host boundary rather than copying shared CSS.

## Package count rule

A new package requires an independently useful boundary and evidence. The current Core vNext and shared-UI work strengthen existing packages and intentionally keep 17+17 output.
