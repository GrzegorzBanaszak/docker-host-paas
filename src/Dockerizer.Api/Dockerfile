FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Dockerizer.sln ./
COPY src/Dockerizer.Api/Dockerizer.Api.csproj src/Dockerizer.Api/
COPY src/Dockerizer.Application/Dockerizer.Application.csproj src/Dockerizer.Application/
COPY src/Dockerizer.Domain/Dockerizer.Domain.csproj src/Dockerizer.Domain/
COPY src/Dockerizer.Infrastructure/Dockerizer.Infrastructure.csproj src/Dockerizer.Infrastructure/
RUN dotnet restore src/Dockerizer.Api/Dockerizer.Api.csproj

COPY src ./src
RUN dotnet publish src/Dockerizer.Api/Dockerizer.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5169

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl git docker.io \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./
EXPOSE 5169
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD curl -f http://localhost:5169/health || exit 1
ENTRYPOINT ["dotnet", "Dockerizer.Api.dll"]
