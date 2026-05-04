output "api_url" {
  description = "API URL exposed on the Docker host."
  value       = "http://${var.ssh_host}:${var.api_external_port}"
}

output "frontend_url" {
  description = "Frontend URL exposed on the Docker host."
  value       = var.enable_tunnel ? "http://${var.frontend_tunnel_bind_host}:${var.frontend_tunnel_external_port}" : "http://${var.ssh_host}:${var.frontend_external_port}"
}

output "postgres_host_port" {
  description = "Postgres host port when expose_databases is true."
  value       = var.expose_databases ? var.postgres_external_port : null
  sensitive   = true
}

output "redis_host_port" {
  description = "Redis host port when expose_databases is true."
  value       = var.expose_databases ? var.redis_external_port : null
  sensitive   = true
}
