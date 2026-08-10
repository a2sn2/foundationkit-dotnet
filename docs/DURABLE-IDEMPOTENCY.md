# FoundationKit Durable Idempotency

## Purpose

Phase 7 established a bounded `Idempotency-Key` HTTP contract and exposed the intent through endpoint/OpenAPI metadata. It deliberately did not claim durable replay semantics.

Phase 10 adds an opt-in durable implementation for relational EF Core consumers without creating a new package or choosing a database vendor.

The goal is narrower than "exactly once": for an HTTP mutation carrying one idempotency key, FoundationKit can durably bind that key to one request fingerprint, prevent concurrent duplicate execution, and replay a bounded completed response when the exact request is retried inside the replay window.

## Package boundaries

The implementation remains inside existing packages:

- `FoundationKit.Application` owns provider-neutral acquisition/replay contracts through `IIdempotencyStore`;
- `FoundationKit.Infrastructure` owns a provider-neutral **relational EF Core** adapter and model-builder registration;
- `FoundationKit.WebApi` owns request fingerprinting, durable acquisition/replay orchestration, bounded body capture, and HTTP conflict responses;
- the consuming host owns provider selection, table migration, retention/cleanup operations, database availability, and deployment policy.

`FoundationKit.Infrastructure` now explicitly references `Microsoft.EntityFrameworkCore.Relational`. This does not select SQL Server, PostgreSQL, or another vendor; it makes the relational nature of this adapter explicit instead of hiding it behind base EF abstractions.

## Enablement

Existing applications that do not register an `IIdempotencyStore` keep the Phase 7 behavior: the header is still validated according to module API configuration, but requests execute normally and no durable replay is claimed.

A relational consumer enables the reference adapter with:

```csharp
builder.Services.AddFoundationEfIdempotencyStore<MyDbContext>();
```

and adds the Foundation model to its own `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddFoundationIdempotencyStore();
    base.OnModelCreating(modelBuilder);
}
```

The consumer then owns the corresponding migration in its application schema.

Optional HTTP limits can be configured without changing the existing `AddFoundationWebApi` signature:

```csharp
builder.Services.ConfigureFoundationIdempotency(options =>
{
    options.ReplayWindow = TimeSpan.FromHours(24);
    options.MaximumRequestBodyBytes = 1024 * 1024;
    options.MaximumReplayBodyBytes = 1024 * 1024;
});
```

## Durable identity

The relational reference store uses the composite identity:

```text
ProjectId + OperationScope + SHA256(Idempotency-Key)
```

The raw idempotency key is never persisted.

`ProjectId` is part of the primary key so independent FoundationKit projects using the same database infrastructure cannot collide merely because they chose the same external idempotency key.

## Request fingerprint

FoundationKit computes a SHA-256 fingerprint from the canonical request inputs relevant to the mutation:

```text
HTTP method
+ Foundation API operation scope
+ actual path/query
+ normalized content type
+ If-Match when required
+ SHA256(request body bytes)
```

The raw request body is not persisted.

Including `If-Match` is important: the same JSON mutation submitted against a different resource version is a different request and must not silently reuse an earlier idempotency result.

## Acquisition outcomes

`IIdempotencyStore.AcquireAsync` returns one of:

- `Acquired` — this request owns the new durable in-progress entry and may execute;
- `Replay` — the same fingerprint completed previously and its bounded response can be returned;
- `FingerprintConflict` — the key is already bound to different request semantics;
- `InProgress` — another execution currently owns the same key/fingerprint;
- `NonReplayable` — the earlier outcome can no longer be replayed safely.

The EF reference adapter acquires by inserting the project/scope/key row under a database primary-key uniqueness constraint. A failed insert is only classified as an existing-key case after the expected row can actually be read; unrelated database failures are rethrown instead of being mislabeled as idempotency conflicts.

## Response replay

A replayable completed entry stores bounded transport data only:

- HTTP status code;
- content type;
- response body bytes;
- `Location`;
- `ETag`.

The default request and response capture limits are 1 MiB each. Configuration is bounded by a 16 MiB hard ceiling. The replay window defaults to 24 hours and is configurable between one minute and seven days.

Request bodies are read through bounded buffering even when `Content-Length` is absent, so chunked transfer does not bypass fingerprinting.

## Fail-closed behavior

FoundationKit prioritizes avoiding an unsafe duplicate side effect over automatically recovering an indeterminate request.

After a key is acquired:

- an exception marks the entry non-replayable when possible, then propagates to the normal exception pipeline;
- a `5xx` response is non-replayable;
- a response larger than the configured replay limit is non-replayable;
- a durable finalization failure is logged and the key remains fail-closed rather than being deleted and automatically reused;
- an expired completed entry becomes non-replayable rather than being transparently recycled by the request path.

There is intentionally no delete-and-reacquire path in request processing. Cleanup/retention is an operational host concern and must not create an execution race.

## HTTP behavior

When durable idempotency is enabled for an operation:

- exact retry inside the replay window returns the stored response without running the application mutation again;
- same key with changed body/path/query/content type/`If-Match` returns `409 Conflict` with `Foundation.Api.Idempotency.FingerprintConflict`;
- concurrent duplicate execution returns `409 Conflict` with `Foundation.Api.Idempotency.InProgress`;
- an indeterminate/non-replayable key returns `409 Conflict` with `Foundation.Api.Idempotency.NonReplayable`;
- missing/malformed `Idempotency-Key` and `If-Match` continue to use the existing Phase 7 validation contracts before durable acquisition.

Replay remains inside the Foundation request response pipeline, so correlation/status handling and security headers are not bypassed.

## Compatibility

Phase 10 is additive:

- the original `AddFoundationWebApi(IServiceCollection, Action<FoundationErrorHandlingOptions>?)` CLR signature is preserved;
- applications without `IIdempotencyStore` keep their prior behavior;
- the durable store is opt-in;
- adopting the EF adapter requires an additive consumer-owned migration;
- no reusable package count change is introduced.

This phase does not promote any package to 1.0 or claim universal provider maturity.

## Non-goals

Phase 10 does **not** claim:

- distributed exactly-once delivery;
- atomicity between arbitrary external side effects and the idempotency row;
- message-broker deduplication;
- outbox/inbox semantics;
- automatic reconciliation of an indeterminate external transaction;
- a universal cleanup scheduler;
- a non-relational reference provider.

Those require their own boundaries and evidence.

## Required runtime proof

Workbench owns the SQL Server migration and must prove at runtime that:

1. a create with a new key executes and returns its normal ID/version/ETag;
2. the exact create retry returns the same ID/version/ETag rather than inserting another row;
3. the same key with changed body returns fingerprint conflict;
4. an update retry replays the completed version instead of applying the mutation twice;
5. changing `If-Match` under the same update key is a fingerprint conflict;
6. a delete retry remains the original `204` instead of re-executing and becoming `404`;
7. the existing API Engine, OpenAPI/Postman SSOT, project-isolation, security, package, Composer, and compatibility gates remain green.

The capability is not considered complete until that exact-head SQL proof succeeds.
