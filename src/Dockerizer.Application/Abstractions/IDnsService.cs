using Dockerizer.Application.Dns;

namespace Dockerizer.Application.Abstractions;

public interface IDnsService
{
    Task<DnsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DnsRouteDto>> GetRoutesAsync(CancellationToken cancellationToken);
    Task<DnsRouteDto?> GetRouteAsync(Guid jobId, CancellationToken cancellationToken);
}
