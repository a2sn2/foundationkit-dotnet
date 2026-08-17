using FoundationCurrentUser = FoundationKit.Application.Abstractions.ICurrentUser;
using AbpCurrentUser = Volo.Abp.Users.ICurrentUser;

namespace FoundationKit.Identity;

/// <summary>
/// Adapts ABP's ambient current-user context to FoundationKit's minimal user contract.
/// </summary>
public sealed class AbpCurrentUserAdapter(AbpCurrentUser currentUser) : FoundationCurrentUser
{
    private readonly AbpCurrentUser _currentUser = currentUser
        ?? throw new ArgumentNullException(nameof(currentUser));

    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public Guid? UserId => _currentUser.Id;

    public string? Email => _currentUser.Email;

    public bool IsInRole(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return _currentUser.IsInRole(role);
    }
}
