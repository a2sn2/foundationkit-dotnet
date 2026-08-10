using System.Linq.Expressions;
using System.Reflection;
using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Isolation;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Application.Validation;
using FoundationKit.Domain.Primitives;
using FoundationKit.Infrastructure.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Tests;

public sealed class FoundationPlatformTests
{
    [Fact]
    public void Project_identity_is_canonical_and_namespaces_are_isolated()
    {
        var first = BuildProvider("Project-One");
        var second = BuildProvider("project-two");

        var firstContext = first.GetRequiredService<IFoundationProjectContext>();
        var firstNamespace = first.GetRequiredService<FoundationResourceNamespace>();
        var secondNamespace = second.GetRequiredService<FoundationResourceNamespace>();

        Assert.Equal("project-one", firstContext.ProjectId.Value);
        Assert.Equal(
            "foundation:project-one:cache:customers:42",
            firstNamespace.Create("Cache", "customers:42"));
        Assert.NotEqual(
            firstNamespace.Create("cache", "customers:42"),
            secondNamespace.Create("cache", "customers:42"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("-bad")]
    [InlineData("bad-")]
    [InlineData("bad id")]
    [InlineData("bad/id")]
    public void Project_identity_rejects_unsafe_values(string value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FoundationProjectId(value));
    }

    [Fact]
    public void Module_builder_composes_capabilities_without_global_registry_state()
    {
        var first = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Customers", "customers")
            .Crud(options => options.DeleteEnabled = false)
            .Auditing()
            .Authorization("customers")
            .Concurrency()
            .Build();
        var second = new FoundationModuleBuilder<OtherEntity, Guid>()
            .Named("Employees", "employees")
            .Crud()
            .Build();

        var registry = new FoundationModuleRegistry([first, second]);

        Assert.Equal(2, registry.Modules.Count);
        Assert.True(first.HasCapability(FoundationModuleCapabilities.Crud));
        Assert.True(first.HasCapability(FoundationModuleCapabilities.Auditing));
        Assert.False(first.Crud!.DeleteEnabled);
        Assert.Equal("employees", registry.Find("Employees")!.Route);
    }

    [Fact]
    public async Task Generic_crud_service_executes_complete_lifecycle_and_custom_manager_hooks()
    {
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var manager = new RecordingManager();
        var observer = new RecordingObserver();
        var module = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Customers", "customers")
            .Crud()
            .Build();
        var service = CreateService(repository, unitOfWork, manager, observer, module);

        var created = await service.CreateAsync(new CreateRequest("Alpha"));
        Assert.True(created.IsSuccess);
        Assert.Equal("Alpha", created.Value.Item.Name);

        var read = await service.GetAsync(created.Value.Id);
        Assert.True(read.IsSuccess);
        Assert.Equal("Alpha", read.Value.Name);

        var listed = await service.ListAsync(new PageRequest(1, 20));
        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value.Items);

        var updated = await service.UpdateAsync(
            created.Value.Id,
            new UpdateRequest("Beta", expectedVersion: 1));
        Assert.True(updated.IsSuccess);
        Assert.Equal("Beta", updated.Value.Name);

        var deleted = await service.DeleteAsync(created.Value.Id);
        Assert.True(deleted.IsSuccess);
        Assert.Empty(repository.Items);
        Assert.Equal(3, unitOfWork.SaveCount);
        Assert.Equal([CrudOperation.Create, CrudOperation.Update, CrudOperation.Delete], manager.Operations);
        Assert.Equal([CrudOperation.Create, CrudOperation.Update, CrudOperation.Delete], observer.Operations);
    }

    [Fact]
    public async Task Authorization_policy_fails_closed_before_create_mutation()
    {
        var repository = new FakeRepository();
        var module = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Customers", "customers")
            .Crud()
            .Authorization("customers")
            .Build();
        var service = new CrudApplicationService<TestEntity, Guid, CreateRequest, UpdateRequest, ReadModel>(
            repository,
            new FakeUnitOfWork(),
            new TestMapper(),
            new NoOpCrudValidator<CreateRequest>(),
            new NoOpCrudValidator<UpdateRequest>(),
            new DenyAllCrudAuthorizationPolicy<TestEntity, Guid>(),
            new VersionConcurrencyPolicy(),
            new DefaultCrudManager<TestEntity, Guid, CreateRequest, UpdateRequest>(),
            [],
            module,
            new FoundationProjectContext(new FoundationProjectId("test-project")));

        var result = await service.CreateAsync(new CreateRequest("Blocked"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Concurrency_policy_returns_conflict_before_update()
    {
        var repository = new FakeRepository();
        var entity = new TestEntity(Guid.NewGuid(), "Alpha");
        await repository.AddAsync(entity);
        var module = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Customers", "customers")
            .Crud()
            .Concurrency()
            .Build();
        var service = CreateService(repository, new FakeUnitOfWork(), new RecordingManager(), new RecordingObserver(), module);

        var result = await service.UpdateAsync(entity.Id, new UpdateRequest("Beta", expectedVersion: 99));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Alpha", entity.Name);
    }

    [Fact]
    public void Reusable_public_surface_has_no_mutable_public_static_fields()
    {
        var assemblies = new[]
        {
            typeof(Entity<>).Assembly,
            typeof(CrudApplicationService<,,,,>).Assembly,
            typeof(FoundationPlatformServiceCollectionExtensions).Assembly
        };

        var mutable = assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => !field.IsLiteral && !field.IsInitOnly)
            .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToArray();

        Assert.Empty(mutable);
    }

    private static ServiceProvider BuildProvider(string projectId)
    {
        var services = new ServiceCollection();
        services.AddFoundationProject(projectId);
        return services.BuildServiceProvider();
    }

    private static CrudApplicationService<TestEntity, Guid, CreateRequest, UpdateRequest, ReadModel> CreateService(
        FakeRepository repository,
        FakeUnitOfWork unitOfWork,
        RecordingManager manager,
        RecordingObserver observer,
        FoundationModuleDefinition<TestEntity, Guid> module) =>
        new(
            repository,
            unitOfWork,
            new TestMapper(),
            new NoOpCrudValidator<CreateRequest>(),
            new NoOpCrudValidator<UpdateRequest>(),
            new AllowAllCrudAuthorizationPolicy<TestEntity, Guid>(),
            new VersionConcurrencyPolicy(),
            manager,
            [observer],
            module,
            new FoundationProjectContext(new FoundationProjectId("test-project")));

    private sealed class TestEntity(Guid id, string name) : Entity<Guid>(id)
    {
        public string Name { get; set; } = name;

        public int Version { get; set; } = 1;
    }

    private sealed class OtherEntity(Guid id) : Entity<Guid>(id);

    private sealed record CreateRequest(string Name);

    private sealed record UpdateRequest(string Name, int ExpectedVersion);

    private sealed record ReadModel(Guid Id, string Name, int Version);

    private sealed class TestMapper : ICrudMapper<TestEntity, Guid, CreateRequest, UpdateRequest, ReadModel>
    {
        public TestEntity Create(CreateRequest request) => new(Guid.NewGuid(), request.Name);

        public void ApplyUpdate(TestEntity entity, UpdateRequest request)
        {
            entity.Name = request.Name;
            entity.Version++;
        }

        public ReadModel ToReadModel(TestEntity entity) => new(entity.Id, entity.Name, entity.Version);
    }

    private sealed class VersionConcurrencyPolicy : ICrudConcurrencyPolicy<TestEntity, UpdateRequest>
    {
        public Result Validate(TestEntity entity, UpdateRequest request) =>
            entity.Version == request.ExpectedVersion
                ? Result.Success()
                : Result.Failure(Error.Conflict(
                    "Test.VersionConflict",
                    "The test entity version does not match."));
    }

    private sealed class RecordingManager : ICrudManager<TestEntity, Guid, CreateRequest, UpdateRequest>
    {
        public List<CrudOperation> Operations { get; } = [];

        public ValueTask<Result> BeforeCreateAsync(TestEntity entity, CreateRequest request, CancellationToken cancellationToken = default)
        {
            Operations.Add(CrudOperation.Create);
            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<Result> BeforeUpdateAsync(TestEntity entity, UpdateRequest request, CancellationToken cancellationToken = default)
        {
            Operations.Add(CrudOperation.Update);
            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<Result> BeforeDeleteAsync(TestEntity entity, CancellationToken cancellationToken = default)
        {
            Operations.Add(CrudOperation.Delete);
            return ValueTask.FromResult(Result.Success());
        }
    }

    private sealed class RecordingObserver : ICrudOperationObserver<TestEntity, Guid>
    {
        public List<CrudOperation> Operations { get; } = [];

        public ValueTask OnSucceededAsync(CrudOperationEvent<TestEntity, Guid> operation, CancellationToken cancellationToken = default)
        {
            Operations.Add(operation.Operation);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeRepository : IRepository<TestEntity, Guid>
    {
        public List<TestEntity> Items { get; } = [];

        public Task<TestEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

        public Task<TestEntity?> FirstOrDefaultAsync(ISpecification<TestEntity> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Apply(specification).FirstOrDefault());

        public Task<IReadOnlyList<TestEntity>> ListAsync(ISpecification<TestEntity>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestEntity>>(specification is null ? Items.ToArray() : Apply(specification).ToArray());

        public Task<int> CountAsync(ISpecification<TestEntity>? specification = null, CancellationToken cancellationToken = default)
        {
            var count = specification?.Criteria is null
                ? Items.Count
                : Items.Count(specification.Criteria.Compile());
            return Task.FromResult(count);
        }

        public Task AddAsync(TestEntity entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default)
        {
            Items.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Remove(TestEntity entity) => Items.Remove(entity);

        public void RemoveRange(IEnumerable<TestEntity> entities)
        {
            foreach (var entity in entities.ToArray())
                Items.Remove(entity);
        }

        private IEnumerable<TestEntity> Apply(ISpecification<TestEntity> specification)
        {
            IEnumerable<TestEntity> query = Items;
            if (specification.Criteria is not null)
                query = query.Where(specification.Criteria.Compile());
            if (specification.OrderBy is not null)
                query = query.OrderBy(specification.OrderBy.Compile());
            if (specification.OrderByDescending is not null)
                query = query.OrderByDescending(specification.OrderByDescending.Compile());
            if (specification.Skip.HasValue)
                query = query.Skip(specification.Skip.Value);
            if (specification.Take.HasValue)
                query = query.Take(specification.Take.Value);
            return query;
        }
    }
}
