using Dockerizer.Application.Abstractions;
using Dockerizer.Domain;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Persistence;
using Dockerizer.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dockerizer.Worker.Services;

public sealed class JobExecutionService(
    DockerizerDbContext dbContext,
    IJobQueue jobQueue,
    GitRepositoryCloner gitRepositoryCloner,
    RepositoryStackDetector repositoryStackDetector,
    ContainerizationTemplateGenerator containerizationTemplateGenerator,
    DockerImageBuilder dockerImageBuilder,
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

        job.Status = JobStatus.Running;
        job.StartedAtUtc = DateTimeOffset.UtcNow;
        job.CompletedAtUtc = null;
        job.ErrorMessage = null;
        job.GeneratedImageTag = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var workspacePath = PrepareWorkspace(job.Id);
            await jobLogWriter.WriteLineAsync(workspacePath, $"Starting job for repository {job.RepositoryUrl}.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, workspacePath, cancellationToken);
            await gitRepositoryCloner.CloneAsync(job, workspacePath, cancellationToken);
            await jobLogWriter.WriteLineAsync(workspacePath, "Repository cloned successfully.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, workspacePath, cancellationToken);
            job.DetectedStack = await repositoryStackDetector.DetectAsync(workspacePath, cancellationToken);
            await jobLogWriter.WriteLineAsync(workspacePath, $"Detected stack: {job.DetectedStack}.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, workspacePath, cancellationToken);
            await containerizationTemplateGenerator.GenerateAsync(workspacePath, job.DetectedStack, cancellationToken);
            await jobLogWriter.WriteLineAsync(workspacePath, "Containerization files generated.", cancellationToken);
            await ThrowIfCanceledAsync(job.Id, workspacePath, cancellationToken);
            job.GeneratedImageTag = await dockerImageBuilder.BuildAsync(job, workspacePath, cancellationToken);
            await jobLogWriter.WriteLineAsync(workspacePath, $"Docker image built: {job.GeneratedImageTag}.", cancellationToken);

            job.Status = JobStatus.Succeeded;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await CleanupWorkspaceIfConfiguredAsync(job.Id, cancellationToken);

            logger.LogInformation(
                "Job {JobId} completed successfully with detected stack {DetectedStack} and image {GeneratedImageTag}.",
                jobId,
                job.DetectedStack,
                job.GeneratedImageTag);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JobCanceledException)
        {
            logger.LogInformation("Job {JobId} was canceled during processing.", jobId);
            await CleanupWorkspaceIfConfiguredAsync(job.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            var workspacePath = SafeWorkspacePath(job.Id);
            if (workspacePath is not null)
            {
                await jobLogWriter.WriteLineAsync(workspacePath, $"Job failed: {ex.Message}", CancellationToken.None);
            }

            job.Status = JobStatus.Failed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.ErrorMessage = ex.Message;
            await dbContext.SaveChangesAsync(cancellationToken);

            await CleanupWorkspaceIfConfiguredAsync(job.Id, cancellationToken);

            logger.LogError(ex, "Job {JobId} failed.", jobId);
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

    private string? SafeWorkspacePath(Guid jobId)
    {
        try
        {
            var workspaceRoot = Path.GetFullPath(_workerOptions.WorkspaceRoot);
            return Path.GetFullPath(Path.Combine(workspaceRoot, jobId.ToString("N")));
        }
        catch
        {
            return null;
        }
    }

    private async Task ThrowIfCanceledAsync(Guid jobId, string workspacePath, CancellationToken cancellationToken)
    {
        var currentStatus = await dbContext.Jobs
            .Where(x => x.Id == jobId)
            .Select(x => x.Status)
            .FirstAsync(cancellationToken);

        if (currentStatus != JobStatus.Canceled)
        {
            return;
        }

        await jobLogWriter.WriteLineAsync(workspacePath, "Job processing canceled.", cancellationToken);
        throw new JobCanceledException();
    }

    private Task CleanupWorkspaceIfConfiguredAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (!_workerOptions.CleanupWorkspaceAfterCompletion)
        {
            return Task.CompletedTask;
        }

        return jobArtifactService.CleanupWorkspaceAsync(jobId, cancellationToken);
    }

    private sealed class JobCanceledException : Exception;
}
