using FoundationKit.Application.Results;
using FoundationKit.WebApi.Errors;
using Microsoft.AspNetCore.Http;

namespace FoundationKit.WebApi.Results;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(
        this Result result,
        Func<IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke() ?? global::Microsoft.AspNetCore.Http.Results.NoContent()
            : result.Error.ToProblem();

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : result.Error.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        var statusCode = FoundationHttpErrorMapping.GetStatusCode(error.Type);

        return global::Microsoft.AspNetCore.Http.Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["errorType"] = error.Type.ToString()
            });
    }
}
