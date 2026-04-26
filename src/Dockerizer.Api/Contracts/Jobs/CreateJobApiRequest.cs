namespace Dockerizer.Api.Contracts.Jobs;

public sealed record CreateJobApiRequest(string RepositoryUrl, string? Branch);
