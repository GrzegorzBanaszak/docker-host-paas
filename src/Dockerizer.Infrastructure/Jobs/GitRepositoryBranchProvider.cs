using System.Diagnostics;
using Dockerizer.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class GitRepositoryBranchProvider(
    IOptions<RepositorySecurityOptions> repositorySecurityOptions,
    ILogger<GitRepositoryBranchProvider> logger) : IRepositoryBranchProvider
{
    private readonly RepositorySecurityOptions _repositorySecurityOptions = repositorySecurityOptions.Value;

    public async Task<IReadOnlyCollection<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("ls-remote");
        startInfo.ArgumentList.Add("--heads");
        startInfo.ArgumentList.Add("--refs");
        startInfo.ArgumentList.Add(repositoryUrl);

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _repositorySecurityOptions.CloneTimeoutSeconds)));
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start git ls-remote process.");
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
            throw new TimeoutException($"git ls-remote exceeded the configured timeout of {_repositorySecurityOptions.CloneTimeoutSeconds} seconds.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Could not load repository branches. {standardError.Trim()}");
        }

        var branches = standardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && parts[1].StartsWith("refs/heads/", StringComparison.Ordinal))
            .Select(parts => parts[1]["refs/heads/".Length..])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(GetBranchPriority)
            .ThenBy(branch => branch, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        logger.LogInformation("Loaded {BranchCount} branches for repository {RepositoryUrl}.", branches.Length, repositoryUrl);

        return branches;
    }

    private static int GetBranchPriority(string branch) =>
        branch switch
        {
            "main" => 0,
            "master" => 1,
            _ => 2,
        };

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
