terraform {
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "3.6.2"
    }
  }
}

provider "docker" {
  host = "ssh://${var.ssh_user}@${var.ssh_host}:${var.ssh_port}"
}
