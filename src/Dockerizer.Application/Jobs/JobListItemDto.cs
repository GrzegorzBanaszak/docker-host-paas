namespace Dockerizer.Application.Jobs;

public sealed record JobListItemDto(
    Guid Id,
    string Name,
    string RepositoryUrl,
    string? Branch,
    string? ProjectPath,
    string Status,
    string? DetectedStack,
    string? GeneratedImageTag,
    string? ContainerStatus,
    int? PublishedPort,
    string? DeploymentUrl,
    DateTimeOffset CreatedAtUtc);
