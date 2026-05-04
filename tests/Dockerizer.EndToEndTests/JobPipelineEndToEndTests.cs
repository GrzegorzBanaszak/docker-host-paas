using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Images;
using Dockerizer.Application.Jobs;
using Dockerizer.Domain;
using Dockerizer.Domain.Entities;
using Dockerizer.Infrastructure.Artifacts;
using Dockerizer.Infrastructure.Configuration;
using Dockerizer.Infrastructure.Containers;
using Dockerizer.Infrastructure.Images;
using Dockerizer.Infrastructure.Jobs;
using Dockerizer.Infrastructure.Persistence;
using Dockerizer.Worker.Configuration;
using Dockerizer.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            new CreateJobCommand("artisan-bakery-landing-page", "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page", "main", null),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var files = await jobsService.GetFilesAsync(created.Id, CancellationToken.None);
        var logs = await jobsService.GetLogsAsync(created.Id, CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal("artisan-bakery-landing-page", job!.Name);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("node-backend", job.DetectedStack);
        Assert.NotNull(job.GeneratedImageTag);
        Assert.NotNull(job.ImageId);
        Assert.StartsWith("dockerizer:", job.GeneratedImageTag);
        Assert.StartsWith("sha256:", job.ImageId);
        Assert.Equal(3000, job.ContainerPort);
        Assert.Equal(45000, job.PublishedPort);
        Assert.Equal("http://localhost:45000", job.DeploymentUrl);
        Assert.Equal("fake-container-id", job.ContainerId);
        Assert.Equal("dockerizer-job-" + created.Id.ToString("N"), job.ContainerName);
        Assert.Equal("running", job.ContainerStatus);
        Assert.NotNull(job.DeployedAtUtc);
        Assert.NotNull(job.CurrentImageId);
        Assert.Single(job.Images);
        Assert.True(Assert.Single(job.Images).IsCurrent);

        Assert.Contains(files, file => file.Name == "Dockerfile");
        Assert.Contains(files, file => file.Name == ".dockerignore");
        Assert.NotNull(logs);
        Assert.Contains("Container deployed", logs!.Content);
        Assert.Single(fakeRuntime.RunRequests);
        Assert.False(Directory.Exists(Path.Combine(_tempRoot, "workspace", created.Id.ToString("N"))));
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
            new CreateJobCommand("artisan-bakery-landing-page", "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page", "main", null),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var dockerfile = await jobsService.GetFileContentAsync(created.Id, "Dockerfile", CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("static-html", job.DetectedStack);
        Assert.Equal(80, job.ContainerPort);
        Assert.NotNull(job.CurrentImage);
        Assert.NotNull(dockerfile);
        Assert.Contains("nginx:1.27-alpine", dockerfile!.Content);
        Assert.Contains("/usr/share/nginx/html", dockerfile.Content);
    }

    [Fact]
    public async Task ProcessAsync_ForReactVite_UsesStaticBuildContainerization()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "react-vite");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("react-vite-fixture", "https://github.com/example/react-vite-fixture", "main", null),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var dockerfile = await jobsService.GetFileContentAsync(created.Id, "Dockerfile", CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("react-vite", job.DetectedStack);
        Assert.Equal(80, job.ContainerPort);
        Assert.NotNull(dockerfile);
        Assert.Contains("npm run build", dockerfile!.Content);
        Assert.Contains("nginx:1.27-alpine", dockerfile.Content);
        Assert.Contains("/app/dist", dockerfile.Content);
    }

    [Fact]
    public async Task ProcessAsync_ForNextJs_UsesNodeSsrContainerization()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "nextjs");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("nextjs-fixture", "https://github.com/example/nextjs-fixture", "main", null),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var dockerfile = await jobsService.GetFileContentAsync(created.Id, "Dockerfile", CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("nextjs", job.DetectedStack);
        Assert.Equal(3000, job.ContainerPort);
        Assert.NotNull(dockerfile);
        Assert.Contains("npm run build", dockerfile!.Content);
        Assert.Contains("COPY --from=build /app ./", dockerfile.Content);
        Assert.Contains("npm\", \"run\", \"start", dockerfile.Content);
    }

    [Fact]
    public async Task ProcessAsync_ForMonorepoBackendPath_UsesNodeBackendDetection()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "node-monorepo");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("node-monorepo-backend", "https://github.com/example/node-monorepo", "main", "backend"),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var dockerfile = await jobsService.GetFileContentAsync(created.Id, "Dockerfile", CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("backend", job.ProjectPath);
        Assert.Equal("node-backend", job.DetectedStack);
        Assert.Equal(3000, job.ContainerPort);
        Assert.NotNull(dockerfile);
        Assert.Contains("CMD [\"npm\", \"start\"]", dockerfile!.Content);
    }

    [Fact]
    public async Task ProcessAsync_ForMonorepoFrontendPath_UsesReactViteDetection()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "node-monorepo");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("node-monorepo-frontend", "https://github.com/example/node-monorepo", "main", "frontend"),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(nameof(JobStatus.Succeeded), job!.Status);
        Assert.Equal("frontend", job.ProjectPath);
        Assert.Equal("react-vite", job.DetectedStack);
        Assert.Equal(80, job.ContainerPort);
    }

    [Fact]
    public async Task RetryAsync_CleansDeploymentState_AndEnqueuesJobAgain()
    {
        Directory.CreateDirectory(_tempRoot);
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var queue = new InMemoryJobQueue();
        var artifactService = CreateArtifactService(context);
        var jobsService = new JobsService(
            context,
            queue,
            artifactService,
            fakeRuntime,
            new FakeDockerImageStore(),
            Options.Create(new ApplicationRoutingOptions()),
            CreateRepositoryInspectionService(),
            new RepositoryProjectPathResolver());

        var job = new Job
        {
            Name = "artisan-bakery-landing-page",
            RepositoryUrl = "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page",
            Status = JobStatus.Succeeded,
            GeneratedImageTag = "dockerizer:test-image",
            ImageId = "sha256:test-image-id",
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
        Assert.Null(retried.ImageId);
        Assert.Null(retried.CurrentImageId);
        Assert.Null(retried.ContainerId);
        Assert.Null(retried.ContainerName);
        Assert.Null(retried.PublishedPort);
        Assert.Null(retried.DeploymentUrl);
        Assert.Single(fakeRuntime.StopRequests);
        Assert.Equal(job.Id, Assert.Single(queue.EnqueuedJobIds));
    }

    [Fact]
    public async Task DeleteAsync_RemovesJobAndAssignedResources()
    {
        Directory.CreateDirectory(_tempRoot);
        var fakeRuntime = new FakeDockerContainerRuntime();
        var fakeImageStore = new FakeDockerImageStore();
        await using var context = CreateDbContext();
        var artifactService = CreateArtifactService(context);
        var jobsService = new JobsService(
            context,
            new InMemoryJobQueue(),
            artifactService,
            fakeRuntime,
            fakeImageStore,
            Options.Create(new ApplicationRoutingOptions()),
            CreateRepositoryInspectionService(),
            new RepositoryProjectPathResolver());

        var job = new Job
        {
            Name = "delete-me",
            RepositoryUrl = "https://github.com/example/delete-me",
            Status = JobStatus.Succeeded,
            GeneratedImageTag = "dockerizer:delete-me",
            ImageId = "sha256:delete-me",
            ContainerId = "container-delete-me",
            ContainerName = "dockerizer-job-delete-me",
            ContainerPort = 3000,
            Artifacts =
            [
                new JobArtifact { Kind = "log", Name = "job.log", Content = "log" }
            ],
            Images =
            [
                new JobImage
                {
                    Status = JobStatus.Succeeded,
                    ImageTag = "dockerizer:delete-me",
                    ImageId = "sha256:delete-me",
                    Artifacts =
                    [
                        new ImageArtifact { Kind = "generated-file", Name = "Dockerfile", Content = "FROM nginx" }
                    ]
                }
            ]
        };
        job.CurrentImage = job.Images.Single();
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var workspacePath = Path.Combine(_tempRoot, "workspace", job.Id.ToString("N"));
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(Path.Combine(workspacePath, "Dockerfile"), "FROM nginx");

        var deleted = await jobsService.DeleteAsync(job.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Contains(job.Id, fakeRuntime.StopRequests);
        Assert.Contains(fakeImageStore.RemoveRequests, request => request.ImageId == "sha256:delete-me" && request.ImageTag == "dockerizer:delete-me");
        Assert.False(await context.Jobs.AnyAsync(x => x.Id == job.Id));
        Assert.False(await context.JobArtifacts.AnyAsync(x => x.JobId == job.Id));
        Assert.False(await context.JobImages.AnyAsync(x => x.JobId == job.Id));
        Assert.False(await context.ImageArtifacts.AnyAsync());
        Assert.False(Directory.Exists(workspacePath));
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
            Name = "artisan-bakery-landing-page",
            RepositoryUrl = "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page",
            Status = JobStatus.Succeeded,
            GeneratedImageTag = "dockerizer:test-image",
            ImageId = "sha256:test-image-id",
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

    [Fact]
    public async Task StartStopRestartContainerAsync_UpdatesLiveContainerStatus()
    {
        Directory.CreateDirectory(_tempRoot);
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);

        var job = new Job
        {
            Name = "artisan-bakery-landing-page",
            RepositoryUrl = "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page",
            Status = JobStatus.Succeeded,
            GeneratedImageTag = "dockerizer:test-image",
            ImageId = "sha256:test-image-id",
            ContainerId = "container-1",
            ContainerName = "dockerizer-job-test",
            ContainerPort = 3000,
            PublishedPort = 45000,
            DeploymentUrl = "http://localhost:45000",
            DeployedAtUtc = DateTimeOffset.UtcNow,
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var stopped = await jobsService.StopContainerAsync(job.Id, CancellationToken.None);
        var restarted = await jobsService.RestartContainerAsync(job.Id, CancellationToken.None);

        Assert.NotNull(stopped);
        Assert.Equal("exited", stopped!.ContainerStatus);
        Assert.NotNull(restarted);
        Assert.Equal("running", restarted!.ContainerStatus);
        Assert.Single(fakeRuntime.StopRequests);
        Assert.Single(fakeRuntime.RestartRequests);
    }

    [Fact]
    public async Task RebuildAsync_CreatesNewImage_AndMarksLatestAsCurrent()
    {
        Directory.CreateDirectory(_tempRoot);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "node-basic");
        var fakeRuntime = new FakeDockerContainerRuntime();
        await using var context = CreateDbContext();
        var jobsService = CreateJobsService(context, fakeRuntime);
        var imagesService = CreateImagesService(context);
        var worker = CreateJobExecutionService(context, new FixtureRepositoryCloner(fixturePath), fakeRuntime);

        var created = await jobsService.CreateAsync(
            new CreateJobCommand("artisan-bakery-landing-page", "https://github.com/GrzegorzBanaszak/artisan-bakery-landing-page", "main", null),
            CancellationToken.None);

        await worker.ProcessAsync(created.Id, CancellationToken.None);
        await jobsService.RebuildAsync(created.Id, CancellationToken.None);
        await worker.ProcessAsync(created.Id, CancellationToken.None);

        var job = await jobsService.GetByIdAsync(created.Id, CancellationToken.None);
        var images = await imagesService.GetAllAsync(CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal(2, job!.Images.Count);
        Assert.NotNull(job.CurrentImageId);
        Assert.Equal(2, images.Count(x => x.JobId == created.Id));
        Assert.Equal(job.CurrentImageId, images.First(x => x.IsCurrent && x.JobId == created.Id).Id);
    }

    [Fact]
    public async Task CleanupWorkspaceAsync_Removes_ReadOnlyFiles()
    {
        Directory.CreateDirectory(_tempRoot);
        await using var context = CreateDbContext();
        var artifactService = CreateArtifactService(context);
        var jobId = Guid.NewGuid();
        var workspacePath = Path.Combine(_tempRoot, "workspace", jobId.ToString("N"));
        var gitDirectoryPath = Path.Combine(workspacePath, "repository", ".git", "objects", "pack");
        Directory.CreateDirectory(gitDirectoryPath);

        var packFilePath = Path.Combine(gitDirectoryPath, "pack-test.idx");
        await File.WriteAllTextAsync(packFilePath, "content");
        File.SetAttributes(packFilePath, FileAttributes.ReadOnly);

        await artifactService.CleanupWorkspaceAsync(jobId, CancellationToken.None);

        Assert.False(Directory.Exists(workspacePath));
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
            CreateArtifactService(context),
            fakeRuntime,
            new FakeDockerImageStore(),
            Options.Create(new ApplicationRoutingOptions()),
            CreateRepositoryInspectionService(),
            new RepositoryProjectPathResolver());
    }

    private ImagesService CreateImagesService(DockerizerDbContext context)
    {
        return new ImagesService(
            context,
            CreateArtifactService(context),
            new FakeDockerImageStore());
    }

    private RepositoryInspectionService CreateRepositoryInspectionService()
    {
        return new RepositoryInspectionService(
            new FakeRepositoryBranchProvider(),
            new ArtifactOptions
            {
                WorkspaceRoot = Path.Combine(_tempRoot, "workspace"),
            },
            Options.Create(new RepositorySecurityOptions()),
            new RepositoryProjectTypeDetector(),
            new RepositoryProjectPathResolver(),
            NullLogger<RepositoryInspectionService>.Instance);
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
            new RepositoryStackDetector(new RepositoryProjectTypeDetector()),
            new RepositoryProjectPathResolver(),
            new ContainerizationTemplateGenerator(NullLogger<ContainerizationTemplateGenerator>.Instance),
            new ContainerPortResolver(),
            new FakeDockerImageBuilder(),
            fakeRuntime,
            new JobLogWriter(CreateArtifactService(context)),
            CreateArtifactService(context),
            Options.Create(new WorkerOptions
            {
                WorkspaceRoot = Path.Combine(_tempRoot, "workspace"),
                CleanupWorkspaceAfterCompletion = true,
            }),
            NullLogger<JobExecutionService>.Instance);
    }

    private JobArtifactService CreateArtifactService(DockerizerDbContext context)
    {
        return new JobArtifactService(
            context,
            new ArtifactOptions
        {
            WorkspaceRoot = Path.Combine(_tempRoot, "workspace"),
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
        public Task<DockerBuildResult> BuildAsync(Job job, JobImage image, string repositoryPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.True(File.Exists(Path.Combine(repositoryPath, "Dockerfile")));
            return Task.FromResult(new DockerBuildResult($"dockerizer:{image.Id:N}", $"sha256:{image.Id:N}"));
        }
    }

    private sealed class FakeRepositoryBranchProvider : IRepositoryBranchProvider
    {
        public Task<IReadOnlyCollection<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<string>>(["main", "release"]);
        }
    }

    private sealed class FakeDockerContainerRuntime : IDockerContainerRuntime
    {
        public List<(Guid JobId, int ContainerPort)> RunRequests { get; } = [];
        public List<Guid> StopRequests { get; } = [];
        public List<Guid> RestartRequests { get; } = [];
        public List<Guid> StartRequests { get; } = [];
        private readonly Dictionary<Guid, string> _statuses = [];

        public Task<ContainerDeploymentResult> RunAsync(Job job, int containerPort, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RunRequests.Add((job.Id, containerPort));
            _statuses[job.Id] = "running";

            return Task.FromResult(new ContainerDeploymentResult(
                "fake-container-id",
                $"dockerizer-job-{job.Id:N}",
                containerPort,
                45000,
                job.PublicAccessEnabled,
                null,
                "http://localhost:45000",
                "port-published",
                DateTimeOffset.UtcNow));
        }

        public Task<ContainerDeploymentResult> StartAsync(Job job, int containerPort, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartRequests.Add(job.Id);
            _statuses[job.Id] = "running";

            return Task.FromResult(new ContainerDeploymentResult(
                job.ContainerId ?? "fake-container-id",
                job.ContainerName ?? $"dockerizer-job-{job.Id:N}",
                containerPort,
                job.PublishedPort ?? 45000,
                job.PublicAccessEnabled,
                job.PublicHostname,
                job.DeploymentUrl ?? "http://localhost:45000",
                job.RouteStatus ?? "port-published",
                DateTimeOffset.UtcNow));
        }

        public Task<ContainerDeploymentResult> RestartAsync(Job job, int containerPort, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RestartRequests.Add(job.Id);
            _statuses[job.Id] = "running";

            return Task.FromResult(new ContainerDeploymentResult(
                job.ContainerId ?? "fake-container-id",
                job.ContainerName ?? $"dockerizer-job-{job.Id:N}",
                containerPort,
                job.PublishedPort ?? 45000,
                job.PublicAccessEnabled,
                job.PublicHostname,
                job.DeploymentUrl ?? "http://localhost:45000",
                job.RouteStatus ?? "port-published",
                DateTimeOffset.UtcNow));
        }

        public Task StopAsync(Job job, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopRequests.Add(job.Id);
            _statuses[job.Id] = "exited";
            return Task.CompletedTask;
        }

        public Task<ContainerRuntimeStatus> GetStatusAsync(Job job, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = _statuses.TryGetValue(job.Id, out var currentStatus)
                ? currentStatus
                : string.IsNullOrWhiteSpace(job.ContainerId)
                    ? "not_found"
                    : "running";

            return Task.FromResult(new ContainerRuntimeStatus(
                status,
                job.ContainerId,
                job.ContainerName,
                job.PublishedPort,
                job.PublicHostname,
                job.RouteStatus,
                job.DeploymentUrl));
        }

        public Task StopAndRemoveAsync(Job job, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopRequests.Add(job.Id);
            _statuses[job.Id] = "not_found";
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDockerImageStore : IDockerImageStore
    {
        public List<(string? ImageId, string? ImageTag)> RemoveRequests { get; } = [];

        public Task RemoveAsync(string? imageId, string? imageTag, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemoveRequests.Add((imageId, imageTag));
            return Task.CompletedTask;
        }
    }
}
