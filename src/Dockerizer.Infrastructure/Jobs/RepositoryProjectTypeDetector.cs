using System.Text.Json;

namespace Dockerizer.Infrastructure.Jobs;

public sealed class RepositoryProjectTypeDetector
{
    public string Detect(string repositoryPath)
    {
        if (TryDetectNodeProjectType(repositoryPath, out var nodeProjectType))
        {
            return nodeProjectType;
        }

        if (File.Exists(Path.Combine(repositoryPath, "pyproject.toml")) ||
            File.Exists(Path.Combine(repositoryPath, "requirements.txt")))
        {
            return "python";
        }

        if (File.Exists(Path.Combine(repositoryPath, "composer.json")))
        {
            return "php";
        }

        if (File.Exists(Path.Combine(repositoryPath, "go.mod")))
        {
            return "go";
        }

        if (File.Exists(Path.Combine(repositoryPath, "pom.xml")) ||
            File.Exists(Path.Combine(repositoryPath, "build.gradle")) ||
            File.Exists(Path.Combine(repositoryPath, "build.gradle.kts")))
        {
            return "java";
        }

        if (Directory.EnumerateFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories).Any() ||
            Directory.EnumerateFiles(repositoryPath, "*.sln", SearchOption.TopDirectoryOnly).Any())
        {
            return "dotnet";
        }

        if (File.Exists(Path.Combine(repositoryPath, "Dockerfile")))
        {
            return "dockerfile-only";
        }

        if (File.Exists(Path.Combine(repositoryPath, "index.html")))
        {
            return "static-html";
        }

        return "unknown";
    }

    private static bool TryDetectNodeProjectType(string repositoryPath, out string projectType)
    {
        var packageJsonPath = Path.Combine(repositoryPath, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            projectType = string.Empty;
            return false;
        }

        projectType = "nodejs";

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            var root = document.RootElement;
            var dependencies = ReadPackageKeys(root, "dependencies");
            var devDependencies = ReadPackageKeys(root, "devDependencies");
            var allPackages = new HashSet<string>(dependencies, StringComparer.OrdinalIgnoreCase);
            allPackages.UnionWith(devDependencies);
            var scripts = ReadScripts(root);

            if (allPackages.Contains("next") ||
                ScriptContainsCommand(scripts, "dev", "next") ||
                ScriptContainsCommand(scripts, "build", "next") ||
                ScriptContainsCommand(scripts, "start", "next"))
            {
                projectType = "nextjs";
                return true;
            }

            var hasReact = allPackages.Contains("react");
            var hasVite = allPackages.Contains("vite") ||
                allPackages.Contains("@vitejs/plugin-react") ||
                File.Exists(Path.Combine(repositoryPath, "vite.config.ts")) ||
                File.Exists(Path.Combine(repositoryPath, "vite.config.js")) ||
                File.Exists(Path.Combine(repositoryPath, "vite.config.mjs")) ||
                File.Exists(Path.Combine(repositoryPath, "vite.config.cjs"));

            if (hasReact && hasVite)
            {
                projectType = "react-vite";
                return true;
            }

            if (LooksLikeNodeBackend(allPackages, scripts))
            {
                projectType = "node-backend";
                return true;
            }

            return true;
        }
        catch (JsonException)
        {
            return true;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private static HashSet<string> ReadPackageKeys(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in property.EnumerateObject())
        {
            result.Add(item.Name);
        }

        return result;
    }

    private static Dictionary<string, string> ReadScripts(JsonElement root)
    {
        if (!root.TryGetProperty("scripts", out var property) || property.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in property.EnumerateObject())
        {
            if (item.Value.ValueKind == JsonValueKind.String)
            {
                result[item.Name] = item.Value.GetString() ?? string.Empty;
            }
        }

        return result;
    }

    private static bool ScriptContainsCommand(IReadOnlyDictionary<string, string> scripts, string scriptName, string token) =>
        scripts.TryGetValue(scriptName, out var command) &&
        command.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeNodeBackend(
        IReadOnlySet<string> packages,
        IReadOnlyDictionary<string, string> scripts)
    {
        string[] backendPackages =
        [
            "express",
            "fastify",
            "koa",
            "hapi",
            "@hapi/hapi",
            "@nestjs/core",
            "@nestjs/common",
            "nestjs",
            "restify",
            "sails"
        ];

        if (backendPackages.Any(packages.Contains))
        {
            return true;
        }

        return ScriptContainsCommand(scripts, "start", "node") ||
            ScriptContainsCommand(scripts, "start", "nodemon") ||
            ScriptContainsCommand(scripts, "start", "tsx") ||
            ScriptContainsCommand(scripts, "start", "ts-node") ||
            ScriptContainsCommand(scripts, "start", "nest start");
    }
}
