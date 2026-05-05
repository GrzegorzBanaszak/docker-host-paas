namespace Dockerizer.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public string? DefaultBranch { get; set; }
    public string? DefaultProjectPath { get; set; }
    public Guid? CurrentJobId { get; set; }
    public Job? CurrentJob { get; set; }
    public Guid? CurrentImageId { get; set; }
    public JobImage? CurrentImage { get; set; }
    public bool PublicAccessEnabled { get; set; }
    public string? PublicHostname { get; set; }
    public string? DeploymentUrl { get; set; }
    public string? RouteStatus { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
    public ICollection<Job> Jobs { get; set; } = [];
}
