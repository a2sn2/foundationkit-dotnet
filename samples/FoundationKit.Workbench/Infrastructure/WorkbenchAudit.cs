using System.Collections.Concurrent;
using FoundationKit.Auditing;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class WorkbenchAuditContextAccessor : IAuditContextAccessor
{
    public AuditContext Current { get; } = new(
        ActorId: "workbench-reference",
        CorrelationId: null,
        TenantId: null,
        Source: "workbench");
}

public sealed class WorkbenchAuditSink : IAuditSink
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();

    public IReadOnlyCollection<AuditEvent> Events => _events.ToArray();

    public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        _events.Enqueue(auditEvent);
        return ValueTask.CompletedTask;
    }
}
