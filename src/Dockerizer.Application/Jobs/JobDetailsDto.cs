using Dockerizer.Application.Images;

namespace Dockerizer.Application.Jobs;

public sealed record JobDetailsDto(
    Guid Id,
    string Name,
    string RepositoryUrl,
    string? Branch,
    string? ProjectPath,
    string Status,
    string? DetectedStack,
    string? GeneratedImageTag,
    string? ImageId,
    string? ContainerId,
    string? ContainerName,
    string? ContainerStatus,
    int? ContainerPort,
    int? PublishedPort,
    bool PublicAccessEnabled,
    string? PublicHostname,
    string? RouteStatus,
    string? DeploymentUrl,
    string? ErrorMessage,
    Guid? CurrentImageId,
    JobImageSummaryDto? CurrentImage,
    IReadOnlyCollection<JobImageSummaryDto> Images,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? DeployedAtUtc,
    DateTimeOffset? CompletedAtUtc);
