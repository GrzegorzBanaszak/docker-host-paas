namespace Dockerizer.Application.Dns;

public sealed record DnsTunnelStatusDto(
    string Status,
    string? TunnelId,
    string? TunnelHostname,
    string ServiceTarget,
    DnsSecretStatusDto TunnelToken,
    DnsSecretStatusDto ApiToken);
