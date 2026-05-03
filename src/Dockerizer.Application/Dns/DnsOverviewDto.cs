namespace Dockerizer.Application.Dns;

public sealed record DnsOverviewDto(
    DnsRoutingSettingsDto Routing,
    DnsTunnelStatusDto Tunnel,
    DnsRecordDto? ExpectedWildcardRecord,
    IReadOnlyCollection<DnsRecordDto> ConfiguredRecords,
    int PublicHostnameCount,
    int RunningContainerCount,
    int RouteErrorCount,
    DateTimeOffset? LastUpdatedUtc);
