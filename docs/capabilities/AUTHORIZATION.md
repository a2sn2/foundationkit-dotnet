# Authorization Capability

`FoundationKit.Authorization` provides permission definitions, role-to-permission grants, authorization subjects/evaluator contracts, and owner-or-privileged access semantics.

The original synchronous `IAuthorizationEvaluator` remains available for FoundationKit-owned role grants. The additive `IAsyncAuthorizationEvaluator` and `AbpPermissionAuthorizationEvaluator` allow consumers that already use ABP OSS to delegate permission grants to ABP `IPermissionChecker` while preserving FoundationKit's authenticated-subject and owner short-circuit semantics.

Application role names, persistence, tenant/organization scope, route policy names, and the choice to adopt ABP remain host-owned.

Core vNext generic CRUD continues to use its semantic authorization extension point and fails closed when a module declares authorization but supplies no policy. The ABP bridge does not silently turn provider permissions into CRUD business policy.

Current maturity remains `ReferenceOnly`.

See `docs/PLATFORM-LEVERAGE-AUDIT.md`.
