using System.Diagnostics;
using Dockerizer.Application.Abstractions;
using Dockerizer.Domain;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Persistence;
using Dockerizer.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dockerizer.Worker.Services;

public sealed class JobExecutionService(
    DockerizerDbContext dbContext,
    IJobQueue jobQueue,
    IGitRepositoryCloner gitRepositoryCloner,
    RepositoryStackDetector repositoryStackDetector,
    ContainerizationTemplateGenerator containerizationTemplateGenerator,
    ContainerPortResolver containerPortResolver,
    IDockerImageBuilder dockerImageBuilder,
    IDockerContainerRuntime dockerContainerRuntime,
    JobLogWriter jobLogWriter,
    JobArtifactService jobArtifactService,
    IOptions<WorkerOptions> workerOptions,
    ILogger<JobExecutionService> logger)
{
    private readonly WorkerOptions _workerOptions = workerOptions.Value;

    public Task<Guid?> DequeueAsync(CancellationToken cancellationToken) =>
        jobQueue.DequeueAsync(cancellationToken);

    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs.FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Job {JobId} was not found in the database.", jobId);
            return;
        }

        if (job.Status is JobStatus.Running or JobStatus.Canceled)
        {
            logger.LogInformation("Skipping job {JobId} with status {Status}.", jobId, job.Status);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = JobStatus.Running;
        job.StartedAtUtc = now;
        job.CompletedAtUtc = null;
        job.ErrorMessage = null;
        job.DetectedStack = null;
        job.GeneratedImageTag = null;
        job.ImageId = null;
        job.ContainerId = null;
        job.ContainerName = null;
        job.ContainerPort = null;
        job.PublishedPort = null;
        job.DeploymentUrl = null;
        job.DeployedAtUtc = null;
        job.CurrentImageId = null;

        var image = new JobImage
        {
            JobId = job.Id,
            Status = JobStatus.Running,
            CreatedAtUtc = now,
            StartedAtUtc = now,
        };

        dbContext.JobImages.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);
        await jobArtifactService.ResetExecutionArtifactsAsync(job.Id, cancellationToken);

        try
        {
            var workspacePath = PrepareWorkspace(job.Id);
            var repositoryPath = Path.Combine(workspacePath, "repository");
            await jobLogWriter.WriteLineAsync(image.Id, $"Starting job for repository {job.RepositoryUrl}.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, cancellationToken);
            await gitRepositoryCloner.CloneAsync(job, repositoryPath, cancellationToken);
            await jobLogWriter.WriteLineAsync(image.Id, "Repository cloned successfully.", cancellationToken);

            image.SourceCommitSha = await TryResolveCommitShaAsync(repositoryPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(image.SourceCommitSha))
            {
                await jobLogWriter.WriteLineAsync(image.Id, $"Resolved commit: {image.SourceCommitSha}.", cancellationToken);
            }

            await ThrowIfCanceledAsync(job.Id, cancellationToken);
            image.DetectedStack = await repositoryStackDetector.DetectAsync(repositoryPath, cancellationToken);
            job.DetectedStack = image.DetectedStack;
            await jobLogWriter.WriteLineAsync(image.Id, $"Detected stack: {image.DetectedStack}.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, cancellationToken);
            await containerizationTemplateGenerator.GenerateAsync(repositoryPath, image.DetectedStack, cancellationToken);
            await jobArtifactService.CaptureImageGeneratedFilesAsync(image.Id, repositoryPath, cancellationToken);
            await jobLogWriter.WriteLineAsync(image.Id, "Containerization files generated and stored.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, cancellationToken);
            image.ContainerPort = containerPortResolver.Resolve(repositoryPath, image.DetectedStack);
            job.ContainerPort = image.ContainerPort;
            await jobLogWriter.WriteLineAsync(image.Id, $"Resolved container port: {job.ContainerPort}.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, cancellationToken);
            var buildResult = await dockerImageBuilder.BuildAsync(job, image, repositoryPath, cancellationToken);
            image.ImageTag = buildResult.ImageTag;
            image.ImageId = buildResult.ImageId;
            image.BuiltAtUtc = DateTimeOffset.UtcNow;
            job.GeneratedImageTag = buildResult.ImageTag;
            job.ImageId = buildResult.ImageId;
            await jobLogWriter.WriteLineAsync(image.Id, $"Docker image built: {job.GeneratedImageTag} ({job.ImageId}).", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, cancellationToken);
            var deployment = await dockerContainerRuntime.RunAsync(job, job.ContainerPort.Value, cancellationToken);
            job.ContainerId = deployment.ContainerId;
            job.ContainerName = deployment.ContainerName;
            job.PublishedPort = deployment.PublishedPort;
            job.DeploymentUrl = deployment.DeploymentUrl;
            job.DeployedAtUtc = deployment.DeployedAtUtc;
            await jobLogWriter.WriteLineAsync(image.Id, $"Container deployed: {job.ContainerName} at {job.DeploymentUrl}.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, cancellationToken);

            job.Status = JobStatus.Succeeded;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.CurrentImageId = image.Id;
            image.Status = JobStatus.Succeeded;
            image.CompletedAtUtc = job.CompletedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Job {JobId} completed successfully with detected stack {DetectedStack}, image {GeneratedImageTag}, and image id {ImageId}.",
                jobId,
                job.DetectedStack,
                job.GeneratedImageTag,
                job.ImageId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JobCanceledException)
        {
            logger.LogInformation("Job {JobId} was canceled during processing.", jobId);
            await dockerContainerRuntime.StopAndRemoveAsync(job, CancellationToken.None);
            job.ContainerId = null;
            job.ContainerName = null;
            job.PublishedPort = null;
            job.DeploymentUrl = null;
            job.DeployedAtUtc = null;
            image.Status = JobStatus.Canceled;
            image.ErrorMessage = "Job was canceled.";
            image.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            await jobLogWriter.WriteLineAsync(image.Id, $"Job failed: {ex.Message}", CancellationToken.None);

            await dockerContainerRuntime.StopAndRemoveAsync(job, CancellationToken.None);
            job.ContainerId = null;
            job.ContainerName = null;
            job.PublishedPort = null;
            job.DeploymentUrl = null;
            job.DeployedAtUtc = null;

            job.Status = JobStatus.Failed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.ErrorMessage = ex.Message;
            image.Status = JobStatus.Failed;
            image.ErrorMessage = ex.Message;
            image.CompletedAtUtc = job.CompletedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogError(ex, "Job {JobId} failed.", jobId);
        }
        finally
        {
            try
            {
                await jobArtifactService.CleanupWorkspaceAsync(job.Id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Workspace cleanup failed for job {JobId}.", job.Id);
            }
        }
    }

    private string PrepareWorkspace(Guid jobId)
    {
        var workspaceRoot = Path.GetFullPath(_workerOptions.WorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);

        var workspacePath = Path.GetFullPath(Path.Combine(workspaceRoot, jobId.ToString("N")));
        if (!workspacePath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved workspace path escapes the configured workspace root.");
        }

        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, recursive: true);
        }

        return workspacePath;
    }

    private async Task ThrowIfCanceledAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var currentStatus = await dbContext.Jobs
            .Where(x => x.Id == jobId)
            .Select(x => x.Status)
            .FirstAsync(cancellationToken);

        if (currentStatus != JobStatus.Canceled)
        {
            return;
        }

        throw new JobCanceledException();
    }

    private static async Task<string?> TryResolveCommitShaAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{repositoryPath}\" rev-parse HEAD",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            return null;
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var standardOutput = (await standardOutputTask).Trim();
        _ = await standardErrorTask;

        return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(standardOutput)
            ? standardOutput
            : null;
    }

    private sealed class JobCanceledException : Exception;
}
