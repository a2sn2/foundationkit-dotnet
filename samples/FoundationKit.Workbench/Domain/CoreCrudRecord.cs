using System.ComponentModel.DataAnnotations;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Workbench.Domain;

public sealed class CoreCrudRecord : Entity<Guid>
{
    private CoreCrudRecord()
    {
    }

    public CoreCrudRecord(Guid id, string name, DateTimeOffset createdUtc)
        : base(id)
    {
        Rename(name);
        CreatedUtc = createdUtc.ToUniversalTime();
        Version = 1;
    }

    [Required]
    [StringLength(120)]
    public string Name { get; private set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Version { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();
        if (normalized.Length > 120)
            throw new ArgumentOutOfRangeException(nameof(name), "Name cannot exceed 120 characters.");

        Name = normalized;
    }

    public void AdvanceVersion() => Version = checked(Version + 1);
}
