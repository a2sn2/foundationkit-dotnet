# FoundationKit.Notifications.Smtp

`FoundationKit.Notifications.Smtp` is the reusable SMTP transport adapter for `FoundationKit.Notifications`. It maps the provider-neutral `NotificationMessage` contract to `System.Net.Mail` without owning account semantics, product copy, persistence, queues, or secrets management.

## Implemented v1 surface

- `SmtpNotificationOptions`;
- `SmtpNotificationOptionsValidator`;
- `SmtpNotificationSender : INotificationSender`;
- `ISmtpNotificationObserver` for bounded operational diagnostics.

The sender snapshots validated options at construction. Missing host/from configuration returns `NotConfigured`; supported SMTP/format/operation failures return `Failed`; caller cancellation stays cancellation.

## Dependency and ownership boundary

The adapter depends on `FoundationKit.Notifications` plus .NET SMTP APIs. It does not own EF Core, ASP.NET Core Identity, product configuration sections, product copy, a secret store, logging framework, or account lifecycle.

Transport settings are limited to host, port, TLS flag, optional username/password, and from address. Structural validation is reusable; approved relay, authentication requirements, certificate trust, secret rotation, TLS policy, monitoring, and operational ownership remain deployment/product decisions.

`ISmtpNotificationObserver` receives only notification purpose and bounded failure type information. It never receives destination, body, tokens, credentials, or the provider exception object.

## Current consumer evidence

Athar is the primary SMTP consumer for account-security notifications. It retains its own configuration, Arabic product copy, tokens, Identity semantics, fail-closed Production TLS policy, and logging.

Madar independently consumes the same optional SMTP adapter for bounded operational notifications when configured, while retaining Madar notification purpose/copy and delivery-audit semantics.

This is real second-product evidence, but both current consumers still use the same SMTP transport family. Provider diversity, durable queues/retries, bounce handling, routing/fallback, and Production delivery operations are not proven.

## Explicit non-goals

- queues/retries/background scheduling;
- templates/localization infrastructure;
- multi-provider routing/fallback;
- credentials/secret persistence or rotation;
- bounce/complaint processing;
- delivery history;
- bulk campaigns;
- SMS/push/webhook/WhatsApp channels.

## Maturity

`provider-smtp` remains `ReferenceOnly`. The implementation and multiple consumers are real evidence, but the broader provider/operations compatibility and support commitment required for promotion are not yet established. This maturity does not assert Production relay/security compliance.
