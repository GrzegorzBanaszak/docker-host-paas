namespace Dockerizer.Application.Images;

public sealed record ImageListItemDto(
    Guid Id,
    Guid JobId,
    string JobName,
    string RepositoryUrl,
    string? Branch,
    string? ProjectPath,
    string Status,
    string? DetectedStack,
    string? ImageTag,
    string? ImageId,
    string? SourceCommitSha,
    int? ContainerPort,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool IsCurrent);
