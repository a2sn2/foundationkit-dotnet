using Volo.Abp.Authorization.Permissions;

namespace FoundationKit.Authorization;

public interface IAsyncAuthorizationEvaluator
{
    ValueTask<bool> HasPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default);

    ValueTask<bool> CanAccessOwnedResourceAsync(
        Guid ownerUserId,
        string privilegedPermission,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Uses ABP's permission checker for provider-managed grants while retaining
/// FoundationKit's ownership short-circuit semantics.
/// </summary>
public sealed class AbpPermissionAuthorizationEvaluator : IAsyncAuthorizationEvaluator
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly IAuthorizationSubject _subject;
    private readonly Func<string, string> _nameMap;

    public AbpPermissionAuthorizationEvaluator(
        IPermissionChecker permissionChecker,
        IAuthorizationSubject subject,
        Func<string, string>? nameMap = null)
    {
        _permissionChecker = permissionChecker
            ?? throw new ArgumentNullException(nameof(permissionChecker));
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _nameMap = nameMap ?? (static permission => permission);
    }

    public async ValueTask<bool> HasPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default)
    {
        var normalized = PermissionId.Normalize(permission);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_subject.IsAuthenticated)
        {
            return false;
        }

        var providerName = _nameMap(normalized);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        var granted = await _permissionChecker.IsGrantedAsync(providerName).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return granted;
    }

    public async ValueTask<bool> CanAccessOwnedResourceAsync(
        Guid ownerUserId,
        string privilegedPermission,
        CancellationToken cancellationToken = default)
    {
        if (!_subject.IsAuthenticated)
        {
            return false;
        }

        if (_subject.UserId is { } userId && userId == ownerUserId)
        {
            return true;
        }

        return await HasPermissionAsync(privilegedPermission, cancellationToken)
            .ConfigureAwait(false);
    }
}
