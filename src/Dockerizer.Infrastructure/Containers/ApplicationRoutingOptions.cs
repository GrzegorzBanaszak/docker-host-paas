namespace Dockerizer.Infrastructure.Containers;

public sealed class ApplicationRoutingOptions
{
    public const string SectionName = "ApplicationRouting";

    public string Mode { get; set; } = "Port";
    public string PublicScheme { get; set; } = "https";
    public string BaseDomain { get; set; } = string.Empty;
    public string DockerNetwork { get; set; } = "dockerizer-public";
    public string ReverseProxy { get; set; } = "Traefik";

    public bool UsesTunnelWildcard =>
        Mode.Equals("TunnelWildcard", StringComparison.OrdinalIgnoreCase);

    public bool UsesPortPublishing =>
        !UsesTunnelWildcard;
}
