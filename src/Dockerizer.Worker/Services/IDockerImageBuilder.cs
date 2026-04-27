using Dockerizer.Domain.Entities;

namespace Dockerizer.Worker.Services;

public interface IDockerImageBuilder
{
    Task<string> BuildAsync(Job job, string repositoryPath, CancellationToken cancellationToken);
}
