locals {
  infra_network_name  = "${var.project_name}-infra"
  public_network_name = "${var.project_name}-public"
  postgres_volume     = "${var.project_name}-postgres-data"
  redis_volume        = "${var.project_name}-redis-data"
  worker_data_volume  = "${var.project_name}-worker-data"

  postgres_connection_string = "Host=${var.project_name}-postgres;Port=5432;Database=${var.postgres_db};Username=${var.postgres_user};Password=${var.postgres_password}"
  redis_connection_string    = "${var.project_name}-redis:6379"
  docker_runtime_binding_host = coalesce(
    var.docker_runtime_binding_host,
    var.ssh_host,
  )
  docker_runtime_public_base_url = coalesce(
    var.docker_runtime_public_base_url,
    "http://${var.ssh_host}",
  )

  common_app_env = [
    "ConnectionStrings__Postgres=${local.postgres_connection_string}",
    "ConnectionStrings__Redis=${local.redis_connection_string}",
    "Redis__QueueKey=${var.project_name}:jobs",
    "DockerRuntime__ContainerNamePrefix=${var.project_name}-job",
    "DockerRuntime__BindingHost=${local.docker_runtime_binding_host}",
    "DockerRuntime__PublicBaseUrl=${local.docker_runtime_public_base_url}",
    "DockerRuntime__HostPortRangeStart=${var.host_port_range_start}",
    "DockerRuntime__HostPortRangeEnd=${var.host_port_range_end}",
    "DockerRuntime__StartupTimeoutSeconds=60",
    "DockerRuntime__StartupPollIntervalMilliseconds=1000",
    "DockerRuntime__ContainerCpuLimit=1.0",
    "DockerRuntime__ContainerMemoryLimit=512m",
    "DockerRuntime__ContainerPidsLimit=256",
    "DockerRuntime__DisableContainerNetwork=false",
    "ApplicationRouting__Mode=TunnelWildcard",
    "ApplicationRouting__PublicScheme=https",
    "ApplicationRouting__BaseDomain=${var.application_base_domain}",
    "ApplicationRouting__DockerNetwork=${local.public_network_name}",
    "ApplicationRouting__ReverseProxy=Traefik",
    "RepositorySecurity__AllowedHosts__0=github.com",
    "RepositorySecurity__CloneTimeoutSeconds=120",
    "Worker__WorkspaceRoot=${var.worker_workspace_root}",
  ]

  frontend_labels = var.enable_tunnel && var.frontend_hostname != "" ? {
    "traefik.enable"                                                              = "true"
    "traefik.http.routers.${var.project_name}-frontend.rule"                      = "Host(`${var.frontend_hostname}`)"
    "traefik.http.routers.${var.project_name}-frontend.entrypoints"               = "web"
    "traefik.http.services.${var.project_name}-frontend.loadbalancer.server.port" = "80"
  } : {}
}

resource "docker_network" "infra" {
  name = local.infra_network_name
}

resource "docker_network" "public" {
  name = local.public_network_name
}

resource "docker_volume" "postgres" {
  name = local.postgres_volume
}

resource "docker_volume" "redis" {
  name = local.redis_volume
}

resource "docker_volume" "worker_data" {
  name = local.worker_data_volume
}

resource "docker_image" "postgres" {
  name         = var.postgres_image
  keep_locally = true
}

resource "docker_image" "redis" {
  name         = var.redis_image
  keep_locally = true
}

resource "docker_image" "traefik" {
  count        = var.enable_tunnel ? 1 : 0
  name         = var.traefik_image
  keep_locally = true
}

resource "docker_image" "cloudflared" {
  count        = var.enable_tunnel ? 1 : 0
  name         = var.cloudflared_image
  keep_locally = true
}

resource "docker_container" "postgres" {
  name    = "${var.project_name}-postgres"
  image   = docker_image.postgres.image_id
  restart = "unless-stopped"

  env = [
    "POSTGRES_DB=${var.postgres_db}",
    "POSTGRES_USER=${var.postgres_user}",
    "POSTGRES_PASSWORD=${var.postgres_password}",
  ]

  volumes {
    volume_name    = docker_volume.postgres.name
    container_path = "/var/lib/postgresql/data"
  }

  networks_advanced {
    name    = docker_network.infra.name
    aliases = ["${var.project_name}-postgres", "postgres"]
  }

  dynamic "ports" {
    for_each = var.expose_databases ? [1] : []
    content {
      internal = 5432
      external = var.postgres_external_port
    }
  }

  healthcheck {
    test         = ["CMD-SHELL", "pg_isready -U ${var.postgres_user} -d ${var.postgres_db}"]
    interval     = "10s"
    timeout      = "5s"
    retries      = 5
    start_period = "10s"
  }
}

resource "docker_container" "redis" {
  name    = "${var.project_name}-redis"
  image   = docker_image.redis.image_id
  restart = "unless-stopped"
  command = ["redis-server", "--appendonly", "yes"]

  volumes {
    volume_name    = docker_volume.redis.name
    container_path = "/data"
  }

  networks_advanced {
    name    = docker_network.infra.name
    aliases = ["${var.project_name}-redis", "redis"]
  }

  dynamic "ports" {
    for_each = var.expose_databases ? [1] : []
    content {
      internal = 6379
      external = var.redis_external_port
    }
  }

  healthcheck {
    test         = ["CMD", "redis-cli", "ping"]
    interval     = "10s"
    timeout      = "5s"
    retries      = 5
    start_period = "10s"
  }
}

resource "docker_container" "api" {
  name    = "${var.project_name}-api"
  image   = var.api_image
  restart = "unless-stopped"

  env = concat(local.common_app_env, [
    "ASPNETCORE_URLS=http://+:5169",
    "ASPNETCORE_ENVIRONMENT=Production",
  ])

  ports {
    internal = 5169
    external = var.api_external_port
  }

  volumes {
    host_path      = "/var/run/docker.sock"
    container_path = "/var/run/docker.sock"
  }

  volumes {
    volume_name    = docker_volume.worker_data.name
    container_path = "/app/${var.worker_workspace_root}"
  }

  networks_advanced {
    name    = docker_network.infra.name
    aliases = ["${var.project_name}-api", "dockerizer-api"]
  }

  networks_advanced {
    name = docker_network.public.name
  }

  depends_on = [
    docker_container.postgres,
    docker_container.redis,
  ]
}

resource "docker_container" "worker" {
  name    = "${var.project_name}-worker"
  image   = var.worker_image
  restart = "unless-stopped"

  env = concat(local.common_app_env, [
    "DOTNET_ENVIRONMENT=Production",
    "Worker__DockerImagePrefix=${var.docker_image_prefix}",
    "Worker__DockerBuildTimeoutMinutes=10",
    "Worker__QueuePollIntervalSeconds=5",
    "Worker__CleanupWorkspaceAfterCompletion=true",
  ])

  volumes {
    host_path      = "/var/run/docker.sock"
    container_path = "/var/run/docker.sock"
  }

  volumes {
    volume_name    = docker_volume.worker_data.name
    container_path = "/app/${var.worker_workspace_root}"
  }

  networks_advanced {
    name    = docker_network.infra.name
    aliases = ["${var.project_name}-worker"]
  }

  networks_advanced {
    name = docker_network.public.name
  }

  depends_on = [
    docker_container.postgres,
    docker_container.redis,
  ]
}

resource "docker_container" "frontend" {
  name    = "${var.project_name}-frontend"
  image   = var.frontend_image
  restart = "unless-stopped"

  networks_advanced {
    name    = docker_network.infra.name
    aliases = ["${var.project_name}-frontend"]
  }

  networks_advanced {
    name = docker_network.public.name
  }

  dynamic "ports" {
    for_each = [1]
    content {
      internal = 80
      external = var.enable_tunnel ? var.frontend_tunnel_external_port : var.frontend_external_port
      ip       = var.enable_tunnel ? var.frontend_tunnel_bind_host : "0.0.0.0"
    }
  }

  dynamic "labels" {
    for_each = local.frontend_labels
    content {
      label = labels.key
      value = labels.value
    }
  }

  depends_on = [
    docker_container.api,
  ]
}

resource "docker_container" "traefik" {
  count   = var.enable_tunnel ? 1 : 0
  name    = "${var.project_name}-traefik"
  image   = docker_image.traefik[0].image_id
  restart = "unless-stopped"

  env = [
    "DOCKER_API_VERSION=${var.traefik_docker_api_version}",
  ]

  command = [
    "--providers.docker=true",
    "--providers.docker.exposedbydefault=false",
    "--entrypoints.web.address=:80",
  ]

  volumes {
    host_path      = "/var/run/docker.sock"
    container_path = "/var/run/docker.sock"
    read_only      = true
  }

  networks_advanced {
    name = docker_network.public.name
  }
}

resource "docker_container" "cloudflared" {
  count   = var.enable_tunnel ? 1 : 0
  name    = "${var.project_name}-cloudflared"
  image   = docker_image.cloudflared[0].image_id
  restart = "unless-stopped"

  command = ["tunnel", "--no-autoupdate", "run", "--token", var.cloudflare_tunnel_token]

  networks_advanced {
    name = docker_network.public.name
  }

  depends_on = [
    docker_container.traefik,
  ]
}
