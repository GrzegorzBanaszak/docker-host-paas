namespace Dockerizer.Worker.Services;

public sealed record DockerBuildResult(string ImageTag, string ImageId);
