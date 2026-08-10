using FoundationKit.Application.Crud;
using FoundationKit.Application.Modules;

namespace FoundationKit.WebApi.Api;

public sealed record FoundationApiOperationMetadata(
    string ModuleName,
    CrudOperation Operation,
    string Method,
    string Route,
    FoundationApiIdempotencyMode Idempotency,
    FoundationApiConcurrencyMode Concurrency,
    bool AuthorizationEnabled,
    string? AuthorizationPolicy,
    string? RateLimitPolicyName);
