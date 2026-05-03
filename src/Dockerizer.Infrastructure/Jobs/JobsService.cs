using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Images;
using Dockerizer.Application.Jobs;
using Dockerizer.Domain;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class JobsService(
    DockerizerDbContext dbContext,
    IJobQueue jobQueue,
    JobArtifactService artifactService,
    IDockerContainerRuntime dockerContainerRuntime,
    IOptions<ApplicationRoutingOptions> applicationRoutingOptions,
    RepositoryInspectionService repositoryInspectionService,
    RepositoryProjectPathResolver repositoryProjectPathResolver) : IJobsService
{
    private readonly ApplicationRoutingOptions _applicationRoutingOptions = applicationRoutingOptions.Value;

    public async Task<JobDetailsDto> CreateAsync(CreateJobCommand command, CancellationToken cancellationToken)
    {
        var job = new Job
        {
            Name = command.Name.Trim(),
            RepositoryUrl = command.RepositoryUrl.Trim(),
            Branch = string.IsNullOrWhiteSpace(command.Branch) ? null : command.Branch.Trim(),
            ProjectPath = string.IsNullOrWhiteSpace(command.ProjectPath) ? null : repositoryProjectPathResolver.Normalize(command.ProjectPath),
            Status = JobStatus.Queued,
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        await jobQueue.EnqueueAsync(job.Id, cancellationToken);

        return MapDetails(job);
    }

    public Task<RepositoryInspectionDto> GetRepositoryInspectionAsync(string repositoryUrl, string? projectPath, CancellationToken cancellationToken) =>
        repositoryInspectionService.InspectAsync(repositoryUrl.Trim(), projectPath, cancellationToken);

    public async Task<IReadOnlyCollection<JobListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var jobs = await dbContext.Jobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = new List<JobListItemDto>(jobs.Count);
        foreach (var job in jobs)
        {
            var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
            items.Add(new JobListItemDto(
                job.Id,
                job.Name,
                job.RepositoryUrl,
                job.Branch,
                job.ProjectPath,
                job.Status.ToString(),
                job.DetectedStack,
                job.GeneratedImageTag,
                containerStatus.ContainerName ?? job.ContainerName,
                containerStatus.Status,
                job.ContainerPort,
                containerStatus.PublishedPort ?? job.PublishedPort,
                job.PublicAccessEnabled,
                containerStatus.PublicHostname ?? job.PublicHostname,
                containerStatus.RouteStatus ?? job.RouteStatus,
                containerStatus.DeploymentUrl ?? job.DeploymentUrl,
                job.DeployedAtUtc,
                job.CreatedAtUtc));
        }

        return items;
    }

    public async Task<JobDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (job is null)
        {
            return null;
        }

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public async Task<JobDetailsDto?> StartContainerAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        if (!job.ContainerPort.HasValue)
        {
            throw new InvalidOperationException("Container port is not available for this job.");
        }

        var deployment = await dockerContainerRuntime.StartAsync(job, job.ContainerPort.Value, cancellationToken);
        ApplyDeployment(job, deployment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public async Task<JobDetailsDto?> RestartContainerAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        if (!job.ContainerPort.HasValue)
        {
            throw new InvalidOperationException("Container port is not available for this job.");
        }

        var deployment = await dockerContainerRuntime.RestartAsync(job, job.ContainerPort.Value, cancellationToken);
        ApplyDeployment(job, deployment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public async Task<JobDetailsDto?> StopContainerAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        await dockerContainerRuntime.StopAsync(job, cancellationToken);
        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public async Task<JobDetailsDto?> EnablePublicRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureTunnelRoutingIsConfigured();

        var job = await dbContext.Jobs
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        EnsureJobCanBeRouted(job);

        job.PublicAccessEnabled = true;
        job.PublicHostname = BuildPublicHostname(job);
        job.RouteStatus = null;
        job.DeploymentUrl = null;
        job.DnsRecordId = null;

        var deployment = await dockerContainerRuntime.RunAsync(job, job.ContainerPort!.Value, cancellationToken);
        ApplyDeployment(job, deployment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public async Task<JobDetailsDto?> DisablePublicRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        job.PublicAccessEnabled = false;
        job.PublicHostname = null;
        job.DeploymentUrl = null;
        job.RouteStatus = null;
        job.DnsRecordId = null;

        if (!string.IsNullOrWhiteSpace(job.GeneratedImageTag) && job.ContainerPort.HasValue)
        {
            var deployment = await dockerContainerRuntime.RunAsync(job, job.ContainerPort.Value, cancellationToken);
            ApplyDeployment(job, deployment);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public Task<JobDetailsDto?> RebuildAsync(Guid id, CancellationToken cancellationToken) =>
        RetryAsync(id, cancellationToken);

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

        await dockerContainerRuntime.StopAndRemoveAsync(job, cancellationToken);
        await artifactService.CleanupWorkspaceAsync(job.Id, cancellationToken);

        job.Status = JobStatus.Queued;
        job.CurrentImageId = null;
        job.DetectedStack = null;
        job.GeneratedImageTag = null;
        job.ImageId = null;
        job.ContainerId = null;
        job.ContainerName = null;
        job.ContainerPort = null;
        job.PublishedPort = null;
        job.PublicHostname = null;
        job.DeploymentUrl = null;
        job.RouteStatus = null;
        job.DnsRecordId = null;
        job.ErrorMessage = null;
        job.StartedAtUtc = null;
        job.DeployedAtUtc = null;
        job.CompletedAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await jobQueue.EnqueueAsync(job.Id, cancellationToken);

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public async Task<JobDetailsDto?> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        if (job.Status is JobStatus.Failed or JobStatus.Canceled)
        {
            return MapDetails(job);
        }

        await dockerContainerRuntime.StopAndRemoveAsync(job, cancellationToken);

        job.Status = JobStatus.Canceled;
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        job.ErrorMessage ??= "Job was canceled.";
        job.ContainerId = null;
        job.ContainerName = null;
        job.ContainerPort = null;
        job.PublishedPort = null;
        job.PublicHostname = null;
        job.DeploymentUrl = null;
        job.RouteStatus = null;
        job.DnsRecordId = null;
        job.DeployedAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapDetails(job, containerStatus);
    }

    public Task<JobLogDto?> GetLogsAsync(Guid id, CancellationToken cancellationToken) =>
        GetCurrentImageLogsAsync(id, cancellationToken);

    public Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid id, CancellationToken cancellationToken) =>
        GetCurrentImageFilesAsync(id, cancellationToken);

    public Task<JobFileContentDto?> GetFileContentAsync(Guid id, string fileId, CancellationToken cancellationToken) =>
        GetCurrentImageFileContentAsync(id, fileId, cancellationToken);

    private async Task<JobLogDto?> GetCurrentImageLogsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var currentImageId = await dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.CurrentImageId)
            .FirstOrDefaultAsync(cancellationToken);

        return currentImageId.HasValue
            ? await artifactService.GetImageLogsAsync(currentImageId.Value, cancellationToken)
            : await artifactService.GetLogsAsync(jobId, cancellationToken);
    }

    private async Task<IReadOnlyCollection<JobFileDto>> GetCurrentImageFilesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var currentImageId = await dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.CurrentImageId)
            .FirstOrDefaultAsync(cancellationToken);

        return currentImageId.HasValue
            ? await artifactService.GetImageFilesAsync(currentImageId.Value, cancellationToken)
            : await artifactService.GetFilesAsync(jobId, cancellationToken);
    }

    private async Task<JobFileContentDto?> GetCurrentImageFileContentAsync(Guid jobId, string fileId, CancellationToken cancellationToken)
    {
        var currentImageId = await dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.CurrentImageId)
            .FirstOrDefaultAsync(cancellationToken);

        return currentImageId.HasValue
            ? await artifactService.GetImageFileContentAsync(currentImageId.Value, fileId, cancellationToken)
            : await artifactService.GetFileContentAsync(jobId, fileId, cancellationToken);
    }

    private static void ApplyDeployment(Job job, ContainerDeploymentResult deployment)
    {
        job.ContainerId = deployment.ContainerId;
        job.ContainerName = deployment.ContainerName;
        job.ContainerPort = deployment.ContainerPort;
        job.PublishedPort = deployment.PublishedPort;
        job.PublicAccessEnabled = deployment.PublicAccessEnabled;
        job.PublicHostname = deployment.PublicHostname;
        job.DeploymentUrl = deployment.DeploymentUrl;
        job.RouteStatus = deployment.RouteStatus;
        job.DeployedAtUtc = deployment.DeployedAtUtc;
    }

    private static JobDetailsDto MapDetails(Job job, ContainerRuntimeStatus? containerStatus = null) =>
        new(
            job.Id,
            job.Name,
            job.RepositoryUrl,
            job.Branch,
            job.ProjectPath,
            job.Status.ToString(),
            job.DetectedStack,
            job.GeneratedImageTag,
            job.ImageId,
            job.ContainerId,
            job.ContainerName,
            containerStatus?.Status,
            job.ContainerPort,
            job.PublishedPort,
            job.PublicAccessEnabled,
            containerStatus?.PublicHostname ?? job.PublicHostname,
            containerStatus?.RouteStatus ?? job.RouteStatus,
            containerStatus?.DeploymentUrl ?? job.DeploymentUrl,
            job.ErrorMessage,
            job.CurrentImageId,
            job.Images
                .OrderByDescending(image => image.CreatedAtUtc)
                .Select(image => MapImageSummary(image, job.CurrentImageId))
                .FirstOrDefault(image => image.IsCurrent),
            job.Images
                .OrderByDescending(image => image.CreatedAtUtc)
                .Select(image => MapImageSummary(image, job.CurrentImageId))
                .ToList(),
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.DeployedAtUtc,
            job.CompletedAtUtc);

    private void EnsureTunnelRoutingIsConfigured()
    {
        if (!_applicationRoutingOptions.UsesTunnelWildcard)
        {
            throw new InvalidOperationException("Public DNS routes require ApplicationRouting:Mode=TunnelWildcard.");
        }

        if (string.IsNullOrWhiteSpace(_applicationRoutingOptions.BaseDomain))
        {
            throw new InvalidOperationException("ApplicationRouting:BaseDomain is required before publishing DNS routes.");
        }
    }

    private static void EnsureJobCanBeRouted(Job job)
    {
        if (string.IsNullOrWhiteSpace(job.GeneratedImageTag))
        {
            throw new InvalidOperationException("Job must have a built image before public access can be enabled.");
        }

        if (!job.ContainerPort.HasValue)
        {
            throw new InvalidOperationException("Job must have a resolved container port before public access can be enabled.");
        }
    }

    private string BuildPublicHostname(Job job)
    {
        var baseDomain = _applicationRoutingOptions.BaseDomain.Trim().TrimStart('.').TrimEnd('.').ToLowerInvariant();
        return $"{BuildSlug(job)}.{baseDomain}";
    }

    private static string BuildSlug(Job job)
    {
        var source = string.IsNullOrWhiteSpace(job.Name)
            ? "app"
            : job.Name.Trim().ToLowerInvariant();
        var builder = new System.Text.StringBuilder(source.Length);
        var lastWasHyphen = false;

        foreach (var character in source)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
                lastWasHyphen = false;
                continue;
            }

            if (!lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "app";
        }

        if (slug.Length > 48)
        {
            slug = slug[..48].Trim('-');
        }

        return $"{slug}-{job.Id.ToString("N")[..8]}";
    }

    private static JobImageSummaryDto MapImageSummary(JobImage image, Guid? currentImageId) =>
        new(
            image.Id,
            image.Status.ToString(),
            image.DetectedStack,
            image.ImageTag,
            image.ImageId,
            image.SourceCommitSha,
            image.ContainerPort,
            image.CreatedAtUtc,
            image.CompletedAtUtc,
            currentImageId == image.Id);
}
