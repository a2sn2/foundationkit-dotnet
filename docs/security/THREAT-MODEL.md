# FoundationKit Core Threat Model

## Assets

- reusable package source and release artifacts;
- capability/contract metadata and Composer generated output;
- Workbench reference database/configuration;
- host-defined credentials, identities, authorization policies and business data.

## Trust boundaries

1. package consumer ↔ FoundationKit public contracts;
2. HTTP client ↔ host WebApi pipeline;
3. host application ↔ database/provider;
4. Composer manifest/input ↔ generated filesystem output;
5. project identity namespace ↔ shared external provider resources.

## Primary threats and controls

- **cross-project state collision** — immutable project identity, host-local DI/module definitions, canonical resource namespace, no public mutable static state;
- **authorization omission** — generic module authorization fails closed when requested without a semantic policy;
- **over-posting/entity leakage** — explicit request/response contracts and mapper rather than binding EF entities directly;
- **stale writes** — concurrency policy plus EF concurrency exception translation;
- **sensitive audit leakage** — bounded audit schema and sensitive attribute-name rejection;
- **provider coupling** — lower layers cannot select SQL Server or own host migrations;
- **supply-chain drift** — central package management, vulnerability/SBOM scans, CodeQL/Trivy, package hashes;
- **generator path abuse** — Composer validates destination ownership/boundaries and deterministic generation is tested.

## Residual risks

Production identity topology, tenant isolation, external cache/storage/message providers, secrets/KMS, network ingress, SIEM, retention/privacy, backup operations, load limits and incident response remain deployment-specific until their reusable/provider contracts are explicitly implemented and tested.
