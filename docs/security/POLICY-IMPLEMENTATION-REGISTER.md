# FoundationKit — Policy Implementation Register

> **Canonical finding-level source of truth** for repository security, governance, recovery, and production evidence.
>
> Repository: `a2sn2/foundationkit-dotnet`  
> Baseline before this hardening program: `b9de00ba29928111637786f921c1c01249ddcada`  
> Program branch: `hardening/global-grade-baseline`  
> Source assessment: `FoundationKit_ISO27001_Repository_Audit_AR.md` (2026-08-07)  
> Post-review technical closure source: `c3f7754441a3f39956836aef48377cda5119c7f4`  
> Post-review evidence: `docs/security/evidence/STEP-06-PR34-REVIEW-CLOSURE.md`.

`CURRENT-SECURITY-STATUS.md` is an executive view and MUST remain consistent with this register. When they conflict, this register controls finding-level status.

## Status model

- `Open` — confirmed repository gap with no implemented control.
- `Implemented / verification pending` — control exists in source/configuration but the latest affected source head has not yet completed the required evidence run.
- `Verified` — reproducible evidence demonstrates the control in the explicitly stated repository scope.
- `Partially Satisfied` — useful control exists but the finding is not fully satisfied in repository/deployment scope.
- `Owner Decision Recorded` — a repository/Foundation governance value has been explicitly approved; deployment proof may still be required.
- `External Configuration Required` — completion depends on GitHub/hosting/DBA/KMS/SIEM/backup or another external platform.
- `Residual Risk Tracked` — the risk is recorded and monitored; this does not mean accepted.
- `Residual Risk Accepted` — requires named authority, date, rationale, and evidence; never inferred.

A green build is not production approval. `Verified` always names its scope; organizational/external requirements remain separate.

## Mandatory policy set

1. Segregation of Duties Policy.
2. Data Transfer Policy.
3. Password Management Policy.
4. Logging and Monitoring Policy.
5. Data Backup Policy.
6. Personally Identifiable Information Protection Policy.
7. Secure Software Development Life Cycle Policy.
8. Malware Protection Policy.
9. Cryptography and Key Management Policy.
10. Application Security Policy.
11. Change Management Policy.
12. Risk Management Policy.

## Findings register

| ID | Sev. | Finding / risk | Policies | Current status | Implemented control / evidence / residual action |
|---|---:|---|---|---|---|
| FK-GOV-001 / PR34-REV-01 | High | Independent review / maker-checker for repository changes | SoD, Change, SDLC | **Owner Decision Recorded / Production activation required** | On 2026-08-09 the owner explicitly deferred independent PR approval during the experimental/pre-production phase. Current experimental governance keeps PRs, required CI/security checks, branch freshness, conversation resolution, deletion restriction and force-push blocking, but does not claim SoD. Before real Production/release-governed changes, at least 1 independent approval plus latest-push separation and stale-approval dismissal must be enabled and proven by a real governed PR. See `PRODUCTION-GOVERNANCE-CHECKLIST.md` and issue #35. |
| FK-GOV-002 | High | Protected `main` / required checks evidence | SoD, Change | **Verified non-review protection baseline / Production review gate deferred** | The owner activated ruleset `Protect main` targeting default branch `main`, with empty bypass list, deletion restriction, force-push blocking, pull-request requirement, required status checks and branch-up-to-date enforcement. Independent approval is intentionally deferred during experimentation and must be re-enabled before Production. Dated governance evidence is tracked in issue #35. |
| FK-RISK-001 | High | Repository risk/threat model absent | Risk, SDLC, AppSec | **Verified baseline** | `RISK-REGISTER.md`, `THREAT-MODEL.md`, `SECURITY-DECISIONS.md`; residual/external risks remain explicitly tracked. |
| FK-SDLC-001 | High | Security CI gates absent | SDLC, Malware, AppSec | **Verified** | Secret scan, NuGet audit, CodeQL, Trivy, negative suite, SBOM/integrity evidence, build/test/publish/pack. STEP-06 records successful post-review runs at `c3f7754...`. |
| FK-SUP-001 | High | Dependency governance weak | Malware, SDLC | **Verified baseline / further hardening available** | Central package floors, NuGet audit, Trivy, baseline CycloneDX inventory, Dependabot. Lock files/source mapping remain optional next-hardening items or registry decisions. |
| FK-SUP-002 | Medium | Mutable GitHub Action references | SDLC, Malware | **Verified for hardening-touched workflows** | Security-sensitive workflow actions touched by the program are SHA-pinned and passed. |
| FK-REL-001 / PR34-SBOM-01 | High | Release integrity/provenance incomplete | SDLC, Malware, Crypto | **Partially Satisfied** | CycloneDX **dependency inventory / baseline SBOM** + SHA-256 package/publish manifests exist. Complete provenance/signing/attestation is not claimed; signing authority remains external/organizational. |
| FK-TEST-001 | High | Security-negative coverage weak | SDLC, AppSec | **Verified for current automated abuse scope** | Unit + black-box authz/CSRF/BOLA/enumeration/maker-checker + MFA step-up + runtime 429 passed at the STEP-06 technical closure source. |
| FK-AUTH-001 / PR34-AUTH-01 / PR34-AUTH-02 | High | Account lifecycle / MFA lifecycle incomplete | Password, AppSec | **Verified repository capability / external delivery remains** | Confirmation/reset/change password, TOTP/recovery login, full password+fresh-MFA step-up for disable/recovery rotation, security notifications for password/MFA lifecycle. Post-review black-box suite passed; production SMTP/provider remains deployment configuration. |
| FK-AUTH-002 / PR34-PASS-01 | Medium–High | Password standard not organization-approved; no compromised-password blocklist | Password, Risk | **Owner Decision Recorded / Production screening incomplete** | Foundation baseline is recorded in `SECURITY-DECISIONS.md`: minimum 15 characters, no mandatory composition rules by default, compromised/common-password screening required before Production Approved where passwords are used. Values are configurable/explicit; screening provider remains product/platform work. |
| FK-AUTH-003 | High | Admin seed can create/promote administrator | Password, SoD, Change | **Verified production fail-closed baseline** | Production rejects seed; Development seed refuses silent promotion; automated evidence passed. |
| FK-SEC-001 | Medium | Local/admin credential disclosure | Password, Crypto, Logging | **Verified launcher/CI baseline** | Official launchers avoid routine password echo; generated CI credentials are masked; local credential protection exists. |
| FK-APP-001 | High | Swagger exposed outside Development | AppSec, Data Transfer | **Verified baseline** | Swagger/UI Development-only; configuration/build tests passed. |
| FK-APP-002 / PR34-APP-01 / PR34-EVID-02 | High | Rate limiting/proxy identity can collapse clients or be spoofed; prior evidence overclaim | Password, AppSec, Data Transfer, Risk | **Verified for current repository/runtime test scope** | Explicit trusted-proxy allow-list; `ForwardedHeaders` before HTTPS/rate limiting; untrusted headers ignored; auth partition per effective client IP; write partition per user/IP; black-box suite proved real HTTP `429`. Final production ingress IP/topology remains deployment evidence. |
| FK-APP-003 | High | Administrator can review own initiative | SoD, AppSec | **Verified** | Domain maker-checker rule + unit/black-box test passed. |
| FK-APP-004 | Medium | Negative application-security coverage weak | AppSec, SDLC | **Verified for current suite** | Current suite covers authz, CSRF, BOLA, enumeration, maker-checker, MFA step-up and runtime 429. Owner baseline targets ASVS Level 2; broader product-specific ASVS mapping remains future work. |
| FK-APP-005 | Medium | Readiness leaks implementation detail | AppSec, Logging | **Verified** | Public readiness returns status only; integration evidence passed. |
| FK-APP-006 | Medium | App-specific CSP/cache policy incomplete | AppSec | **Open / design required** | Blazor-compatible CSP/cache policy requires explicit compatibility/security testing; no false claim of completion. |
| FK-APP-007 / PR34-AUTH-03 | Medium | Production settings can silently fall back to permissive defaults | AppSec, Password, Change | **Verified production explicit-decision mechanism** | Production requires explicit AllowedHosts plus explicit true/false decisions for confirmed email, admin MFA and reverse-proxy mode; password policy values are explicit. Configuration tests passed. |
| FK-DATA-001 | High | DB transport encryption/cert validation | Data Transfer, Crypto | **Implemented production contract / external certificate evidence** | Production rejects disabled encryption and `TrustServerCertificate=True`; deployment must prove trusted server certificate/route. |
| FK-DATA-002 | High | Runtime SQL `sa` / privilege separation | SoD, AppSec, Change | **Implemented production contract / external provisioning** | Production rejects `sa`; owner baseline requires separate least-privilege runtime/migration identities; platform/DBA provisioning remains external. |
| FK-DB-001 | High | Startup migrations in Production | Change, SDLC, SoD | **Verified production fail-closed baseline** | Production rejects automatic migration/role seeding; reviewed deployment migration step required. |
| FK-DB-002 | High | Migration/recovery evidence | Change, Backup | **Verified restore baseline / change-specific schema action remains** | Real CHECKSUM backup, VERIFYONLY, isolated restore and schema-qualified table checks passed. Destructive schema changes still require per-change rollback/rollforward plan. |
| FK-AUD-001 | High | Audit shares application DB trust boundary | Logging, AppSec | **External Configuration Required** | DB audit is useful but not claimed tamper-evident; central append-only/restricted sink required. |
| FK-AUD-002 / PR34-LOG-01 | Medium | Structured security audit/event coverage incomplete | Logging, AppSec | **Partially Satisfied** | Event schema/catalog is explicitly a **target contract**, not proof every event is emitted. Runtime emission/DB audit enrichment/central correlation remain work. |
| FK-LOG-001 | High | Central observability/alerting/retention absent | Logging | **External Configuration Required** | Owner baseline sets security log retention to 365 days; SIEM/sink, alert routing and on-call evidence remain deployment implementation. |
| FK-PII-001 | High | PII lifecycle/retention/deletion incomplete | PII | **Partially Satisfied / product decision required** | PII inventory/minimization exists. Owner deliberately did not invent a universal PII retention period; each real product must approve legal basis, notice, retention and deletion before Production handling of PII. |
| FK-CRY-001 / PR34-CRY-01 | High | Secret/crypto transport/lifecycle incomplete | Crypto, Data Transfer | **Verified repository transport baseline / external provider evidence** | Crypto inventory, SQL TLS checks, SMTP TLS fail-closed and secret contracts. Configuration tests passed; Vault/KMS/CA/rotation provider and operational evidence remain external. |
| FK-CRY-002 | High | Data Protection keys not durable/protected | Crypto, Password | **Implemented capability / external material required** | Durable file persistence + X.509 protection capability; certificate/key storage/rotation lifecycle is external. |
| FK-BACK-001 | Critical | Backup exists without proven restore | Backup, Risk | **Verified** | CHECKSUM backup, VERIFYONLY, real isolated restore, core-table validation and cleanup passed in CI evidence. |
| FK-BACK-002 | High | Production backup encryption/off-site/retention | Backup, Crypto, PII | **Owner Decision Recorded / External Configuration Required** | Baseline: 35 daily + 12 monthly restore points, encrypted/off-site/immutable in Production. Provider and operational proof remain deployment work. |
| FK-DOCK-001 | High | Container hardening weak | Malware, AppSec, SDLC | **Verified baseline** | Non-root app runtime, capability/no-new-privilege controls, health checks; integration assertion passed. |
| FK-DOCK-002 | Medium | Mutable image tags/digests | Malware, SDLC | **Partially Satisfied** | Dependabot + Trivy gates; production digest pin/update/promotion process remains open. |
| FK-SUP-003 | High where applicable | Upstream unfixed image CVEs | Malware, Risk | **Residual Risk Tracked** | Fixable HIGH/CRITICAL findings block CI; unfixed findings remain visible in SARIF and `R-FK-016`; no implicit acceptance. |
| FK-TUN-001 | High if public | Development Quick Tunnel exposed publicly | Data Transfer, AppSec, PII | **Accepted demo-only boundary** | Temporary random tunnel is synthetic-demo only; not a production ingress and must not carry real/sensitive data. |
| FK-WB-001 | High if exposed | Workbench intentionally lacks auth | AppSec, Password | **Sample-only boundary** | Controlled/local reference only; not a production/public-data service. |
| FK-CHG-001 / PR34-EVID-01 | High | Change/evidence chain inconsistent | Change, Risk, SDLC | **Verified repository evidence chain** | Canonical register and executive status are synchronized; STEP-05 preserves prior integrated evidence and STEP-06 records post-review closure evidence. Current experimental governance and Production activation requirement are tracked in `SECURITY-DECISIONS.md`, `PRODUCTION-GOVERNANCE-CHECKLIST.md`, and issue #35. |
| FK-INC-001 | Medium | Vulnerability reporting channel/SLA | Risk, Malware, Logging | **Owner Decision Recorded / channel external** | SLA baseline: Critical 24h, High 7d, Medium 30d, Low 90d. Private reporting channel/response ownership still require repository/org configuration. |
| FK-OPS-001 | High | Incident/rollback/recovery operations incomplete | Logging, Backup, Risk | **Owner Decision Recorded / external ownership pending** | Incident/rollback/recovery/PIR runbook exists. Baseline RPO 4h / RTO 8h and max security exception 30d are approved; named production responders/platform proof remain external. |

## PR #34 review-closure matrix

| Review ID | Repository action | Current state |
|---|---|---|
| PR34-REV-01 | Independent GitHub review + protected branch evidence | **Historical PR #34 exception retained. Independent approval is intentionally deferred during current experimentation and becomes mandatory before real Production/release governance.** |
| PR34-APP-01 | Trusted forwarded headers, explicit proxy IP allow-list, ordering before HTTPS/rate limiting, trusted/untrusted proxy tests | **Verified at STEP-06 technical closure source** |
| PR34-AUTH-01 | Password + fresh TOTP/recovery proof for MFA disable/recovery rotation | **Verified by black-box security suite** |
| PR34-AUTH-02 | Independent email security notifications for password/MFA lifecycle | **Repository capability verified; delivery provider external** |
| PR34-CRY-01 | Production SMTP TLS fail-closed | **Verified by configuration tests** |
| PR34-EVID-01 | Canonical register synchronized; executive status derived from it | **Closed in repository** |
| PR34-EVID-02 | Real runtime 429 black-box assertion | **Verified by Security Scan run `31191780614`** |
| PR34-AUTH-03 | Explicit Production decisions required instead of silent false fallback | **Verified by configuration tests** |
| PR34-PASS-01 | Password values configurable + explicit in Production; Foundation baseline recorded | **Repository design closed; compromised-password provider remains Production work** |
| PR34-LOG-01 | Event catalog explicitly labeled target contract; runtime coverage remains partial | **Evidence claim corrected** |
| PR34-SBOM-01 | SBOM terminology narrowed to dependency inventory/baseline SBOM; no full-provenance claim | **Evidence claim corrected** |

## Owner-approved Foundation decisions

See `SECURITY-DECISIONS.md` for the canonical decision record. Current baseline includes:

- independent reviewers: `0` required during experimental/pre-production iteration; at least `1` independent reviewer required before real Production/release-governed changes;
- administrator MFA in Production: `required`;
- normal-user MFA: available, not globally mandatory by Foundation;
- password baseline: minimum `15`, default no mandatory composition rules; compromised/common-password screening required before Production Approved where passwords are used;
- ASVS target: `Level 2`;
- RPO: `4h`;
- RTO: `8h`;
- security log retention: `365d`;
- backup retention: `35 daily + 12 monthly` restore points;
- vulnerability SLA: Critical `24h`, High `7d`, Medium `30d`, Low `90d`;
- security exception maximum duration: `30d`;
- production provider/network infrastructure: deferred until a concrete product deployment.

PII retention remains product/legal-purpose-specific by design; Foundation does not fabricate a universal legal retention period.

## Verification rule

A finding moves to `Verified` only when this register points to reproducible evidence for the affected security-relevant source head. Documentation-only evidence/status commits after `c3f7754...` do not invalidate runtime evidence unless they modify application/security source, workflows, dependencies, deployment behavior or tests.

External/governance configuration is recorded independently from runtime CI. The active `Protect main` ruleset provides non-review protection during experimentation; independent approval is explicitly deferred by owner decision and must be re-enabled and evidenced before Production. This experimental exception is not SoD evidence and does not imply Production Approval.