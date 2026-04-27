namespace Dockerizer.Application.Jobs;

public sealed record JobDetailsDto(
    Guid Id,
    string RepositoryUrl,
    string? Branch,
    string Status,
    string? DetectedStack,
    string? GeneratedImageTag,
    string? ContainerId,
    string? ContainerName,
    int? ContainerPort,
    int? PublishedPort,
    string? DeploymentUrl,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? DeployedAtUtc,
    DateTimeOffset? CompletedAtUtc);
