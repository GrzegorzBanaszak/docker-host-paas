using Dockerizer.Application.Abstractions;
using Dockerizer.Infrastructure.Configuration;
using Dockerizer.Infrastructure.Jobs;
using Dockerizer.Infrastructure.Artifacts;
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
            CleanupWorkspaceAfterCompletion = bool.TryParse(configuration["Worker:CleanupWorkspaceAfterCompletion"], out var cleanupWorkspaceAfterCompletion)
                && cleanupWorkspaceAfterCompletion,
        };

        services.AddSingleton(Options.Create(redisOptions));
        services.AddSingleton(artifactOptions);
        services.AddSingleton<JobArtifactService>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddScoped<IJobQueue, RedisJobQueue>();
        services.AddScoped<IJobsService, JobsService>();

        return services;
    }
}
