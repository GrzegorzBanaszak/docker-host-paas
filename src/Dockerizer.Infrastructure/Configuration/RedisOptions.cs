namespace Dockerizer.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string QueueKey { get; set; } = "dockerizer:jobs";
}
