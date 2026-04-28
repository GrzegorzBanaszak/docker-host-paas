using Dockerizer.Infrastructure.Artifacts;

namespace Dockerizer.Worker.Services;

public sealed class JobLogWriter(JobArtifactService artifactService)
{
    public Task WriteLineAsync(Guid jobId, string message, CancellationToken cancellationToken) =>
        artifactService.AppendLogLineAsync(jobId, message, cancellationToken);
}
