using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Infrastructure.Persistence;

public sealed class ConcurrencyAwareEfUnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork
    where TDbContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new FoundationConcurrencyException(
                "EF Core detected an optimistic concurrency conflict.",
                exception);
        }
    }
}
