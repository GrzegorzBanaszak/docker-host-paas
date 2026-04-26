using Dockerizer.Application.Abstractions;
using Dockerizer.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dockerizer.Infrastructure.Queue;

public sealed class RedisJobQueue(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<RedisOptions> redisOptions) : IJobQueue
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
    private readonly RedisOptions _options = redisOptions.Value;

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.ListLeftPushAsync(_options.QueueKey, jobId.ToString());
    }

    public async Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var value = await _database.ListRightPopAsync(_options.QueueKey);
            if (value.HasValue && Guid.TryParse(value.ToString(), out var jobId))
            {
                return jobId;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        return null;
    }
}
