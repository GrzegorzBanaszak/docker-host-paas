using System.Text;
using Dockerizer.Application.Jobs;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dockerizer.Infrastructure.Artifacts;

public sealed class JobArtifactService(
    DockerizerDbContext dbContext,
    ArtifactOptions options)
{
    private const string GeneratedFileArtifactKind = "generated-file";
    private const string LogArtifactKind = "log";
    private const string LogArtifactName = "job.log";
    private static readonly string[] GeneratedFiles = ["Dockerfile", ".dockerignore"];

    public async Task AppendLogLineAsync(Guid jobId, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifact = await dbContext.JobArtifacts
            .FirstOrDefaultAsync(
                x => x.JobId == jobId && x.Kind == LogArtifactKind && x.Name == LogArtifactName,
                cancellationToken);

        if (artifact is null)
        {
            artifact = new JobArtifact
            {
                JobId = jobId,
                Kind = LogArtifactKind,
                Name = LogArtifactName,
            };

            dbContext.JobArtifacts.Add(artifact);
        }

        artifact.Content += $"[{DateTimeOffset.UtcNow:O}] {message}{Environment.NewLine}";
        artifact.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetExecutionArtifactsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifacts = await dbContext.JobArtifacts
            .Where(x => x.JobId == jobId)
            .ToListAsync(cancellationToken);

        if (artifacts.Count == 0)
        {
            return;
        }

        dbContext.JobArtifacts.RemoveRange(artifacts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CaptureGeneratedFilesAsync(Guid jobId, string repositoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingArtifacts = await dbContext.JobArtifacts
            .Where(x => x.JobId == jobId && x.Kind == GeneratedFileArtifactKind)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var fileName in GeneratedFiles)
        {
            var filePath = Path.Combine(repositoryPath, fileName);
            var artifact = existingArtifacts.FirstOrDefault(x => x.Name == fileName);

            if (!File.Exists(filePath))
            {
                if (artifact is not null)
                {
                    dbContext.JobArtifacts.Remove(artifact);
                }

                continue;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (artifact is null)
            {
                artifact = new JobArtifact
                {
                    JobId = jobId,
                    Kind = GeneratedFileArtifactKind,
                    Name = fileName,
                };

                dbContext.JobArtifacts.Add(artifact);
            }

            artifact.Content = content;
            artifact.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobLogDto?> GetLogsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifact = await dbContext.JobArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.JobId == jobId && x.Kind == LogArtifactKind && x.Name == LogArtifactName,
                cancellationToken);

        return artifact is null ? null : new JobLogDto(artifact.Content);
    }

    public async Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifacts = await dbContext.JobArtifacts
            .AsNoTracking()
            .Where(x => x.JobId == jobId && x.Kind == GeneratedFileArtifactKind)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return artifacts
            .Select(x => new JobFileDto(x.Name, x.Name, Encoding.UTF8.GetByteCount(x.Content)))
            .ToList();
    }

    public async Task<JobFileContentDto?> GetFileContentAsync(Guid jobId, string fileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!GeneratedFiles.Contains(fileId, StringComparer.Ordinal))
        {
            return null;
        }

        var artifact = await dbContext.JobArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.JobId == jobId && x.Kind == GeneratedFileArtifactKind && x.Name == fileId,
                cancellationToken);

        return artifact is null ? null : new JobFileContentDto(artifact.Name, artifact.Name, artifact.Content);
    }

    public async Task CleanupWorkspaceAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workspacePath = GetWorkspacePath(jobId);
        if (Directory.Exists(workspacePath))
        {
            await DeleteDirectoryRobustlyAsync(workspacePath, cancellationToken);
        }
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

    private static async Task DeleteDirectoryRobustlyAsync(string directoryPath, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                ResetAttributes(directoryPath);
                Directory.Delete(directoryPath, recursive: true);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
            }
        }

        ResetAttributes(directoryPath);
        Directory.Delete(directoryPath, recursive: true);
    }

    private static void ResetAttributes(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var childDirectory in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            var directoryInfo = new DirectoryInfo(childDirectory);
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            var fileInfo = new FileInfo(filePath);
            fileInfo.Attributes = FileAttributes.Normal;
        }

        var rootDirectoryInfo = new DirectoryInfo(directoryPath);
        rootDirectoryInfo.Attributes &= ~FileAttributes.ReadOnly;
    }
}
