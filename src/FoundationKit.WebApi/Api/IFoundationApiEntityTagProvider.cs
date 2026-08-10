namespace FoundationKit.WebApi.Api;

public interface IFoundationApiEntityTagProvider<in TRead>
{
    string? GetEntityTag(TRead response);
}
