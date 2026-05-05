namespace Dockerizer.Application.Projects;

public sealed record CreateProjectCommand(
    string Name,
    string RepositoryUrl,
    string? DefaultBranch,
    string? DefaultProjectPath);
