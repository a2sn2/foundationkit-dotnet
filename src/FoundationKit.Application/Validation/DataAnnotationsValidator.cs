using System.ComponentModel.DataAnnotations;

namespace FoundationKit.Application.Validation;

public sealed class DataAnnotationsValidator<T> : IValidator<T>
{
    public ValueTask<IReadOnlyList<ValidationFailure>> ValidateAsync(
        T instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);
        if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
            return ValueTask.FromResult<IReadOnlyList<ValidationFailure>>(Array.Empty<ValidationFailure>());

        var failures = results
            .SelectMany(result =>
            {
                var members = result.MemberNames.DefaultIfEmpty(string.Empty);
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "The value is invalid."
                    : result.ErrorMessage;

                return members.Select(member => new ValidationFailure(
                    member,
                    "Foundation.Validation.DataAnnotation",
                    message));
            })
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<ValidationFailure>>(failures);
    }
}
