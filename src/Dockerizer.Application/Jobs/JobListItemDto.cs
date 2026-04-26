namespace Dockerizer.Application.Jobs;

public sealed record JobListItemDto(
    Guid Id,
    string RepositoryUrl,
    string? Branch,
    string Status,
    string? DetectedStack,
    string? GeneratedImageTag,
    DateTimeOffset CreatedAtUtc);
