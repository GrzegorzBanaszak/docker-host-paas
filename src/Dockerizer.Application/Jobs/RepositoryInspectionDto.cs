namespace Dockerizer.Application.Jobs;

public sealed record RepositoryInspectionDto(
    IReadOnlyCollection<string> Branches,
    string? ProjectPath,
    string? DetectedStack);
