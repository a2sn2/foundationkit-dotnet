# Current Security Status

Status date: 2026-08-15.

FoundationKit is at the **Consumer-ready Core baseline — Pre-production**. Current repository automation proves source/package/reference/generated-product behavior for the exact commit that produced the evidence; it does not certify an arbitrary deployment or imply Production Approval.

## Active repository evidence

- tracked-source secret and repository-hygiene checks;
- NuGet vulnerability audit plus dependency inventory/SBOM evidence;
- analyzers and warnings-as-errors;
- architecture, package-boundary, compatibility and project-isolation tests;
- CodeQL and repository/container security scanning;
- deterministic Composer schema-v1/schema-v2 generation and regeneration checks;
- exact 17 NuGet packages + 17 symbol packages output/integrity evidence;
- generated full-stack SQL Server proof including CRUD, validation, authorization, audit, concurrency, durable idempotency and project isolation;
- SQL read-engine proof for server-side filter/sort/page, indexes and SQL-view-backed read models/reports;
- runtime OpenAPI contract proof with deterministic Postman derivation;
- deterministic C# typed-client generation plus live SQL/API execution proof;
- generated Blazor frontend build/publish proof using the shared `FoundationKit.Blazor` Soft Orbit design system;
- Core Studio/Composer local generation and runnable generated-Blazor proof;
- Workbench SQL Server full-stack integration and contract-source-of-truth smoke;
- Windows manager checks and published documentation/Pages checks.

The final repository-coherence PR was merged only after its complete PR-head workflow set was green, and the resulting `main` head subsequently ran the repository workflow suite successfully. This is engineering evidence for the repository baseline, not an external compliance attestation.

## Current governance mode

`main` is intentionally **not protected** during the current experimental/pre-production phase, and independent approval is not an enforced repository gate. This is a deliberate temporary governance choice recorded by issue #35 and the security decision register.

Accordingly, the current repository state does **not** satisfy a Production Segregation-of-Duties claim. Protected-main/ruleset enforcement, required independent review, stale-approval handling, conversation resolution and exact required release checks must be activated and evidenced before the first real Production-governed release/deployment.

## Not asserted

- Production Approved;
- ISO/IEC, PCI DSS, SOC or other certification;
- universal provider/runtime compatibility;
- organization-wide Segregation of Duties;
- production KMS/secrets/certificate lifecycle;
- production SIEM/central monitoring and incident operations;
- backup/restore or disaster-recovery acceptance in a real environment;
- WAF/ingress/network architecture approval;
- penetration-test acceptance;
- product-specific legal/privacy/retention approval;
- production identity/MFA/recovery implementation for a concrete product.

## Next security trigger

No additional generic repository control should be invented merely to make the Core appear more complete. The next security work should be driven by either:

1. a concrete consumer project that exposes a reusable security gap; or
2. the decision to enter a real Production/release-governed stage, which activates the mandatory controls tracked by issue #35 and `PRODUCTION-GOVERNANCE-CHECKLIST.md`.
