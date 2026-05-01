using System.Diagnostics;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Dockerizer.Worker.Services;

public sealed class GitRepositoryCloner(
    IOptions<RepositorySecurityOptions> repositorySecurityOptions,
    ILogger<GitRepositoryCloner> logger) : IGitRepositoryCloner
{
    private readonly RepositorySecurityOptions _repositorySecurityOptions = repositorySecurityOptions.Value;

    public async Task CloneAsync(Job job, string targetPath, CancellationToken cancellationToken)
    {
        ValidateRepositoryUrl(job.RepositoryUrl);
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
        if (!string.IsNullOrWhiteSpace(job.Branch))
        {
            startInfo.ArgumentList.Add("--branch");
            startInfo.ArgumentList.Add(job.Branch);
        }

        startInfo.ArgumentList.Add(job.RepositoryUrl);
        startInfo.ArgumentList.Add(targetPath);

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _repositorySecurityOptions.CloneTimeoutSeconds)));

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start git clone process.");
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
            throw new TimeoutException($"git clone exceeded the configured timeout of {_repositorySecurityOptions.CloneTimeoutSeconds} seconds.");
        }

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
