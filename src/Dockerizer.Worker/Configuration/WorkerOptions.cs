namespace Dockerizer.Worker.Configuration;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string WorkspaceRoot { get; set; } = ".worker-data/repos";
    public int QueuePollIntervalSeconds { get; set; } = 5;
    public string DockerImagePrefix { get; set; } = "dockerizer";
    public int DockerBuildTimeoutMinutes { get; set; } = 10;
    public bool CleanupWorkspaceAfterCompletion { get; set; } = true;
}
