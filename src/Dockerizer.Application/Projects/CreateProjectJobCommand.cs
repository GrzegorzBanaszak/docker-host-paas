namespace Dockerizer.Application.Projects;

public sealed record CreateProjectJobCommand(
    string? Name,
    string? Branch,
    string? ProjectPath);
