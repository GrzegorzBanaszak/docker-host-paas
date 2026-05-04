# Dockerizer cloud infrastructure

Terraform in this directory manages Docker resources on a remote Ubuntu host over SSH.

## Prerequisites

- Docker installed and running on the Ubuntu server.
- SSH user added to the `docker` group on the Ubuntu server.
- Application images built on the remote Docker engine before `terraform apply`.

## Build images on the remote Docker engine

Run these commands from the repository root:

```powershell
docker context create ubuntu-prod --docker "host=ssh://deploy@203.0.113.10"

docker --context ubuntu-prod build -f src\Dockerizer.Api\Dockerfile -t dockerizer-api:latest .
docker --context ubuntu-prod build -f src\Dockerizer.Worker\Dockerfile -t dockerizer-worker:latest .
docker --context ubuntu-prod build -f frontend\Dockerfile -t dockerizer-frontend:latest .
```

## Deploy

```powershell
Copy-Item infra\cloud\terraform.tfvars.example infra\cloud\terraform.tfvars
terraform -chdir=infra\cloud init
terraform -chdir=infra\cloud plan
terraform -chdir=infra\cloud apply
```

Set real values in `terraform.tfvars` before applying.

Do not commit `terraform.tfvars`; it contains secrets.

## Frontend access with tunnel mode

When `enable_tunnel = true`, generated applications can still use the public
Cloudflare tunnel. The admin frontend can be exposed separately on port `8080`.

For LAN access:

```hcl
frontend_tunnel_bind_host     = "0.0.0.0"
frontend_tunnel_external_port = 8080
```

Then browse to:

```text
http://203.0.113.10:8080
```

Private generated application containers use the Docker runtime host settings.
Set them to the Ubuntu server address when you want private apps exposed on the
server LAN IP instead of `localhost`:

```hcl
docker_runtime_binding_host    = "203.0.113.10"
docker_runtime_public_base_url = "http://203.0.113.10"
```

For SSH-tunnel-only access, use:

```hcl
frontend_tunnel_bind_host     = "127.0.0.1"
frontend_tunnel_external_port = 8080
```

Open it from your local machine:

```powershell
ssh -L 8080:127.0.0.1:8080 deploy@203.0.113.10
```

Then browse to:

```text
http://localhost:8080
```
