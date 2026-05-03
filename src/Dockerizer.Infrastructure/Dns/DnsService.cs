using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Dns;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dockerizer.Infrastructure.Dns;

public sealed class DnsService(
    DockerizerDbContext dbContext,
    IDockerContainerRuntime dockerContainerRuntime,
    IOptions<ApplicationRoutingOptions> applicationRoutingOptions,
    IConfiguration configuration) : IDnsService
{
    private readonly ApplicationRoutingOptions _routingOptions = applicationRoutingOptions.Value;

    public async Task<DnsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var routes = await GetRoutesAsync(cancellationToken);
        var routing = BuildRoutingSettings();
        var tunnel = BuildTunnelStatus();
        var expectedRecord = BuildExpectedWildcardRecord(tunnel);
        var configuredRecords = routes
            .Where(route => route.PublicAccessEnabled && !string.IsNullOrWhiteSpace(route.PublicHostname))
            .Select(BuildConfiguredRecord)
            .ToList();

        return new DnsOverviewDto(
            routing,
            tunnel,
            expectedRecord,
            configuredRecords,
            routes.Count(route => route.PublicAccessEnabled && !string.IsNullOrWhiteSpace(route.PublicHostname)),
            routes.Count(route => route.ContainerStatus == "running"),
            routes.Count(route => route.RouteStatus == "failed"),
            routes
                .Select(route => route.DeployedAtUtc ?? route.CreatedAtUtc)
                .Cast<DateTimeOffset?>()
                .OrderByDescending(date => date)
                .FirstOrDefault());
    }

    public async Task<IReadOnlyCollection<DnsRouteDto>> GetRoutesAsync(CancellationToken cancellationToken)
    {
        var jobs = await dbContext.Jobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var routes = new List<DnsRouteDto>(jobs.Count);
        foreach (var job in jobs)
        {
            var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
            routes.Add(MapRoute(job, containerStatus));
        }

        return routes;
    }

    public async Task<DnsRouteDto?> GetRouteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

        if (job is null)
        {
            return null;
        }

        var containerStatus = await dockerContainerRuntime.GetStatusAsync(job, cancellationToken);
        return MapRoute(job, containerStatus);
    }

    private DnsRoutingSettingsDto BuildRoutingSettings() =>
        new(
            _routingOptions.UsesTunnelWildcard ? "TunnelWildcard" : "Port",
            NormalizeScheme(_routingOptions.PublicScheme),
            NormalizeNullable(_routingOptions.BaseDomain),
            string.IsNullOrWhiteSpace(_routingOptions.DockerNetwork) ? "not configured" : _routingOptions.DockerNetwork.Trim(),
            string.IsNullOrWhiteSpace(_routingOptions.ReverseProxy) ? "not configured" : _routingOptions.ReverseProxy.Trim());

    private DnsTunnelStatusDto BuildTunnelStatus()
    {
        var tunnelId = FirstConfiguredValue("Cloudflare:TunnelId", "CLOUDFLARE_TUNNEL_ID");
        var tunnelToken = FirstConfiguredValue("Cloudflare:TunnelToken", "CLOUDFLARE_TUNNEL_TOKEN");
        var apiToken = FirstConfiguredValue("Cloudflare:ApiToken", "CLOUDFLARE_API_TOKEN");
        var tunnelHostname = string.IsNullOrWhiteSpace(tunnelId)
            ? null
            : $"{tunnelId.Trim()}.cfargotunnel.com";
        var status = !_routingOptions.UsesTunnelWildcard
            ? "not_used"
            : !string.IsNullOrWhiteSpace(tunnelToken)
                ? "configured"
                : "not_configured";

        return new DnsTunnelStatusDto(
            status,
            NormalizeNullable(tunnelId),
            tunnelHostname,
            _routingOptions.ReverseProxy.Equals("Traefik", StringComparison.OrdinalIgnoreCase) ? "http://traefik:80" : "unknown",
            new DnsSecretStatusDto("CLOUDFLARE_TUNNEL_TOKEN", string.IsNullOrWhiteSpace(tunnelToken) ? "not_configured" : "configured"),
            new DnsSecretStatusDto("CLOUDFLARE_API_TOKEN", string.IsNullOrWhiteSpace(apiToken) ? "not_configured" : "configured"));
    }

    private DnsRecordDto? BuildExpectedWildcardRecord(DnsTunnelStatusDto tunnel)
    {
        var baseDomain = NormalizeNullable(_routingOptions.BaseDomain);
        if (!_routingOptions.UsesTunnelWildcard || string.IsNullOrWhiteSpace(baseDomain))
        {
            return null;
        }

        return new DnsRecordDto(
            "CNAME",
            $"*.{baseDomain}",
            tunnel.TunnelHostname ?? "<tunnel-id>.cfargotunnel.com",
            "proxied",
            tunnel.TunnelHostname is null ? "needs_tunnel_id" : "expected");
    }

    private DnsRecordDto BuildConfiguredRecord(DnsRouteDto route) =>
        new(
            "HOST",
            route.PublicHostname!,
            route.DeploymentUrl ?? FormatRouteTarget(route),
            _routingOptions.UsesTunnelWildcard ? "proxied" : "direct",
            route.RouteStatus ?? "configured");

    private static string FormatRouteTarget(DnsRouteDto route)
    {
        if (route.PublishedPort.HasValue && route.ContainerPort.HasValue)
        {
            return $"{route.PublishedPort.Value} -> {route.ContainerPort.Value}";
        }

        if (route.ContainerPort.HasValue)
        {
            return $"proxy -> {route.ContainerPort.Value}";
        }

        return "target unavailable";
    }

    private DnsRouteDto MapRoute(Job job, ContainerRuntimeStatus containerStatus) =>
        new(
            job.Id,
            job.Name,
            job.RepositoryUrl,
            job.GeneratedImageTag,
            containerStatus.ContainerName ?? job.ContainerName,
            containerStatus.Status,
            job.ContainerPort,
            containerStatus.PublishedPort ?? job.PublishedPort,
            job.PublicAccessEnabled,
            containerStatus.PublicHostname ?? job.PublicHostname,
            containerStatus.RouteStatus ?? job.RouteStatus,
            containerStatus.DeploymentUrl ?? job.DeploymentUrl,
            job.DeployedAtUtc,
            job.CreatedAtUtc);

    private string? FirstConfiguredValue(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeScheme(string scheme)
    {
        scheme = string.IsNullOrWhiteSpace(scheme) ? "https" : scheme.Trim().TrimEnd(':', '/', '\\').ToLowerInvariant();
        return scheme is "http" or "https" ? scheme : "https";
    }
}
