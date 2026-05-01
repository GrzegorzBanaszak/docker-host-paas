using System.Diagnostics;
using System.Net.Sockets;
using Dockerizer.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Containers;

public sealed class DockerContainerRuntime(
    IOptions<DockerRuntimeOptions> options,
    ILogger<DockerContainerRuntime> logger) : IDockerContainerRuntime
{
    private readonly DockerRuntimeOptions _options = options.Value;

    public async Task<ContainerDeploymentResult> RunAsync(Job job, int containerPort, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(job.GeneratedImageTag))
        {
            throw new InvalidOperationException("Docker image tag is required before running a container.");
        }

        var containerName = BuildContainerName(job.Id);
        await StopAndRemoveByNameAsync(containerName, CancellationToken.None);

        var containerId = await StartContainerAsync(
            containerName,
            containerPort,
            job.GeneratedImageTag,
            cancellationToken);

        try
        {
            var publishedPort = await ResolvePublishedPortAsync(containerName, containerPort, cancellationToken);
            await WaitUntilReachableAsync(containerId, publishedPort, cancellationToken);
            return BuildDeploymentResult(containerId, containerName, containerPort, publishedPort);
        }
        catch
        {
            await StopAndRemoveByNameAsync(containerName, CancellationToken.None);
            throw;
        }
    }

    public async Task<ContainerDeploymentResult> StartAsync(Job job, int containerPort, CancellationToken cancellationToken)
    {
        EnsureContainerCanBeManaged(job, containerPort);

        var inspection = await InspectExistingContainerAsync(job, containerPort, cancellationToken);
        if (inspection is null)
        {
            return await RunAsync(job, containerPort, cancellationToken);
        }

        if (inspection.Status == "running")
        {
            return BuildDeploymentResult(
                inspection.ContainerId!,
                inspection.ContainerName!,
                containerPort,
                inspection.PublishedPort!.Value);
        }

        var result = await RunDockerCommandAsync($"start {inspection.ContainerId}", cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker start failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        var publishedPort = inspection.PublishedPort ?? await ResolvePublishedPortAsync(inspection.ContainerName!, containerPort, cancellationToken);
        await WaitUntilReachableAsync(inspection.ContainerId!, publishedPort, cancellationToken);
        return BuildDeploymentResult(inspection.ContainerId!, inspection.ContainerName!, containerPort, publishedPort);
    }

    public async Task<ContainerDeploymentResult> RestartAsync(Job job, int containerPort, CancellationToken cancellationToken)
    {
        EnsureContainerCanBeManaged(job, containerPort);

        var inspection = await InspectExistingContainerAsync(job, containerPort, cancellationToken);
        if (inspection is null)
        {
            return await RunAsync(job, containerPort, cancellationToken);
        }

        var result = await RunDockerCommandAsync($"restart {inspection.ContainerId}", cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker restart failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        var publishedPort = inspection.PublishedPort ?? await ResolvePublishedPortAsync(inspection.ContainerName!, containerPort, cancellationToken);
        await WaitUntilReachableAsync(inspection.ContainerId!, publishedPort, cancellationToken);
        return BuildDeploymentResult(inspection.ContainerId!, inspection.ContainerName!, containerPort, publishedPort);
    }

    public async Task StopAsync(Job job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inspection = await InspectExistingContainerAsync(job, job.ContainerPort, cancellationToken);
        if (inspection is null || inspection.Status is "exited" or "dead")
        {
            return;
        }

        var result = await RunDockerCommandAsync($"stop {inspection.ContainerId}", cancellationToken);
        if (result.ExitCode == 0)
        {
            return;
        }

        var error = (result.StandardError ?? string.Empty).Trim();
        if (error.Contains("No such container", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException($"docker stop failed with exit code {result.ExitCode}: {error}");
    }

    public async Task<ContainerRuntimeStatus> GetStatusAsync(Job job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inspection = await InspectExistingContainerAsync(job, job.ContainerPort, cancellationToken);
        if (inspection is null)
        {
            return new ContainerRuntimeStatus(
                "not_found",
                job.ContainerId,
                job.ContainerName,
                job.PublishedPort,
                job.PublishedPort.HasValue ? BuildDeploymentUrl(job.PublishedPort.Value) : job.DeploymentUrl);
        }

        return new ContainerRuntimeStatus(
            inspection.Status,
            inspection.ContainerId,
            inspection.ContainerName,
            inspection.PublishedPort,
            inspection.PublishedPort.HasValue ? BuildDeploymentUrl(inspection.PublishedPort.Value) : null);
    }

    public async Task StopAndRemoveAsync(Job job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var containerName = string.IsNullOrWhiteSpace(job.ContainerName)
            ? BuildContainerName(job.Id)
            : job.ContainerName;

        if (!string.IsNullOrWhiteSpace(job.ContainerId))
        {
            await StopAndRemoveTargetAsync(job.ContainerId, CancellationToken.None);
        }

        await StopAndRemoveByNameAsync(containerName, CancellationToken.None);
    }

    private async Task<string> StartContainerAsync(
        string containerName,
        int containerPort,
        string imageTag,
        CancellationToken cancellationToken)
    {
        var arguments =
            $"run -d --name {containerName} {BuildRunSecurityArguments()} -p {_options.BindingHost}::{containerPort} {imageTag}";

        var result = await RunDockerCommandAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker run failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        var containerId = result.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new InvalidOperationException("docker run did not return a container id.");
        }

        return containerId;
    }

    private async Task WaitUntilReachableAsync(string containerId, int publishedPort, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.StartupTimeoutSeconds);
        var pollInterval = TimeSpan.FromMilliseconds(_options.StartupPollIntervalMilliseconds);
        var deadlineUtc = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadlineUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await IsTcpPortReachableAsync(_options.BindingHost, publishedPort, cancellationToken))
            {
                return;
            }

            var status = await GetContainerStatusAsync(containerId, cancellationToken);
            if (status is "exited" or "dead")
            {
                var logs = await TryGetContainerLogsAsync(containerId, cancellationToken);
                throw new InvalidOperationException(
                    $"Container exited before it became reachable on port {publishedPort}. Logs: {logs}");
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        var finalLogs = await TryGetContainerLogsAsync(containerId, cancellationToken);
        throw new TimeoutException(
            $"Container did not become reachable on port {publishedPort} within {_options.StartupTimeoutSeconds} seconds. Logs: {finalLogs}");
    }

    private async Task<bool> IsTcpPortReachableAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(1));

        try
        {
            await tcpClient.ConnectAsync(host, port, timeoutCts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetContainerStatusAsync(string containerId, CancellationToken cancellationToken)
    {
        var result = await RunDockerCommandAsync(
            $"inspect -f \"{{{{.State.Status}}}}\" {containerId}",
            cancellationToken);

        return result.ExitCode == 0
            ? result.StandardOutput.Trim().ToLowerInvariant()
            : string.Empty;
    }

    private async Task<string> TryGetContainerLogsAsync(string containerId, CancellationToken cancellationToken)
    {
        var result = await RunDockerCommandAsync($"logs {containerId}", cancellationToken);
        var output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;

        if (string.IsNullOrWhiteSpace(output))
        {
            return "no container logs available";
        }

        output = output.Trim();
        return output.Length <= 1500 ? output : output[..1500];
    }

    private async Task<ContainerInspection?> InspectExistingContainerAsync(Job job, int? containerPort, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var target = !string.IsNullOrWhiteSpace(job.ContainerId)
            ? job.ContainerId
            : !string.IsNullOrWhiteSpace(job.ContainerName)
                ? job.ContainerName
                : BuildContainerName(job.Id);

        var inspectResult = await RunDockerCommandAsync(
            $"inspect -f \"{{{{.Id}}}}|{{{{.Name}}}}|{{{{.State.Status}}}}\" {target}",
            cancellationToken);

        if (inspectResult.ExitCode != 0)
        {
            var error = (inspectResult.StandardError ?? string.Empty).Trim();
            if (error.Contains("No such object", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("No such container", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"docker inspect failed with exit code {inspectResult.ExitCode}: {error}");
        }

        var parts = inspectResult.StandardOutput.Trim().Split('|', 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("docker inspect returned an unexpected container metadata format.");
        }

        int? publishedPort = null;
        if (containerPort.HasValue)
        {
            publishedPort = await TryResolvePublishedPortAsync(parts[1], containerPort.Value, cancellationToken);
        }

        return new ContainerInspection(
            parts[0],
            parts[1].TrimStart('/'),
            parts[2].ToLowerInvariant(),
            publishedPort);
    }

    private async Task<int> ResolvePublishedPortAsync(string containerName, int containerPort, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.StartupTimeoutSeconds);
        var pollInterval = TimeSpan.FromMilliseconds(_options.StartupPollIntervalMilliseconds);
        var deadlineUtc = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadlineUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var publishedPort = await TryResolvePublishedPortAsync(containerName, containerPort, cancellationToken);
            if (publishedPort.HasValue)
            {
                return publishedPort.Value;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Could not determine published port for container {containerName} and internal port {containerPort}.");
    }

    private async Task<int?> TryResolvePublishedPortAsync(string containerName, int containerPort, CancellationToken cancellationToken)
    {
        var result = await RunDockerCommandAsync($"port {containerName} {containerPort}/tcp", cancellationToken);
        if (result.ExitCode != 0)
        {
            var error = (result.StandardError ?? string.Empty).Trim();
            if (error.Contains("No public port", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("No such container", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"docker port failed with exit code {result.ExitCode}: {error}");
        }

        var line = result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var lastColonIndex = line.LastIndexOf(':');
        if (lastColonIndex < 0)
        {
            return null;
        }

        return int.TryParse(line[(lastColonIndex + 1)..].Trim(), out var port)
            ? port
            : null;
    }

    private async Task StopAndRemoveByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        await StopAndRemoveTargetAsync(containerName, cancellationToken);
    }

    private async Task StopAndRemoveTargetAsync(string target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        var result = await RunDockerCommandAsync($"rm -f {target}", cancellationToken);
        if (result.ExitCode == 0)
        {
            logger.LogInformation("Removed container {Target}.", target);
            return;
        }

        var error = (result.StandardError ?? string.Empty).Trim();
        if (error.Contains("No such container", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogWarning(
            "Failed to remove container {Target}. Exit code: {ExitCode}. Error: {Error}",
            target,
            result.ExitCode,
            error);
    }

    private void EnsureContainerCanBeManaged(Job job, int containerPort)
    {
        if (string.IsNullOrWhiteSpace(job.GeneratedImageTag))
        {
            throw new InvalidOperationException("Docker image tag is required before container management actions.");
        }

        if (containerPort <= 0)
        {
            throw new InvalidOperationException("Container port is required before container management actions.");
        }
    }

    private string BuildContainerName(Guid jobId)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.ContainerNamePrefix)
            ? "dockerizer-job"
            : _options.ContainerNamePrefix.Trim().ToLowerInvariant();

        return $"{prefix}-{jobId:N}";
    }

    private string BuildRunSecurityArguments()
    {
        var arguments = new List<string>
        {
            "--security-opt no-new-privileges",
            "--label dockerizer.managed=true"
        };

        if (!string.IsNullOrWhiteSpace(_options.ContainerCpuLimit))
        {
            arguments.Add($"--cpus {Quote(_options.ContainerCpuLimit)}");
        }

        if (!string.IsNullOrWhiteSpace(_options.ContainerMemoryLimit))
        {
            arguments.Add($"--memory {Quote(_options.ContainerMemoryLimit)}");
        }

        if (_options.ContainerPidsLimit > 0)
        {
            arguments.Add($"--pids-limit {_options.ContainerPidsLimit}");
        }

        if (_options.DisableContainerNetwork)
        {
            arguments.Add("--network none");
        }

        return string.Join(' ', arguments);
    }

    private static string Quote(string value) => $"\"{value.Trim()}\"";

    private string BuildDeploymentUrl(int publishedPort)
    {
        if (!Uri.TryCreate(_options.PublicBaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("DockerRuntime:PublicBaseUrl must be a valid absolute URI.");
        }

        var builder = new UriBuilder(baseUri)
        {
            Port = publishedPort,
        };

        return builder.Uri.ToString().TrimEnd('/');
    }

    private ContainerDeploymentResult BuildDeploymentResult(
        string containerId,
        string containerName,
        int containerPort,
        int publishedPort) =>
        new(
            containerId,
            containerName,
            containerPort,
            publishedPort,
            BuildDeploymentUrl(publishedPort),
            DateTimeOffset.UtcNow);

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
            throw new InvalidOperationException($"Failed to start docker process for arguments: {arguments}");
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

    private sealed record ContainerInspection(
        string ContainerId,
        string ContainerName,
        string Status,
        int? PublishedPort);
}
