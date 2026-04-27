namespace Dockerizer.Infrastructure.Containers;

public sealed record ContainerDeploymentResult(
    string ContainerId,
    string ContainerName,
    int ContainerPort,
    int PublishedPort,
    string DeploymentUrl,
    DateTimeOffset DeployedAtUtc);
