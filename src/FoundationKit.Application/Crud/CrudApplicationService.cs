using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Isolation;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Application.Validation;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Application.Crud;

public sealed class CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead>
    where TEntity : Entity<TId>
    where TId : notnull
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICrudMapper<TEntity, TId, TCreate, TUpdate, TRead> _mapper;
    private readonly IValidator<TCreate> _createValidator;
    private readonly IValidator<TUpdate> _updateValidator;
    private readonly ICrudAuthorizationPolicy<TEntity, TId> _authorization;
    private readonly ICrudConcurrencyPolicy<TEntity, TUpdate> _concurrency;
    private readonly ICrudManager<TEntity, TId, TCreate, TUpdate> _manager;
    private readonly ICrudQueryPolicy<TEntity, TId> _queryPolicy;
    private readonly ICrudOperationObserver<TEntity, TId>[] _observers;
    private readonly FoundationModuleDefinition<TEntity, TId> _module;
    private readonly IFoundationProjectContext _projectContext;

    public CrudApplicationService(
        IRepository<TEntity, TId> repository,
        IUnitOfWork unitOfWork,
        ICrudMapper<TEntity, TId, TCreate, TUpdate, TRead> mapper,
        IValidator<TCreate> createValidator,
        IValidator<TUpdate> updateValidator,
        ICrudAuthorizationPolicy<TEntity, TId> authorization,
        ICrudConcurrencyPolicy<TEntity, TUpdate> concurrency,
        ICrudManager<TEntity, TId, TCreate, TUpdate> manager,
        IEnumerable<ICrudOperationObserver<TEntity, TId>> observers,
        FoundationModuleDefinition<TEntity, TId> module,
        IFoundationProjectContext projectContext,
        ICrudQueryPolicy<TEntity, TId>? queryPolicy = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _concurrency = concurrency ?? throw new ArgumentNullException(nameof(concurrency));
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _queryPolicy = queryPolicy ?? new DefaultCrudQueryPolicy<TEntity, TId>();
        _observers = (observers ?? throw new ArgumentNullException(nameof(observers))).ToArray();
        _module = module ?? throw new ArgumentNullException(nameof(module));
        _projectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));

        if (_module.Crud is null)
            throw new InvalidOperationException($"Module '{_module.Name}' does not enable CRUD.");
    }

    public async Task<Result<CrudItemResult<TId, TRead>>> CreateAsync(
        TCreate request,
        CancellationToken cancellationToken = default)
    {
        if (!_module.Crud!.CreateEnabled)
            return Result<CrudItemResult<TId, TRead>>.Failure(OperationDisabled(CrudOperation.Create));

        var validation = await ValidateAsync(_createValidator, request, cancellationToken).ConfigureAwait(false);
        if (validation is not null)
            return Result<CrudItemResult<TId, TRead>>.Failure(validation);

        var authorization = await _authorization.AuthorizeAsync(
            new CrudAuthorizationContext<TEntity, TId>(CrudOperation.Create, false, default, null, request),
            cancellationToken).ConfigureAwait(false);
        if (authorization.IsFailure)
            return Result<CrudItemResult<TId, TRead>>.Failure(authorization.Error);

        var entity = _mapper.Create(request);
        var manager = await _manager.BeforeCreateAsync(entity, request, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
            return Result<CrudItemResult<TId, TRead>>.Failure(manager.Error);

        await _repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        var save = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (save is not null)
            return Result<CrudItemResult<TId, TRead>>.Failure(save);

        await NotifyAsync(CrudOperation.Create, entity, cancellationToken).ConfigureAwait(false);
        return Result<CrudItemResult<TId, TRead>>.Success(new(entity.Id, _mapper.ToReadModel(entity)));
    }

    public async Task<Result<TRead>> GetAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (!_module.Crud!.ReadEnabled)
            return Result<TRead>.Failure(OperationDisabled(CrudOperation.Read));

        var entity = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return Result<TRead>.Failure(NotFound(id));

        var authorization = await _authorization.AuthorizeAsync(
            new CrudAuthorizationContext<TEntity, TId>(CrudOperation.Read, true, id, entity, null),
            cancellationToken).ConfigureAwait(false);
        return authorization.IsFailure
            ? Result<TRead>.Failure(authorization.Error)
            : Result<TRead>.Success(_mapper.ToReadModel(entity));
    }

    public Task<Result<PagedResult<TRead>>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ListAsync(CrudListRequest.FromPage(request), cancellationToken);
    }

    public async Task<Result<PagedResult<TRead>>> ListAsync(
        CrudListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_module.Crud!.ListEnabled)
            return Result<PagedResult<TRead>>.Failure(OperationDisabled(CrudOperation.List));

        var authorization = await _authorization.AuthorizeAsync(
            new CrudAuthorizationContext<TEntity, TId>(CrudOperation.List, false, default, null, request),
            cancellationToken).ConfigureAwait(false);
        if (authorization.IsFailure)
            return Result<PagedResult<TRead>>.Failure(authorization.Error);

        var queryPlan = _queryPolicy.Build(request);
        if (queryPlan.IsFailure)
            return Result<PagedResult<TRead>>.Failure(queryPlan.Error);

        var pageSize = Math.Min(request.Page.PageSize, _module.Crud.MaximumPageSize);
        var boundedPage = new PageRequest(request.Page.Page, pageSize);
        var plan = queryPlan.Value;
        var total = await _repository.CountAsync(
            new CrudQuerySpecification<TEntity, TId>(plan),
            cancellationToken).ConfigureAwait(false);
        var entities = await _repository.ListAsync(
            new CrudQuerySpecification<TEntity, TId>(plan, boundedPage),
            cancellationToken).ConfigureAwait(false);
        var items = entities.Select(_mapper.ToReadModel).ToArray();

        return Result<PagedResult<TRead>>.Success(
            new PagedResult<TRead>(items, boundedPage.Page, boundedPage.PageSize, total));
    }

    public Task<Result<TRead>> UpdateAsync(
        TId id,
        TUpdate request,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(id, request, null, cancellationToken);

    public async Task<Result<TRead>> UpdateAsync(
        TId id,
        TUpdate request,
        CrudConcurrencyPrecondition? precondition,
        CancellationToken cancellationToken = default)
    {
        if (!_module.Crud!.UpdateEnabled)
            return Result<TRead>.Failure(OperationDisabled(CrudOperation.Update));

        var validation = await ValidateAsync(_updateValidator, request, cancellationToken).ConfigureAwait(false);
        if (validation is not null)
            return Result<TRead>.Failure(validation);

        var entity = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return Result<TRead>.Failure(NotFound(id));

        var authorization = await _authorization.AuthorizeAsync(
            new CrudAuthorizationContext<TEntity, TId>(CrudOperation.Update, true, id, entity, request),
            cancellationToken).ConfigureAwait(false);
        if (authorization.IsFailure)
            return Result<TRead>.Failure(authorization.Error);

        var normalizedPrecondition = precondition?.Normalize();
        var concurrency = _concurrency.Validate(entity, request, normalizedPrecondition);
        if (concurrency.IsFailure)
            return Result<TRead>.Failure(concurrency.Error);

        var manager = await _manager.BeforeUpdateAsync(entity, request, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
            return Result<TRead>.Failure(manager.Error);

        _mapper.ApplyUpdate(entity, request);
        var save = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (save is not null)
            return Result<TRead>.Failure(save);

        await NotifyAsync(CrudOperation.Update, entity, cancellationToken).ConfigureAwait(false);
        return Result<TRead>.Success(_mapper.ToReadModel(entity));
    }

    public async Task<Result> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (!_module.Crud!.DeleteEnabled)
            return Result.Failure(OperationDisabled(CrudOperation.Delete));

        var entity = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return Result.Failure(NotFound(id));

        var authorization = await _authorization.AuthorizeAsync(
            new CrudAuthorizationContext<TEntity, TId>(CrudOperation.Delete, true, id, entity, null),
            cancellationToken).ConfigureAwait(false);
        if (authorization.IsFailure)
            return Result.Failure(authorization.Error);

        var manager = await _manager.BeforeDeleteAsync(entity, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
            return Result.Failure(manager.Error);

        _repository.Remove(entity);
        var save = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (save is not null)
            return Result.Failure(save);

        await NotifyAsync(CrudOperation.Delete, entity, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Error?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (FoundationConcurrencyException)
        {
            return Error.Conflict(
                "Foundation.Crud.ConcurrencyConflict",
                "The resource changed after it was loaded. Reload it and retry the operation.");
        }
    }

    private async Task NotifyAsync(
        CrudOperation operation,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        if (_observers.Length == 0)
            return;

        var notification = new CrudOperationEvent<TEntity, TId>(
            _projectContext.ProjectId,
            _module.Name,
            operation,
            entity.Id,
            entity);

        foreach (var observer in _observers)
            await observer.OnSucceededAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Error?> ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var failures = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (failures.Count == 0)
            return null;

        var description = string.Join(
            "; ",
            failures.Select(failure => $"{failure.PropertyName}: {failure.Message}"));
        return Error.Validation("Foundation.Crud.Validation", description);
    }

    private Error OperationDisabled(CrudOperation operation) => Error.NotFound(
        "Foundation.Crud.OperationDisabled",
        $"Operation '{operation}' is not enabled for module '{_module.Name}'.");

    private Error NotFound(TId id) => Error.NotFound(
        "Foundation.Crud.NotFound",
        $"Module '{_module.Name}' does not contain resource '{id}'.");

    private sealed class CrudQuerySpecification<TPageEntity, TPageId> : Specification<TPageEntity>
        where TPageEntity : Entity<TPageId>
        where TPageId : notnull
    {
        public CrudQuerySpecification(
            CrudQueryPlan<TPageEntity> plan,
            PageRequest? page = null)
            : base(plan.Criteria)
        {
            if (plan.OrderBy is null)
            {
                ApplyOrderBy(entity => entity.Id);
            }
            else if (plan.SortDirection == CrudSortDirection.Descending)
            {
                ApplyOrderByDescending(plan.OrderBy);
            }
            else
            {
                ApplyOrderBy(plan.OrderBy);
            }

            if (page is not null)
                ApplyPaging(page.Skip, page.PageSize);

            UseNoTracking();
        }
    }
}
