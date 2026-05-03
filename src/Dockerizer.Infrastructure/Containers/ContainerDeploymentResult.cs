namespace Dockerizer.Infrastructure.Containers;

public sealed record ContainerDeploymentResult(
    string ContainerId,
    string ContainerName,
    int ContainerPort,
    int? PublishedPort,
    bool PublicAccessEnabled,
    string? PublicHostname,
    string? DeploymentUrl,
    string RouteStatus,
    DateTimeOffset DeployedAtUtc);
