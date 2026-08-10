namespace FoundationKit.Application.Results;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    BusinessRule = 6,
    Failure = 7,
    TooManyRequests = 8,
    ServiceUnavailable = 9,
    Timeout = 10,
    PreconditionRequired = 11,
    PreconditionFailed = 12
}
