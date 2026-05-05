namespace Dockerizer.Application.Projects;

public sealed record UpdateProjectCommand(
    string Name,
    string RepositoryUrl,
    string? DefaultBranch,
    string? DefaultProjectPath);
