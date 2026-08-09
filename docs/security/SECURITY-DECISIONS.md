# FoundationKit — Security Decisions

This register separates **owner-approved Foundation defaults** from decisions that must remain product/deployment-specific. Repository code may enforce an explicit decision or fail closed, but it must not pretend that a generic Foundation repository can certify a future production environment.

Owner authorization for the Foundation-stage baseline was granted on 2026-08-07. On 2026-08-09 the owner explicitly refined repository governance for the current experimental phase: GitHub branch/ruleset enforcement and independent PR approval are intentionally deferred until real Production/release governance begins. A future product may tighten these values, but must not silently weaken Production requirements without an explicit reviewed exception.

## Approved Foundation baseline

| Decision ID | Decision | Approved value / scope | Status / evidence expectation |
|---|---|---|---|
| D-001 | Required independent PR reviewer count | **Experimental/pre-production: 0 required independent approvals. Production/release-governed changes: 1 independent reviewer minimum.** | No current SoD claim is made. Before real Production, independent approval must be enabled and demonstrated by a real governed PR. |
| D-002 | MFA scope | **Administrators: required in Production. Normal users: capability available; not globally mandatory by Foundation.** | Product risk may require MFA for additional populations. Production must explicitly configure the decision. |
| D-003 | Password baseline | **Minimum 15 characters for the default password-only baseline; no mandatory composition rules; compromised/common-password blocking required before Production Approved where password authentication is used.** | Repository keeps values configurable. A product may adopt a stronger passphrase/IdP standard. Compromised-password provider/source remains product/platform work and is not falsely claimed as complete. |
| D-004 | Application security verification target | **OWASP ASVS Level 2 target baseline** | Applicability must be mapped per product; this is a target, not certification. |
| D-005 | Recovery Point Objective (RPO) | **4 hours baseline** | Production backup design must demonstrate the target or explicitly approve a stricter/different product value. |
| D-006 | Recovery Time Objective (RTO) | **8 hours baseline** | Production recovery exercise must demonstrate the target. |
| D-007 | Security log retention | **365 days baseline** | Central sink, access controls and legal/product constraints remain deployment-specific. |
| D-008 | PII/user-data retention | **Product/legal-purpose specific; no universal duration is approved by Foundation.** | Every real product must publish a retention/deletion schedule before handling production PII. Foundation must not invent a legal retention period. |
| D-009 | Backup retention | **35 daily restore points + 12 monthly restore points baseline; encrypted/off-site/immutable storage required for Production.** | Provider and implementation remain deployment-specific. |
| D-010 | Vulnerability remediation SLA | **Critical: 24h; High: 7d; Medium: 30d; Low: 90d** from confirmed triage, unless an approved exception applies | Upstream-unfixed findings remain visible and tracked; SLA does not mean silently suppressing vendor risk. |
| D-011 | Security exception maximum duration | **30 days** before re-approval/renewal | Exception must name owner, rationale, compensating controls, expiry and evidence. |
| D-012 | Release approval authority | **Real Production / release-sensitive changes require at least one independent approver. Experimental iteration does not.** | Before Production go-live, author/latest-pusher separation, stale-approval dismissal and required-review enforcement must be enabled where GitHub supports them. Author must not self-satisfy the Production approval gate. |
| D-013 | Residual-risk acceptance authority | **Repository owner for Foundation-only residual repository risk; product/business + security authority for Production risk** | Acceptance must be explicit, dated and scoped; never inferred from green CI. |
| D-014 | Secret manager / KMS / certificate provider | **Product/deployment-specific; no provider selected at Foundation stage** | Production must choose an approved external provider and provide lifecycle/rotation evidence. |
| D-015 | Production hosting/network architecture | **Deferred until a concrete product deployment** | Foundation closes as a global-ready technical baseline, not as a deployed production environment. Final ingress/TLS/network controls remain external. |
| D-016 | Production database account provisioning | **Separate least-privilege runtime and migration principals required; `sa` prohibited for runtime** | Exact principal names/provisioning are DBA/platform-specific. |

## Experimental governance interpretation

Current repository mode is **experimental / pre-production**. As of 2026-08-09 there is **no active GitHub branch ruleset**. The owner intentionally removed the temporary `Protect main` ruleset because formal repository governance is not a current development blocker.

During this phase, PRs and the existing CI/security workflows remain the preferred development process, but they are a working convention rather than an externally enforced protected-branch control. Independent approval is also intentionally **not** a blocking gate.

This temporary workflow choice does not satisfy Segregation of Duties and must not be used as Production Approval or compliance evidence.

The mandatory activation checklist for Production is `PRODUCTION-GOVERNANCE-CHECKLIST.md`.

## Authentication configuration interpretation

The approved Foundation direction is:

- `RequireAdministratorMfa=true` in real Production unless a documented product-specific exception is approved.
- Normal-user MFA remains available and may be made mandatory by a product risk decision.
- Default production password baseline: minimum `15` characters.
- Default production password composition flags should be `false` unless a product-specific standard requires otherwise; strength should come from length, screening, rate limiting, MFA and secure recovery rather than artificial composition complexity.
- Compromised/common-password screening is a **Production requirement** where password authentication remains enabled, but the external source/provider is deliberately not invented by this generic repository.

## Recovery / operations interpretation

Foundation baseline objectives:

- RPO: `4h`.
- RTO: `8h`.
- Security log retention: `365d`.
- Backup retention: `35 daily + 12 monthly` restore points, with encryption/off-site/immutability in Production.
- Vulnerability SLA: Critical `24h`, High `7d`, Medium `30d`, Low `90d`.
- Security exception validity: maximum `30d` before renewal.

These are engineering/governance defaults for future product planning. They do not prove that any current external environment meets the objectives.

## Production boundary decision

**Current stage decision:** continue experimental Foundation/product development and defer concrete Production infrastructure and repository/release governance until a specific product is selected for real deployment.

Therefore no cloud vendor, domain, KMS/Vault, SIEM, SMTP provider, production SQL service, backup provider, legal PII schedule or ingress topology is fabricated in this repository. Those become a deployment change with its own evidence, load/security acceptance, independent approval and residual-risk decision.

Before the first real Production deployment/release, `D-001` and `D-012` require independent approval enforcement to be restored and proven, and the protected-branch/ruleset controls in `PRODUCTION-GOVERNANCE-CHECKLIST.md` must be activated.

## Decision record rule

Future changes to an approved value must record:

```text
Decision ID:
Previous value:
New value/scope:
Approver:
Approval date:
Rationale:
Evidence reference:
Review/expiry date:
Affected policies:
Affected implementation/tests:
```

No chat, green build, or code change alone converts an external deployment requirement into Production Approval.