namespace Dockerizer.Infrastructure.Jobs;

public sealed class RepositoryProjectPathResolver
{
    public string Normalize(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return string.Empty;
        }

        var normalized = projectPath
            .Trim()
            .Replace('\\', '/')
            .Trim('/');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Any(segment => segment == "." || segment == ".."))
        {
            throw new InvalidOperationException("ProjectPath must stay inside the repository and cannot contain '.' or '..' segments.");
        }

        return string.Join('/', segments);
    }

    public string Resolve(string repositoryRootPath, string? projectPath)
    {
        var normalized = Normalize(projectPath);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return repositoryRootPath;
        }

        var resolvedPath = Path.GetFullPath(Path.Combine(repositoryRootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var repositoryRootFullPath = Path.GetFullPath(repositoryRootPath);

        if (!resolvedPath.StartsWith(repositoryRootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ProjectPath resolves outside the repository root.");
        }

        if (!Directory.Exists(resolvedPath))
        {
            throw new InvalidOperationException($"ProjectPath '{normalized}' does not exist in the repository.");
        }

        return resolvedPath;
    }
}
