using Dockerizer.Infrastructure.Jobs;

namespace Dockerizer.Worker.Services;

public sealed class RepositoryStackDetector(RepositoryProjectTypeDetector repositoryProjectTypeDetector)
{
    public Task<string> DetectAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(repositoryProjectTypeDetector.Detect(repositoryPath));
    }
}
