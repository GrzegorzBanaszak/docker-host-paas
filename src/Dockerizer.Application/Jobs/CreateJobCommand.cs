namespace Dockerizer.Application.Jobs;

public sealed record CreateJobCommand(string RepositoryUrl, string? Branch);
