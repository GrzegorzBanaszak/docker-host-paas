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

        var publishedPort = await FindAvailableHostPortAsync(cancellationToken);
        var containerId = await StartContainerAsync(
            containerName,
            publishedPort,
            containerPort,
            job.GeneratedImageTag,
            cancellationToken);

        try
        {
            await WaitUntilReachableAsync(containerId, publishedPort, cancellationToken);
        }
        catch
        {
            await StopAndRemoveByNameAsync(containerName, CancellationToken.None);
            throw;
        }

        var deployedAtUtc = DateTimeOffset.UtcNow;
        var deploymentUrl = BuildDeploymentUrl(publishedPort);

        logger.LogInformation(
            "Container {ContainerName} ({ContainerId}) is reachable at {DeploymentUrl}.",
            containerName,
            containerId,
            deploymentUrl);

        return new ContainerDeploymentResult(
            containerId,
            containerName,
            containerPort,
            publishedPort,
            deploymentUrl,
            deployedAtUtc);
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
        int publishedPort,
        int containerPort,
        string imageTag,
        CancellationToken cancellationToken)
    {
        var arguments =
            $"run -d --name {containerName} -p {_options.BindingHost}:{publishedPort}:{containerPort} {imageTag}";

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

    private async Task<int> FindAvailableHostPortAsync(CancellationToken cancellationToken)
    {
        for (var port = _options.HostPortRangeStart; port <= _options.HostPortRangeEnd; port++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await IsPortFreeAsync(port, cancellationToken))
            {
                continue;
            }

            return port;
        }

        throw new InvalidOperationException(
            $"Could not find a free host port in range {_options.HostPortRangeStart}-{_options.HostPortRangeEnd}.");
    }

    private async Task<bool> IsPortFreeAsync(int port, CancellationToken cancellationToken)
    {
        using var listener = new TcpListener(System.Net.IPAddress.Parse(_options.BindingHost), port);

        try
        {
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
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

    private string BuildContainerName(Guid jobId)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.ContainerNamePrefix)
            ? "dockerizer-job"
            : _options.ContainerNamePrefix.Trim().ToLowerInvariant();

        return $"{prefix}-{jobId:N}";
    }

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
}
