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
    public void Project_namespaces_are_isolated()
    {
        var one = new ServiceCollection().AddFoundationProject("Project-One").BuildServiceProvider();
        var two = new ServiceCollection().AddFoundationProject("project-two").BuildServiceProvider();
        var first = one.GetRequiredService<FoundationResourceNamespace>().Create("cache", "customers:42");
        var second = two.GetRequiredService<FoundationResourceNamespace>().Create("cache", "customers:42");
        Assert.Equal("foundation:project-one:cache:customers:42", first);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-bad")]
    [InlineData("bad-")]
    [InlineData("bad id")]
    [InlineData("bad/id")]
    public void Project_identity_rejects_unsafe_values(string value) =>
        Assert.ThrowsAny<ArgumentException>(() => new FoundationProjectId(value));

    [Fact]
    public void Module_definition_is_bounded_and_composable()
    {
        var module = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Customers", "customers")
            .Crud(options => options.DeleteEnabled = false)
            .Api(api =>
            {
                api.RoutePrefix = "platform/v1";
                api.Idempotency = FoundationApiIdempotencyMode.Required;
                api.Concurrency = FoundationApiConcurrencyMode.RequireIfMatch;
                api.MaximumFilters = 2;
                api.MaximumSorts = 1;
            })
            .Auditing()
            .Authorization("customers")
            .Concurrency()
            .Build();
        var registry = new FoundationModuleRegistry([module]);

        Assert.Equal("customers", registry.Find("customers")!.Route);
        Assert.Equal("platform/v1", module.Api.RoutePrefix);
        Assert.Equal(FoundationApiIdempotencyMode.Required, module.Api.Idempotency);
        Assert.False(module.Crud!.DeleteEnabled);
        Assert.True(module.HasCapability(FoundationModuleCapability.Auditing));
    }

    [Fact]
    public void Api_module_options_reject_unbounded_query_configuration()
    {
        var builder = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Customers", "customers")
            .Crud();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Api(api => api.MaximumFilters = 26));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Api(api => api.MaximumSorts = 11));
    }

    [Fact]
    public async Task Crud_lifecycle_uses_manager_concurrency_and_observer()
    {
        var repository = new FakeRepository();
        var unit = new FakeUnitOfWork();
        var manager = new RecordingManager();
        var observer = new RecordingObserver();
        var module = new FoundationModuleBuilder<TestEntity, Guid>().Named("Customers", "customers").Crud().Build();
        var service = BuildService(repository, unit, manager, observer, module);

        var created = await service.CreateAsync(new CreateRequest("Alpha"));
        Assert.True(created.IsSuccess);
        var id = created.Value.Id;
        Assert.True((await service.GetAsync(id)).IsSuccess);
        Assert.Single((await service.ListAsync(new PageRequest())).Value.Items);
        Assert.True((await service.UpdateAsync(id, new UpdateRequest("Beta", 1))).IsSuccess);
        Assert.True((await service.UpdateAsync(id, new UpdateRequest("Stale", 1))).IsFailure);
        Assert.True((await service.DeleteAsync(id)).IsSuccess);
        Assert.Empty(repository.Items);
        Assert.Equal(3, unit.SaveCount);
        Assert.Equal([CrudOperation.Create, CrudOperation.Update, CrudOperation.Delete], observer.Operations);
    }

    [Fact]
    public async Task Authorization_can_fail_closed_before_create()
    {
        var repository = new FakeRepository();
        var module = new FoundationModuleBuilder<TestEntity, Guid>().Named("Customers", "customers").Crud().Authorization().Build();
        var service = new CrudApplicationService<TestEntity, Guid, CreateRequest, UpdateRequest, ReadModel>(
            repository, new FakeUnitOfWork(), new Mapper(), new NoOpCrudValidator<CreateRequest>(),
            new NoOpCrudValidator<UpdateRequest>(), new DenyAllCrudAuthorizationPolicy<TestEntity, Guid>(),
            new VersionPolicy(), new DefaultCrudManager<TestEntity, Guid, CreateRequest, UpdateRequest>(), [], module,
            new FoundationProjectContext(new FoundationProjectId("test-project")));
        var result = await service.CreateAsync(new CreateRequest("Blocked"));
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Empty(repository.Items);
    }

    private static CrudApplicationService<TestEntity, Guid, CreateRequest, UpdateRequest, ReadModel> BuildService(
        FakeRepository repository, FakeUnitOfWork unit, RecordingManager manager, RecordingObserver observer,
        FoundationModuleDefinition<TestEntity, Guid> module) => new(
            repository, unit, new Mapper(), new NoOpCrudValidator<CreateRequest>(), new NoOpCrudValidator<UpdateRequest>(),
            new AllowAllCrudAuthorizationPolicy<TestEntity, Guid>(), new VersionPolicy(), manager, [observer], module,
            new FoundationProjectContext(new FoundationProjectId("test-project")));

    private sealed class TestEntity(Guid id, string name) : Entity<Guid>(id)
    {
        public string Name { get; set; } = name;
        public int Version { get; set; } = 1;
    }

    private sealed record CreateRequest(string Name);
    private sealed record UpdateRequest(string Name, int ExpectedVersion);
    private sealed record ReadModel(Guid Id, string Name, int Version);

    private sealed class Mapper : ICrudMapper<TestEntity, Guid, CreateRequest, UpdateRequest, ReadModel>
    {
        public TestEntity Create(CreateRequest request) => new(Guid.NewGuid(), request.Name);
        public void ApplyUpdate(TestEntity entity, UpdateRequest request) { entity.Name = request.Name; entity.Version++; }
        public ReadModel ToReadModel(TestEntity entity) => new(entity.Id, entity.Name, entity.Version);
    }

    private sealed class VersionPolicy : ICrudConcurrencyPolicy<TestEntity, UpdateRequest>
    {
        public Result Validate(
            TestEntity entity,
            UpdateRequest request,
            CrudConcurrencyPrecondition? precondition = null) => entity.Version == request.ExpectedVersion
            ? Result.Success()
            : Result.Failure(Error.Conflict("Test.VersionConflict", "Version mismatch."));
    }

    private sealed class RecordingManager : ICrudManager<TestEntity, Guid, CreateRequest, UpdateRequest>;

    private sealed class RecordingObserver : ICrudOperationObserver<TestEntity, Guid>
    {
        public List<CrudOperation> Operations { get; } = [];
        public ValueTask OnSucceededAsync(CrudOperationEvent<TestEntity, Guid> operation, CancellationToken cancellationToken = default)
        { Operations.Add(operation.Operation); return ValueTask.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(1); }
    }

    private sealed class FakeRepository : IRepository<TestEntity, Guid>
    {
        public List<TestEntity> Items { get; } = [];
        public Task<TestEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<TestEntity?> FirstOrDefaultAsync(ISpecification<TestEntity> specification, CancellationToken cancellationToken = default) => Task.FromResult(Apply(specification).FirstOrDefault());
        public Task<IReadOnlyList<TestEntity>> ListAsync(ISpecification<TestEntity>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TestEntity>>(specification is null ? Items.ToArray() : Apply(specification).ToArray());
        public Task<int> CountAsync(ISpecification<TestEntity>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult(specification?.Criteria is null ? Items.Count : Items.Count(specification.Criteria.Compile()));
        public Task AddAsync(TestEntity entity, CancellationToken cancellationToken = default) { Items.Add(entity); return Task.CompletedTask; }
        public Task AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default) { Items.AddRange(entities); return Task.CompletedTask; }
        public void Remove(TestEntity entity) => Items.Remove(entity);
        public void RemoveRange(IEnumerable<TestEntity> entities) { foreach (var entity in entities.ToArray()) Items.Remove(entity); }

        private IEnumerable<TestEntity> Apply(ISpecification<TestEntity> specification)
        {
            IEnumerable<TestEntity> query = Items;
            if (specification.Criteria is not null) query = query.Where(specification.Criteria.Compile());
            if (specification.OrderBy is not null) query = query.OrderBy(specification.OrderBy.Compile());
            if (specification.OrderByDescending is not null) query = query.OrderByDescending(specification.OrderByDescending.Compile());
            if (specification.Skip.HasValue) query = query.Skip(specification.Skip.Value);
            if (specification.Take.HasValue) query = query.Take(specification.Take.Value);
            return query;
        }
    }
}
