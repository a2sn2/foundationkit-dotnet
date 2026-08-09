# FoundationKit — Production Governance Activation Checklist

## Current operating mode

As of 2026-08-09, the repository is intentionally operating in an **experimental / pre-production development mode**.

The repository owner has explicitly decided that **formal GitHub branch/ruleset enforcement and independent pull-request approval are not blocking requirements during this experimental phase**. A temporary `Protect main` ruleset was created and then intentionally removed because this is not yet the real Production governance stage.

Current development may continue to use pull requests and the existing CI/security workflows as the preferred working process, but no active protected-branch/ruleset control is claimed.

This decision does **not** claim that Segregation of Duties (SoD) is satisfied in the experimental phase, and it must not be used as Production Approval, compliance certification, or evidence of independent review.

## Recommended experimental workflow

Even without an active ruleset, prefer:

- changes through pull requests rather than direct `main` edits;
- successful CI/security checks before merge;
- exact-head verification for significant changes;
- resolved review conversations when reviews exist;
- no routine force pushes or branch deletion that destroys useful history;
- secret scanning, dependency audit, CodeQL, Trivy, tests, publish, pack, and SQL/E2E verification for repository changes that trigger those workflows.

These are current engineering practices, not externally enforced Production controls.

## Mandatory activation before real Production

Before the first real Production deployment, production release, or formal production-governed change process, the owner must explicitly enable and verify all of the following:

1. Create and activate a protected-branch/ruleset policy for `main` or the designated Production release branch.
2. Require pull requests before merge.
3. Require at least **1 independent approving reviewer**.
4. The approving reviewer must be someone other than the PR author / latest pusher where GitHub supports that distinction.
5. Enable stale-approval dismissal when new reviewable commits are pushed.
6. Require approval of the most recent reviewable push by someone other than its pusher where supported.
7. Require conversation resolution before merge.
8. Require the exact CI/security/status checks used by the release process and verify their current names.
9. Require the branch to be up to date before merge.
10. Block force pushes and restrict deletion of the protected branch.
11. Define the allowed emergency/break-glass bypass path, named authority, scope, expiry, and audit evidence before any Production exception is used.
12. Record the effective GitHub ruleset/branch-protection evidence and activation date in the security/governance register.
13. Re-check CODEOWNERS / reviewer ownership if Production ownership has been established.
14. Verify that Production-specific external controls are ready: secrets/KMS or equivalent, Production SQL identities and TLS, central logging/SIEM, backup/restore, incident ownership, SMTP/provider requirements where applicable, and product-specific PII/legal requirements.

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
