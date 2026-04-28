using System.Diagnostics;
using Dockerizer.Domain.Entities;
using Dockerizer.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace Dockerizer.Worker.Services;

public sealed class DockerImageBuilder(
    IOptions<WorkerOptions> workerOptions,
    ILogger<DockerImageBuilder> logger) : IDockerImageBuilder
{
    private readonly WorkerOptions _workerOptions = workerOptions.Value;

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
            Arguments = $"build --iidfile \"{imageIdFilePath}\" -t {imageTag} \"{repositoryPath}\"",
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

        await process.WaitForExitAsync(linkedCts.Token);

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
}
