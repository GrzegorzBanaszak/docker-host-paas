using System.Diagnostics;
using System.Text.Json;
using Dockerizer.Application.Abstractions;
using Dockerizer.Application.System;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Containers;

public sealed class DockerSystemResourceService(IOptions<DockerRuntimeOptions> options) : ISystemResourceService
{
    private readonly DockerRuntimeOptions _options = options.Value;

    public async Task<SystemResourceSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var result = await RunDockerCommandAsync(
            "stats --no-stream --format \"{{json .}}\"",
            cancellationToken);

        if (result.ExitCode != 0)
        {
            return BuildUnavailableSnapshot(result.StandardError.Trim());
        }

        var containers = new List<ContainerResourceUsageDto>();
        foreach (var line in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var usage = TryParseContainerStats(line);
            if (usage is not null && IsDockerizerContainer(usage.Name))
            {
                containers.Add(usage);
            }
        }

        return new SystemResourceSnapshotDto(
            "available",
            null,
            FormatLimit(_options.ContainerCpuLimit, "unlimited"),
            FormatLimit(_options.ContainerMemoryLimit, "unlimited"),
            _options.ContainerPidsLimit > 0 ? _options.ContainerPidsLimit.ToString() : "unlimited",
            _options.DisableContainerNetwork,
            containers.OrderBy(x => x.Name).ToList());
    }

    private SystemResourceSnapshotDto BuildUnavailableSnapshot(string errorMessage) =>
        new(
            "unavailable",
            string.IsNullOrWhiteSpace(errorMessage) ? "Docker resource usage is unavailable." : errorMessage,
            FormatLimit(_options.ContainerCpuLimit, "unlimited"),
            FormatLimit(_options.ContainerMemoryLimit, "unlimited"),
            _options.ContainerPidsLimit > 0 ? _options.ContainerPidsLimit.ToString() : "unlimited",
            _options.DisableContainerNetwork,
            []);

    private bool IsDockerizerContainer(string name) =>
        name.StartsWith(_options.ContainerNamePrefix, StringComparison.OrdinalIgnoreCase);

    private static ContainerResourceUsageDto? TryParseContainerStats(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            return new ContainerResourceUsageDto(
                ReadString(root, "Container"),
                ReadString(root, "Name"),
                ReadString(root, "CPUPerc"),
                ReadString(root, "MemUsage"),
                ReadString(root, "MemPerc"),
                ReadString(root, "NetIO"),
                ReadString(root, "BlockIO"),
                ReadString(root, "PIDs"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string FormatLimit(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static async Task<DockerCommandResult> RunDockerCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            return new DockerCommandResult(1, string.Empty, "Failed to start docker process.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new DockerCommandResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }

    private sealed record DockerCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
