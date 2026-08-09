# FoundationKit.Auditing

`FoundationKit.Auditing` is the provider-neutral optional auditing capability. It remains separate from the kernel so products that do not need the reusable audit contract do not take an audit-provider dependency.

## Public boundary

- `AuditRequest` — caller-owned action/subject/outcome/reason metadata;
- `AuditContext` / `IAuditContextAccessor` — actor, correlation, tenant and source context supplied by the host;
- `AuditEvent` — normalized immutable event;
- `AuditOutcome` — succeeded/failed/denied;
- `IAuditSink` — provider-neutral persistence/export port;
- `IAuditRecorder` / `AuditRecorder` — context/time stamping and sink dispatch.

The model bounds identifiers/attributes, copies mutable inputs defensively, and rejects common credential/secret attribute names. It intentionally does not accept arbitrary request/response bodies or object snapshots.

## Ownership boundary

The consuming product/provider owns:

- SQL/append-only/SIEM sink implementation;
- retention and legal policy;
- access control and tamper-evidence strategy;
- delivery/outbox/retry behavior;
- data classification and which approved metadata may be recorded.

A sink failure is not silently swallowed by the reusable recorder. Whether a business operation must fail closed, retry or continue is a product/risk decision.

## Current consumer evidence

Athar and Madar both consume the reusable auditing contracts while keeping their persistence/action vocabulary inside their products.

- Athar records its initiative/account/security-oriented audit data through product-owned persistence.
- Madar records case/routing/approval/attachment/search-related audit evidence through its own SQL-backed sink and product action names.

This is stronger adoption evidence than the original single-consumer extraction, but it does not prove a universal production sink, immutable storage, SIEM integration, signing or retention policy.

## Maturity

Capability Model v1 keeps Auditing at `ReferenceOnly`. Package implementation and multiple consumers are real evidence, but `Stable` would require the broader quality/adoption/compatibility/support commitment encoded by Maturity Evidence v1; Production Approval remains a separate deployment concern.
