# Security Policy

## Supported repository scope

FoundationKit is pre-1.0. Security fixes target the current `main` branch/current package line unless a separate maintenance commitment is published.

The repository currently contains:

- 17 reusable FoundationKit packages;
- Workbench — executable architecture/reference consumer;
- Athar — complete Arabic reference product;
- Madar — operational case-management product through v0.10;
- Composer/catalog tooling, deployment samples, tests, Atlas, and security/recovery automation.

## Reporting a vulnerability

Do **not** open a public GitHub issue containing an undisclosed exploitable vulnerability, credential, token, PII, customer record, private document, or confidential architecture detail.

An approved private repository/organization reporting channel is still an external Production/governance requirement. Until such a channel is configured and evidenced, contact the repository owner through the GitHub profile and agree on a private channel **before** sharing sensitive material:

<https://github.com/a2sn2>

Include the affected package/product/tool, commit/artifact/image/version, impact, reproduction conditions, and suggested mitigation when available. The repository-side triage/retest process is documented in `docs/security/VULNERABILITY-MANAGEMENT.md`.

## Public development surfaces

GitHub issues, screenshots, Postman collections, Swagger, Atlas/Pages, and demo/tunnel surfaces must not contain production tokens, passwords, personal/customer/employee data, financial secrets, or confidential business payloads.

Workbench is reference/sample scope and must not be exposed as an unauthenticated Production service. Athar and Madar implement deeper product security controls, but their Development/CI bootstrap, Compose topology, local credential handling, and test data are **not** Production deployment templates unless explicitly documented as such.

## Local development credentials

Local helpers may create generated development credentials/state under ignored `.local/` paths or process environment variables. Windows launchers protect supported local credential files with current-user ACLs where applicable.

Never commit:

- real connection strings/passwords;
- `.env`/local secrets files;
- access/refresh/API tokens;
- private keys or production certificates;
- database backups;
- customer/employee/production datasets.

## Automated security baseline

Normal pull-request verification includes, as applicable:

- tracked-source secret and repository-hygiene checks;
- NuGet vulnerability audit and CycloneDX dependency inventory;
- CodeQL;
- Trivy repository/container scanning;
- container-hardening policy checks;
- Athar black-box negative security coverage;
- Workbench/Athar/Madar SQL/E2E regressions;
- package/publish integrity evidence.

Exact evidence belongs to the exact tested head. Scanner success is not Production Approval.

## Production boundary

Reusable FoundationKit packages provide technical primitives, not a complete deployment/security architecture. A real Production product still owns or must evidence identity/provider configuration, product authorization, TLS/ingress, secret/KMS lifecycle, network policy, SQL principals/certificates, audit/SIEM, backup/recovery, PII retention, monitoring/alerting, performance/penetration acceptance, and incident/release governance.

Issue #35 remains the mandatory protected-branch/independent-review governance gate before real Production. Current repository operation is experimental/pre-production and does not claim Segregation of Duties, Production Approval, or ISO/IEC 27001 certification.
