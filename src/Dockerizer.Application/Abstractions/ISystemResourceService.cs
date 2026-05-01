using Dockerizer.Application.System;

namespace Dockerizer.Application.Abstractions;

public interface ISystemResourceService
{
    Task<SystemResourceSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken);
}
