namespace Dockerizer.Domain.Entities;

public sealed class JobImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public string? DetectedStack { get; set; }
    public string? ImageTag { get; set; }
    public string? ImageId { get; set; }
    public string? SourceCommitSha { get; set; }
    public int? ContainerPort { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? BuiltAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public ICollection<ImageArtifact> Artifacts { get; set; } = [];
}
