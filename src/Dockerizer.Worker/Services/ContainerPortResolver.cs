namespace Dockerizer.Worker.Services;

public sealed class ContainerPortResolver
{
    public int Resolve(string repositoryPath, string detectedStack)
    {
        var dockerfilePath = Path.Combine(repositoryPath, "Dockerfile");
        if (File.Exists(dockerfilePath))
        {
            var explicitPort = TryReadExposedPort(dockerfilePath);
            if (explicitPort.HasValue)
            {
                return explicitPort.Value;
            }
        }

        return detectedStack switch
        {
            "nodejs" => 3000,
            "node-backend" => 3000,
            "nextjs" => 3000,
            "react-vite" => 80,
            "python" => 8000,
            "php" => 8000,
            "go" => 8080,
            "java" => 8080,
            "dotnet" => 8080,
            "dockerfile-only" => 8080,
            "static-html" => 80,
            _ => 8080,
        };
    }

    private static int? TryReadExposedPort(string dockerfilePath)
    {
        foreach (var rawLine in File.ReadLines(dockerfilePath))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("EXPOSE ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tokens = line["EXPOSE ".Length..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                var candidate = token.Split('/', 2, StringSplitOptions.TrimEntries)[0];
                if (int.TryParse(candidate, out var port))
                {
                    return port;
                }
            }
        }

        return null;
    }
}
