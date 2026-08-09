# FoundationKit — Current Security and Policy Status

**Living executive reference.** `POLICY-IMPLEMENTATION-REGISTER.md` remains the canonical finding-level register; historical evidence under `docs/security/evidence/` is retained as audit history and must not be mistaken for the current repository head.

Status date: **2026-08-09**.

## Current technical baseline

FoundationKit Core v0.1 is closed as a composable repository baseline. The reusable output remains **17 NuGet packages + 17 symbol packages** and the repository contains three distinct consumers:

- Workbench — executable architecture/reference consumer;
- Athar — complete Arabic reference product;
- Madar — operational case-management product through v0.10.

The latest dependency-servicing hardening before this truth-sync pass was PR #103 / merge `8dc335e05820bef2f69537c121b9b40befc16842`. Its exact tested head passed CI, Security Scan, CodeQL and Workbench/Athar/Madar SQL integration with 303/303 automated tests and the unchanged 17+17 package invariant.

Current direct .NET 8 servicing packages are aligned to 8.0.29 where applicable; deprecated `Azure.Identity 1.13.2` was replaced by 1.17.2, the SQL client security floor is 5.1.9, and Dependabot now also monitors the Madar Dockerfile.

Migration from the current `net8.0` baseline to .NET 10 LTS is tracked separately in Issue #104 before .NET 8 reaches end of support. The migration is **not** claimed as completed.

## What repository automation currently proves

The normal pull-request gates cover, as applicable:

- tracked-source secret scanning and repository hygiene;
- dependency audit and CycloneDX dependency inventory;
- Release build with analyzers;
- generated capability/contract/maturity-evidence drift protection;
- FoundationKit, Workbench, Athar, and Madar tests;
- publish and exact reusable-package count;
- Workbench/Athar/Madar SQL Server integration;
- Athar authentication/authorization/CSRF/maker-checker/MFA/rate-limit negative coverage and backup/restore proof;
- Madar lifecycle, routing, approvals, attachments and authorized search/reporting privacy regressions;
- container hardening and Trivy gates;
- CodeQL for supported languages.

A green historical run is not proof for a newer behavior/security/dependency-relevant head. Exact-head evidence remains the preferred rule.

## Current repository mode

The repository is intentionally operating in **experimental / pre-production mode**.

There is no active protected-main ruleset currently claimed and independent approval is not a development blocker. Pull requests and full verification remain the preferred workflow, but this convention is not Segregation-of-Duties evidence and is not Production Approval.

Before the first real Production deployment or production-governed release, Issue #35 and `PRODUCTION-GOVERNANCE-CHECKLIST.md` require protected branch/ruleset enforcement, required checks, at least one independent reviewer, conversation resolution, force-push/deletion restrictions, a documented break-glass path, and proof through a real governed PR.

## Open or external Production work

Repository automation cannot invent deployment/organizational controls. The following remain open, external, or product-specific as documented in the canonical register:

- Blazor-compatible CSP/cache policy design and testing (`FK-APP-006`);
- private vulnerability-reporting channel and organizational response ownership;
- breached/common-password screening provider where passwords remain in Production;
- real ingress/TLS/reverse-proxy topology;
- Vault/KMS/CA and key/certificate lifecycle;
- least-privilege runtime/migration SQL principals and trusted server certificates;
- central SIEM/log sink, metrics/tracing, alerts, on-call and retention operations;
- production SMTP provider;
- production backup provider and periodic recovery evidence;
- product-specific PII legal basis, notice, retention/deletion/export decisions;
- production object storage/malware scanning/retention for Madar attachments;
- product-specific ASVS applicability, penetration/load acceptance and residual-risk approval;
- immutable image/release promotion and full artifact signing/provenance when required.

The repository has a documented vulnerability process in `VULNERABILITY-MANAGEMENT.md`; private intake configuration remains external until an approved channel is enabled.

## Historical hardening evidence

PR #34 and its `STEP-05`/`STEP-06` evidence remain useful historical proof for the original security hardening program. Their old commit IDs and workflow run IDs are **historical evidence**, not the definition of the current repository head.

The canonical register preserves that evidence and the individual finding IDs. This executive document intentionally summarizes current state rather than presenting PR #34 as the current change.

## Owner-approved baseline still in force

- administrator MFA required in Production;
- password-only baseline minimum 15 characters, with no mandatory composition rules by default;
- compromised/common-password screening required before Production Approval where passwords remain enabled;
- ASVS target Level 2;
- RPO 4h / RTO 8h;
- security-log retention baseline 365 days;
- backup retention baseline 35 daily + 12 monthly restore points;
- vulnerability SLA: Critical 24h, High 7d, Medium 30d, Low 90d;
- security exception maximum 30 days;
- concrete cloud/hosting/KMS/SIEM/SMTP/production-SQL/backup providers selected per deployment rather than embedded into FoundationKit.

## Classification

**FoundationKit has a verified technical repository baseline for the documented automated scope and is suitable for development, QA, staging/pilot, and building real products.**

**Production Approved: not asserted.**  
**ISO/IEC 27001 Certified: not asserted.**  
**Segregation of Duties: not asserted for the current experimental repository workflow.**
