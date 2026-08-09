# FoundationKit.Authorization

`FoundationKit.Authorization` is the provider-neutral permission/ownership capability above `FoundationKit.Identity`. It separates identity facts from reusable authorization mechanics while leaving product roles, permission IDs, business meaning, persistence and organization/tenant scope with the consuming product.

## Current v1 surface

- `IAuthorizationSubject` — authenticated state, current user ID and role-membership facts;
- `PermissionId` / `PermissionDefinition` — bounded product-owned permission identifiers;
- `RolePermissionGrant` / `RolePermissionMap` — immutable in-memory role-to-permission grants;
- `IAuthorizationEvaluator` / `RolePermissionAuthorizationEvaluator` — permission and owner-or-privileged checks.

Unknown permissions fail closed. Owner access requires an authenticated matching user ID or an explicitly supplied privileged permission; FoundationKit does not create a universal Administrator bypass.

The v1 package does not own role/permission database tables, EF migrations, user-role persistence, ASP.NET Core policy registration, organization/tenant scope, ABAC languages, external PDP engines, or administration UI.

## Current consumer evidence

Athar owns `athar.*` permission IDs and maps its product roles to them. Application logic asks semantic permission/ownership questions while ASP.NET Core policies remain a coarse outer defense.

Madar independently owns `madar.*` permissions and uses the reusable evaluator for case/application authorization while keeping Requester/Operator/Supervisor/Administrator semantics, department membership, case visibility and persistence inside Madar.

This is real cross-product adoption of the permission/ownership boundary; it is not evidence for a generic Organization/Multi-Tenancy model.

## Maturity

Authorization remains `ReferenceOnly`. The repository now has more than one consumer, but organization/tenant/scoped-policy compatibility and the broader long-term support commitment required for promotion are still intentionally unproven. Maturity Evidence v1 governs promotion independently of Production Approval.
