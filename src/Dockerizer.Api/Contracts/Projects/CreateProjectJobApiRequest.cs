namespace Dockerizer.Api.Contracts.Projects;

public sealed record CreateProjectJobApiRequest(
    string? Name,
    string? Branch,
    string? ProjectPath);
