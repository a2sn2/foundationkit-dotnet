# Madar Changelog

This product changelog records Madar-specific behavior. Repository-wide reusable FoundationKit changes remain documented in the root `CHANGELOG.md`.

## [Unreleased] — v0.10 authorized case search and reporting

### Added

- Product-owned SQL-backed case search that applies the existing creator/assignee/`madar.cases.read-all` visibility boundary before filters, paging, or report aggregation.
- Bounded filters for text, case type, priority, lifecycle status, SLA state, department, assignee, and creation-date range.
- Deterministic result paging ordered by `UpdatedUtc` descending then case ID, with default page size 25 and hard maximum 100.
- Same-scope operational summary counts for total/unassigned, lifecycle states, and SLA states; hidden cases cannot influence rows or counts for narrower roles.
- Authenticated `GET /api/cases/search` endpoint and typed Blazor client support without introducing per-query write/audit noise.
- Arabic `/reports/cases` page with filters, summary cards, result table, and bounded previous/next paging.
- Application tests for authentication, normalization, `read-all` scope, and validation boundaries plus real SQL/E2E coverage for hidden-case isolation, filters, paging, and invalid input.
- Dedicated Arabic guide `docs/MADAR-SEARCH-REPORTING-AR.md` and Atlas route registration.

### Deliberately deferred

- external search/index providers such as Elasticsearch, OpenSearch, or Lucene;
- OCR, attachment-content indexing, and database full-text search;
- saved searches, exports, charts, scheduled reports, BI integration, or report sharing;
- cross-tenant or organization-hierarchy analytics;
- reusable `FoundationKit.Search` / `FoundationKit.Reporting` extraction until independent product evidence exists.

## [v0.9] — secure case attachments

### Added

- Product-owned append-only `CaseAttachment` metadata with case/uploader IDs, bounded original filename/content type/size, server-generated private storage key, creation time, and SQL Server rowversion.
- Existing case visibility reused for list/upload/download: creator, current assignee, or a role with `madar.cases.read-all`; inaccessible cases remain masked as not found.
- 10 MiB upload limit with PDF/PNG/JPEG/TXT allow-list, extension-to-MIME matching, and basic content-signature validation before storage.
- Private `ICaseAttachmentContentStore` abstraction with a filesystem implementation for the current experimental Development/CI stack; storage remains outside `wwwroot` and uses a private Docker volume.
- Authenticated attachment list/download and anti-CSRF/write-rate-limited upload endpoints under `/api/cases/{caseId}/attachments`.
- `madar.case.attachment-uploaded` and `madar.case.attachment-downloaded` audit actions containing only `attachmentId` in custom attributes; file bytes, filename, storage key/path, and provider details are excluded.
- Arabic attachment panel integrated into case details, including file selection, bounded upload, metadata list, and authorized download.
- SQL migration/model snapshot plus unit and SQL/E2E coverage for validation, authorization masking, closed-case readability, private content persistence, authorized download, and audit privacy.

### Deliberately deferred

- edit/delete/versioning of attachments;
- malware-scanner provider integration;
- OCR, indexing, or full-text search inside files;
- signed URLs, CDN, or direct public file paths;
- Production object-storage/KMS/provider selection;
- reusable `FoundationKit.Files` / `FoundationKit.Storage` extraction until independent product evidence exists.

## [v0.8] — controlled transfer and reassignment

### Added

- Supervisor/Administrator `madar.cases.transfer` and `madar.cases.reassign` product permissions; Operator remains unable to move work administratively.
- Controlled transfer of already-routed `new`, `assigned`, or `in-progress` cases to a different active department, clearing the assignee and returning the case to `new` in the target queue.
- Transfer invariants that reject unrouted, same-department, resolved, and closed cases without mutating persisted work.
- Controlled reassignment of `assigned` / `in-progress` cases to a different eligible Operator while preserving lifecycle state, routing state, and SLA evidence.
- Routed reassignment defense requiring the new Operator to be a member of the active case department.
- `madar.case.transferred` and `madar.case.reassigned` persistent audit actions with bounded department/user/status identifiers only.
- Reassignment notification through the existing case notification coordinator after the SQL business commit; transport failure cannot roll back the reassignment.
- Authenticated, anti-CSRF, write-rate-limited `POST /api/cases/{caseId}/transfer` and `POST /api/cases/{caseId}/reassignment` endpoints.
- Arabic case-details controls for transfer and reassignment, with server-side authorization and membership rules remaining authoritative.
- Domain/application tests plus real SQL/E2E proof for progress → reassignment → cross-department transfer → target queue → claim, including SLA preservation and bounded timeline metadata.

### Deliberately deferred

- automatic/round-robin routing, capacity, presence, skills, and bulk reassignment;
- transfer approval workflows or dedicated routing-history tables beyond the existing persistent audit timeline;
- organization hierarchy, branches, teams, or multi-tenancy;
- queue-specific business-hours/SLA policy;
- WhatsApp/email ingestion;
- reusable FoundationKit organization/routing extraction until independent product evidence exists.

## [v0.7] — department administration

### Added

- Administrator-only `madar.departments.manage` permission for product-owned department and Operator-membership administration.
- Department creation with normalized immutable code, bounded display name, active state, creation time, administrative `UpdatedUtc`, and existing SQL rowversion concurrency.
- Department rename/activation/deactivation flow with a fail-closed guard that blocks deactivation while the department still owns any non-closed case.
- Operator-membership list/add/remove flow backed by ASP.NET Core Identity role eligibility.
- Deterministic duplicate-membership conflict before the database unique key becomes the user-facing failure mode.
- Membership-removal guard that blocks removal while the Operator still owns a non-closed assignment in the department.
- Bounded audit actions for department create/update and membership add/remove without copying email, case content, or other unnecessary PII into audit attributes.
- Product-owned administration API under `/api/admin/departments`, protected by authentication, Application-layer permission checks, anti-CSRF, write rate limiting, and existing SQL concurrency handling.
- Arabic Administrator UI at `/admin/departments` for department lifecycle and Operator membership management.
- SQL migration adding `Departments.UpdatedUtc` while preserving existing departments by backfilling it from `CreatedUtc`.
- Unit and SQL/E2E verification for permission gates, validation, deactivation protection, membership eligibility/duplicates, removal protection, persistence, audit evidence, and the existing routing integration.

### Deliberately deferred

- reusable `FoundationKit.Organization` extraction until independent product evidence exists;
- organization trees, branches, teams, parent/child departments, and multi-tenancy;
- arbitrary user/role administration;
- multiple queues per department, skills, capacity, presence, round-robin, or automatic assignment;
- queue-specific business-hours/SLA policy;
- WhatsApp/email ingestion.

## [v0.6] — department queues and routing

### Added

- Product-owned `Department` and `DepartmentMembership` models with SQL Server persistence, bounded codes/names, active state, membership indexes, and rowversion on departments.
- Nullable `DepartmentId` and `RoutedUtc` case routing state so historical/unrouted cases remain valid.
- Supervisor/Administrator routing of a new unassigned case to an active department without changing its lifecycle state from `new`.
- Membership-scoped department queue reads, with existing broad-read authority allowing Supervisor/Administrator visibility across active departments.
- Operator claim flow that requires both the Operator claim permission and active department membership, then reuses the existing Case assignment workflow to move `new → assigned`.
- Routed direct-assignment defense: an Operator must belong to the routed department; legacy unrouted direct assignment remains supported.
- `madar.case.routed` and `madar.case.claimed` audit evidence with bounded department/user identifiers only.
- Deterministic local/CI bootstrap `operations` department with the seeded Operator as a member.
- Arabic case-details routing/claim controls and a membership-aware department queue embedded in the existing cases page.
- Unit coverage for domain routing, membership-scoped queue reads, claim behavior, and routed assignment restrictions.

### Deliberately deferred

- reusable `FoundationKit.Organization` extraction until independent product evidence exists;
- organization trees, branches, teams, multiple queues per department, or multi-tenancy;
- skill/round-robin/load/capacity/presence based automatic routing;
- routing-history aggregate beyond bounded audit evidence;
- queue-specific SLA/business-hours policy;
- WhatsApp/email ingestion.

## [v0.5] — operational case notifications

### Added

- Reuse of `FoundationKit.Notifications` for bounded provider-neutral notification messages/results and `FoundationKit.Notifications.Smtp` for the current optional email transport.
- Product-owned Arabic notification copy for assignment, approval decision, and resolution events.
- Identity-backed notification destination resolution without exposing recipient addresses through API contracts.
- Optional SMTP configuration under `Madar:Notifications:Smtp`; an empty host/from-address is treated as `NotConfigured` by the existing provider contract.
- `madar.case.notification-delivery` audit evidence containing only purpose, target user ID, and bounded delivery status; destination and body are deliberately excluded.
- Notification delivery occurs only after the corresponding business transaction is saved, so SMTP `Failed` / `NotConfigured` outcomes do not roll back assignment, approval decision, or resolution.
- Unit coverage for delivered, not-configured, failed, and audit-privacy behavior.

### Deliberately deferred

- background jobs, outbox delivery, retry/backoff, or delayed scheduling;
- templates, preferences, recipient groups, fallback channels, or in-app inbox;
- SMS, push, WhatsApp, or webhook providers;
- SLA reminder scheduling;
- changes to the public `FoundationKit.Notifications` API merely for Madar convenience.

## [v0.4] — sensitive-case approval gate

### Added

- Product-owned `CaseApproval` persistence with requester/reviewer identities, pending/approved/rejected state, bounded decision notes, timestamps, and SQL Server rowversion.
- Sensitive-case policy: `access-request` and `compliance-case` require the latest approval to be approved before `in-progress → resolved`.
- Existing `FoundationKit.Approvals` reuse for permission-first decision eligibility, maker-checker enforcement, strict approve/reject normalization, and workflow-backed decision resolution.
- New `madar.cases.approve` product permission granted to Supervisor and Administrator in the current role model.
- Authenticated approval history/request/decision API under `/api/cases/{caseId}/approvals`, with anti-CSRF and write rate limiting on writes.
- `madar.CaseApprovals` SQL table with deterministic case-history index, requester/reviewer foreign keys, and migration/snapshot coverage.
- `madar.case.approval-requested` and `madar.case.approval-decided` audit actions with bounded metadata; decision notes remain product data and are excluded from audit attributes.
- Arabic approval panel on the existing case-details route, including request, maker-checker decision, status, notes, and history.
- Unit coverage for permission-first maker-checker behavior, rejection/re-request behavior, domain defense-in-depth, and audit-note exclusion.
- SQL smoke coverage proving resolution is blocked before approval, a different authorized actor approves, the same case then resolves/closes, approval history persists, and decision notes do not leak into the audit timeline.

### Deliberately deferred

- multi-stage, parallel, or quorum approvals;
- dynamic approver routing/delegation;
- approval SLA/background scheduling;
- edit/delete/versioning of approval records;
- organization hierarchy/multi-tenancy;
- changes to the public `FoundationKit.Approvals` API merely for Madar convenience.

## [v0.3] — case collaboration

- Product-owned append-only `CaseComment` model with case/author IDs, plain-text body, creation time, and SQL Server rowversion.
- Body validation that trims input and accepts only 1..2000 characters.
- Existing case read-scope reused for comment list/add access: creator, current assignee, or role with `madar.cases.read-all`.
- Inaccessible/missing parent cases use the existing not-found masking rule.
- `GET /api/cases/{caseId}/comments` and protected `POST /api/cases/{caseId}/comments`.
- `madar.CaseComments` SQL table with case/author foreign keys and deterministic `(CaseId, CreatedUtc, Id)` ordering/indexing.
- `madar.case.comment-added` audit action containing bounded metadata only; comment text is deliberately excluded from audit attributes.
- Typed Blazor API support and Arabic comments panel on the existing case-details route.
- SQL smoke coverage proving assigned-operator add/list, body availability only through the authorized comments API, body absence from the audit timeline, and comment history after case closure.
- No edit/delete/version history, private-note tiers, mentions/watchers, notifications, attachments, rich text, reactions/moderation, or reusable comments package.

## [v0.2] — SLA deadlines and bounded escalation

- Product-configured SLA duration by priority with absolute target snapshot at case creation.
- `not-applicable`, `active`, `met`, and `breached` semantics with exact-target boundary behavior.
- Persistent first breach and escalation timestamps.
- Late-resolution breach materialization.
- Authorized, bounded, idempotent `POST /api/cases/sla/evaluate` scheduler seam.
- SLA SQL migration, Arabic API/UI evidence, and real SQL smoke coverage.
- No production scheduler/provider or reusable `FoundationKit.Jobs` extraction.

## [v0.1.1] — operational readiness closure

- Database-backed readiness endpoint and bounded startup retry/schema validation.
- Protected local Madar Docker launcher and unified Windows manager integration.
- Atlas/Pages coverage, Madar container vulnerability gate/SARIF, and Windows PowerShell verification.

## [v0.1] — first runtime vertical slice

- ASP.NET Core Identity authentication/authorization.
- SQL Server case persistence.
- Case create/list/view/assign and deterministic `new → assigned → in-progress → resolved → closed` lifecycle.
- Persistent audit timeline.
- Arabic API/Blazor flow, Docker/SQL end-to-end verification, and optimistic-concurrency handling.

## Evidence rule

A changelog entry describes repository behavior only. Exact CI/security evidence belongs to the exact PR head that produced it. Repository evidence is not Production Approval, independent Segregation-of-Duties approval, ISO/IEC 27001 certification, or a production infrastructure/security attestation.
