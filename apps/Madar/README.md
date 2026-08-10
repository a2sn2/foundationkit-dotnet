# Madar

> Status: **Madar v0.10 is the repository's operational case-management product built on FoundationKit.** Repository verification is technical evidence for the tested scope; it is not Production Approval, Segregation-of-Duties evidence, or external certification.

Madar is intentionally product-owned. Its case model, SQL schema, Identity configuration, permissions, Arabic UI, departments/routing, SLA policy, attachments, search/reporting semantics, and deployment topology stay under `apps/Madar`. FoundationKit is reused only where a provider-neutral contract already fits.

## Start here — Windows UAT

The primary human/UAT path is **Native Madar + local SQL Server**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 doctor
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 start -Target Madar -Mode Native
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 credentials -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 open -Target Madar
```

Default URL:

```text
http://localhost:8100
```

Status, logs, and stop:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 logs -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 stop -Target Madar
```

Native stop preserves the local `MadarDb` SQL database. The canonical launcher publishes and starts Madar from ignored `.local/` state, so Visual Studio-generated `launchSettings.json` files cannot move the application away from port `8100`.

### Temporary tester sharing

With Madar already `READY`, choose one temporary UAT tunnel:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

Microsoft Dev Tunnels are started anonymously for the explicit UAT session; Cloudflare uses a Quick Tunnel. Both commands stay attached to the terminal and end with `Ctrl+C`. Treat the generated URL as temporary Development access: share it only with intended testers and use test accounts/data.

### Docker boundary

Docker is **retained**, but it is no longer the required Windows human/UAT path. Use it explicitly for container/integration/regression work:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\foundationkit.ps1 start -Target Madar -Mode Docker
```

Existing Docker Compose, readiness, SQL/E2E, container-hardening, and security-scan coverage remain part of repository verification.

### Credentials caveat

Local Development credentials are generated under ignored `.local/madar-product.env` and protected with a Windows ACL. Bootstrap is idempotent: if `MadarDb` already contains `admin@madar.local` or `operator@madar.local`, startup does **not** overwrite those existing passwords. Therefore a newly generated local password file is authoritative only for users created from that same bootstrap state.

### Release publish

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\scripts\madar-product.ps1 publish
```

Outputs:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

A Release artifact is not a Production deployment.

Canonical handoff documents:

- [`../../docs/MADAR-SPECIFICATION-AR.md`](../../docs/MADAR-SPECIFICATION-AR.md)
- [`../../docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`](../../docs/MADAR-LOCAL-RUN-PUBLISH-AR.md)
- [`../../docs/MADAR-ACCEPTANCE-CHECKLIST-AR.md`](../../docs/MADAR-ACCEPTANCE-CHECKLIST-AR.md)
- [`../../docs/MADAR-OPERATIONS-AR.md`](../../docs/MADAR-OPERATIONS-AR.md)

The static GitHub Pages demo under `site/madar-demo/` is deliberately **not** the ASP.NET Core/SQL runtime and does not persist real product data.

## Product structure

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

Infrastructure does not enter Domain. The Blazor client does not reference Infrastructure or `MadarDbContext`. EF Core migrations under `Madar.Infrastructure/Migrations` remain the schema source of truth.

## Implemented product depth

```text
v0.1   Identity + authorization + SQL + case lifecycle + audit + Arabic API/Blazor
v0.1.1 Readiness + bounded startup retry + local/Docker operational integration
v0.2   SLA deadlines + first breach/escalation evidence
v0.3   Append-only case comments
v0.4   Maker-checker approval gate for sensitive resolution
v0.5   Bounded operational notifications
v0.6   Department queues + routing + Operator claim flow
v0.7   Department administration + safe Operator membership
v0.8   Controlled transfer + reassignment
v0.9   Secure append-only case attachments/documents
v0.10  Authorized case search + same-scope operational reporting
```

Lifecycle:

```text
new → assigned → in-progress → resolved → closed
```

Routing is contextual rather than a workflow state. Transfer moves active work to a different active department, clears assignment, returns the case to `new` in the target queue, and preserves prior content/history. Reassignment changes the eligible Operator while preserving the active lifecycle/SLA evidence.

## Current capabilities

- authenticated Requester, Operator, Supervisor, and Administrator roles;
- application-layer permission and case-visibility enforcement;
- SQL Server persistence and product-owned migrations;
- assignment, department routing, claim, transfer, and reassignment;
- append-only comments;
- sensitive-case maker-checker approvals;
- bounded best-effort operational notifications;
- SLA target/breach/escalation evidence;
- private append-only attachments with bounded type/size/signature checks;
- authorization-preserving SQL-backed case search and operational counts;
- Arabic Blazor product UI;
- liveness/readiness, Native UAT, Docker regression, CI, SQL/E2E, security scanning, and audit evidence.

Detailed product documents:

- [`../../docs/MADAR-SPECIFICATION-AR.md`](../../docs/MADAR-SPECIFICATION-AR.md)
- [`../../docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`](../../docs/MADAR-LOCAL-RUN-PUBLISH-AR.md)
- [`../../docs/MADAR-ACCEPTANCE-CHECKLIST-AR.md`](../../docs/MADAR-ACCEPTANCE-CHECKLIST-AR.md)
- [`../../docs/MADAR-OPERATIONS-AR.md`](../../docs/MADAR-OPERATIONS-AR.md)
- [`../../docs/MADAR-COMMENTS-AR.md`](../../docs/MADAR-COMMENTS-AR.md)
- [`../../docs/MADAR-APPROVALS-AR.md`](../../docs/MADAR-APPROVALS-AR.md)
- [`../../docs/MADAR-NOTIFICATIONS-AR.md`](../../docs/MADAR-NOTIFICATIONS-AR.md)
- [`../../docs/MADAR-DEPARTMENT-ROUTING-AR.md`](../../docs/MADAR-DEPARTMENT-ROUTING-AR.md)
- [`../../docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](../../docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md)
- [`../../docs/MADAR-CASE-TRANSFER-AR.md`](../../docs/MADAR-CASE-TRANSFER-AR.md)
- [`../../docs/MADAR-ATTACHMENTS-AR.md`](../../docs/MADAR-ATTACHMENTS-AR.md)
- [`../../docs/MADAR-SEARCH-REPORTING-AR.md`](../../docs/MADAR-SEARCH-REPORTING-AR.md)

## Authorization boundary

Madar uses ASP.NET Core Identity with secure cookie authentication, anti-CSRF validation for writes, password policy, login lockout, and authentication/write rate limits.

| Role | Current responsibility |
|---|---|
| `Requester` | create cases and read cases they created |
| `Operator` | read assigned/created cases, read member department queues, claim queued cases, progress own assignments |
| `Supervisor` | broad case read, route/assign/reassign/transfer/progress/close, evaluate SLA, make approval decisions |
| `Administrator` | all currently defined Madar permissions, including department/membership administration |

The Application layer remains authoritative. Attachment and search/reporting surfaces reuse the existing case-visibility scope instead of introducing weaker parallel authorization rules.

An unauthenticated `GET /api/auth/me` may return 401 during initial client authentication-state discovery. The client intentionally maps an authentication failure there to an anonymous principal; this is expected behavior, not an authenticated-session failure.

## Attachments and search/reporting

Attachments store metadata in SQL Server and content behind a private `ICaseAttachmentContentStore`. Native Development content is stored under ignored `.local/madar-native/attachments`; Docker Development/CI keeps its private mounted storage. Neither is served from `wwwroot`. Current limits are 10 MiB and PDF/PNG/JPEG/TXT. Signature checks reduce type-confusion risk but are not malware scanning.

`GET /api/cases/search` applies visibility before filters, counts, and paging. Narrower roles therefore cannot infer hidden cases through rows or summary counters. Search remains relational/EF-backed and product-owned; no external index or generic FoundationKit Search/Reporting package is implied.

## FoundationKit reuse

Madar consumes the five base FoundationKit packages plus reusable Security, Authorization, Auditing, Workflow, Approvals, Notifications, and the optional SMTP provider where their contracts fit.

Madar does **not** create `FoundationKit.Organization`, `FoundationKit.Files`, `FoundationKit.Storage`, `FoundationKit.Search`, `FoundationKit.Reporting`, or `FoundationKit.Jobs`. Departments/routing, attachments, SLA, and search/reporting remain product-owned until independent reuse evidence proves a general boundary.

## Main API/UI surfaces

```text
GET  /health/live
GET  /health/ready
POST /api/auth/login
GET  /api/auth/me

GET/POST /api/cases
GET      /api/cases/search
GET      /api/cases/{caseId}
POST     /api/cases/{caseId}/assignment
POST     /api/cases/{caseId}/route
POST     /api/cases/{caseId}/transfer
POST     /api/cases/{caseId}/reassignment
POST     /api/cases/{caseId}/claim
POST     /api/cases/{caseId}/transition
GET      /api/cases/{caseId}/timeline
POST     /api/cases/sla/evaluate
GET/POST /api/cases/{caseId}/comments
GET/POST /api/cases/{caseId}/approvals
GET/POST /api/cases/{caseId}/attachments
GET      /api/departments/{departmentId}/queue
GET/POST /api/admin/departments
```

```text
/                    product landing page
/login               authentication
/cases               case workspace + queues + creation + SLA
/reports/cases       authorized search + operational summary
/cases/{CaseId:guid} case details, collaboration, attachments, approvals, audit
/admin/departments   department and Operator membership administration
```

## Verification and Production boundary

The normal repository gate builds, tests, publishes, packages, scans, and runs SQL-backed Workbench/Athar/Madar regressions. Madar tests cover authorization, lifecycle, SLA, comments, approvals, routing/administration, transfer/reassignment, attachments, and v0.10 search/reporting privacy boundaries.

The Release publish action produces a folder/ZIP and SHA-256 checksum; temporary UAT tunnels expose a Development instance; the static Pages demo is explanatory. None of these is Production Approval.

Production still requires deployment-specific decisions for organization/tenancy, object storage/KMS/malware scanning/retention, durable notification delivery/background scheduling, ingress/TLS, secrets, SQL identities, observability, backup, legal/privacy policy, performance acceptance, and repository governance.

## Tracking

- #71 — v0.1: complete.
- #74 — v0.1.1: complete.
- #76 — v0.2: complete.
- #78 — v0.3: complete.
- #80 — v0.4: complete.
- #82 — v0.5: complete.
- #84 — v0.6: complete.
- #86 — v0.7: complete.
- #88 — v0.8: complete.
- #92 — v0.9: complete.
- #94 / PR #95 — v0.10 authorized case search/reporting: complete.
- #115 / PR #116 — local handoff, publish, Pages demo, specification, and acceptance readiness: complete.
- #117 / PR #118 — Native UAT + temporary Microsoft/Cloudflare sharing hardening.

## Product rule

When Madar reveals a missing concern, first classify it as product-specific or reusable. A new FoundationKit package requires independent consumer/provider evidence and a clean provider-neutral contract; it is never created merely to reduce roadmap checkboxes.
