using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Images;
using Dockerizer.Application.Jobs;
using Dockerizer.Application.Projects;
using Dockerizer.Domain;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Jobs;
using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dockerizer.Infrastructure.Projects;

public sealed class ProjectsService(
    DockerizerDbContext dbContext,
    IJobQueue jobQueue,
    IJobsService jobsService,
    IDockerContainerRuntime dockerContainerRuntime,
    RepositoryProjectPathResolver repositoryProjectPathResolver) : IProjectsService
{
    public async Task<IReadOnlyCollection<ProjectListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .AsNoTracking()
            .Include(project => project.CurrentJob)
            .Include(project => project.Jobs)
            .OrderByDescending(project => project.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = new List<ProjectListItemDto>(projects.Count);
        foreach (var project in projects)
        {
            var currentJob = project.CurrentJob ?? project.Jobs
                .OrderByDescending(job => job.CreatedAtUtc)
                .FirstOrDefault(job => job.Status == JobStatus.Succeeded);

            var containerStatus = currentJob is null
                ? null
                : await dockerContainerRuntime.GetStatusAsync(currentJob, cancellationToken);

            items.Add(MapListItem(project, currentJob, containerStatus));
        }

        return items;
    }

    public async Task<ProjectDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Include(project => project.CurrentJob)
            .Include(project => project.CurrentImage)
            .Include(project => project.Jobs)
                .ThenInclude(job => job.Images)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var currentJob = project.CurrentJob ?? project.Jobs
            .OrderByDescending(job => job.CreatedAtUtc)
            .FirstOrDefault(job => job.Id == project.CurrentJobId);

        var containerStatus = currentJob is null
            ? null
            : await dockerContainerRuntime.GetStatusAsync(currentJob, cancellationToken);

        return MapDetails(project, currentJob, containerStatus);
    }

    public async Task<ProjectDetailsDto> CreateAsync(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = command.Name.Trim(),
            RepositoryUrl = command.RepositoryUrl.Trim(),
            DefaultBranch = NormalizeNullable(command.DefaultBranch),
            DefaultProjectPath = string.IsNullOrWhiteSpace(command.DefaultProjectPath)
                ? null
                : repositoryProjectPathResolver.Normalize(command.DefaultProjectPath),
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(project.Id, cancellationToken))!;
    }

    public async Task<ProjectDetailsDto?> UpdateAsync(Guid id, UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        project.Name = command.Name.Trim();
        project.RepositoryUrl = command.RepositoryUrl.Trim();
        project.DefaultBranch = NormalizeNullable(command.DefaultBranch);
        project.DefaultProjectPath = string.IsNullOrWhiteSpace(command.DefaultProjectPath)
            ? null
            : repositoryProjectPathResolver.Normalize(command.DefaultProjectPath);
        project.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(project => project.CurrentJob)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        if (project is null)
        {
            return false;
        }

        if (project.CurrentJob is not null)
        {
            await dockerContainerRuntime.StopAndRemoveAsync(project.CurrentJob, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        project.ArchivedAtUtc = now;
        project.UpdatedAtUtc = now;
        project.PublicAccessEnabled = false;
        project.PublicHostname = null;
        project.DeploymentUrl = null;
        project.RouteStatus = "archived";

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProjectDetailsDto?> CreateJobAsync(Guid id, CreateProjectJobCommand command, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var job = new Job
        {
            ProjectId = project.Id,
            Name = string.IsNullOrWhiteSpace(command.Name) ? project.Name : command.Name.Trim(),
            RepositoryUrl = project.RepositoryUrl,
            Branch = string.IsNullOrWhiteSpace(command.Branch) ? project.DefaultBranch : command.Branch.Trim(),
            ProjectPath = string.IsNullOrWhiteSpace(command.ProjectPath)
                ? project.DefaultProjectPath
                : repositoryProjectPathResolver.Normalize(command.ProjectPath),
            PublicAccessEnabled = project.PublicAccessEnabled,
            Status = JobStatus.Queued,
        };

        dbContext.Jobs.Add(job);
        project.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await jobQueue.EnqueueAsync(job.Id, cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDetailsDto?> EnablePublicRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (!project.CurrentJobId.HasValue)
        {
            throw new InvalidOperationException("Project must have a current successful job before public access can be enabled.");
        }

        await jobsService.EnablePublicRouteAsync(project.CurrentJobId.Value, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProjectDetailsDto?> DisablePublicRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (project.CurrentJobId.HasValue)
        {
            await jobsService.DisablePublicRouteAsync(project.CurrentJobId.Value, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    private static ProjectListItemDto MapListItem(Project project, Job? currentJob, ContainerRuntimeStatus? containerStatus) =>
        new(
            project.Id,
            project.Name,
            project.RepositoryUrl,
            project.DefaultBranch,
            project.DefaultProjectPath,
            project.CurrentJobId,
            project.CurrentImageId,
            currentJob?.Status.ToString(),
            currentJob?.DetectedStack,
            currentJob?.GeneratedImageTag,
            containerStatus?.ContainerName ?? currentJob?.ContainerName,
            containerStatus?.Status,
            currentJob?.ContainerPort,
            containerStatus?.PublishedPort ?? currentJob?.PublishedPort,
            project.PublicAccessEnabled,
            containerStatus?.PublicHostname ?? project.PublicHostname,
            containerStatus?.RouteStatus ?? project.RouteStatus,
            containerStatus?.DeploymentUrl ?? project.DeploymentUrl,
            currentJob?.DeployedAtUtc,
            project.Jobs.Count,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            project.ArchivedAtUtc);

    private static ProjectDetailsDto MapDetails(Project project, Job? currentJob, ContainerRuntimeStatus? containerStatus)
    {
        var images = project.Jobs
            .SelectMany(job => job.Images)
            .OrderByDescending(image => image.CreatedAtUtc)
            .Select(image => MapImageSummary(image, project.CurrentImageId))
            .ToList();

        return new ProjectDetailsDto(
            project.Id,
            project.Name,
            project.RepositoryUrl,
            project.DefaultBranch,
            project.DefaultProjectPath,
            project.CurrentJobId,
            project.CurrentImageId,
            currentJob?.Status.ToString(),
            currentJob?.DetectedStack,
            currentJob?.GeneratedImageTag,
            containerStatus?.ContainerName ?? currentJob?.ContainerName,
            containerStatus?.Status,
            currentJob?.ContainerPort,
            containerStatus?.PublishedPort ?? currentJob?.PublishedPort,
            project.PublicAccessEnabled,
            containerStatus?.PublicHostname ?? project.PublicHostname,
            containerStatus?.RouteStatus ?? project.RouteStatus,
            containerStatus?.DeploymentUrl ?? project.DeploymentUrl,
            currentJob?.DeployedAtUtc,
            images.FirstOrDefault(image => image.IsCurrent),
            project.Jobs
                .OrderByDescending(job => job.CreatedAtUtc)
                .Select(job => MapJobListItem(project, job))
                .ToList(),
            images,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            project.ArchivedAtUtc);
    }

    private static JobListItemDto MapJobListItem(Project project, Job job) =>
        new(
            job.Id,
            project.Id,
            project.Name,
            job.Name,
            job.RepositoryUrl,
            job.Branch,
            job.ProjectPath,
            job.Status.ToString(),
            job.DetectedStack,
            job.GeneratedImageTag,
            job.ContainerName,
            null,
            job.ContainerPort,
            job.PublishedPort,
            job.PublicAccessEnabled,
            job.PublicHostname,
            job.RouteStatus,
            job.DeploymentUrl,
            job.DeployedAtUtc,
            job.CreatedAtUtc);

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

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
