using System.Diagnostics;
using Dockerizer.Application.Jobs;
using Dockerizer.Infrastructure.Artifacts;
using Microsoft.Extensions.Logging;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class RepositoryInspectionService(
    IRepositoryBranchProvider repositoryBranchProvider,
    ArtifactOptions artifactOptions,
    ILogger<RepositoryInspectionService> logger)
{
    public async Task<RepositoryInspectionDto> InspectAsync(string repositoryUrl, CancellationToken cancellationToken)
    {
        var branches = await repositoryBranchProvider.GetBranchesAsync(repositoryUrl, cancellationToken);
        var detectedStack = await TryDetectStackAsync(repositoryUrl, cancellationToken);

        return new RepositoryInspectionDto(branches, detectedStack);
    }

    private async Task<string?> TryDetectStackAsync(string repositoryUrl, CancellationToken cancellationToken)
    {
        var workspaceRoot = Path.GetFullPath(artifactOptions.WorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);

        var inspectionRoot = Path.Combine(workspaceRoot, "_repo-inspection");
        Directory.CreateDirectory(inspectionRoot);

        var targetPath = Path.Combine(inspectionRoot, Guid.NewGuid().ToString("N"));

        try
        {
            await CloneDefaultBranchAsync(repositoryUrl, targetPath, cancellationToken);
            return DetectStack(targetPath);
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

    private static async Task CloneDefaultBranchAsync(string repositoryUrl, string targetPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 \"{repositoryUrl}\" \"{targetPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start repository inspection clone.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        _ = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Repository inspection clone failed. {standardError.Trim()}");
        }
    }

    private static string DetectStack(string repositoryPath)
    {
        if (File.Exists(Path.Combine(repositoryPath, "package.json")))
        {
            return "nodejs";
        }

        if (File.Exists(Path.Combine(repositoryPath, "pyproject.toml")) ||
            File.Exists(Path.Combine(repositoryPath, "requirements.txt")))
        {
            return "python";
        }

        if (File.Exists(Path.Combine(repositoryPath, "composer.json")))
        {
            return "php";
        }

        if (File.Exists(Path.Combine(repositoryPath, "go.mod")))
        {
            return "go";
        }

        if (File.Exists(Path.Combine(repositoryPath, "pom.xml")) ||
            File.Exists(Path.Combine(repositoryPath, "build.gradle")) ||
            File.Exists(Path.Combine(repositoryPath, "build.gradle.kts")))
        {
            return "java";
        }

        if (Directory.EnumerateFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories).Any() ||
            Directory.EnumerateFiles(repositoryPath, "*.sln", SearchOption.TopDirectoryOnly).Any())
        {
            return "dotnet";
        }

        if (File.Exists(Path.Combine(repositoryPath, "Dockerfile")))
        {
            return "dockerfile-only";
        }

        if (File.Exists(Path.Combine(repositoryPath, "index.html")))
        {
            return "static-html";
        }

        return "unknown";
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
}
