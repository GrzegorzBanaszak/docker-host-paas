namespace Dockerizer.Infrastructure.Jobs;

public interface IRepositoryBranchProvider
{
    Task<IReadOnlyCollection<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken);
}
