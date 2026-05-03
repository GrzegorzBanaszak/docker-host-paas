namespace Dockerizer.Application.Dns;

public sealed record DnsSecretStatusDto(
    string Name,
    string Status);
