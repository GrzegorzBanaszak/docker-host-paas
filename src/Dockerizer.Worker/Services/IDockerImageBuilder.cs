using Dockerizer.Domain.Entities;

namespace Dockerizer.Worker.Services;

public interface IDockerImageBuilder
{
    Task<DockerBuildResult> BuildAsync(Job job, JobImage image, string repositoryPath, CancellationToken cancellationToken);
}
