using Dockerizer.Application.Jobs;

namespace Dockerizer.Application.Abstractions;

public interface IJobsService
{
    Task<JobDetailsDto> CreateAsync(CreateJobCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<JobDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> RetryAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> CancelAsync(Guid id, CancellationToken cancellationToken);
    Task<JobLogDto?> GetLogsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid id, CancellationToken cancellationToken);
    Task<JobFileContentDto?> GetFileContentAsync(Guid id, string fileId, CancellationToken cancellationToken);
}
