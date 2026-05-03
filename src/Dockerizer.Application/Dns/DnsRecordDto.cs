namespace Dockerizer.Application.Dns;

public sealed record DnsRecordDto(
    string Type,
    string Name,
    string Target,
    string ProxyStatus,
    string Status);
