namespace Dockerizer.Application.Jobs;

public sealed record JobListItemDto(
    Guid Id,
    string Name,
    string RepositoryUrl,
    string? Branch,
    string Status,
    string? DetectedStack,
    string? GeneratedImageTag,
    int? PublishedPort,
    string? DeploymentUrl,
    DateTimeOffset CreatedAtUtc);
