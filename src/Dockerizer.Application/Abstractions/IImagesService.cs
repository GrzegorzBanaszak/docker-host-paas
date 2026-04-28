using Dockerizer.Application.Images;
using Dockerizer.Application.Jobs;

namespace Dockerizer.Application.Abstractions;

public interface IImagesService
{
    Task<IReadOnlyCollection<ImageListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ImageDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<JobLogDto?> GetLogsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid id, CancellationToken cancellationToken);
    Task<JobFileContentDto?> GetFileContentAsync(Guid id, string fileId, CancellationToken cancellationToken);
}
