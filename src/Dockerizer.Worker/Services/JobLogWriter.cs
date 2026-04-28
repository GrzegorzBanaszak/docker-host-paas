using Dockerizer.Infrastructure.Artifacts;

namespace Dockerizer.Worker.Services;

public sealed class JobLogWriter(JobArtifactService artifactService)
{
    public Task WriteLineAsync(Guid imageId, string message, CancellationToken cancellationToken) =>
        artifactService.AppendImageLogLineAsync(imageId, message, cancellationToken);
}
