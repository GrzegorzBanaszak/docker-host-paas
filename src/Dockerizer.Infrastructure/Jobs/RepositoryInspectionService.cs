using System.Diagnostics;
using Dockerizer.Application.Jobs;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class RepositoryInspectionService(
    IRepositoryBranchProvider repositoryBranchProvider,
    ArtifactOptions artifactOptions,
    IOptions<RepositorySecurityOptions> repositorySecurityOptions,
    RepositoryProjectTypeDetector repositoryProjectTypeDetector,
    RepositoryProjectPathResolver repositoryProjectPathResolver,
    ILogger<RepositoryInspectionService> logger)
{
    private readonly RepositorySecurityOptions _repositorySecurityOptions = repositorySecurityOptions.Value;

    public async Task<RepositoryInspectionDto> InspectAsync(string repositoryUrl, string? projectPath, CancellationToken cancellationToken)
    {
        ValidateRepositoryUrl(repositoryUrl);
        var branches = await repositoryBranchProvider.GetBranchesAsync(repositoryUrl, cancellationToken);
        var detectedStack = await TryDetectStackAsync(repositoryUrl, projectPath, cancellationToken);

        return new RepositoryInspectionDto(branches, repositoryProjectPathResolver.Normalize(projectPath), detectedStack);
    }

    private async Task<string?> TryDetectStackAsync(string repositoryUrl, string? projectPath, CancellationToken cancellationToken)
    {
        var workspaceRoot = Path.GetFullPath(artifactOptions.WorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);

        var inspectionRoot = Path.Combine(workspaceRoot, "_repo-inspection");
        Directory.CreateDirectory(inspectionRoot);

        var targetPath = Path.Combine(inspectionRoot, Guid.NewGuid().ToString("N"));

        try
        {
            await CloneDefaultBranchAsync(repositoryUrl, targetPath, _repositorySecurityOptions.CloneTimeoutSeconds, cancellationToken);
            var projectRootPath = repositoryProjectPathResolver.Resolve(targetPath, projectPath);
            return repositoryProjectTypeDetector.Detect(projectRootPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Repository stack detection failed for {RepositoryUrl}.", repositoryUrl);
            return null;
        }
        finally
        {
            TryDeleteDirectory(targetPath);
        }
    }

    private static async Task CloneDefaultBranchAsync(string repositoryUrl, string targetPath, int timeoutSeconds, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("clone");
        startInfo.ArgumentList.Add("--depth");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add(repositoryUrl);
        startInfo.ArgumentList.Add(targetPath);

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start repository inspection clone.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var standardErrorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(process);
            throw new TimeoutException($"repository inspection clone exceeded the configured timeout of {timeoutSeconds} seconds.");
        }

        _ = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Repository inspection clone failed. {standardError.Trim()}");
        }
    }

    private void ValidateRepositoryUrl(string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Repository URL must be an absolute HTTP or HTTPS URL.");
        }

        if (!_repositorySecurityOptions.AllowedHosts.Any(host => string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Repository host '{uri.Host}' is not allowed.");
        }
    }

    private static void TryDeleteDirectory(string directoryPath)
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

        Directory.Delete(directoryPath, recursive: true);
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }
}
