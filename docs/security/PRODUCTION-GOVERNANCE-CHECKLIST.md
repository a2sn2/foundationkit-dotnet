# FoundationKit — Production Governance Activation Checklist

## Current operating mode

As of 2026-08-09, the repository is intentionally operating in an **experimental / pre-production development mode**.

The repository owner has explicitly decided that **independent pull-request approval is not a blocking requirement during this experimental phase**. This is a temporary workflow decision intended to keep rapid iteration possible while the architecture, products, CI, security controls, and operating model are still being developed.

This decision does **not** claim that Segregation of Duties (SoD) is satisfied in the experimental phase, and it must not be used as Production Approval, compliance certification, or evidence of independent review.

## Controls that should remain enabled during experimentation

The `main` branch should continue to retain the non-review protection baseline where supported:

- pull request required before merge;
- required CI/security/status checks must pass;
- branch must be up to date before merge;
- review conversations must be resolved;
- branch deletion restricted;
- force pushes blocked;
- no routine bypass entries;
- secret scanning, dependency audit, CodeQL, Trivy, tests, publish, pack, and SQL/E2E verification remain part of the repository gate.

Independent approval is the only governance gate intentionally deferred for the current experimental phase.

## Mandatory activation before real Production

Before the first real Production deployment, production release, or formal production-governed change process, the owner must explicitly re-enable and verify all of the following:

1. Require at least **1 independent approving reviewer** for pull requests targeting `main` or the production release branch.
2. The approving reviewer must be someone other than the PR author / latest pusher where GitHub supports that distinction.
3. Enable stale-approval dismissal when new reviewable commits are pushed.
4. Require approval of the most recent reviewable push by someone other than its pusher where supported.
5. Keep conversation resolution required before merge.
6. Keep required CI/security/status checks mandatory and verify the exact required check names.
7. Keep branch-up-to-date enforcement before merge.
8. Keep force-push blocking and deletion restriction.
9. Define the allowed emergency/break-glass bypass path, named authority, scope, expiry, and audit evidence before any production exception is used.
10. Record the effective GitHub ruleset/branch-protection evidence and date in the security/governance register.
11. Re-check CODEOWNERS / reviewer ownership if production ownership has been established.
12. Verify that Production-specific external controls are ready: secrets/KMS or equivalent, production SQL identities and TLS, central logging/SIEM, backup/restore, incident ownership, SMTP/provider requirements where applicable, and product-specific PII/legal requirements.

## Required evidence before Production Approved can be considered

At minimum, retain evidence of:

- active Production governance ruleset or branch-protection configuration;
- at least one real PR that demonstrates independent approval enforcement;
- successful exact-head CI/security gates;
- no unresolved review threads;
- production deployment/rollback/recovery ownership;
- product-specific security and data-handling decisions;
- residual-risk review by the appropriate authority.

## Boundary

This checklist records a **future activation requirement**. It does not make the current experimental repository Production Approved, ISO/IEC 27001 certified, SoD-compliant, or externally audited.
