using FoundationKit.Application.Events;
using FoundationKit.Application.Persistence;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<DomainEventsSaveChangesInterceptor>();
        return services;
    }

    public static IServiceCollection AddFoundationEfReadModel<TReadModel, TDbContext>(
        this IServiceCollection services)
        where TReadModel : class
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IReadModelStore<TReadModel>, EfReadModelStore<TReadModel, TDbContext>>();
        return services;
    }
}
