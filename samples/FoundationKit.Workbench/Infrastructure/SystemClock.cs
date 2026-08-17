using FoundationKit.Application.Abstractions;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
}
