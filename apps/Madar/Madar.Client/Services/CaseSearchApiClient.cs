using System.Globalization;
using FoundationKit.Blazor.Api;
using Madar.Contracts.Cases;

namespace Madar.Client.Services;

public sealed class CaseSearchApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<CaseSearchResponseDto>> SearchAsync(
        CaseSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = new List<string>
        {
            Pair("offset", request.Offset.ToString(CultureInfo.InvariantCulture)),
            Pair("limit", request.Limit.ToString(CultureInfo.InvariantCulture))
        };

        Add(parameters, "query", request.Query);
        Add(parameters, "caseType", request.CaseType);
        Add(parameters, "priority", request.Priority);
        Add(parameters, "status", request.Status);
        Add(parameters, "slaState", request.SlaState);
        Add(parameters, "departmentId", request.DepartmentId?.ToString("D"));
        Add(parameters, "assignedToUserId", request.AssignedToUserId?.ToString("D"));
        Add(parameters, "createdFromUtc", request.CreatedFromUtc?.ToString("O", CultureInfo.InvariantCulture));
        Add(parameters, "createdToUtc", request.CreatedToUtc?.ToString("O", CultureInfo.InvariantCulture));

        var route = $"{CaseSearchRoutes.Search}?{string.Join('&', parameters)}";
        return SendAsync<CaseSearchResponseDto>(
            new HttpRequestMessage(HttpMethod.Get, route),
            cancellationToken);
    }

    private static void Add(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add(Pair(name, value));
    }

    private static string Pair(string name, string value) =>
        $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
}
