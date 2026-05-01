using System.Diagnostics;
using System.Globalization;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace Dockerizer.Worker.Services;

public sealed class DockerImageBuilder(
    IOptions<WorkerOptions> workerOptions,
    IOptions<DockerRuntimeOptions> dockerRuntimeOptions,
    ILogger<DockerImageBuilder> logger) : IDockerImageBuilder
{
    private readonly WorkerOptions _workerOptions = workerOptions.Value;
    private readonly DockerRuntimeOptions _dockerRuntimeOptions = dockerRuntimeOptions.Value;

    public async Task<DockerBuildResult> BuildAsync(Job job, JobImage image, string repositoryPath, CancellationToken cancellationToken)
    {
        var imageTag = BuildImageTag(job, image);
        var timeout = TimeSpan.FromMinutes(_workerOptions.DockerBuildTimeoutMinutes);
        var imageIdFilePath = Path.Combine(Directory.GetParent(repositoryPath)!.FullName, $".docker-image-{image.Id:N}.iid");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"build {BuildResourceLimitArguments()} --iidfile \"{imageIdFilePath}\" -t {imageTag} \"{repositoryPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start docker build process.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var standardErrorTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(process);
            throw new TimeoutException($"docker build exceeded the configured timeout of {_workerOptions.DockerBuildTimeoutMinutes} minutes.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            logger.LogInformation("docker build output for job {JobId}: {Output}", job.Id, standardOutput.Trim());
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker build failed for job {job.Id} with exit code {process.ExitCode}: {standardError.Trim()}");
        }

        if (!File.Exists(imageIdFilePath))
        {
            throw new InvalidOperationException($"docker build completed for job {job.Id}, but no image id file was produced.");
        }

        var imageId = (await File.ReadAllTextAsync(imageIdFilePath, linkedCts.Token)).Trim();
        File.Delete(imageIdFilePath);

        logger.LogInformation("Docker image {ImageTag} built successfully for job {JobId}.", imageTag, job.Id);
        return new DockerBuildResult(imageTag, imageId);
    }

    private string BuildImageTag(Job job, JobImage image)
    {
        var prefix = string.IsNullOrWhiteSpace(_workerOptions.DockerImagePrefix)
            ? "dockerizer"
            : _workerOptions.DockerImagePrefix.Trim().ToLowerInvariant();

        return $"{prefix}:{job.Id:N}-{image.Id:N}";
    }

    private string BuildResourceLimitArguments()
    {
        var arguments = new List<string>();

        var cpuQuota = TryConvertCpuLimitToQuota(_dockerRuntimeOptions.ContainerCpuLimit);
        if (cpuQuota.HasValue)
        {
            arguments.Add($"--cpu-quota {cpuQuota.Value}");
        }

        if (!string.IsNullOrWhiteSpace(_dockerRuntimeOptions.ContainerMemoryLimit))
        {
            arguments.Add($"--memory {Quote(_dockerRuntimeOptions.ContainerMemoryLimit)}");
        }

        return string.Join(' ', arguments);
    }

    private static string Quote(string value) => $"\"{value.Trim()}\"";

    private static int? TryConvertCpuLimitToQuota(string? cpuLimit)
    {
        if (!decimal.TryParse(cpuLimit, NumberStyles.Number, CultureInfo.InvariantCulture, out var cpus) || cpus <= 0)
        {
            return null;
        }

        return (int)(cpus * 100000);
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
