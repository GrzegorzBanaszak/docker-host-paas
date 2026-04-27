using Dockerizer.Infrastructure;
using Dockerizer.Worker;
using Dockerizer.Worker.Configuration;
using Dockerizer.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.AddScoped<JobExecutionService>();
builder.Services.AddSingleton<IGitRepositoryCloner, GitRepositoryCloner>();
builder.Services.AddSingleton<RepositoryStackDetector>();
builder.Services.AddSingleton<ContainerizationTemplateGenerator>();
builder.Services.AddSingleton<ContainerPortResolver>();
builder.Services.AddSingleton<IDockerImageBuilder, DockerImageBuilder>();
builder.Services.AddSingleton<JobLogWriter>();
builder.Services.AddHostedService<JobProcessingWorker>();

var host = builder.Build();
host.Run();
