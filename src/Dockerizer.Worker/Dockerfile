FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Dockerizer.sln ./
COPY src/Dockerizer.Application/Dockerizer.Application.csproj src/Dockerizer.Application/
COPY src/Dockerizer.Domain/Dockerizer.Domain.csproj src/Dockerizer.Domain/
COPY src/Dockerizer.Infrastructure/Dockerizer.Infrastructure.csproj src/Dockerizer.Infrastructure/
COPY src/Dockerizer.Worker/Dockerizer.Worker.csproj src/Dockerizer.Worker/
RUN dotnet restore src/Dockerizer.Worker/Dockerizer.Worker.csproj

COPY src ./src
RUN dotnet publish src/Dockerizer.Worker/Dockerizer.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates git docker.io \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Dockerizer.Worker.dll"]
