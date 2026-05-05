using Dockerizer.Application.Images;
using Dockerizer.Application.Jobs;

namespace Dockerizer.Application.Projects;

public sealed record ProjectDetailsDto(
    Guid Id,
    string Name,
    string RepositoryUrl,
    string? DefaultBranch,
    string? DefaultProjectPath,
    Guid? CurrentJobId,
    Guid? CurrentImageId,
    string? CurrentStatus,
    string? DetectedStack,
    string? GeneratedImageTag,
    string? ContainerName,
    string? ContainerStatus,
    int? ContainerPort,
    int? PublishedPort,
    bool PublicAccessEnabled,
    string? PublicHostname,
    string? RouteStatus,
    string? DeploymentUrl,
    DateTimeOffset? DeployedAtUtc,
    JobImageSummaryDto? CurrentImage,
    IReadOnlyCollection<JobListItemDto> Jobs,
    IReadOnlyCollection<JobImageSummaryDto> Images,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ArchivedAtUtc);
