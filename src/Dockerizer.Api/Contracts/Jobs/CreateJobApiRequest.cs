namespace Dockerizer.Api.Contracts.Jobs;

public sealed record CreateJobApiRequest(string Name, string RepositoryUrl, string? Branch, string? ProjectPath);
