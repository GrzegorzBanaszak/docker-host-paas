namespace Dockerizer.Worker.Services;

public sealed class RepositoryStackDetector
{
    public Task<string> DetectAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(Path.Combine(repositoryPath, "package.json")))
        {
            return Task.FromResult("nodejs");
        }

        if (File.Exists(Path.Combine(repositoryPath, "pyproject.toml")) ||
            File.Exists(Path.Combine(repositoryPath, "requirements.txt")))
        {
            return Task.FromResult("python");
        }

        if (File.Exists(Path.Combine(repositoryPath, "composer.json")))
        {
            return Task.FromResult("php");
        }

        if (File.Exists(Path.Combine(repositoryPath, "go.mod")))
        {
            return Task.FromResult("go");
        }

        if (File.Exists(Path.Combine(repositoryPath, "pom.xml")) ||
            File.Exists(Path.Combine(repositoryPath, "build.gradle")) ||
            File.Exists(Path.Combine(repositoryPath, "build.gradle.kts")))
        {
            return Task.FromResult("java");
        }

        if (Directory.EnumerateFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories).Any() ||
            Directory.EnumerateFiles(repositoryPath, "*.sln", SearchOption.TopDirectoryOnly).Any())
        {
            return Task.FromResult("dotnet");
        }

        if (File.Exists(Path.Combine(repositoryPath, "Dockerfile")))
        {
            return Task.FromResult("dockerfile-only");
        }

        if (File.Exists(Path.Combine(repositoryPath, "index.html")))
        {
            return Task.FromResult("static-html");
        }

        return Task.FromResult("unknown");
    }
}
