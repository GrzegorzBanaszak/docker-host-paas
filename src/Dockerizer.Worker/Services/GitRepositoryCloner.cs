using System.Diagnostics;
using Dockerizer.Domain.Entities;

namespace Dockerizer.Worker.Services;

public sealed class GitRepositoryCloner(ILogger<GitRepositoryCloner> logger) : IGitRepositoryCloner
{
    public async Task CloneAsync(Job job, string targetPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var arguments = string.IsNullOrWhiteSpace(job.Branch)
            ? $"clone --depth 1 \"{job.RepositoryUrl}\" \"{targetPath}\""
            : $"clone --depth 1 --branch \"{job.Branch}\" \"{job.RepositoryUrl}\" \"{targetPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start git clone process.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            logger.LogInformation("git clone output for job {JobId}: {Output}", job.Id, standardOutput.Trim());
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git clone failed for job {job.Id} with exit code {process.ExitCode}: {standardError.Trim()}");
        }
    }
}
