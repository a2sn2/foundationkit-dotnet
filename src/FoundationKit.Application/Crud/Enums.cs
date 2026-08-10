namespace FoundationKit.Application.Crud;

public enum CrudOperation
{
    Create = 0,
    Read = 1,
    List = 2,
    Update = 3,
    Delete = 4
}

public enum CrudFilterOperator
{
    Equal = 0,
    NotEqual = 1,
    Contains = 2,
    StartsWith = 3,
    EndsWith = 4,
    GreaterThan = 5,
    GreaterThanOrEqual = 6,
    LessThan = 7,
    LessThanOrEqual = 8
}

public enum CrudSortDirection
{
    Ascending = 0,
    Descending = 1
}
