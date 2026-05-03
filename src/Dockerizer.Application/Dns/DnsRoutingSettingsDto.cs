namespace Dockerizer.Application.Dns;

public sealed record DnsRoutingSettingsDto(
    string Mode,
    string PublicScheme,
    string? BaseDomain,
    string DockerNetwork,
    string ReverseProxy);
