# Security Policy

FoundationKit is a reusable pre-production software foundation. Repository security evidence is not a claim that an arbitrary deployment is production-approved.

## Report a vulnerability

Do not publish secrets, exploit details, or sensitive deployment data in a public issue. Use the repository owner's private security-reporting channel where available.

## Repository controls

The repository uses secret scanning policy checks, dependency vulnerability audit, SBOM generation, CodeQL, Trivy, Release builds with analyzers/warnings-as-errors, architecture tests, package integrity hashes, container hardening checks, and Workbench SQL integration.

## Core security boundaries

- reusable packages must not contain deployment credentials;
- project-specific configuration and data stay in the consuming host;
- reusable database abstractions do not choose production SQL topology;
- authorization-enabled generic modules fail closed without an explicit semantic policy;
- project identity is a namespacing boundary, not authentication or tenant authorization;
- generated/public API compatibility is reviewed separately from deployment security.

## Production boundary

A real deployment still needs threat modeling, TLS/ingress controls, secret/KMS policy, least-privilege database identities, backup/restore operations, observability/SIEM, incident response, load/performance acceptance, penetration testing where appropriate, privacy/legal decisions, and protected-branch/independent-review governance.

See `docs/security/CURRENT-SECURITY-STATUS.md`, `docs/security/THREAT-MODEL.md`, and `docs/PRODUCTION-READINESS-AR.md`.
