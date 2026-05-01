namespace Dockerizer.Application.System;

public sealed record ContainerResourceUsageDto(
    string ContainerId,
    string Name,
    string CpuPercent,
    string MemoryUsage,
    string MemoryPercent,
    string NetworkIo,
    string BlockIo,
    string Pids);
