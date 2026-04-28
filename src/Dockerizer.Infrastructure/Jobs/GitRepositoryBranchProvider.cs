using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class GitRepositoryBranchProvider(ILogger<GitRepositoryBranchProvider> logger) : IRepositoryBranchProvider
{
    public async Task<IReadOnlyCollection<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"ls-remote --heads --refs \"{repositoryUrl}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start git ls-remote process.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

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
}
