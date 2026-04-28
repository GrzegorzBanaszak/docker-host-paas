namespace Dockerizer.Infrastructure.Containers;

public interface IDockerImageStore
{
    Task RemoveAsync(string? imageId, string? imageTag, CancellationToken cancellationToken);
}
