namespace Dockerizer.Infrastructure.Artifacts;

public sealed class ArtifactOptions
{
    public const string WorkspaceRootConfigKey = "Worker:WorkspaceRoot";

    public string WorkspaceRoot { get; set; } = ".worker-data/repos";
    public bool CleanupWorkspaceAfterCompletion { get; set; }
}
