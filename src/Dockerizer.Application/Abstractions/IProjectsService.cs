using Dockerizer.Application.Projects;

namespace Dockerizer.Application.Abstractions;

public interface IProjectsService
{
    Task<IReadOnlyCollection<ProjectListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProjectDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectDetailsDto> CreateAsync(CreateProjectCommand command, CancellationToken cancellationToken);
    Task<ProjectDetailsDto?> UpdateAsync(Guid id, UpdateProjectCommand command, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectDetailsDto?> CreateJobAsync(Guid id, CreateProjectJobCommand command, CancellationToken cancellationToken);
    Task<ProjectDetailsDto?> EnablePublicRouteAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectDetailsDto?> DisablePublicRouteAsync(Guid id, CancellationToken cancellationToken);
}
