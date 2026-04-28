namespace Dockerizer.Infrastructure.Containers;

public sealed record ContainerRuntimeStatus(
    string Status,
    string? ContainerId,
    string? ContainerName,
    int? PublishedPort,
    string? DeploymentUrl);
