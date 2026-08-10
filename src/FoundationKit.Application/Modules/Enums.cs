namespace FoundationKit.Application.Modules;

[Flags]
public enum FoundationModuleCapability
{
    None = 0,
    Crud = 1 << 0,
    Auditing = 1 << 1,
    Authorization = 1 << 2,
    Concurrency = 1 << 3,
    Workflow = 1 << 4,
    Caching = 1 << 5,
    Security = 1 << 6,
    Identity = 1 << 7,
    Approvals = 1 << 8,
    Notifications = 1 << 9,
    Settings = 1 << 10,
    FeatureManagement = 1 << 11,
    Localization = 1 << 12
}

public enum FoundationApiIdempotencyMode
{
    Disabled = 0,
    Optional = 1,
    Required = 2
}

public enum FoundationApiConcurrencyMode
{
    ApplicationPolicy = 0,
    RequireIfMatch = 1
}
