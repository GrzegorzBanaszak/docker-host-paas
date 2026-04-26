namespace Dockerizer.Application.Jobs;

public sealed record JobDetailsDto(
    Guid Id,
    string RepositoryUrl,
    string? Branch,
    string Status,
    string? DetectedStack,
    string? GeneratedImageTag,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
