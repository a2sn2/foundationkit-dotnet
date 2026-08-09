# FoundationKit.Security

`FoundationKit.Security` is the opt-in HTTP/security-convention capability above `FoundationKit.WebApi`. It centralizes reusable security mechanics without owning authentication, users, product roles, or infrastructure providers.

## Current v1 surface

### Trusted reverse proxy forwarding

`TrustedProxyOptions` / `TrustedProxySecurity` provide a fail-closed boundary for forwarded client address/scheme information. When enabled, explicit trusted proxy IPs are required, trust-all defaults are cleared, forward count is bounded, and only the configured forwarding headers are processed.

Forwarding must execute before security decisions that depend on `RemoteIpAddress` or `Request.Scheme`.

### Rate-limit partition conventions

`FoundationRateLimitPartitions` provides deterministic partition keys for authentication and authenticated writes. It does not choose permit counts, windows, queue behavior, persistence, Redis, or a distributed rate-limit provider.

### Authentication assurance

`FoundationAuthenticationAssurance` defines the shared MFA assurance convention (`amr=mfa`) and policy helper. It does not authenticate a user, enroll a factor, issue recovery codes, persist sessions, or perform product step-up flows.

## Current consumers

Athar consumes the boundary for trusted-proxy handling, rate-limit partitioning and administrator MFA assurance. Madar independently consumes FoundationKit security conventions in its product authentication/write-rate-limit composition while keeping product Identity/permissions/configuration inside Madar.

This second product consumer and the automated repository/security gates strengthen evidence beyond the original extraction.

## Maturity

Security remains `Preview`. The reason is no longer “waiting for Identity” or “only one consumer”—Identity exists and Athar/Madar provide real use. `Stable` would require a stronger long-term compatibility/support commitment across additional real ingress/deployment topologies and the full Maturity Evidence v1 criteria.

`Preview` is a reusable technical maturity signal, not Production Approval or a statement that a deployment's reverse proxy, edge rate limits, IdP, certificates, SIEM, or network topology are configured correctly.
