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

    public async Task AppendImageLogLineAsync(Guid imageId, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifact = await dbContext.ImageArtifacts
            .FirstOrDefaultAsync(
                x => x.JobImageId == imageId && x.Kind == LogArtifactKind && x.Name == LogArtifactName,
                cancellationToken);

        if (artifact is null)
        {
            artifact = new ImageArtifact
            {
                JobImageId = imageId,
                Kind = LogArtifactKind,
                Name = LogArtifactName,
            };

            dbContext.ImageArtifacts.Add(artifact);
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

        await CaptureGeneratedFilesCoreAsync(
            existingArtifacts,
            createArtifact: fileName => new JobArtifact
            {
                JobId = jobId,
                Kind = GeneratedFileArtifactKind,
                Name = fileName,
            },
            removeArtifact: artifact => dbContext.JobArtifacts.Remove(artifact),
            addArtifact: artifact => dbContext.JobArtifacts.Add((JobArtifact)artifact),
            setArtifactContent: (artifact, content, updatedAtUtc) =>
            {
                var typedArtifact = (JobArtifact)artifact;
                typedArtifact.Content = content;
                typedArtifact.UpdatedAtUtc = updatedAtUtc;
            },
            repositoryPath,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CaptureImageGeneratedFilesAsync(Guid imageId, string repositoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingArtifacts = await dbContext.ImageArtifacts
            .Where(x => x.JobImageId == imageId && x.Kind == GeneratedFileArtifactKind)
            .ToListAsync(cancellationToken);

        await CaptureGeneratedFilesCoreAsync(
            existingArtifacts,
            createArtifact: fileName => new ImageArtifact
            {
                JobImageId = imageId,
                Kind = GeneratedFileArtifactKind,
                Name = fileName,
            },
            removeArtifact: artifact => dbContext.ImageArtifacts.Remove((ImageArtifact)artifact),
            addArtifact: artifact => dbContext.ImageArtifacts.Add((ImageArtifact)artifact),
            setArtifactContent: (artifact, content, updatedAtUtc) =>
            {
                var typedArtifact = (ImageArtifact)artifact;
                typedArtifact.Content = content;
                typedArtifact.UpdatedAtUtc = updatedAtUtc;
            },
            repositoryPath,
            cancellationToken);

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

    public async Task<JobLogDto?> GetImageLogsAsync(Guid imageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifact = await dbContext.ImageArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.JobImageId == imageId && x.Kind == LogArtifactKind && x.Name == LogArtifactName,
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

    public async Task<IReadOnlyCollection<JobFileDto>> GetImageFilesAsync(Guid imageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifacts = await dbContext.ImageArtifacts
            .AsNoTracking()
            .Where(x => x.JobImageId == imageId && x.Kind == GeneratedFileArtifactKind)
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

    public async Task<JobFileContentDto?> GetImageFileContentAsync(Guid imageId, string fileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!GeneratedFiles.Contains(fileId, StringComparer.Ordinal))
        {
            return null;
        }

        var artifact = await dbContext.ImageArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.JobImageId == imageId && x.Kind == GeneratedFileArtifactKind && x.Name == fileId,
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

    private static async Task CaptureGeneratedFilesCoreAsync<TArtifact>(
        IReadOnlyCollection<TArtifact> existingArtifacts,
        Func<string, TArtifact> createArtifact,
        Action<TArtifact> removeArtifact,
        Action<TArtifact> addArtifact,
        Action<TArtifact, string, DateTimeOffset> setArtifactContent,
        string repositoryPath,
        CancellationToken cancellationToken)
        where TArtifact : class
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var fileName in GeneratedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = Path.Combine(repositoryPath, fileName);
            var artifact = existingArtifacts.FirstOrDefault(x => GetArtifactName(x) == fileName);

            if (!File.Exists(filePath))
            {
                if (artifact is not null)
                {
                    removeArtifact(artifact);
                }

                continue;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (artifact is null)
            {
                artifact = createArtifact(fileName);
                addArtifact(artifact);
            }

            setArtifactContent(artifact, content, now);
        }
    }

    private static string GetArtifactName<TArtifact>(TArtifact artifact) where TArtifact : class =>
        artifact switch
        {
            JobArtifact jobArtifact => jobArtifact.Name,
            ImageArtifact imageArtifact => imageArtifact.Name,
            _ => throw new InvalidOperationException($"Unsupported artifact type: {typeof(TArtifact).Name}."),
        };

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
