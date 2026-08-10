using FoundationKit.Application.Crud;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Results;
using Microsoft.AspNetCore.Http;

namespace FoundationKit.WebApi.Api;

internal static class FoundationApiQueryParser
{
    private const int MaximumFieldLength = 64;
    private const int MaximumFilterValueLength = 512;
    private const int MaximumIdempotencyKeyLength = 128;
    private const int MaximumConcurrencyTokenLength = 256;

    public static bool TryParseCrudList(
        HttpContext context,
        CrudModuleOptions crud,
        FoundationApiModuleOptions api,
        out CrudListRequest request,
        out Error error)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(crud);
        ArgumentNullException.ThrowIfNull(api);

        request = default!;
        error = Error.None;

        if (!TryReadPositiveInt(context.Request.Query["page"], 1, out var page) ||
            !TryReadPositiveInt(context.Request.Query["pageSize"], PageRequest.DefaultPageSize, out var requestedPageSize))
        {
            error = Error.Validation(
                "Foundation.Api.Pagination.Invalid",
                "Page and pageSize must be positive integers.");
            return false;
        }

        var filters = context.Request.Query["filter"];
        if (filters.Count > api.MaximumFilters)
        {
            error = Error.Validation(
                "Foundation.Api.Filter.TooMany",
                $"At most {api.MaximumFilters} filter expressions are allowed.");
            return false;
        }

        var parsedFilters = new List<CrudFilter>(filters.Count);
        foreach (var raw in filters)
        {
            if (!TryParseFilter(raw, out var filter))
            {
                error = Error.Validation(
                    "Foundation.Api.Filter.Invalid",
                    "Each filter must use 'field|operator|value' with a supported operator.");
                return false;
            }
            parsedFilters.Add(filter);
        }

        var sorts = context.Request.Query["sort"];
        if (sorts.Count > api.MaximumSorts)
        {
            error = Error.Validation(
                "Foundation.Api.Sort.TooMany",
                $"At most {api.MaximumSorts} sort expressions are allowed.");
            return false;
        }

        var parsedSorts = new List<CrudSort>(sorts.Count);
        var seenSortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in sorts)
        {
            if (!TryParseSort(raw, out var sort) || !seenSortFields.Add(sort.Field))
            {
                error = Error.Validation(
                    "Foundation.Api.Sort.Invalid",
                    "Each sort must use 'field|asc' or 'field|desc', and a field may only appear once.");
                return false;
            }
            parsedSorts.Add(sort);
        }

        var boundedPageSize = Math.Min(requestedPageSize, crud.MaximumPageSize);
        request = new CrudListRequest(
            new PageRequest(page, boundedPageSize),
            parsedFilters.AsReadOnly(),
            parsedSorts.AsReadOnly());
        return true;
    }

    public static bool TryValidateIdempotencyKey(
        HttpContext context,
        FoundationApiIdempotencyMode mode,
        out Error error)
    {
        ArgumentNullException.ThrowIfNull(context);
        error = Error.None;

        if (mode == FoundationApiIdempotencyMode.Disabled)
            return true;

        var values = context.Request.Headers["Idempotency-Key"];
        if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
        {
            if (mode == FoundationApiIdempotencyMode.Optional)
                return true;

            error = Error.Validation(
                "Foundation.Api.IdempotencyKey.Required",
                "The Idempotency-Key header is required for this operation.");
            return false;
        }

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            error = Error.Validation(
                "Foundation.Api.IdempotencyKey.Invalid",
                "Exactly one non-empty Idempotency-Key header value is allowed.");
            return false;
        }

        var key = values[0]!.Trim();
        if (key.Length > MaximumIdempotencyKeyLength || key.Any(char.IsControl))
        {
            error = Error.Validation(
                "Foundation.Api.IdempotencyKey.Invalid",
                $"Idempotency-Key must be at most {MaximumIdempotencyKeyLength} characters and contain no control characters.");
            return false;
        }

        return true;
    }

    public static bool TryReadIfMatch(
        HttpContext context,
        out CrudConcurrencyPrecondition precondition,
        out Error error)
    {
        ArgumentNullException.ThrowIfNull(context);
        precondition = default!;
        error = Error.None;

        var values = context.Request.Headers["If-Match"];
        if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
        {
            error = Error.PreconditionRequired(
                "Foundation.Api.IfMatch.Required",
                "The If-Match header is required for this operation.");
            return false;
        }

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            error = Error.Validation(
                "Foundation.Api.IfMatch.Invalid",
                "Exactly one non-empty If-Match header value is allowed.");
            return false;
        }

        var token = values[0]!.Trim();
        if (token.Length > MaximumConcurrencyTokenLength || token.Any(char.IsControl) || token.Contains(','))
        {
            error = Error.Validation(
                "Foundation.Api.IfMatch.Invalid",
                $"If-Match must contain one token of at most {MaximumConcurrencyTokenLength} characters and no control characters.");
            return false;
        }

        precondition = new CrudConcurrencyPrecondition(token);
        return true;
    }

    private static bool TryParseFilter(string? raw, out CrudFilter filter)
    {
        filter = default!;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var parts = raw.Split('|', 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !IsValidField(parts[0]) || !IsValidValue(parts[2]))
            return false;

        if (!TryParseOperator(parts[1], out var @operator))
            return false;

        filter = new CrudFilter(parts[0], @operator, parts[2]);
        return true;
    }

    private static bool TryParseSort(string? raw, out CrudSort sort)
    {
        sort = default!;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var parts = raw.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !IsValidField(parts[0]))
            return false;

        var direction = parts[1].ToLowerInvariant() switch
        {
            "asc" => CrudSortDirection.Ascending,
            "desc" => CrudSortDirection.Descending,
            _ => (CrudSortDirection?)null
        };

        if (direction is null)
            return false;

        sort = new CrudSort(parts[0], direction.Value);
        return true;
    }

    private static bool TryParseOperator(string value, out CrudFilterOperator @operator)
    {
        var parsed = value.Trim().ToLowerInvariant() switch
        {
            "eq" => CrudFilterOperator.Equal,
            "ne" => CrudFilterOperator.NotEqual,
            "contains" => CrudFilterOperator.Contains,
            "startswith" => CrudFilterOperator.StartsWith,
            "endswith" => CrudFilterOperator.EndsWith,
            "gt" => CrudFilterOperator.GreaterThan,
            "gte" => CrudFilterOperator.GreaterThanOrEqual,
            "lt" => CrudFilterOperator.LessThan,
            "lte" => CrudFilterOperator.LessThanOrEqual,
            _ => (CrudFilterOperator?)null
        };

        if (parsed is null)
        {
            @operator = default;
            return false;
        }

        @operator = parsed.Value;
        return true;
    }

    private static bool IsValidField(string value) =>
        value.Length is > 0 and <= MaximumFieldLength &&
        char.IsAsciiLetter(value[0]) &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static bool IsValidValue(string value) =>
        value.Length <= MaximumFilterValueLength &&
        !value.Any(char.IsControl);

    private static bool TryReadPositiveInt(string? raw, int fallback, out int value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = fallback;
            return true;
        }

        return int.TryParse(
                   raw,
                   System.Globalization.NumberStyles.None,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out value) &&
               value > 0;
    }
}
