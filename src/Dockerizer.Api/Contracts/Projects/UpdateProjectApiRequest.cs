namespace Dockerizer.Api.Contracts.Projects;

public sealed record UpdateProjectApiRequest(
    string Name,
    string RepositoryUrl,
    string? DefaultBranch,
    string? DefaultProjectPath);
