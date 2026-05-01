namespace Dockerizer.Application.Jobs;

public sealed record CreateJobCommand(string Name, string RepositoryUrl, string? Branch, string? ProjectPath);
