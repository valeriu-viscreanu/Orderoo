# Orderoo

A .NET order management API built with CQRS (MediatR), backed by SQL Server and RabbitMQ.

## Stack

- **.NET 10.0** — Minimal APIs
- **Entity Framework Core** — SQL Server
- **MediatR** — CQRS command/query dispatch and in-memory event publishing
- **FluentValidation** — request validation via a MediatR pipeline behavior
- **MassTransit + RabbitMQ** — event publishing
- **AspNetCoreRateLimit** — IP-based rate limiting

See [.ai-context.md](.ai-context.md) for the full architecture breakdown.

## Project structure

```
OrderApi/           # The web API (commands, queries, handlers, endpoints)
OrderProcessor/      # Background order processor (not yet wired into compose/k8s — see note below)
docker-compose.yml    # Local dev: SQL Server, RabbitMQ, OrderApi
k8s/                 # Kubernetes manifests (see k8s/README.md)
```

> **Note:** `OrderProcessor` isn't deployed by either `docker-compose.yml` or `k8s/` yet.
> Its config still targets Kafka while the rest of the system uses RabbitMQ — it needs
> porting before it can join the stack.

## Running locally — Docker Compose

```bash
docker compose up --build
```

This starts SQL Server, RabbitMQ (with management UI), and OrderApi with host ports
already published:

| Service | URL |
| --- | --- |
| OrderApi | http://localhost:8080 |
| RabbitMQ management UI | http://localhost:15672 (`admin` / `admin123`) |
| SQL Server | `localhost,1433` (`sa` / `YourStrong@Password123`) |

## Running locally — Kubernetes

See [k8s/README.md](k8s/README.md) for the full step-by-step guide (Docker Desktop + kind).
Quick version:

```bash
docker build -t orderoo/orderapi:latest ./OrderApi
kubectl apply -k k8s/
kubectl -n orderoo get pods -w
```

Kubernetes Services are `ClusterIP` (internal only) — use `kubectl port-forward` to reach
them from your machine, e.g.:

```bash
kubectl -n orderoo port-forward svc/orderapi 8080:8080
```

## API

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/orders` | List order summaries |
| `GET` | `/api/orders/{id}` | Get a single order |
| `POST` | `/api/orders` | Create an order |

See [OrderApi/OrderApi.http](OrderApi/OrderApi.http) for example requests.
