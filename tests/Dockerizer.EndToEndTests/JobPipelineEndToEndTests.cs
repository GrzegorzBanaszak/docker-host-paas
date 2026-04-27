using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Jobs;
using Dockerizer.Domain;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Jobs;
using Dockerizer.Infrastructure.Persistence;
using Dockerizer.Worker.Configuration;
using Dockerizer.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dockerizer.EndToEndTests;

public sealed class JobPipelineEndToEndTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "dockerizer-e2e", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ProcessAsync_CompletesPipeline_AndPersistsDeploymentMetadata()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "node-basic");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page", "main"),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var files = await jobsService.GetFilesAsync(created.Id, CancellationToken.None);
        var logs = await jobsService.GetLogsAsync(created.Id, CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("nodejs", job.DetectedStack);
        Assert.Equal("dockerizer:test-image", job.GeneratedImageTag);
        Assert.Equal(3000, job.ContainerPort);
        Assert.Equal(45000, job.PublishedPort);
        Assert.Equal("http://localhost:45000", job.DeploymentUrl);
        Assert.Equal("fake-container-id", job.ContainerId);
        Assert.Equal("dockerizer-job-" + created.Id.ToString("N"), job.ContainerName);
        Assert.NotNull(job.DeployedAtUtc);

        Assert.Contains(files, file => file.Name == "Dockerfile");
        Assert.Contains(files, file => file.Name == ".dockerignore");
        Assert.NotNull(logs);
        Assert.Contains("Container deployed", logs!.Content);
        Assert.Single(fakeRuntime.RunRequests);
    }

    [Fact]
    public async Task ProcessAsync_ForStaticHtml_UsesNginxStyleContainerization()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "static-html");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page", "main"),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var dockerfile = await jobsService.GetFileContentAsync(created.Id, "Dockerfile", CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("static-html", job.DetectedStack);
        Assert.Equal(80, job.ContainerPort);
        Assert.NotNull(dockerfile);
        Assert.Contains("nginx:1.27-alpine", dockerfile!.Content);
        Assert.Contains("/usr/share/nginx/html", dockerfile.Content);
    }

    [Fact]
    public async Task RetryAsync_CleansDeploymentState_AndEnqueuesJobAgain()
    {
        Directory.CreateDirectory(_tempRoot);
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var queue = new InMemoryJobQueue();
        var artifactService = CreateArtifactService();
        var jobsService = new JobsService(context, queue, artifactService, fakeRuntime);

        var job = new Job
        {
            RepositoryUrl = "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page",
            Status = JobStatus.Succeeded,
            GeneratedImageTag = "dockerizer:test-image",
            ContainerId = "container-1",
            ContainerName = "dockerizer-job-test",
            ContainerPort = 3000,
            PublishedPort = 45000,
            DeploymentUrl = "http://localhost:45000",
            DeployedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var retried = await jobsService.RetryAsync(job.Id, CancellationToken.None);

        Assert.NotNull(retried);
        Assert.Equal(nameof(JobStatus.Queued), retried!.Status);
        Assert.Null(retried.GeneratedImageTag);
        Assert.Null(retried.ContainerId);
        Assert.Null(retried.ContainerName);
        Assert.Null(retried.PublishedPort);
        Assert.Null(retried.DeploymentUrl);
        Assert.Single(fakeRuntime.StopRequests);
        Assert.Equal(job.Id, Assert.Single(queue.EnqueuedJobIds));
    }

    [Fact]
    public async Task CancelAsync_StopsContainer_AndMarksJobCanceled()
    {
        Directory.CreateDirectory(_tempRoot);
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);

        var job = new Job
        {
            RepositoryUrl = "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page",
            Status = JobStatus.Succeeded,
            GeneratedImageTag = "dockerizer:test-image",
            ContainerId = "container-1",
            ContainerName = "dockerizer-job-test",
            ContainerPort = 3000,
            PublishedPort = 45000,
            DeploymentUrl = "http://localhost:45000",
            DeployedAtUtc = DateTimeOffset.UtcNow,
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var canceled = await jobsService.CancelAsync(job.Id, CancellationToken.None);

        Assert.NotNull(canceled);
        Assert.Equal(nameof(JobStatus.Canceled), canceled!.Status);
        Assert.Null(canceled.ContainerId);
        Assert.Null(canceled.ContainerName);
        Assert.Null(canceled.PublishedPort);
        Assert.Null(canceled.DeploymentUrl);
        Assert.Single(fakeRuntime.StopRequests);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private DockerizerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DockerizerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DockerizerDbContext(options);
    }

    private JobsService CreateJobsService(DockerizerDbContext context, FakeDockerContainerRuntime fakeRuntime)
    {
        return new JobsService(
            context,
            new InMemoryJobQueue(),
            CreateArtifactService(),
            fakeRuntime);
    }

    private JobExecutionService CreateJobExecutionService(
        DockerizerDbContext context,
        IGitRepositoryCloner gitRepositoryCloner,
        FakeDockerContainerRuntime fakeRuntime)
    {
        return new JobExecutionService(
            context,
            new InMemoryJobQueue(),
            gitRepositoryCloner,
            new RepositoryStackDetector(),
            new ContainerizationTemplateGenerator(NullLogger<ContainerizationTemplateGenerator>.Instance),
            new ContainerPortResolver(),
            new FakeDockerImageBuilder(),
            fakeRuntime,
            new JobLogWriter(),
            CreateArtifactService(),
            Options.Create(new WorkerOptions
            {
                WorkspaceRoot = Path.Combine(_tempRoot, "workspace"),
                CleanupWorkspaceAfterCompletion = false,
            }),
            NullLogger<JobExecutionService>.Instance);
    }

    private JobArtifactService CreateArtifactService()
    {
        return new JobArtifactService(new ArtifactOptions
        {
            WorkspaceRoot = Path.Combine(_tempRoot, "workspace"),
            CleanupWorkspaceAfterCompletion = false,
        });
    }

    private sealed class InMemoryJobQueue : IJobQueue
    {
        public List<Guid> EnqueuedJobIds { get; } = [];

        public Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnqueuedJobIds.Add(jobId);
            return Task.CompletedTask;
        }

        public Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Guid?>(EnqueuedJobIds.Count > 0 ? EnqueuedJobIds[0] : null);
        }
    }

    private sealed class FixtureRepositoryCloner(string fixturePath) : IGitRepositoryCloner
    {
        public Task CloneAsync(Job job, string targetPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CopyDirectory(fixturePath, targetPath);
            return Task.CompletedTask;
        }

        private static void CopyDirectory(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);

            foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(sourcePath, targetPath, StringComparison.Ordinal));
            }

            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var destinationFile = file.Replace(sourcePath, targetPath, StringComparison.Ordinal);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(file, destinationFile, overwrite: true);
            }
        }
    }

    private sealed class FakeDockerImageBuilder : IDockerImageBuilder
    {
        public Task<string> BuildAsync(Job job, string repositoryPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.True(File.Exists(Path.Combine(repositoryPath, "Dockerfile")));
            return Task.FromResult("dockerizer:test-image");
        }
    }

    private sealed class FakeDockerContainerRuntime : IDockerContainerRuntime
    {
        public List<(Guid JobId, int ContainerPort)> RunRequests { get; } = [];
        public List<Guid> StopRequests { get; } = [];

        public Task<ContainerDeploymentResult> RunAsync(Job job, int containerPort, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RunRequests.Add((job.Id, containerPort));

            return Task.FromResult(new ContainerDeploymentResult(
                "fake-container-id",
                $"dockerizer-job-{job.Id:N}",
                containerPort,
                45000,
                "http://localhost:45000",
                DateTimeOffset.UtcNow));
        }

        public Task StopAndRemoveAsync(Job job, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopRequests.Add(job.Id);
            return Task.CompletedTask;
        }
    }
}
