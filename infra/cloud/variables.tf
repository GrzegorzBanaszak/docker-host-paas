variable "ssh_user" {
  description = "SSH user used by the Docker provider to connect to the Ubuntu server."
  type        = string
}

variable "ssh_host" {
  description = "Ubuntu server hostname or IP address."
  type        = string
}

variable "ssh_port" {
  description = "SSH port on the Ubuntu server."
  type        = number
  default     = 22
}

variable "project_name" {
  description = "Name prefix used for Docker resources."
  type        = string
  default     = "dockerizer"
}

variable "api_image" {
  description = "Backend API image already built on the remote Docker engine."
  type        = string
  default     = "dockerizer-api:latest"
}

variable "worker_image" {
  description = "Backend worker image already built on the remote Docker engine."
  type        = string
  default     = "dockerizer-worker:latest"
}

variable "frontend_image" {
  description = "Frontend image already built on the remote Docker engine."
  type        = string
  default     = "dockerizer-frontend:latest"
}

variable "postgres_image" {
  description = "Postgres image."
  type        = string
  default     = "postgres:17-alpine"
}

variable "redis_image" {
  description = "Redis image."
  type        = string
  default     = "redis:7-alpine"
}

variable "postgres_db" {
  description = "Postgres database name."
  type        = string
  default     = "dockerizer"
}

variable "postgres_user" {
  description = "Postgres username."
  type        = string
  default     = "dockerizer"
}

variable "postgres_password" {
  description = "Postgres password."
  type        = string
  sensitive   = true
}

variable "api_external_port" {
  description = "Host port for the API."
  type        = number
  default     = 5169
}

variable "frontend_external_port" {
  description = "Host port for the frontend when not using the Cloudflare tunnel."
  type        = number
  default     = 80
}

variable "frontend_tunnel_bind_host" {
  description = "Host IP used to expose the frontend when enable_tunnel is true. Use 0.0.0.0 for LAN access or 127.0.0.1 for SSH-tunnel-only access."
  type        = string
  default     = "127.0.0.1"
}

variable "frontend_tunnel_external_port" {
  description = "Host port for private frontend access when enable_tunnel is true."
  type        = number
  default     = 8080
}

variable "expose_databases" {
  description = "Expose Postgres and Redis ports on the Docker host. Keep false for production."
  type        = bool
  default     = false
}

variable "postgres_external_port" {
  description = "Host port for Postgres when expose_databases is true."
  type        = number
  default     = 5432
}

variable "redis_external_port" {
  description = "Host port for Redis when expose_databases is true."
  type        = number
  default     = 6379
}

variable "worker_workspace_root" {
  description = "Path inside containers where cloned repositories and generated artifacts are stored."
  type        = string
  default     = ".worker-data/repos"
}

variable "docker_image_prefix" {
  description = "Prefix used by the worker for generated application images."
  type        = string
  default     = "dockerizer"
}

variable "application_base_domain" {
  description = "Base domain used for generated application routes."
  type        = string
  default     = "gbanaszak.pl"
}

variable "docker_runtime_binding_host" {
  description = "Host IP used by generated private application containers for published ports. Defaults to ssh_host."
  type        = string
  default     = null
}

variable "docker_runtime_public_base_url" {
  description = "Base URL shown for generated private application containers. Defaults to http://ssh_host."
  type        = string
  default     = null
}

variable "host_port_range_start" {
  description = "First host port for generated application containers."
  type        = number
  default     = 45000
}

variable "host_port_range_end" {
  description = "Last host port for generated application containers."
  type        = number
  default     = 45999
}

variable "enable_tunnel" {
  description = "Enable Traefik and Cloudflare Tunnel containers."
  type        = bool
  default     = false
}

variable "cloudflare_tunnel_token" {
  description = "Cloudflare Tunnel token. Required when enable_tunnel is true."
  type        = string
  default     = ""
  sensitive   = true
}

variable "cloudflared_image" {
  description = "Cloudflared image."
  type        = string
  default     = "cloudflare/cloudflared:latest"
}

variable "traefik_image" {
  description = "Traefik image."
  type        = string
  default     = "traefik:v3"
}

variable "traefik_docker_api_version" {
  description = "Docker API version used by Traefik's Docker provider."
  type        = string
  default     = "1.44"
}

variable "frontend_hostname" {
  description = "Hostname routed by Traefik to the frontend when enable_tunnel is true. Empty disables frontend Traefik labels."
  type        = string
  default     = ""
}
