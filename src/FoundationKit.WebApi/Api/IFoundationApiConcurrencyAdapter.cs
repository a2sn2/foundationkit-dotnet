using FoundationKit.Application.Results;

namespace FoundationKit.WebApi.Api;

public interface IFoundationApiConcurrencyAdapter<TUpdate, in TRead>
{
    Result<TUpdate> ApplyIfMatch(TUpdate request, string ifMatch);

    string? GetEntityTag(TRead response);
}
