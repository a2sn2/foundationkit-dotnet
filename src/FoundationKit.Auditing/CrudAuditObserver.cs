using FoundationKit.Application.Crud;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Auditing;

public sealed class CrudAuditObserver<TEntity, TId>(IAuditRecorder recorder)
    : ICrudOperationObserver<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public async ValueTask OnSucceededAsync(
        CrudOperationEvent<TEntity, TId> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await recorder.RecordAsync(
            new AuditRequest(
                Action: $"crud.{operation.Operation.ToString().ToLowerInvariant()}",
                SubjectType: operation.ModuleName,
                SubjectId: operation.Id.ToString(),
                Attributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["projectId"] = operation.ProjectId.Value
                }),
            cancellationToken).ConfigureAwait(false);
    }
}
