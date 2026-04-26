using Dockerizer.Application.Jobs;

namespace Dockerizer.Infrastructure.Artifacts;

public sealed class JobArtifactService(ArtifactOptions options)
{
    private static readonly string[] GeneratedFiles = ["Dockerfile", ".dockerignore"];

    public Task<JobLogDto?> GetLogsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var logPath = Path.Combine(GetWorkspacePath(jobId), "job.log");
        if (!File.Exists(logPath))
        {
            return Task.FromResult<JobLogDto?>(null);
        }

        var content = File.ReadAllText(logPath);
        return Task.FromResult<JobLogDto?>(new JobLogDto(content));
    }

    public Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspacePath = GetWorkspacePath(jobId);
        if (!Directory.Exists(workspacePath))
        {
            return Task.FromResult<IReadOnlyCollection<JobFileDto>>([]);
        }

        var files = GeneratedFiles
            .Select(fileName => new FileInfo(Path.Combine(workspacePath, fileName)))
            .Where(file => file.Exists)
            .Select(file => new JobFileDto(file.Name, file.Name, file.Length))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<JobFileDto>>(files);
    }

    public Task<JobFileContentDto?> GetFileContentAsync(Guid jobId, string fileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!GeneratedFiles.Contains(fileId, StringComparer.Ordinal))
        {
            return Task.FromResult<JobFileContentDto?>(null);
        }

        var filePath = Path.Combine(GetWorkspacePath(jobId), fileId);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<JobFileContentDto?>(null);
        }

        var content = File.ReadAllText(filePath);
        return Task.FromResult<JobFileContentDto?>(new JobFileContentDto(fileId, fileId, content));
    }

    public Task CleanupWorkspaceAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workspacePath = GetWorkspacePath(jobId);
        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string GetWorkspacePath(Guid jobId)
    {
        var workspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        var workspacePath = Path.GetFullPath(Path.Combine(workspaceRoot, jobId.ToString("N")));
        if (!workspacePath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved workspace path escapes the configured workspace root.");
        }

        return workspacePath;
    }
}
