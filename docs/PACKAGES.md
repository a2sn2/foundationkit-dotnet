# FoundationKit Package Contracts

FoundationKit currently ships 17 reusable packages. Package version, capability maturity, and capability contract version are separate concepts.

## Base packages

- `FoundationKit.Domain` — entity/aggregate/value-object/event primitives with no framework dependency.
- `FoundationKit.Application` — results/errors, validation, pagination, repositories/specifications, UoW/clock/current-user/event ports, capability graph, project isolation contracts, module definitions, and generic CRUD application orchestration.
- `FoundationKit.Infrastructure` — provider-neutral EF repository/UoW/event adapters plus Core module/EF composition helpers; no SQL Server provider selection or reusable migrations.
- `FoundationKit.WebApi` — HTTP result/Problem Details/correlation/header helpers plus generic CRUD endpoint mapping.
- `FoundationKit.Blazor` — typed API results/errors, resilient response parsing, async state and ViewModel/client primitives.

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

## Package count rule

A new package requires an independently useful boundary and evidence. The current Core vNext work strengthens existing packages and intentionally keeps 17+17 output.
