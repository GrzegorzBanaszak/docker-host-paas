using Dockerizer.Application.Jobs;

namespace Dockerizer.Application.Abstractions;

public interface IJobsService
{
    Task<JobDetailsDto> CreateAsync(CreateJobCommand command, CancellationToken cancellationToken);
    Task<RepositoryInspectionDto> GetRepositoryInspectionAsync(string repositoryUrl, string? projectPath, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<JobDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> StartContainerAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> RestartContainerAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> StopContainerAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> RebuildAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> RetryAsync(Guid id, CancellationToken cancellationToken);
    Task<JobDetailsDto?> CancelAsync(Guid id, CancellationToken cancellationToken);
    Task<JobLogDto?> GetLogsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JobFileDto>> GetFilesAsync(Guid id, CancellationToken cancellationToken);
    Task<JobFileContentDto?> GetFileContentAsync(Guid id, string fileId, CancellationToken cancellationToken);
}
