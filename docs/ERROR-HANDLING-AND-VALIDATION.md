# Error handling and validation baseline

FoundationKit uses one consistent failure model across application services and ASP.NET Core hosts. Expected business outcomes should not become exceptions, while unexpected request-time failures must still produce safe, traceable Problem Details.

## Failure layers

1. **Structural input validation**
   - `DataAnnotationsValidator<T>` is the default validator registered by `AddFoundationEfCrudModule`.
   - Use attributes such as `Required`, `StringLength`, `Range`, `RegularExpression`, and other built-in DataAnnotations for simple field-level constraints.
   - Request contracts may carry the input-facing attributes. Entities may also carry structural metadata when it accurately describes the domain shape or persistence model.
   - Entity methods must still guard real invariants because entities can be created or mutated outside the HTTP request path.
   - Do not create a custom `IValidator<T>` only to repeat simple attributes. Replace the default validator only for cross-field rules, external lookups, contextual rules, or other validation that annotations cannot express cleanly.

2. **Expected application/business failures**
   - Return `Result` / `Result<T>` with a typed `ErrorType`.
   - Validation, not-found, conflict, authentication/authorization, business-rule, throttling, service-unavailable, and timeout categories map consistently to HTTP status codes.
   - CRUD manager, authorization, concurrency, and observer hooks stay explicit and composable.

3. **Request-time exceptions**
   - `FoundationExceptionHandler` is registered by `AddFoundationWebApi` and activated by `UseFoundationRequestPipeline`.
   - Known framework/platform exceptions are mapped through `IFoundationExceptionMapper`.
   - Consumers can register additional `IFoundationExceptionMapper` implementations for provider- or product-specific exception types without replacing FoundationKit's global handler.
   - Unknown exceptions return HTTP 500 with code `Foundation.Unhandled`.
   - Exception messages and exception types are not returned by default. `IncludeExceptionDetails` is opt-in and should only be enabled in controlled development environments.
   - The original exception is logged server-side with the request correlation identifier.

4. **Empty HTTP error responses**
   - The FoundationKit request pipeline also normalizes otherwise-empty 4xx/5xx statuses, including 401, 403, 404, 405, 408, 413, 415, 429, 502, 503, and 504, into the same Problem Details shape.

5. **Startup and configuration failures**
   - Failures that occur before the ASP.NET Core request pipeline is running cannot be converted into an HTTP Problem Details response.
   - Missing required configuration, failed startup migrations, invalid DI/module registration, and other boot-time invariants intentionally fail fast so an unhealthy process is not advertised as ready.
   - Hosts should capture those failures through their normal process/container/service logs and health/restart policy. FoundationKit does not swallow startup exceptions.

## Problem Details contract

Error responses use RFC-style Problem Details and include stable FoundationKit metadata:

- `code`: stable machine-readable error code.
- `errorType`: typed FoundationKit category when the failure comes from the application/error model.
- `correlationId`: request correlation identifier used to find the matching server log entry.
- `projectId`: current Foundation project identity when a project context is registered.
- `exceptionType`: emitted only when exception details are explicitly enabled.

The server log keeps the original exception. The client receives safe public detail plus the correlation identifier required for support and diagnostics.

## Error category to HTTP mapping

| Error type | HTTP |
|---|---:|
| Validation | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| BusinessRule | 422 |
| TooManyRequests | 429 |
| ServiceUnavailable | 503 |
| Timeout | 504 |
| Failure / unknown | 500 |

## Enum convention

Closed FoundationKit sets are represented as enums instead of string literals where that improves correctness. Core CRUD operations use `CrudOperation`, built-in module capabilities use the `[Flags]` enum `FoundationModuleCapability`, and application failure categories use `ErrorType`. Public error codes remain strings because they are extensible wire-contract identifiers rather than a closed set.

## Testing convention

Do not write tests that merely prove the .NET framework implements `Required`, `Range`, or `StringLength`. FoundationKit tests should prove that the annotation adapter is connected, that custom validation/business rules execute at the correct boundary, that exception-to-Problem-Details mappings are correct, and that unexpected exceptions do not leak sensitive details. Integration smoke tests verify the same contract through the real Workbench HTTP + EF Core + SQL Server path.
