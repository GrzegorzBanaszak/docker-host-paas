using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Dockerizer.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Containers;

public sealed class DockerContainerRuntime(
    IOptions<DockerRuntimeOptions> options,
    IOptions<ApplicationRoutingOptions> routingOptions,
    ILogger<DockerContainerRuntime> logger) : IDockerContainerRuntime
{
    private readonly DockerRuntimeOptions _options = options.Value;
    private readonly ApplicationRoutingOptions _routingOptions = routingOptions.Value;

    public async Task<ContainerDeploymentResult> RunAsync(Job job, int containerPort, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(job.GeneratedImageTag))
        {
            throw new InvalidOperationException("Docker image tag is required before running a container.");
        }

        var containerName = BuildContainerName(job.Id);
        await StopAndRemoveByNameAsync(containerName, CancellationToken.None);

        var containerId = await StartContainerAsync(
            job,
            containerName,
            containerPort,
            job.GeneratedImageTag,
            cancellationToken);

        try
        {
            if (ShouldPublishHostPort(job))
            {
                var publishedPort = await ResolvePublishedPortAsync(containerName, containerPort, cancellationToken);
                await WaitUntilReachableAsync(containerId, publishedPort, cancellationToken);
                return BuildDeploymentResult(job, containerId, containerName, containerPort, publishedPort);
            }

            await WaitUntilRunningAsync(containerId, cancellationToken);
            return BuildDeploymentResult(job, containerId, containerName, containerPort, null);
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
            if (ShouldPublishHostPort(job) && !inspection.PublishedPort.HasValue)
            {
                return await RunAsync(job, containerPort, cancellationToken);
            }

            int? publishedPort = ShouldPublishHostPort(job)
                ? inspection.PublishedPort ?? await ResolvePublishedPortAsync(inspection.ContainerName!, containerPort, cancellationToken)
                : null;

            return BuildDeploymentResult(
                job,
                inspection.ContainerId!,
                inspection.ContainerName!,
                containerPort,
                publishedPort);
        }

        var result = await RunDockerCommandAsync($"start {inspection.ContainerId}", cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker start failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        if (ShouldPublishHostPort(job))
        {
            if (!inspection.PublishedPort.HasValue)
            {
                return await RunAsync(job, containerPort, cancellationToken);
            }

            var publishedPort = inspection.PublishedPort.Value;
            await WaitUntilReachableAsync(inspection.ContainerId!, publishedPort, cancellationToken);
            return BuildDeploymentResult(job, inspection.ContainerId!, inspection.ContainerName!, containerPort, publishedPort);
        }

        await WaitUntilRunningAsync(inspection.ContainerId!, cancellationToken);
        return BuildDeploymentResult(job, inspection.ContainerId!, inspection.ContainerName!, containerPort, null);
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

        if (ShouldPublishHostPort(job))
        {
            if (!inspection.PublishedPort.HasValue)
            {
                return await RunAsync(job, containerPort, cancellationToken);
            }

            var publishedPort = inspection.PublishedPort.Value;
            await WaitUntilReachableAsync(inspection.ContainerId!, publishedPort, cancellationToken);
            return BuildDeploymentResult(job, inspection.ContainerId!, inspection.ContainerName!, containerPort, publishedPort);
        }

        await WaitUntilRunningAsync(inspection.ContainerId!, cancellationToken);
        return BuildDeploymentResult(job, inspection.ContainerId!, inspection.ContainerName!, containerPort, null);
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
                job.PublicAccessEnabled ? job.PublicHostname : null,
                job.RouteStatus ?? BuildRouteStatus(job),
                job.PublishedPort.HasValue && ShouldPublishHostPort(job)
                    ? BuildPortDeploymentUrl(job.PublishedPort.Value)
                    : job.DeploymentUrl);
        }

        return new ContainerRuntimeStatus(
            inspection.Status,
            inspection.ContainerId,
            inspection.ContainerName,
            inspection.PublishedPort,
            job.PublicAccessEnabled ? job.PublicHostname ?? TryBuildPublicHostname(job) : null,
            job.RouteStatus ?? BuildRouteStatus(job),
            BuildDeploymentUrl(job, inspection.PublishedPort));
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
        Job job,
        string containerName,
        int containerPort,
        string imageTag,
        CancellationToken cancellationToken)
    {
        EnsureRoutingConfiguration(job.PublicAccessEnabled);

        var arguments =
            $"run -d --name {containerName} {BuildRunSecurityArguments()} {BuildRunRoutingArguments(job, containerPort)} {imageTag}";

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
        if (ShouldPublishHostPort(job) && containerPort.HasValue)
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

    private string BuildRunRoutingArguments(Job job, int containerPort)
    {
        if (_routingOptions.UsesPortPublishing)
        {
            return $"-p {_options.BindingHost}::{containerPort}";
        }

        var routerName = $"dockerizer-{job.Id:N}";
        var serviceName = routerName;

        var arguments = new List<string>
        {
            $"--network {Quote(_routingOptions.DockerNetwork)}"
        };

        if (!job.PublicAccessEnabled)
        {
            arguments.Add($"-p {_options.BindingHost}::{containerPort}");
            return string.Join(' ', arguments);
        }

        var hostname = BuildPublicHostname(job);
        arguments.AddRange([
            "--label traefik.enable=true",
            $"--label {Quote($"traefik.http.routers.{routerName}.rule=Host(`{hostname}`)")}",
            $"--label {Quote($"traefik.http.routers.{routerName}.entrypoints=web")}",
            $"--label {Quote($"traefik.http.routers.{routerName}.service={serviceName}")}",
            $"--label {Quote($"traefik.http.services.{serviceName}.loadbalancer.server.port={containerPort}")}"
        ]);

        return string.Join(' ', arguments);
    }

    private void EnsureRoutingConfiguration(bool requirePublicRoute)
    {
        if (_routingOptions.UsesPortPublishing)
        {
            return;
        }

        if (_options.DisableContainerNetwork)
        {
            throw new InvalidOperationException("ApplicationRouting:Mode=TunnelWildcard requires DockerRuntime:DisableContainerNetwork=false.");
        }

        if (requirePublicRoute && string.IsNullOrWhiteSpace(_routingOptions.BaseDomain))
        {
            throw new InvalidOperationException("ApplicationRouting:BaseDomain is required when ApplicationRouting:Mode=TunnelWildcard.");
        }

        if (string.IsNullOrWhiteSpace(_routingOptions.DockerNetwork))
        {
            throw new InvalidOperationException("ApplicationRouting:DockerNetwork is required when ApplicationRouting:Mode=TunnelWildcard.");
        }

        if (!_routingOptions.ReverseProxy.Equals("Traefik", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Traefik reverse proxy labels are currently supported for tunnel wildcard routing.");
        }
    }

    private async Task WaitUntilRunningAsync(string containerId, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.StartupTimeoutSeconds);
        var pollInterval = TimeSpan.FromMilliseconds(_options.StartupPollIntervalMilliseconds);
        var deadlineUtc = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadlineUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await GetContainerStatusAsync(containerId, cancellationToken);
            if (status == "running")
            {
                return;
            }

            if (status is "exited" or "dead")
            {
                var logs = await TryGetContainerLogsAsync(containerId, cancellationToken);
                throw new InvalidOperationException(
                    $"Container exited before it became reachable through reverse proxy routing. Logs: {logs}");
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        var finalLogs = await TryGetContainerLogsAsync(containerId, cancellationToken);
        throw new TimeoutException(
            $"Container did not reach running state within {_options.StartupTimeoutSeconds} seconds. Logs: {finalLogs}");
    }

    private static string Quote(string value) => $"\"{value.Trim()}\"";

    private string BuildPortDeploymentUrl(int publishedPort)
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

    private string? BuildDeploymentUrl(Job job, int? publishedPort)
    {
        if (_routingOptions.UsesPortPublishing)
        {
            return publishedPort.HasValue ? BuildPortDeploymentUrl(publishedPort.Value) : job.DeploymentUrl;
        }

        if (!job.PublicAccessEnabled)
        {
            return publishedPort.HasValue ? BuildPortDeploymentUrl(publishedPort.Value) : job.DeploymentUrl;
        }

        var hostname = job.PublicHostname ?? TryBuildPublicHostname(job);
        return string.IsNullOrWhiteSpace(hostname)
            ? job.DeploymentUrl
            : $"{NormalizeScheme(_routingOptions.PublicScheme)}://{hostname}";
    }

    private string BuildPublicHostname(Job job)
    {
        var hostname = TryBuildPublicHostname(job);
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new InvalidOperationException("Could not build a public hostname for the container.");
        }

        return hostname;
    }

    private string? TryBuildPublicHostname(Job job)
    {
        if (_routingOptions.UsesPortPublishing || !job.PublicAccessEnabled || string.IsNullOrWhiteSpace(_routingOptions.BaseDomain))
        {
            return job.PublicHostname;
        }

        var baseDomain = _routingOptions.BaseDomain.Trim().TrimStart('.').TrimEnd('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            return job.PublicHostname;
        }

        return $"{BuildSlug(job)}.{baseDomain}";
    }

    private static string BuildSlug(Job job)
    {
        var source = string.IsNullOrWhiteSpace(job.Name)
            ? "app"
            : job.Name.Trim().ToLowerInvariant();
        var builder = new StringBuilder(source.Length);
        var lastWasHyphen = false;

        foreach (var character in source)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
                lastWasHyphen = false;
                continue;
            }

            if (!lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "app";
        }

        if (slug.Length > 48)
        {
            slug = slug[..48].Trim('-');
        }

        return $"{slug}-{job.Id.ToString("N")[..8]}";
    }

    private string BuildRouteStatus(Job job) =>
        _routingOptions.UsesPortPublishing
            ? "port-published"
            : job.PublicAccessEnabled
                ? "reverse-proxy-configured"
                : "private";

    private bool ShouldPublishHostPort(Job job) =>
        _routingOptions.UsesPortPublishing || !job.PublicAccessEnabled;

    private static string NormalizeScheme(string scheme)
    {
        scheme = string.IsNullOrWhiteSpace(scheme) ? "https" : scheme.Trim().TrimEnd(':', '/', '\\').ToLowerInvariant();
        return scheme is "http" or "https" ? scheme : "https";
    }

    private ContainerDeploymentResult BuildDeploymentResult(
        Job job,
        string containerId,
        string containerName,
        int containerPort,
        int? publishedPort) =>
        new(
            containerId,
            containerName,
            containerPort,
            publishedPort,
            job.PublicAccessEnabled,
            TryBuildPublicHostname(job),
            BuildDeploymentUrl(job, publishedPort),
            BuildRouteStatus(job),
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
