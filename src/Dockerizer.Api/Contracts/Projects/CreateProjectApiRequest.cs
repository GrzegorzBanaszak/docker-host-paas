namespace Dockerizer.Api.Contracts.Projects;

public sealed record CreateProjectApiRequest(
    string Name,
    string RepositoryUrl,
    string? DefaultBranch,
    string? DefaultProjectPath);
