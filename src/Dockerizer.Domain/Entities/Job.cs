namespace Dockerizer.Domain.Entities;

public sealed class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RepositoryUrl { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public string? DetectedStack { get; set; }
    public string? GeneratedImageTag { get; set; }
    public string? ContainerId { get; set; }
    public string? ContainerName { get; set; }
    public int? ContainerPort { get; set; }
    public int? PublishedPort { get; set; }
    public string? DeploymentUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? DeployedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
