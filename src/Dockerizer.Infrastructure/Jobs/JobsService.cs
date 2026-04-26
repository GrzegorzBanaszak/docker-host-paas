using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Jobs;
using Dockerizer.Domain;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class JobsService(
    DockerizerDbContext dbContext,
    IJobQueue jobQueue,
    JobArtifactService artifactService) : IJobsService
{
    public async Task<JobDetailsDto> CreateAsync(CreateJobCommand command, CancellationToken cancellationToken)
    {
        var job = new Job
        {
            RepositoryUrl = command.RepositoryUrl.Trim(),
            Branch = string.IsNullOrWhiteSpace(command.Branch) ? null : command.Branch.Trim(),
            Status = JobStatus.Queued,
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        await jobQueue.EnqueueAsync(job.Id, cancellationToken);

        return MapDetails(job);
    }

    public async Task<IReadOnlyCollection<JobListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Jobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAtUtc)
            .Select(job => new JobListItemDto(
                job.Id,
                job.RepositoryUrl,
                job.Branch,
                job.Status.ToString(),
                job.DetectedStack,
                job.GeneratedImageTag,
                job.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<JobDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return job is null ? null : MapDetails(job);
    }

    public async Task<JobDetailsDto?> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        if (job.Status == JobStatus.Running)
        {
            throw new InvalidOperationException("Running jobs cannot be retried.");
        }

        await artifactService.CleanupWorkspaceAsync(job.Id, cancellationToken);

        job.Status = JobStatus.Queued;
        job.DetectedStack = null;
        job.GeneratedImageTag = null;
        job.ErrorMessage = null;
        job.StartedAtUtc = null;
        job.CompletedAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await jobQueue.EnqueueAsync(job.Id, cancellationToken);

        return MapDetails(job);
    }

    public async Task<JobDetailsDto?> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        if (job.Status is JobStatus.Succeeded or JobStatus.Failed or JobStatus.Canceled)
        {
            return MapDetails(job);
        }

        job.Status = JobStatus.Canceled;
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        job.ErrorMessage ??= "Job was canceled.";

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDetails(job);
    }

    public Task<JobLogDto?> GetLogsAsync(Guid id, CancellationToken cancellationToken) =>
        artifactService.GetLogsAsync(id, cancellationToken);

    public Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid id, CancellationToken cancellationToken) =>
        artifactService.GetFilesAsync(id, cancellationToken);

    public Task<JobFileContentDto?> GetFileContentAsync(Guid id, string fileId, CancellationToken cancellationToken) =>
        artifactService.GetFileContentAsync(id, fileId, cancellationToken);

    private static JobDetailsDto MapDetails(Job job) =>
        new(
            job.Id,
            job.RepositoryUrl,
            job.Branch,
            job.Status.ToString(),
            job.DetectedStack,
            job.GeneratedImageTag,
            job.ErrorMessage,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);
}
