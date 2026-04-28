using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Images;
using Dockerizer.Application.Jobs;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dockerizer.Infrastructure.Images;

public sealed class ImagesService(
    DockerizerDbContext dbContext,
    JobArtifactService artifactService,
    IDockerImageStore dockerImageStore) : IImagesService
{
    public async Task<IReadOnlyCollection<ImageListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.JobImages
            .AsNoTracking()
            .OrderByDescending(image => image.CreatedAtUtc)
            .Select(image => new ImageListItemDto(
                image.Id,
                image.JobId,
                image.Job.Name,
                image.Job.RepositoryUrl,
                image.Job.Branch,
                image.Status.ToString(),
                image.DetectedStack,
                image.ImageTag,
                image.ImageId,
                image.SourceCommitSha,
                image.ContainerPort,
                image.CreatedAtUtc,
                image.CompletedAtUtc,
                image.Job.CurrentImageId == image.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ImageDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var image = await dbContext.JobImages
            .AsNoTracking()
            .Include(x => x.Job)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return image is null ? null : MapDetails(image);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var image = await dbContext.JobImages
            .Include(x => x.Job)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (image is null)
        {
            return;
        }

        if (image.Job.CurrentImageId == image.Id)
        {
            throw new InvalidOperationException("Current image cannot be deleted while it is assigned to the job.");
        }

        await dockerImageStore.RemoveAsync(image.ImageId, image.ImageTag, cancellationToken);
        dbContext.JobImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<JobLogDto?> GetLogsAsync(Guid id, CancellationToken cancellationToken) =>
        artifactService.GetImageLogsAsync(id, cancellationToken);

    public Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid id, CancellationToken cancellationToken) =>
        artifactService.GetImageFilesAsync(id, cancellationToken);

    public Task<JobFileContentDto?> GetFileContentAsync(Guid id, string fileId, CancellationToken cancellationToken) =>
        artifactService.GetImageFileContentAsync(id, fileId, cancellationToken);

    private static ImageDetailsDto MapDetails(JobImage image) =>
        new(
            image.Id,
            image.JobId,
            image.Job.Name,
            image.Job.RepositoryUrl,
            image.Job.Branch,
            image.Job.Status.ToString(),
            image.Job.DeploymentUrl,
            image.Status.ToString(),
            image.DetectedStack,
            image.ImageTag,
            image.ImageId,
            image.SourceCommitSha,
            image.ContainerPort,
            image.ErrorMessage,
            image.CreatedAtUtc,
            image.StartedAtUtc,
            image.BuiltAtUtc,
            image.CompletedAtUtc,
            image.Job.CurrentImageId == image.Id);
}
