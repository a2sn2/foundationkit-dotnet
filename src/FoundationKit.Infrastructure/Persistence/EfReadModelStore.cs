using FoundationKit.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Infrastructure.Persistence;

public sealed class EfReadModelStore<TReadModel, TDbContext>(TDbContext dbContext)
    : IReadModelStore<TReadModel>
    where TReadModel : class
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext = dbContext;

    public Task<TReadModel?> FirstOrDefaultAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        EnsureProjectionOnly(specification);
        return SpecificationEvaluator
            .Apply(_dbContext.Set<TReadModel>().AsNoTracking(), specification)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TReadModel>> ListAsync(
        ISpecification<TReadModel>? specification = null,
        CancellationToken cancellationToken = default)
    {
        EnsureProjectionOnly(specification);
        return await SpecificationEvaluator
            .Apply(_dbContext.Set<TReadModel>().AsNoTracking(), specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountAsync(
        ISpecification<TReadModel>? specification = null,
        CancellationToken cancellationToken = default)
    {
        EnsureProjectionOnly(specification);
        var query = _dbContext.Set<TReadModel>().AsNoTracking().AsQueryable();
        if (specification?.Criteria is not null)
            query = query.Where(specification.Criteria);
        return query.CountAsync(cancellationToken);
    }

    private static void EnsureProjectionOnly(ISpecification<TReadModel>? specification)
    {
        if (specification is not null && specification.Includes.Count != 0)
        {
            throw new InvalidOperationException(
                "Read-model specifications cannot declare Includes. Compose related data in the read source/view instead.");
        }
    }
}
