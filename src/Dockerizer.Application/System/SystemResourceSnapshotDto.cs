namespace Dockerizer.Application.System;

public sealed record SystemResourceSnapshotDto(
    string Status,
    string? ErrorMessage,
    string CpuLimit,
    string MemoryLimit,
    string PidsLimit,
    bool NetworkDisabled,
    IReadOnlyCollection<ContainerResourceUsageDto> Containers);
