# Madar

> Status: **v0.1–v0.9 implementation is complete on the current feature branch only after exact-head verification succeeds**. Repository evidence demonstrates implemented behavior for the verified commit; it is not Production Approval, Segregation-of-Duties evidence, or an external security certification.

Madar is an operational case-management and orchestration product built on FoundationKit. It remains intentionally separate from reusable FoundationKit packages, the Workbench architecture sample, and the Athar reference product.

## Product purpose

Madar turns operational work into traceable cases that can be created, routed, assigned, transferred, reassigned, progressed through controlled states, audited, governed by SLA expectations, collaborated on, approved where sensitive, accompanied by bounded operational notifications, and supported by private case attachments/documents.

Representative case types include customer complaints, operational incidents, internal service requests, access requests, compliance cases, technical escalations, and operational exceptions.

## Product boundary

Madar owns its business model, SQL schema, Identity configuration, permissions, Arabic UI copy, organization/routing semantics, SLA policy values, attachment policy/storage abstraction, runtime composition, and deployment topology. FoundationKit capabilities are reused only where their contracts fit the product.

```text
apps/Madar/
├── Madar.Domain
├── Madar.Application
├── Madar.Infrastructure
├── Madar.Contracts
├── Madar.Api
└── Madar.Client

tests/
└── Madar.Tests
```

Dependency direction:

```text
Madar.Domain
    ↑
Madar.Application ← Madar.Contracts
    ↑
Madar.Infrastructure
    ↑
Madar.Api ← Madar.Client hosting

Madar.Client → Madar.Contracts + FoundationKit.Blazor
```

Infrastructure dependencies do not enter Domain. The Blazor client does not reference Infrastructure or `MadarDbContext`. EF Core migrations under `Madar.Infrastructure/Migrations` are the product schema source of truth.

## Implemented product depth

```text
v0.1   Auth + SQL + case lifecycle + audit + Arabic API/Blazor
v0.1.1 Readiness + startup retry + local/Docker operational integration
v0.2   SLA deadlines + first breach/escalation evidence
v0.3   Append-only case comments
v0.4   Maker-checker approval gate for sensitive case resolution
v0.5   Bounded operational notifications
v0.6   Department queues + routing + operator claim flow
v0.7   Department administration + safe Operator membership
v0.8   Controlled transfer + reassignment
v0.9   Secure append-only case attachments/documents          ← current product depth
```

The deterministic lifecycle remains:

```text
new → assigned → in-progress → resolved → closed
```

Routing remains contextual rather than a workflow state:

```text
new/unassigned
     ↓ route
Department queue
     ↓ claim or direct assignment
assigned
     ↓
in-progress → resolved → closed
```

Transfer explicitly resets active work into the target department queue:

```text
Department A
new / assigned / in-progress
     ↓ transfer
Department B
new + unassigned
     ↓ claim / assign
assigned
```

## Department routing and administration

Madar owns `Department`, `DepartmentMembership`, case `DepartmentId`/`RoutedUtc`, membership-aware queues, claim, transfer, and reassignment semantics.

A Supervisor/Administrator can route a `new`, unassigned case to an active department. An Operator can read a department queue only when the user is an active member of that department. Claim requires Operator eligibility, `madar.cases.claim`, and membership, then reuses the existing assignment workflow.

Administrator-only department administration supports create/rename/activate/deactivate and Operator membership management. Deactivation is blocked while non-closed work remains, and membership removal is blocked while the Operator still owns a non-closed assignment in the department.

v0.8 adds Supervisor/Administrator `madar.cases.transfer` and `madar.cases.reassign`. Reassignment preserves lifecycle/SLA state and requires an eligible target Operator; transfer moves an already-routed active case to a different active department, clears assignment, and returns it to `new` in the target queue while preserving content and history.

See:

- [`../../docs/MADAR-DEPARTMENT-ROUTING-AR.md`](../../docs/MADAR-DEPARTMENT-ROUTING-AR.md)
- [`../../docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](../../docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md)
- [`../../docs/MADAR-CASE-TRANSFER-AR.md`](../../docs/MADAR-CASE-TRANSFER-AR.md)

## v0.9 secure case attachments

Attachments are product-owned append-only case records. Metadata is stored in SQL Server, while content is accessed only through a private `ICaseAttachmentContentStore` abstraction.

Current policy:

```text
Maximum file size: 10 MiB
Allowed: PDF, PNG, JPEG, TXT
User-facing edit/delete/versioning: not included
Direct/static file URLs: not exposed
```

The original filename is metadata only. The server generates a private storage key from case/attachment IDs; the original filename never becomes a filesystem path. The API requires the same case-read authorization used elsewhere: creator, current assignee, or a role with `madar.cases.read-all`. Missing and inaccessible cases are both masked as not found.

Upload checks filename safety, extension-to-MIME agreement, declared/actual bounded length, and basic content signatures for PDF/PNG/JPEG; plain text rejects NUL-containing samples. These checks reduce accidental/type-confusion risk but **are not a malware scanner**.

The current experimental Development/CI implementation stores bytes outside `wwwroot` in a private configurable filesystem root. `deploy/madar-compose.yml` uses a dedicated private Docker volume. Outside Development, `Madar:Attachments:StorageRoot` must be configured explicitly. Production object-storage, KMS, malware-scanning, retention, and provider choices remain deployment work.

Audit actions are:

```text
madar.case.attachment-uploaded
madar.case.attachment-downloaded
```

Custom audit attributes contain only `attachmentId`; filename, bytes, storage key/path, and provider details are excluded. Attachment history remains readable after case closure whenever case-read authorization remains valid.

See [`../../docs/MADAR-ATTACHMENTS-AR.md`](../../docs/MADAR-ATTACHMENTS-AR.md).

## SLA, collaboration, approvals, and notifications

When SLA is enabled, Madar snapshots an absolute target at case creation. States are `not-applicable`, `active`, `met`, and `breached`; first breach and escalation evidence are persisted. The bounded evaluator remains `POST /api/cases/sla/evaluate`; no reusable jobs/scheduler package is inferred from this alone.

Comments are product-owned append-only collaboration data. `access-request` and `compliance-case` use a maker-checker approval gate before resolution and reuse `FoundationKit.Approvals`. Assignment, reassignment, approval decision, and cross-user resolution can trigger bounded best-effort notifications through `FoundationKit.Notifications` and the optional SMTP provider. Notification failure does not undo an already-saved business operation.

## FoundationKit reuse

Madar currently reuses:

- `FoundationKit.Domain` — aggregate/domain primitives;
- `FoundationKit.Application` — results, persistence, clock, unit-of-work contracts;
- `FoundationKit.Infrastructure` — EF repository/unit-of-work and domain-event dispatch;
- `FoundationKit.WebApi` — request pipeline and HTTP result mapping;
- `FoundationKit.Blazor` — typed API-result handling;
- `FoundationKit.Security` — rate-limit conventions;
- `FoundationKit.Authorization` — role/permission evaluation;
- `FoundationKit.Auditing` — bounded audit events/sink contracts;
- `FoundationKit.Workflow` — case lifecycle transition resolution;
- `FoundationKit.Approvals` — generic approval eligibility/decision semantics;
- `FoundationKit.Notifications` and `.Smtp` — bounded notification contract/current provider.

Madar does **not** introduce `FoundationKit.Organization`, `FoundationKit.Files`, `FoundationKit.Storage`, or a reusable routing/history package in v0.9. Department/routing and attachment semantics remain product-owned until independent reuse evidence demonstrates a sufficiently stable general contract.

## Authentication and authorization

Madar uses ASP.NET Core Identity with secure cookie authentication, anti-CSRF validation for writes, password policy, login lockout, and authentication/write rate limits.

| Role | Current responsibility |
|---|---|
| `Requester` | create cases and see cases they created |
| `Operator` | see assigned cases, member department queues, claim queued cases, progress own assignments |
| `Supervisor` | read all cases, route/assign/reassign/transfer/progress/close, evaluate SLA, make approval decisions |
| `Administrator` | receives all currently defined Madar permissions, including department/membership administration |

Attachment list/upload/download reuse case-read authorization; upload additionally uses the normal anti-CSRF and write-rate-limit path. Application authorization remains authoritative.

## SQL Server persistence

`MadarDbContext` owns:

```text
identity/*
madar/Cases
madar/CaseComments
madar/CaseAttachments
madar/CaseApprovals
madar/Departments
madar/DepartmentMemberships
audit/AuditEvents
```

Current migrations include:

```text
20260808093000_InitialMadar
20260808110000_AddMadarSla
20260808143000_AddCaseComments
20260808155000_AddCaseApprovals
20260808173000_AddDepartmentRouting
20260808180000_AddDepartmentAdministration
20260809070000_AddCaseAttachments
```

v0.8 required no schema change because transfer/reassignment reused routing and assignment columns. v0.9 adds attachment metadata, uploader/case foreign keys, deterministic case-history indexing, unique private storage-key indexing, and rowversion concurrency.

## Bootstrap and local run

Supported Docker flow:

```powershell
.\foundationkit.ps1 start  -Target Madar -Mode Docker
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs   -Target Madar
.\foundationkit.ps1 stop   -Target Madar
```

Specialized launcher:

```powershell
.\scripts\madar-product.ps1 start
```

Development/CI bootstrap seeds Administrator and Operator users plus an `operations` department and membership. This is test/development topology, not Production organization policy.

Read [`../../docs/MADAR-OPERATIONS-AR.md`](../../docs/MADAR-OPERATIONS-AR.md) for the operational runbook.

## API surface highlights

```text
GET  /health/live
GET  /health/ready
GET  /api/security/antiforgery
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
GET  /api/users/operators

GET  /api/cases
POST /api/cases
GET  /api/cases/{caseId}
POST /api/cases/{caseId}/assignment
POST /api/cases/{caseId}/route
POST /api/cases/{caseId}/transfer
POST /api/cases/{caseId}/reassignment
POST /api/cases/{caseId}/claim
POST /api/cases/{caseId}/transition
GET  /api/cases/{caseId}/timeline
POST /api/cases/sla/evaluate

GET/POST /api/cases/{caseId}/comments
GET/POST /api/cases/{caseId}/approvals
POST     /api/cases/{caseId}/approvals/{approvalId}/decision
GET/POST /api/cases/{caseId}/attachments
GET      /api/cases/{caseId}/attachments/{attachmentId}/content

GET  /api/departments
GET  /api/departments/{departmentId}/queue

GET    /api/admin/departments
POST   /api/admin/departments
PUT    /api/admin/departments/{departmentId}
GET    /api/admin/departments/{departmentId}/members
POST   /api/admin/departments/{departmentId}/members
DELETE /api/admin/departments/{departmentId}/members/{userId}
```

## Blazor UI

```text
/                         product landing page
/login                    cookie-authentication login
/cases                    cases + department queue + create + SLA evaluation
/cases/{CaseId:guid}      details + routing/lifecycle + comments + attachments + approvals + audit
/admin/departments        Administrator department + Operator membership management
```

## Automated verification

The repository gate is expected to cover:

- Release build with warnings as errors;
- Madar domain/application attachment validation and authorization tests;
- existing routing/transfer/reassignment/department-administration tests;
- migration/snapshot/readiness correctness;
- Workbench and Athar regressions;
- existing Madar SQL/E2E flows;
- attachment SQL/E2E proof covering rejected signature mismatch, upload, SQL metadata, private content persistence, unauthorized denial, closure readability, authorized download, and audit privacy;
- Security Scan and CodeQL;
- unchanged reusable 17 `.nupkg` + 17 `.snupkg` output.

Exact evidence belongs to the exact PR head that produced it. A previous green run is not proof for later behavior-relevant changes.

## Deliberately deferred

- production organization tree / branch/team hierarchy and multi-tenancy;
- arbitrary product user/role administration;
- transfer approval workflow, bulk reassignment, and dedicated routing-history aggregate;
- multiple queues, skill/round-robin/capacity/presence/automatic routing;
- reusable organization/routing/files/storage extraction without independent evidence;
- durable notification outbox/retries/background scheduler;
- attachment edit/delete/versioning;
- malware-scanning provider, OCR/indexing/full-text search, signed URLs/CDN;
- Production object-storage/KMS/retention/provider configuration;
- advanced search/reporting;
- WhatsApp/email/external channel ingestion.

## Product rule

When Madar reveals a missing capability, first decide whether the behavior is product-specific or truly reusable. A FoundationKit package requires concrete independent evidence and a clean general contract; it is not created merely to reduce roadmap checkboxes.

## Tracking

- #71 — v0.1 first case vertical slice: complete.
- #74 — v0.1.1 readiness/operational integration: complete.
- #76 — v0.2 SLA/escalation: complete.
- #78 — v0.3 comments: complete.
- #80 — v0.4 approvals: complete.
- #82 — v0.5 notifications: complete.
- #84 — v0.6 department queues/routing: complete.
- #86 — v0.7 department administration: complete.
- #88 — v0.8 controlled transfer/reassignment: complete.
- #92 — v0.9 secure case attachments/documents: in verification until the exact PR head is green and merged.
