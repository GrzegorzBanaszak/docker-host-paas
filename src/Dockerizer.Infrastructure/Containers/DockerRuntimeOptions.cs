namespace Dockerizer.Infrastructure.Containers;

public sealed class DockerRuntimeOptions
{
    public const string SectionName = "DockerRuntime";

    public string ContainerNamePrefix { get; set; } = "dockerizer-job";
    public string BindingHost { get; set; } = "127.0.0.1";
    public string PublicBaseUrl { get; set; } = "http://localhost";
    public int HostPortRangeStart { get; set; } = 45000;
    public int HostPortRangeEnd { get; set; } = 45999;
    public int StartupTimeoutSeconds { get; set; } = 60;
    public int StartupPollIntervalMilliseconds { get; set; } = 1000;
}
