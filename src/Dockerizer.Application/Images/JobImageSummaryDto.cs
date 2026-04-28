namespace Dockerizer.Application.Images;

public sealed record JobImageSummaryDto(
    Guid Id,
    string Status,
    string? DetectedStack,
    string? ImageTag,
    string? ImageId,
    string? SourceCommitSha,
    int? ContainerPort,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool IsCurrent);
