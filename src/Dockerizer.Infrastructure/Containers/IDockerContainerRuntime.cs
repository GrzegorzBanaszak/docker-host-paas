using Dockerizer.Domain.Entities;

namespace Dockerizer.Infrastructure.Containers;

public interface IDockerContainerRuntime
{
    Task<ContainerDeploymentResult> RunAsync(Job job, int containerPort, CancellationToken cancellationToken);
    Task<ContainerDeploymentResult> StartAsync(Job job, int containerPort, CancellationToken cancellationToken);
    Task<ContainerDeploymentResult> RestartAsync(Job job, int containerPort, CancellationToken cancellationToken);
    Task StopAsync(Job job, CancellationToken cancellationToken);
    Task<ContainerRuntimeStatus> GetStatusAsync(Job job, CancellationToken cancellationToken);
    Task StopAndRemoveAsync(Job job, CancellationToken cancellationToken);
}
