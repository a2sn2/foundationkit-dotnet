using System.Text;
using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Cases;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Contracts.Security;
using Madar.Domain.Cases;
using Xunit;

namespace Madar.Tests;

public sealed class CaseAttachmentTests
{
    [Fact]
    public void Create_NormalizesAllowedMetadataAndGeneratesPrivateKey()
    {
        var caseId = Guid.NewGuid();
        var result = CaseAttachment.Create(
            caseId,
            Guid.NewGuid(),
            "  evidence.PDF  ",
            "Application/PDF; charset=binary",
            128,
            Utc(10));

        Assert.True(result.IsSuccess);
        Assert.Equal("evidence.PDF", result.Value.OriginalFileName);
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.Equal(128, result.Value.SizeBytes);
        Assert.StartsWith($"{caseId:N}/", result.Value.StorageKey, StringComparison.Ordinal);
        Assert.DoesNotContain("evidence", result.Value.StorageKey, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("report.exe", "application/pdf")]
    [InlineData("report.pdf", "application/octet-stream")]
    [InlineData("../report.pdf", "application/pdf")]
    [InlineData("folder/report.pdf", "application/pdf")]
    public void Create_RejectsUnsafeOrUnsupportedMetadata(
        string fileName,
        string contentType)
    {
        var result = CaseAttachment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            fileName,
            contentType,
            128,
            Utc(10));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_RejectsOversizedContent()
    {
        var result = CaseAttachment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "evidence.pdf",
            "application/pdf",
            CaseAttachmentPolicy.MaxSizeBytes + 1,
            Utc(10));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseAttachmentErrors.InvalidSize, result.Error);
    }

    [Fact]
    public async Task Upload_CaseCreator_PersistsContentAndAuditsWithoutSensitiveMetadata()
    {
        var creator = TestCurrentUser.Authenticated(MadarRoles.Requester);
        var fixture = CreateFixture(creator);
        var item = CreateCase(creator.UserId!.Value);
        fixture.Cases.Seed(item);
        var payload = PdfBytes("private-marker-51e9");

        await using var stream = new MemoryStream(payload);
        var result = await fixture.Manager.UploadAsync(
            item.Id,
            new CaseAttachmentUpload(
                "customer-evidence.pdf",
                "application/pdf",
                payload.Length,
                stream));

        Assert.True(result.IsSuccess);
        Assert.Equal("customer-evidence.pdf", result.Value.OriginalFileName);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Single(fixture.Attachments.Items);
        Assert.Single(fixture.Content.Items);

        var audit = Assert.Single(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.attachment-uploaded");
        Assert.Equal(item.Id.ToString("D"), audit.SubjectId);
        Assert.Single(audit.Attributes);
        Assert.Contains("attachmentId", audit.Attributes.Keys);
        Assert.DoesNotContain(
            audit.Attributes.Values,
            value => value.Contains("customer-evidence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            audit.Attributes.Values,
            value => value.Contains("private-marker", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Upload_ContentTypeSignatureMismatch_IsRejectedBeforeStorage()
    {
        var creator = TestCurrentUser.Authenticated(MadarRoles.Requester);
        var fixture = CreateFixture(creator);
        var item = CreateCase(creator.UserId!.Value);
        fixture.Cases.Seed(item);
        var payload = Encoding.UTF8.GetBytes("not a PDF");

        await using var stream = new MemoryStream(payload);
        var result = await fixture.Manager.UploadAsync(
            item.Id,
            new CaseAttachmentUpload(
                "evidence.pdf",
                "application/pdf",
                payload.Length,
                stream));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseAttachmentErrors.InvalidContent, result.Error);
        Assert.Empty(fixture.Attachments.Items);
        Assert.Empty(fixture.Content.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Upload_UnrelatedOperator_IsMaskedAsCaseNotFound()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Operator);
        var fixture = CreateFixture(currentUser);
        var item = CreateCase(Guid.NewGuid());
        fixture.Cases.Seed(item);
        var payload = PdfBytes("denied");

        await using var stream = new MemoryStream(payload);
        var result = await fixture.Manager.UploadAsync(
            item.Id,
            new CaseAttachmentUpload(
                "denied.pdf",
                "application/pdf",
                payload.Length,
                stream));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseApplicationErrors.CaseNotFound, result.Error);
        Assert.Empty(fixture.Content.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Download_AssignedOperator_ReturnsBytesAndAuditsWithoutStorageKey()
    {
        var operatorId = Guid.NewGuid();
        var currentUser = TestCurrentUser.Authenticated(
            MadarRoles.Operator,
            operatorId);
        var fixture = CreateFixture(currentUser);
        var item = CreateCase(Guid.NewGuid());
        Assert.True(item.Assign(
            operatorId,
            Guid.NewGuid(),
            Utc(9, 1)).IsSuccess);
        fixture.Cases.Seed(item);

        var attachment = CreateAttachment(
            item.Id,
            Guid.NewGuid(),
            "runbook.txt",
            "text/plain",
            12);
        var expected = Encoding.UTF8.GetBytes("hello world!");
        fixture.Attachments.Seed(attachment);
        fixture.Content.Seed(attachment.StorageKey, expected);

        var result = await fixture.Manager.DownloadAsync(
            item.Id,
            attachment.Id);

        Assert.True(result.IsSuccess);
        await using var returned = result.Value.Content;
        using var buffer = new MemoryStream();
        await returned.CopyToAsync(buffer);
        Assert.Equal(expected, buffer.ToArray());

        var audit = Assert.Single(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.attachment-downloaded");
        Assert.Single(audit.Attributes);
        Assert.Equal(attachment.Id.ToString("D"), audit.Attributes["attachmentId"]);
        Assert.DoesNotContain(
            audit.Attributes.Values,
            value => value.Contains(attachment.StorageKey, StringComparison.Ordinal));
    }

    [Fact]
    public async Task List_CaseCreator_RemainsReadableAfterCaseClosure()
    {
        var creator = TestCurrentUser.Authenticated(MadarRoles.Requester);
        var fixture = CreateFixture(creator);
        var item = CreateCase(creator.UserId!.Value);
        var assignee = Guid.NewGuid();
        var actor = Guid.NewGuid();
        Assert.True(item.Assign(assignee, actor, Utc(9, 1)).IsSuccess);
        Assert.True(item.StartProgress(assignee, Utc(9, 2)).IsSuccess);
        Assert.True(item.Resolve(assignee, Utc(9, 3)).IsSuccess);
        Assert.True(item.Close(actor, Utc(9, 4)).IsSuccess);
        fixture.Cases.Seed(item);

        var attachment = CreateAttachment(
            item.Id,
            creator.UserId.Value,
            "history.txt",
            "text/plain",
            7);
        fixture.Attachments.Seed(attachment);

        var result = await fixture.Manager.ListAsync(item.Id);

        Assert.True(result.IsSuccess);
        var listed = Assert.Single(result.Value);
        Assert.Equal(attachment.Id, listed.Id);
        Assert.Equal(CaseStatuses.Closed, item.Status);
    }

    private static Fixture CreateFixture(TestCurrentUser currentUser)
    {
        var cases = new FakeCaseRepository();
        var attachments = new FakeAttachmentStore();
        var content = new FakeContentStore();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new TestClock { UtcNow = Utc(10) };
        var auditSink = new CollectingAuditSink();
        var authorization = new RolePermissionAuthorizationEvaluator(
            currentUser,
            MadarPermissions.CreateRolePermissionMap());
        var recorder = new AuditRecorder(
            auditSink,
            new TestAuditContextAccessor(currentUser),
            clock);

        return new Fixture(
            new CaseAttachmentManager(
                currentUser,
                authorization,
                cases,
                attachments,
                attachments,
                content,
                unitOfWork,
                recorder,
                clock),
            cases,
            attachments,
            content,
            unitOfWork,
            auditSink);
    }

    private static Case CreateCase(Guid creator)
    {
        var result = Case.Create(
            creator,
            "حالة للمرفقات",
            "وصف صالح لحالة تستخدم في اختبارات مستندات ومرفقات مدار.",
            CaseTypes.InternalServiceRequest,
            CasePriorities.Medium,
            Utc(9));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static CaseAttachment CreateAttachment(
        Guid caseId,
        Guid uploader,
        string fileName,
        string contentType,
        long size)
    {
        var result = CaseAttachment.Create(
            caseId,
            uploader,
            fileName,
            contentType,
            size,
            Utc(9, 30));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static byte[] PdfBytes(string marker) =>
        Encoding.UTF8.GetBytes($"%PDF-1.7\n{marker}\n%%EOF");

    private static DateTimeOffset Utc(int hour, int minute = 0) =>
        new(2026, 8, 9, hour, minute, 0, TimeSpan.Zero);

    private sealed record Fixture(
        CaseAttachmentManager Manager,
        FakeCaseRepository Cases,
        FakeAttachmentStore Attachments,
        FakeContentStore Content,
        FakeUnitOfWork UnitOfWork,
        CollectingAuditSink AuditSink);

    private sealed class TestCurrentUser : ICurrentUser, IAuthorizationSubject
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAuthenticated { get; private init; }
        public Guid? UserId { get; private init; }
        public string? Email { get; private init; }
        public bool IsInRole(string role) => _roles.Contains(role);

        public static TestCurrentUser Authenticated(string role, Guid? userId = null)
        {
            var user = new TestCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId ?? Guid.NewGuid(),
                Email = "attachment-test@example.test"
            };
            user._roles.Add(role);
            return user;
        }
    }

    private sealed class FakeCaseRepository : IRepository<Case, Guid>
    {
        public Dictionary<Guid, Case> Items { get; } = [];
        public void Seed(Case item) => Items[item.Id] = item;

        public Task<Case?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(id));

        public Task<Case?> FirstOrDefaultAsync(ISpecification<Case> specification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Case>> ListAsync(ISpecification<Case>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Case>>(Items.Values.ToArray());

        public Task<int> CountAsync(ISpecification<Case>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count);

        public Task AddAsync(Case entity, CancellationToken cancellationToken = default)
        {
            Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Case> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public void Remove(Case entity) => Items.Remove(entity.Id);

        public void RemoveRange(IEnumerable<Case> entities)
        {
            foreach (var entity in entities)
                Items.Remove(entity.Id);
        }
    }

    private sealed class FakeAttachmentStore : ICaseAttachmentStore, ICaseAttachmentQueryService
    {
        public Dictionary<Guid, CaseAttachment> Items { get; } = [];

        public void Seed(CaseAttachment attachment) => Items[attachment.Id] = attachment;

        public Task AddAsync(CaseAttachment attachment, CancellationToken cancellationToken = default)
        {
            Items[attachment.Id] = attachment;
            return Task.CompletedTask;
        }

        public Task<CaseAttachmentStoredRecord?> GetForCaseAsync(
            Guid caseId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
        {
            if (!Items.TryGetValue(attachmentId, out var attachment)
                || attachment.CaseId != caseId)
            {
                return Task.FromResult<CaseAttachmentStoredRecord?>(null);
            }

            return Task.FromResult<CaseAttachmentStoredRecord?>(
                new CaseAttachmentStoredRecord(
                    ToDto(attachment),
                    attachment.StorageKey));
        }

        public Task<CaseAttachmentDto?> GetByIdAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Items.TryGetValue(attachmentId, out var attachment)
                    ? ToDto(attachment)
                    : null);

        public Task<IReadOnlyList<CaseAttachmentDto>> ListForCaseAsync(
            Guid caseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseAttachmentDto>>(
                Items.Values
                    .Where(item => item.CaseId == caseId)
                    .OrderBy(item => item.CreatedUtc)
                    .ThenBy(item => item.Id)
                    .Select(ToDto)
                    .ToArray());

        private static CaseAttachmentDto ToDto(CaseAttachment attachment) =>
            new(
                attachment.Id,
                attachment.CaseId,
                attachment.UploadedByUserId,
                "رافع الاختبار",
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeBytes,
                attachment.CreatedUtc);
    }

    private sealed class FakeContentStore : ICaseAttachmentContentStore
    {
        public Dictionary<string, byte[]> Items { get; } = new(StringComparer.Ordinal);

        public void Seed(string storageKey, byte[] content) =>
            Items[storageKey] = content.ToArray();

        public async Task SaveAsync(
            string storageKey,
            Stream content,
            long expectedSizeBytes,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var value = buffer.ToArray();
            if (value.LongLength != expectedSizeBytes)
                throw new InvalidDataException();

            Items[storageKey] = value;
        }

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(
                Items.TryGetValue(storageKey, out var value)
                    ? new MemoryStream(value, writable: false)
                    : null);
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

    private sealed class CollectingAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];

        public ValueTask WriteAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestAuditContextAccessor(TestCurrentUser currentUser) : IAuditContextAccessor
    {
        public AuditContext Current => new(
            currentUser.UserId?.ToString("D"),
            "madar-attachment-test-correlation",
            null,
            "madar-attachment-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
