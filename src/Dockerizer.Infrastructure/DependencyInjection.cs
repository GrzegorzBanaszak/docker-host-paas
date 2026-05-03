using Dockerizer.Application.Abstractions;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Configuration;
using Dockerizer.Infrastructure.Dns;
using Dockerizer.Infrastructure.Jobs;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Images;
using Dockerizer.Infrastructure.Persistence;
using Dockerizer.Infrastructure.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dockerizer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<DockerizerDbContext>(options =>
            options.UseNpgsql(connectionString));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        var redisOptions = new RedisOptions
        {
            QueueKey = configuration[$"{RedisOptions.SectionName}:QueueKey"] ?? "dockerizer:jobs",
        };
        var artifactOptions = new ArtifactOptions
        {
            WorkspaceRoot = configuration[ArtifactOptions.WorkspaceRootConfigKey] ?? ".worker-data/repos",
        };
        var dockerRuntimeOptions = new DockerRuntimeOptions
        {
            ContainerNamePrefix = configuration[$"{DockerRuntimeOptions.SectionName}:ContainerNamePrefix"] ?? "dockerizer-job",
            BindingHost = configuration[$"{DockerRuntimeOptions.SectionName}:BindingHost"] ?? "127.0.0.1",
            PublicBaseUrl = configuration[$"{DockerRuntimeOptions.SectionName}:PublicBaseUrl"] ?? "http://localhost",
            HostPortRangeStart = int.TryParse(configuration[$"{DockerRuntimeOptions.SectionName}:HostPortRangeStart"], out var hostPortRangeStart)
                ? hostPortRangeStart
                : 45000,
            HostPortRangeEnd = int.TryParse(configuration[$"{DockerRuntimeOptions.SectionName}:HostPortRangeEnd"], out var hostPortRangeEnd)
                ? hostPortRangeEnd
                : 45999,
            StartupTimeoutSeconds = int.TryParse(configuration[$"{DockerRuntimeOptions.SectionName}:StartupTimeoutSeconds"], out var startupTimeoutSeconds)
                ? startupTimeoutSeconds
                : 60,
            StartupPollIntervalMilliseconds = int.TryParse(configuration[$"{DockerRuntimeOptions.SectionName}:StartupPollIntervalMilliseconds"], out var startupPollIntervalMilliseconds)
                ? startupPollIntervalMilliseconds
                : 1000,
            ContainerCpuLimit = configuration[$"{DockerRuntimeOptions.SectionName}:ContainerCpuLimit"] ?? "1.0",
            ContainerMemoryLimit = configuration[$"{DockerRuntimeOptions.SectionName}:ContainerMemoryLimit"] ?? "512m",
            ContainerPidsLimit = int.TryParse(configuration[$"{DockerRuntimeOptions.SectionName}:ContainerPidsLimit"], out var containerPidsLimit)
                ? containerPidsLimit
                : 256,
            DisableContainerNetwork = bool.TryParse(configuration[$"{DockerRuntimeOptions.SectionName}:DisableContainerNetwork"], out var disableContainerNetwork) &&
                disableContainerNetwork,
        };
        var applicationRoutingOptions = new ApplicationRoutingOptions
        {
            Mode = configuration[$"{ApplicationRoutingOptions.SectionName}:Mode"] ?? "Port",
            PublicScheme = configuration[$"{ApplicationRoutingOptions.SectionName}:PublicScheme"] ?? "https",
            BaseDomain = configuration[$"{ApplicationRoutingOptions.SectionName}:BaseDomain"] ?? string.Empty,
            DockerNetwork = configuration[$"{ApplicationRoutingOptions.SectionName}:DockerNetwork"] ?? "dockerizer-public",
            ReverseProxy = configuration[$"{ApplicationRoutingOptions.SectionName}:ReverseProxy"] ?? "Traefik",
        };
        var repositorySecurityOptions = new RepositorySecurityOptions
        {
            AllowedHosts = configuration.GetSection($"{RepositorySecurityOptions.SectionName}:AllowedHosts")
                .GetChildren()
                .Select(child => child.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray() is { Length: > 0 } allowedHosts
                    ? allowedHosts
                    : ["github.com"],
            CloneTimeoutSeconds = int.TryParse(configuration[$"{RepositorySecurityOptions.SectionName}:CloneTimeoutSeconds"], out var cloneTimeoutSeconds)
                ? cloneTimeoutSeconds
                : 120,
        };

        services.AddSingleton(Options.Create(redisOptions));
        services.AddSingleton(artifactOptions);
        services.AddSingleton(Options.Create(dockerRuntimeOptions));
        services.AddSingleton(Options.Create(applicationRoutingOptions));
        services.AddSingleton(Options.Create(repositorySecurityOptions));
        services.AddScoped<JobArtifactService>();
        services.AddSingleton<RepositoryProjectPathResolver>();
        services.AddSingleton<RepositoryProjectTypeDetector>();
        services.AddSingleton<IRepositoryBranchProvider, GitRepositoryBranchProvider>();
        services.AddSingleton<RepositoryInspectionService>();
        services.AddSingleton<IDockerContainerRuntime, DockerContainerRuntime>();
        services.AddSingleton<IDockerImageStore, DockerImageStore>();
        services.AddSingleton<ISystemResourceService, DockerSystemResourceService>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddScoped<IJobQueue, RedisJobQueue>();
        services.AddScoped<IDnsService, DnsService>();
        services.AddScoped<IJobsService, JobsService>();
        services.AddScoped<IImagesService, ImagesService>();

        return services;
    }
}
