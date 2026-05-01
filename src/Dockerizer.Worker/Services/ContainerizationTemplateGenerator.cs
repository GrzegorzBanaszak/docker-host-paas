namespace Dockerizer.Worker.Services;

public sealed class ContainerizationTemplateGenerator(ILogger<ContainerizationTemplateGenerator> logger)
{
    public async Task GenerateAsync(string repositoryPath, string detectedStack, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dockerfilePath = Path.Combine(repositoryPath, "Dockerfile");
        var dockerignorePath = Path.Combine(repositoryPath, ".dockerignore");

        if (!File.Exists(dockerfilePath))
        {
            var dockerfileContents = BuildDockerfile(detectedStack);
            await File.WriteAllTextAsync(dockerfilePath, dockerfileContents, cancellationToken);
            logger.LogInformation("Generated Dockerfile for stack {DetectedStack} in {RepositoryPath}.", detectedStack, repositoryPath);
        }
        else
        {
            logger.LogInformation("Dockerfile already exists in {RepositoryPath}. Skipping generation.", repositoryPath);
        }

        if (!File.Exists(dockerignorePath))
        {
            var dockerignoreContents = BuildDockerignore(detectedStack);
            await File.WriteAllTextAsync(dockerignorePath, dockerignoreContents, cancellationToken);
            logger.LogInformation("Generated .dockerignore for stack {DetectedStack} in {RepositoryPath}.", detectedStack, repositoryPath);
        }
        else
        {
            logger.LogInformation(".dockerignore already exists in {RepositoryPath}. Skipping generation.", repositoryPath);
        }
    }

    private static string BuildDockerfile(string detectedStack) =>
        detectedStack switch
        {
            "nodejs" => """
                FROM node:22-alpine
                WORKDIR /app

                COPY package*.json ./
                RUN npm ci

                COPY . .

                EXPOSE 3000
                CMD ["npm", "start"]
                """,
            "node-backend" => """
                FROM node:22-alpine
                WORKDIR /app

                COPY package*.json ./
                RUN npm ci

                COPY . .

                EXPOSE 3000
                CMD ["npm", "start"]
                """,
            "react-vite" => """
                FROM node:22-alpine AS build
                WORKDIR /app

                COPY package*.json ./
                RUN npm ci

                COPY . .
                RUN npm run build

                FROM nginx:1.27-alpine
                COPY --from=build /app/dist /usr/share/nginx/html

                EXPOSE 80
                CMD ["nginx", "-g", "daemon off;"]
                """,
            "nextjs" => """
                FROM node:22-alpine AS build
                WORKDIR /app

                COPY package*.json ./
                RUN npm ci

                COPY . .
                RUN npm run build

                FROM node:22-alpine
                WORKDIR /app
                ENV NODE_ENV=production

                COPY --from=build /app ./

                EXPOSE 3000
                CMD ["npm", "run", "start"]
                """,
            "python" => """
                FROM python:3.12-slim
                WORKDIR /app

                COPY requirements.txt ./
                RUN pip install --no-cache-dir -r requirements.txt

                COPY . .

                EXPOSE 8000
                CMD ["python", "app.py"]
                """,
            "php" => """
                FROM php:8.3-cli-alpine
                WORKDIR /app

                COPY . .

                EXPOSE 8000
                CMD ["php", "-S", "0.0.0.0:8000", "-t", "public"]
                """,
            "go" => """
                FROM golang:1.23-alpine AS build
                WORKDIR /src

                COPY go.mod go.sum* ./
                RUN go mod download

                COPY . .
                RUN go build -o /out/app .

                FROM alpine:3.20
                WORKDIR /app
                COPY --from=build /out/app /app/app

                EXPOSE 8080
                CMD ["/app/app"]
                """,
            "java" => """
                FROM eclipse-temurin:21-jdk-alpine
                WORKDIR /app

                COPY . .

                EXPOSE 8080
                CMD ["sh", "-c", "if [ -f ./mvnw ]; then ./mvnw spring-boot:run; else java -jar app.jar; fi"]
                """,
            "dotnet" => """
                FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
                WORKDIR /src

                COPY . .
                RUN dotnet publish -c Release -o /app/publish

                FROM mcr.microsoft.com/dotnet/aspnet:9.0
                WORKDIR /app
                COPY --from=build /app/publish .

                EXPOSE 8080
                CMD ["sh", "-c", "dotnet $(find . -maxdepth 1 -name '*.dll' | head -n 1)"]
                """,
            "dockerfile-only" => """
                # Existing Dockerfile detected in repository.
                # Generation skipped by Dockerizer.
                """,
            "static-html" => """
                FROM nginx:1.27-alpine

                COPY . /usr/share/nginx/html

                EXPOSE 80
                CMD ["nginx", "-g", "daemon off;"]
                """,
            _ => """
                FROM alpine:3.20
                WORKDIR /app

                COPY . .

                CMD ["sh"]
                """
        };

    private static string BuildDockerignore(string detectedStack)
    {
        var common = """
            .git
            .gitignore
            .DS_Store
            Thumbs.db
            docker-compose*.yml
            .env
            .env.*
            bin/
            obj/
            node_modules/
            .worker-data/
            """;

        return detectedStack switch
        {
            "python" => common + Environment.NewLine + "__pycache__/" + Environment.NewLine + "*.pyc" + Environment.NewLine,
            "nextjs" => common + Environment.NewLine + ".next/" + Environment.NewLine,
            "react-vite" => common + Environment.NewLine + "dist/" + Environment.NewLine,
            _ => common + Environment.NewLine
        };
    }
}
