# FoundationKit — Current Security and Policy Status

**Living executive reference. `POLICY-IMPLEMENTATION-REGISTER.md` is the canonical finding-level source of truth and this file MUST remain consistent with it.**

- Source audit baseline: `b9de00ba29928111637786f921c1c01249ddcada` (2026-08-07).
- Current hardening change: `FK-HARDEN-2026-08-07`, PR #34.
- Prior integrated evidence: `docs/security/evidence/STEP-05-INTEGRATED-VERIFICATION.md`.
- Post-review technical closure source: `c3f7754441a3f39956836aef48377cda5119c7f4`.
- Post-review closure evidence: `docs/security/evidence/STEP-06-PR34-REVIEW-CLOSURE.md`.
- Owner-approved Foundation defaults: `docs/security/SECURITY-DECISIONS.md`.
- Experimental/Production governance reminder: `docs/security/PRODUCTION-GOVERNANCE-CHECKLIST.md` and issue #35.
- This file is not an ISO/IEC 27001 certificate, Statement of Applicability, legal opinion, residual-risk acceptance for a future deployment, or Production Approval.

## Current verdict

**Repository-side technical blockers raised by the PR #34 security/engineering review are closed for the automated repository scope.**

The technical closure commit `c3f7754441a3f39956836aef48377cda5119c7f4` passed all four required pull-request workflows:

- FoundationKit CI `31191780510` — **success**.
- FoundationKit Security Scan `31191780614` — **success**.
- FoundationKit CodeQL `31191780424` — **success**.
- FoundationKit Windows Launcher Check `31191780425` — **success**.

The Security Scan black-box suite explicitly passed authorization, CSRF, BOLA, account enumeration, maker-checker, MFA step-up and real runtime HTTP `429` rate-limiting coverage.

Documentation/evidence-only commits after that technical source do not invalidate the runtime evidence unless they modify application/security source, workflows, dependencies, deployment behavior or tests.

The repository is currently operating in **experimental / pre-production mode**. A temporary `Protect main` ruleset was created and then intentionally removed by the owner on 2026-08-09 because formal GitHub branch/review governance is not a current development blocker. There is therefore **no active protected-main ruleset currently claimed**.

Current development may still use pull requests and CI/security workflows as the preferred working process, but this is a convention rather than enforced branch protection. Independent approval is also intentionally not a current merge blocker. This does not satisfy Segregation of Duties and is not Production Approval or compliance evidence.

Before the first real Production deployment/release, protected-branch/ruleset enforcement and at least one independent reviewer must be enabled and proven. The mandatory activation checklist is `PRODUCTION-GOVERNANCE-CHECKLIST.md`, and issue #35 is retained as the Production go-live reminder.

Historical PR #34 had already been merged before this governance decision. No retroactive independent-approval claim is made.

## PR #34 review-closure status

| Review finding | Current state | Repository closure |
|---|---|---|
| PR34-REV-01 independent approval | **Historical exception retained / Production activation deferred** | PR #34 predates the current governance decision. Experimental work does not require independent approval or active branch protection. Real Production/release-governed changes must restore and prove both. |
| PR34-APP-01 reverse proxy / forwarded headers | **Verified** | Explicit reverse-proxy decision; exact trusted proxy IP allow-list; no trust-all behavior; middleware runs before HTTPS/rate limiting; trusted/untrusted tests passed. |
| PR34-AUTH-01 MFA full re-authentication | **Verified** | MFA disable and recovery-code rotation require current password + fresh TOTP/recovery factor; black-box negative/positive paths passed. |
| PR34-AUTH-02 independent security notifications | **Repository capability verified / provider external** | Notifications exist for password reset/change, MFA enable/disable and recovery-code regeneration; real Production delivery provider remains external configuration. |
| PR34-CRY-01 SMTP TLS fail-closed | **Verified** | Production startup rejects `SmtpEnableSsl != true`; configuration tests passed. |
| PR34-EVID-01 evidence-register inconsistency | **Closed** | `POLICY-IMPLEMENTATION-REGISTER.md` is canonical; STEP-06 records the post-review evidence. |
| PR34-EVID-02 rate-limit evidence overclaim | **Verified** | Security suite now proves actual middleware rejection with HTTP `429`; workflow run `31191780614` passed. |
| PR34-AUTH-03 silent security defaults | **Verified** | Production requires explicit confirmed-email, admin-MFA, reverse-proxy and password-policy decisions; tests passed. |
| PR34-PASS-01 hard-coded password standard | **Repository design closed / Production screening remains** | Values are configurable and explicit. Owner baseline is recorded. Compromised/common-password screening provider is a future Production requirement, not falsely claimed as implemented. |
| PR34-LOG-01 event catalog vs runtime coverage | **Evidence claim corrected** | Catalog is a target contract; runtime/central SIEM coverage remains partial/external. |
| PR34-SBOM-01 SBOM/provenance terminology | **Evidence claim corrected** | Current artifact is a CycloneDX dependency inventory/baseline SBOM; complete signing/provenance/attestation is not claimed. |

## Baseline findings — executive view

| Area | Current state | Notes |
|---|---|---|
| Independent review / protected `main` | **Deferred to Production** | No active GitHub ruleset is currently claimed. PR/CI remains the preferred experimental workflow. Protected-main enforcement and independent review become mandatory before Production. |
| Risk/threat model | **Verified baseline** | Risk register, threat model, decision register and tracked residual technical risks exist. |
| Secure SDLC / malware gates | **Verified** | Secret scan, NuGet audit, CodeQL, Trivy, baseline SBOM, integrity evidence, build/test/publish/pack passed at post-review technical closure. |
| Dependency/supply-chain baseline | **Verified baseline / partial provenance** | Dependabot + vulnerability gates + SHA-pinned security-sensitive Actions. Full artifact signing/provenance remains external/next hardening. |
| Account lifecycle / MFA | **Verified repository capability** | Confirmation/reset/change, TOTP/recovery login, full MFA step-up and independent notifications are implemented; external delivery/provider still required for Production. |
| Password standard | **Owner Decision Recorded / Production screening incomplete** | Foundation baseline: min 15, default no composition rules, breached/common-password screening required before Production Approved where passwords are used. |
| Admin seed / local credential handling | **Verified baseline** | Production seed rejected; official launcher/CI credential exposure controls exist. |
| Swagger / readiness / AllowedHosts | **Verified baseline** | Swagger Development-only, minimal readiness, explicit Production host allow-list. |
| Rate limiting / proxy trust | **Verified for current automated scope** | Trusted proxy handling + effective client IP partitions + runtime 429 passed. Final ingress topology remains deployment evidence. |
| Maker-checker | **Verified** | Administrator cannot review own initiative; automated evidence exists. |
| CSP/cache policy | **Open / design required** | Requires Blazor-compatible design and compatibility/security testing. This is a known non-blocking next-hardening item, not hidden debt. |
| SQL transport / identity | **Repository production contract implemented** | Encrypted validated SQL transport and non-`sa` runtime enforced; actual certificate/principal provisioning is external. |
| Controlled migrations / restore | **Verified restore baseline** | Startup migration privilege blocked in Production; real CHECKSUM/VERIFYONLY/restore validation exists. |
| Audit / structured security logging | **Partially Satisfied** | Product audit/event target catalog exists; append-only central sink/schema enrichment/correlation remain incomplete/external. |
| Central observability | **External Configuration Required** | Owner baseline sets 365-day security-log retention; SIEM/sink, alert routing and on-call evidence require deployment. |
| PII lifecycle | **Partially Satisfied / product-specific** | Inventory/minimization exist. Legal basis, notice and retention/deletion schedule must be decided per real product; no universal period is fabricated. |
| Crypto/Data Protection | **Repository capability implemented** | DP persistence + X.509 protection and transport checks exist; Vault/KMS/CA/rotation evidence remains external. |
| Production backup service | **Owner Decision Recorded / external implementation** | Baseline is 35 daily + 12 monthly restore points, encrypted/off-site/immutable in Production; provider/proof remain external. |
| Container hardening | **Verified baseline** | Non-root and runtime hardening assertions exist; immutable production digest promotion remains partially open. |
| Quick Tunnel | **Demo-only boundary** | Synthetic demonstrations only; not production ingress and not for sensitive data. |
| Workbench | **Sample-only boundary** | Controlled/local reference; not a public production service. |
| Incident/change runbooks | **Repository process implemented / owner objectives recorded** | Baseline RPO 4h, RTO 8h, vulnerability SLA and max 30-day security exception are recorded; production responders/platform evidence remain external. |

## Owner-approved Foundation baseline

The repository owner authorized the following current/future baseline:

- independent reviewers: `0` required during experimental/pre-production iteration; at least `1` independent reviewer required before real Production/release-governed changes;
- protected-main/ruleset enforcement: intentionally deferred during current experimentation; mandatory before real Production/release governance;
- administrator MFA in Production: required;
- normal-user MFA: supported, not globally mandatory by Foundation;
- password minimum: `15` for the default password-only baseline;
- default password composition rules: not mandatory;
- compromised/common-password blocking: required before Production Approved where passwords remain enabled;
- ASVS target: `Level 2`;
- RPO: `4h`;
- RTO: `8h`;
- security log retention: `365d`;
- backup retention: `35 daily + 12 monthly` restore points;
- vulnerability SLA: Critical `24h`, High `7d`, Medium `30d`, Low `90d`;
- security exception maximum: `30d`;
- concrete cloud/hosting/KMS/SIEM/SMTP/production-SQL/backup provider: deferred until a specific product deployment.

PII/user-data retention remains product/legal-purpose-specific rather than being assigned a fabricated universal duration.

## Production boundary

Current decision: **continue experimental Foundation/product development and defer concrete Production infrastructure and repository/release governance until a product is selected for real deployment.**

Therefore the following remain external by design and are not repository blockers for current experimental development:

- protected-main/ruleset enforcement;
- independent approving reviewer enforcement;
- production domain/TLS ingress/network topology;
- Vault/KMS/CA and key/certificate lifecycle;
- central SIEM/log sink/alerts/on-call;
- production SMTP provider;
- least-privilege runtime/migration SQL principals provisioned by the platform/DBA;
- encrypted/off-site/immutable production backup implementation;
- final PII notices/legal retention/deletion schedule;
- product-specific ASVS applicability, penetration/load acceptance and residual-risk approval.

Before real Production, the controls in `PRODUCTION-GOVERNANCE-CHECKLIST.md` must be activated and evidenced.

## Final classification

**FoundationKit has a verified global-ready technical security baseline for the documented repository/automated scope.**

**Current repository operation is experimental/pre-production. No active GitHub branch ruleset is currently claimed, and independent approval is intentionally not a current merge blocker.**

**Production governance requires protected-branch/ruleset enforcement and independent approval to be restored and proven before real Production/release-governed changes.**

**Historical PR #34 predates the current governance decision; no retroactive independent-approval claim is made.**

**Production Approved: not asserted. ISO/IEC 27001 Certified: not asserted.**