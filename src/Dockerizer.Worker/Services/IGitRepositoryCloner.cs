using Dockerizer.Domain.Entities;

namespace Dockerizer.Worker.Services;

public interface IGitRepositoryCloner
{
    Task CloneAsync(Job job, string targetPath, CancellationToken cancellationToken);
}
