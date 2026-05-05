# Dockerizer

Dockerizer is a self-hosted platform for turning GitHub projects into runnable Docker deployments. A user submits a repository URL, the backend creates an asynchronous job, and a worker clones the repository, detects the project type, generates containerization files when needed, builds a Docker image, starts a container, and stores logs, generated files, image history, and deployment metadata.

The project is currently organized as a full-stack application with a .NET backend, a .NET worker, PostgreSQL, Redis, a React/Vite frontend, local Docker Compose infrastructure, and Terraform-based deployment assets for a remote Ubuntu Docker host.

## Current Capabilities

- Create and manage containerization jobs from GitHub repository URLs.
- Inspect repository branches and optional project paths before creating a job.
- Detect common project types, including Node.js backends, React/Vite, Next.js, static HTML, Python, PHP, Go, Java, .NET, and repositories with existing Dockerfiles.
- Generate `Dockerfile` and `.dockerignore` files when the repository does not already provide them.
- Build Docker images and keep image history per job.
- Start, stop, restart, rebuild, retry, cancel, and delete jobs.
- Persist job logs, generated files, image artifacts, container status, deployment URLs, and routing metadata.
- Expose generated applications through published ports or Traefik/Cloudflare Tunnel routing.
- Show system resource snapshots from Docker.

## Architecture

```text
frontend/                 React 18 + Vite + Tailwind UI
src/Dockerizer.Api/       ASP.NET Core API and HTTP endpoints
src/Dockerizer.Worker/    Background worker that processes queued jobs
src/Dockerizer.Application/
                           DTOs and application abstractions
src/Dockerizer.Domain/    Domain entities and job status model
src/Dockerizer.Infrastructure/
                           EF Core, Redis queue, Docker runtime, artifacts, DNS/routing
tests/                    xUnit end-to-end pipeline tests with fixtures
infra/local/              Local PostgreSQL, Redis, Traefik and Cloudflared compose file
infra/cloud/              Terraform deployment for a remote Docker host
docs/                     Project notes and planning documents
```

The runtime flow is:

1. The frontend calls the API.
2. The API validates the request, stores the job in PostgreSQL, and enqueues the job ID in Redis.
3. The worker polls Redis, clones the repository, resolves the selected project path, detects the stack, and generates missing Docker files.
4. The worker builds the image, starts the generated application container, stores artifacts and logs, and updates the job status.
5. The frontend polls the API for status, logs, generated files, image details, and route information.

## Technology Stack

- .NET 9, ASP.NET Core, hosted worker services
- Entity Framework Core 9 with PostgreSQL
- Redis via StackExchange.Redis
- Docker CLI/runtime integration
- React 18, TypeScript, Vite, React Query, React Hook Form, Zod, Tailwind CSS
- xUnit end-to-end tests
- Docker Compose for local dependencies
- Terraform Docker provider for remote deployment

## Prerequisites

- .NET SDK 9
- Node.js 22 or a compatible modern Node.js version
- Docker Engine
- Docker Compose plugin
- PostgreSQL and Redis, normally started from `infra/local/docker-compose.yml`
- Git
- Terraform, only for the remote deployment path

The worker builds and runs untrusted repository code through Docker. Use this project on a controlled host and keep Docker runtime limits, allowed repository hosts, and workspace cleanup settings configured for the target environment.

## Local Development

Start local infrastructure from the repository root:

```powershell
docker compose -f infra\local\docker-compose.yml up -d postgres redis
```

Run the API:

```powershell
dotnet run --project src\Dockerizer.Api\Dockerizer.Api.csproj --launch-profile http
```

Run the worker in a second terminal:

```powershell
dotnet run --project src\Dockerizer.Worker\Dockerizer.Worker.csproj
```

Run the frontend in a third terminal:

```powershell
cd frontend
npm install
npm run dev
```

The frontend development server listens on `http://localhost:5173` and proxies `/api` and `/health` to `http://localhost:5169`.

Development settings use:

- PostgreSQL: `localhost:5432`, database `dockerizer`, user `postgres`, password `postgres`
- Redis: `localhost:6379`
- API: `http://localhost:5169`
- Worker workspace: `../../.worker-data/repos`
- Generated app port range: `45000-45999`

## Optional Tunnel Services

The local compose file also defines Traefik and Cloudflared under the `tunnel` profile:

```powershell
docker compose -f infra\local\docker-compose.yml --profile tunnel up -d
```

Set `CLOUDFLARE_TUNNEL_TOKEN` when using Cloudflare Tunnel. Application routing is configured through the `ApplicationRouting` settings in the API and worker configuration.

## Build and Test

Build the backend solution:

```powershell
dotnet build Dockerizer.sln
```

Run backend tests:

```powershell
dotnet test Dockerizer.sln -v minimal
```

Build the frontend:

```powershell
cd frontend
npm run build
```

Build production images from the repository root:

```powershell
docker build -f src\Dockerizer.Api\Dockerfile -t dockerizer-api:latest .
docker build -f src\Dockerizer.Worker\Dockerfile -t dockerizer-worker:latest .
docker build -f frontend\Dockerfile -t dockerizer-frontend:latest .
```

## API Surface

The frontend currently uses these API areas:

- `GET /health`
- `GET /api/jobs`
- `POST /api/jobs`
- `GET /api/jobs/branches`
- `GET /api/jobs/{id}`
- `GET /api/jobs/{id}/logs`
- `GET /api/jobs/{id}/files`
- `POST /api/jobs/{id}/retry`
- `POST /api/jobs/{id}/rebuild`
- `POST /api/jobs/{id}/cancel`
- `POST /api/jobs/{id}/container/start`
- `POST /api/jobs/{id}/container/restart`
- `POST /api/jobs/{id}/container/stop`
- `DELETE /api/jobs/{id}`
- `GET /api/images`
- `GET /api/images/{id}`
- `DELETE /api/images/{id}`
- `GET /api/dns/overview`
- `GET /api/dns/routes`
- `POST /api/dns/routes/{jobId}/publish`
- `DELETE /api/dns/routes/{jobId}/publish`
- `GET /api/system/resources`

## Remote Deployment

Remote deployment assets live in `infra/cloud`. Terraform manages Docker resources on an Ubuntu host over SSH, while application images are expected to be built on the remote Docker engine before `terraform apply`.

See [infra/cloud/README.md](infra/cloud/README.md) for the full workflow, including remote Docker context setup, Terraform variables, tunnel mode, and frontend access options.

## Configuration Notes

Important configuration sections:

- `ConnectionStrings:Postgres` and `ConnectionStrings:Redis`
- `Redis:QueueKey`
- `Worker:WorkspaceRoot`
- `Worker:DockerImagePrefix`
- `Worker:DockerBuildTimeoutMinutes`
- `Worker:CleanupWorkspaceAfterCompletion`
- `DockerRuntime:*` for generated container naming, port range, startup timeout, CPU, memory, PID limits, and networking
- `ApplicationRouting:*` for port mode or Traefik/Cloudflare Tunnel routing
- `RepositorySecurity:AllowedHosts` and `RepositorySecurity:CloneTimeoutSeconds`

The API applies EF Core migrations automatically on startup.

## Repository State

The MVP is implemented beyond the initial planning notes: job creation, queue processing, image builds, generated file persistence, deployment metadata, image catalog, route management, and frontend screens are present. Remaining production-hardening work should focus on stricter isolation for untrusted code, operational monitoring, backups, authentication/authorization, and deployment-specific secret management.
