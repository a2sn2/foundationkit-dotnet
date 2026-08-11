namespace FoundationKit.Application.Persistence;

/// <summary>
/// Read-only query boundary for database projections such as SQL views.
/// Read models are not domain entities and never enter the writable repository contract.
/// </summary>
public interface IReadModelStore<TReadModel>
    where TReadModel : class
{
    Task<TReadModel?> FirstOrDefaultAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TReadModel>> ListAsync(
        ISpecification<TReadModel>? specification = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TReadModel>? specification = null,
        CancellationToken cancellationToken = default);
}
