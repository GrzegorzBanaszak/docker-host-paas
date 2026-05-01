namespace Dockerizer.Infrastructure.Configuration;

public sealed class RepositorySecurityOptions
{
    public const string SectionName = "RepositorySecurity";

    public string[] AllowedHosts { get; set; } = ["github.com"];
    public int CloneTimeoutSeconds { get; set; } = 120;
}
