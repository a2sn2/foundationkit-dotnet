# Identity Capability

`FoundationKit.Identity` provides account-policy vocabulary, account notification ports, security-event vocabulary, step-up requirements, and provider-neutral current-user integration.

`AbpCurrentUserAdapter` is an optional ABP OSS bridge from `Volo.Abp.Users.ICurrentUser` to FoundationKit's minimal `ICurrentUser` application contract. It allows ABP consumers to reuse the provider's ambient user context without making ABP the FoundationKit application model.

FoundationKit deliberately does not provide a user store, EF identity schema, OAuth/OIDC server, SMTP transport, or application-specific account copy. Those choices belong to a consuming identity composition/provider, which may be ASP.NET Core Identity, ABP, or another approved implementation.

Current maturity remains `ReferenceOnly`.

See `docs/PLATFORM-LEVERAGE-AUDIT.md`.
