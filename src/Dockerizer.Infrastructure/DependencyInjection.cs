using Dockerizer.Application.Abstractions;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Configuration;
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
        };

        services.AddSingleton(Options.Create(redisOptions));
        services.AddSingleton(artifactOptions);
        services.AddSingleton(Options.Create(dockerRuntimeOptions));
        services.AddScoped<JobArtifactService>();
        services.AddSingleton<IRepositoryBranchProvider, GitRepositoryBranchProvider>();
        services.AddSingleton<RepositoryInspectionService>();
        services.AddSingleton<IDockerContainerRuntime, DockerContainerRuntime>();
        services.AddSingleton<IDockerImageStore, DockerImageStore>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddScoped<IJobQueue, RedisJobQueue>();
        services.AddScoped<IJobsService, JobsService>();
        services.AddScoped<IImagesService, ImagesService>();

        return services;
    }
}
