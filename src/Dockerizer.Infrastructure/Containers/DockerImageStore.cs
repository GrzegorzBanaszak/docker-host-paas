using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dockerizer.Infrastructure.Containers;

public sealed class DockerImageStore(ILogger<DockerImageStore> logger) : IDockerImageStore
{
    public async Task RemoveAsync(string? imageId, string? imageTag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var target in BuildTargets(imageId, imageTag))
        {
            var result = await RunDockerCommandAsync($"image rm -f {target}", cancellationToken);
            if (result.ExitCode == 0)
            {
                logger.LogInformation("Removed docker image {Target}.", target);
                return;
            }

            var error = (result.StandardError ?? string.Empty).Trim();
            if (error.Contains("No such image", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"docker image rm failed for {target} with exit code {result.ExitCode}: {error}");
        }
    }

    private static IReadOnlyCollection<string> BuildTargets(string? imageId, string? imageTag)
    {
        var targets = new List<string>();

        if (!string.IsNullOrWhiteSpace(imageId))
        {
            targets.Add(imageId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(imageTag) &&
            !targets.Contains(imageTag.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            targets.Add(imageTag.Trim());
        }

        return targets;
    }

    private static async Task<DockerCommandResult> RunDockerCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start docker process for arguments: {arguments}");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new DockerCommandResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }

    private sealed record DockerCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
