namespace Dockerizer.Application.Images;

public sealed record ImageDetailsDto(
    Guid Id,
    Guid JobId,
    string JobName,
    string RepositoryUrl,
    string? Branch,
    string? ProjectPath,
    string JobStatus,
    string? JobDeploymentUrl,
    string Status,
    string? DetectedStack,
    string? ImageTag,
    string? ImageId,
    string? SourceCommitSha,
    int? ContainerPort,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? BuiltAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool IsCurrent);
