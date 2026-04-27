using Dockerizer.Domain.Entities;

namespace Dockerizer.Infrastructure.Containers;

public interface IDockerContainerRuntime
{
    Task<ContainerDeploymentResult> RunAsync(Job job, int containerPort, CancellationToken cancellationToken);
    Task StopAndRemoveAsync(Job job, CancellationToken cancellationToken);
}
