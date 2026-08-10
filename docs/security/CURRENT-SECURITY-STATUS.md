# Current Security Status

Status date: 2026-08-10.

FoundationKit is a pre-production reusable software foundation. Current repository automation proves source/package/reference behavior for the exact commit that produced the evidence; it does not certify an arbitrary deployment.

## Active evidence

- tracked-source secret/hygiene checks;
- NuGet vulnerability audit and dependency inventory/SBOM;
- analyzers and warnings-as-errors;
- architecture and isolation tests;
- CodeQL and Trivy workflows;
- deterministic Composer generation checks;
- exact 17+17 package output/integrity evidence;
- Workbench SQL integration including generic CRUD concurrency/error paths;
- non-root Workbench container and compose hardening policy.

## Not asserted

- Production Approved;
- ISO/IEC certification;
- universal provider compatibility;
- organization-wide Segregation of Duties;
- production KMS/secrets, SIEM, backup, WAF, penetration-test or legal/privacy acceptance.

Before production-governed release, protected branch/ruleset and independent review requirements tracked by the governance issue must be activated and evidenced on a real governed change.
