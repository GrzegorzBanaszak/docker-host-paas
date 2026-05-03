namespace Dockerizer.Application.Dns;

public sealed record DnsRouteDto(
    Guid JobId,
    string JobName,
    string RepositoryUrl,
    string? GeneratedImageTag,
    string? ContainerName,
    string? ContainerStatus,
    int? ContainerPort,
    int? PublishedPort,
    bool PublicAccessEnabled,
    string? PublicHostname,
    string? RouteStatus,
    string? DeploymentUrl,
    DateTimeOffset? DeployedAtUtc,
    DateTimeOffset CreatedAtUtc);
