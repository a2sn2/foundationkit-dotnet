# FoundationKit.Identity

`FoundationKit.Identity` is the provider-neutral account-lifecycle policy capability above `FoundationKit.Security`. It does **not** turn FoundationKit into an identity provider.

## Current v1 surface

- `AccountSecurityOptions` + validator for reusable account-policy structure;
- `IAccountNotificationSender` for confirmation/reset/security-notification delivery intent;
- `AccountSecurityNotification` event vocabulary;
- `IdentitySensitiveOperation`, `IdentityStepUpFactor`, and `IdentityStepUpPolicy` for sensitive-operation factor requirements.

The package does not own ASP.NET Core Identity stores, user tables/migrations, OAuth/OIDC server behavior, external IdPs, token generation, SMTP/SMS/push, session persistence, tenant membership, or product copy.

## Step-up policy

Current reusable requirements include password proof for password change/MFA setup and password + MFA proof for MFA disable/recovery-code regeneration. The package expresses **required factors**, while the consuming identity implementation decides how a password, TOTP, recovery code, passkey, or external assertion is actually verified.

## Current consumer evidence

Athar is the deepest consumer: it binds the reusable policy, uses the notification port, performs fresh factor verification, and keeps ASP.NET Core Identity, Arabic account copy, SMTP mapping, EF persistence, and endpoints in the product.

Madar provides an independent identity-adjacent product composition: it consumes FoundationKit Security/Authorization around its own ASP.NET Core Identity setup and keeps Madar roles, user store, login flow and product permissions inside the product.

The second product strengthens evidence without implying that FoundationKit now owns a generic IdP or identity schema.

## Maturity

Identity remains `ReferenceOnly`. The original “first extraction / one product” wording no longer describes the repository, but broader provider/account-lifecycle compatibility and a long-term support commitment are still insufficient for promotion. Maturity is enforced through Maturity Evidence v1 and is separate from Production Approval.
