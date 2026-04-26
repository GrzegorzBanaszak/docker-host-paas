using Dockerizer.Worker.Services;

namespace Dockerizer.Worker;

public sealed class JobProcessingWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<JobProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job processing worker started at {StartedAtUtc}.", DateTimeOffset.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var executionService = scope.ServiceProvider.GetRequiredService<JobExecutionService>();
                var jobId = await executionService.DequeueAsync(stoppingToken);

                if (jobId is null)
                {
                    continue;
                }

                await executionService.ProcessAsync(jobId.Value, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while processing background jobs.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
