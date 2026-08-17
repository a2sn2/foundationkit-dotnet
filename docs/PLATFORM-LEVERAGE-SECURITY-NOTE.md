# Platform Leverage Security Note

The post-baseline native OpenAPI adoption initially exposed an unsafe transitive `Microsoft.OpenApi` resolution during vulnerability-audited restore. The repository did not accept that dependency graph.

The leverage branch now pins `Microsoft.OpenApi` to `2.7.5` and upgrades `Swashbuckle.AspNetCore` to `10.2.3`, keeping the existing canonical Swagger transport pipeline compatible with OpenAPI.NET 2.x while ASP.NET Core first-party OpenAPI is proven in parallel.

Composer generation was migrated to the same Swashbuckle/OpenAPI.NET generation surface, including OpenAPI.NET 2.x security-scheme references, so generated products cannot downgrade the patched dependency floor through their own central package file.

This is a CI/security gate outcome, not a Production certification claim. Normal dependency auditing remains mandatory, and `docs/security/PRODUCTION-GOVERNANCE-CHECKLIST.md` remains the production-governance authority.
