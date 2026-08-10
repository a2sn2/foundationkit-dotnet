using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Isolation;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Validation;
using FoundationKit.Domain.Primitives;
using FoundationKit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoundationKit.Infrastructure.Platform;

public static class FoundationPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddFoundationProject(
        this IServiceCollection services,
        string projectId)
    {
        ArgumentNullException.ThrowIfNull(services);
        var context = new FoundationProjectContext(new FoundationProjectId(projectId));
        services.AddSingleton<IFoundationProjectContext>(context);
        services.AddSingleton<FoundationResourceNamespace>();
        services.TryAddSingleton<IFoundationModuleRegistry, FoundationModuleRegistry>();
        return services;
    }

    public static IServiceCollection AddFoundationEfCrudModule<
        TEntity,
        TId,
        TCreate,
        TUpdate,
        TRead,
        TMapper,
        TDbContext>(
        this IServiceCollection services,
        Action<FoundationModuleBuilder<TEntity, TId>> configure)
        where TEntity : Entity<TId>
        where TId : notnull
        where TMapper : class, ICrudMapper<TEntity, TId, TCreate, TUpdate, TRead>
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FoundationModuleBuilder<TEntity, TId>();
        configure(builder);
        var definition = builder.Build();

        services.AddSingleton(definition);
        services.AddSingleton<IFoundationModuleDefinition>(definition);
        services.AddScoped<IRepository<TEntity, TId>, EfRepository<TEntity, TId, TDbContext>>();
        services.TryAddScoped<IUnitOfWork, ConcurrencyAwareEfUnitOfWork<TDbContext>>();
        services.AddScoped<ICrudMapper<TEntity, TId, TCreate, TUpdate, TRead>, TMapper>();
        services.TryAddScoped<IValidator<TCreate>, DataAnnotationsValidator<TCreate>>();
        services.TryAddScoped<IValidator<TUpdate>, DataAnnotationsValidator<TUpdate>>();

        if (definition.HasCapability(FoundationModuleCapability.Authorization))
        {
            services.TryAddScoped<ICrudAuthorizationPolicy<TEntity, TId>, DenyAllCrudAuthorizationPolicy<TEntity, TId>>();
        }
        else
        {
            services.TryAddScoped<ICrudAuthorizationPolicy<TEntity, TId>, AllowAllCrudAuthorizationPolicy<TEntity, TId>>();
        }

        services.TryAddScoped<ICrudConcurrencyPolicy<TEntity, TUpdate>, NoOpCrudConcurrencyPolicy<TEntity, TUpdate>>();

        if (definition.ManagerType is null)
        {
            services.TryAddScoped<ICrudManager<TEntity, TId, TCreate, TUpdate>, DefaultCrudManager<TEntity, TId, TCreate, TUpdate>>();
        }
        else
        {
            var managerContract = typeof(ICrudManager<TEntity, TId, TCreate, TUpdate>);
            if (!managerContract.IsAssignableFrom(definition.ManagerType))
            {
                throw new InvalidOperationException(
                    $"Manager '{definition.ManagerType.FullName}' must implement '{managerContract.FullName}'.");
            }

            services.AddScoped(managerContract, definition.ManagerType);
        }

        services.AddScoped<CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead>>();
        return services;
    }
}
