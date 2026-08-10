using FoundationKit.Application.Crud;
using FoundationKit.Application.Isolation;
using FoundationKit.Auditing;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class CrudAuditingTests
{
    [Fact]
    public async Task Crud_audit_observer_emits_bounded_project_scoped_audit_request()
    {
        var recorder = new RecordingAuditRecorder();
        var observer = new CrudAuditObserver<TestEntity, Guid>(recorder);
        var entity = new TestEntity(Guid.NewGuid());

        await observer.OnSucceededAsync(new CrudOperationEvent<TestEntity, Guid>(
            new FoundationProjectId("project-one"),
            "Customers",
            CrudOperation.Update,
            entity.Id,
            entity));

        var request = Assert.Single(recorder.Requests);
        Assert.Equal("crud.update", request.Action);
        Assert.Equal("Customers", request.SubjectType);
        Assert.Equal(entity.Id.ToString(), request.SubjectId);
        Assert.Equal("project-one", request.Attributes!["projectId"]);
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class RecordingAuditRecorder : IAuditRecorder
    {
        public List<AuditRequest> Requests { get; } = [];

        public ValueTask<AuditEvent> RecordAsync(AuditRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.FromResult(AuditEvent.Create(
                request,
                AuditContext.Empty,
                DateTimeOffset.UtcNow));
        }
    }
}
