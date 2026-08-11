# Orderoo on Kubernetes

Kubernetes manifests translated from `docker-compose.yml`. Written for a local
**Docker Desktop + kind** cluster, but nothing here is Docker Desktop specific.

| File | Contents |
| --- | --- |
| `00-namespace.yaml` | `orderoo` namespace |
| `01-secrets.yaml` | SA password, RabbitMQ creds, DB connection string (**dev only**) |
| `02-configmap.yaml` | Non-secret OrderApi env (ASP.NET + RabbitMQ host/port) |
| `10-sqlserver.yaml` | SQL Server 2022 Deployment + PVC + Service |
| `20-rabbitmq.yaml` | RabbitMQ 3 (management) Deployment + PVC + Service |
| `30-orderapi.yaml` | OrderApi Deployment + Service |
| `kustomization.yaml` | Ties them together for `kubectl apply -k` |

---

## Step 0 — Fix your kubectl first

This matters before anything else. On this machine:

```
kubectl on PATH   -> C:\Windows\system32\kubectl.exe   v1.28.4   (stale leftover)
Docker Desktop    -> ...\Docker\resources\bin\kubectl.exe v1.34.1
Cluster you want  -> v1.36.1
```

kubectl supports **one minor version** of skew. v1.28 against a v1.36 server is
eight versions out and will misbehave in confusing ways. Check what you have:

```bash
kubectl version --client
```

If it says `v1.28.x`, use the Docker Desktop one instead. Put its directory
ahead of `system32` on PATH (System Properties -> Environment Variables ->
edit `Path`, move `C:\Program Files\Docker\Docker\resources\bin` to the top),
then open a new terminal and confirm:

```bash
kubectl version --client
```

Better still, install a matching 1.36 client:

```bash
winget install -e --id Kubernetes.kubectl
```

Deleting `C:\Windows\system32\kubectl.exe` also works, but only do that if you
know nothing else depends on it.

---

## Step 1 — Enable Kubernetes in Docker Desktop

1. Docker Desktop -> **Settings** -> **Kubernetes**
2. Tick **Enable Kubernetes**
3. Cluster provisioning method: **kind**
4. Version: **1.36.1**
5. **Apply & Restart**

kind requires the **containerd image store**. If Docker Desktop complains,
enable it under Settings -> General -> *Use containerd for pulling and storing
images*, then come back.

First start pulls the node image and takes a few minutes. Wait until the
Kubernetes indicator in the Docker Desktop status bar is green.

Then point kubectl at it and confirm the cluster answers:

```bash
kubectl config use-context docker-desktop
```

```bash
kubectl get nodes
```

You want a node in `Ready` state. If `kubectl config current-context` errors
with *current-context is not set*, Kubernetes has not finished starting.

### Give it enough memory

SQL Server alone requests 2Gi and will not start below that. The whole stack
needs roughly **6Gi** of headroom. Docker Desktop on Windows draws from WSL2,
which by default has no cap and will take what it needs — but if you have a
`%USERPROFILE%\.wslconfig` pinning `memory=` low, raise it to at least 8GB and
run `wsl --shutdown` before restarting Docker Desktop.

---

## Step 2 — Build the OrderApi image

The compose file builds OrderApi from source; Kubernetes does not build
anything, so build it yourself first. The tag must match the `image:` field in
`30-orderapi.yaml`.

From the repo root (`C:\sandbox\Orderoo`):

```bash
docker build -t orderoo/orderapi:latest ./OrderApi
```

Confirm it exists:

```bash
docker images orderoo/orderapi
```

---

## Step 3 — Load the image into the kind cluster

**Do not skip this.** kind nodes are separate containers with their own image
store. An image sitting in Docker Desktop is invisible to them, and the pod
will sit in `ErrImagePull` / `ImagePullBackOff` trying to reach Docker Hub for
an image that only exists locally.

Find your cluster's name (Docker Desktop's is usually `desktop`):

```bash
kind get clusters
```

Then load it, substituting the name you just saw:

```bash
kind load docker-image orderoo/orderapi:latest --name desktop
```

If `kind` is not a recognised command, install the CLI:

```bash
winget install -e --id Kubernetes.kind
```

<details>
<summary>Fallback if you cannot install the kind CLI</summary>

Copy the image into the node's containerd store by hand. Replace
`desktop-control-plane` with the container name from `docker ps`:

```bash
docker save orderoo/orderapi:latest -o orderapi.tar && docker cp orderapi.tar desktop-control-plane:/orderapi.tar && docker exec desktop-control-plane ctr -n k8s.io images import /orderapi.tar
```
</details>

You must re-run this load step **every time you rebuild** the image.

---

## Step 4 — Deploy

```bash
kubectl apply -k k8s/
```

Watch it come up:

```bash
kubectl -n orderoo get pods -w
```

Expected sequence: `sqlserver` and `rabbitmq` go `Running` then `READY 1/1`;
`orderapi` sits in `Init:0/2` until both are reachable, then starts. SQL
Server's first boot creates its system databases, so allow **2–3 minutes**
before assuming something is wrong. Press `Ctrl+C` to stop watching.

Everything ready looks like:

```
NAME                         READY   STATUS    RESTARTS   AGE
orderapi-xxxxxxxxxx-xxxxx    1/1     Running   0          2m
rabbitmq-xxxxxxxxxx-xxxxx    1/1     Running   0          3m
sqlserver-xxxxxxxxxx-xxxxx   1/1     Running   0          3m
```

---

## Step 5 — Reach the services

All Services are `ClusterIP` (internal only). `port-forward` is the most
reliable way in on kind — NodePort needs port mappings baked in at cluster
creation, which Docker Desktop controls.

Each command below blocks; run it in its own terminal.

**OrderApi** -> http://localhost:8080

```bash
kubectl -n orderoo port-forward svc/orderapi 8080:8080
```

**RabbitMQ management UI** -> http://localhost:15672 (`admin` / `admin123`)

```bash
kubectl -n orderoo port-forward svc/rabbitmq 15672:15672
```

**SQL Server** -> `localhost,1433` (`sa` / `YourStrong@Password123`)

```bash
kubectl -n orderoo port-forward svc/sqlserver 1433:1433
```

### Smoke test

With the OrderApi forward running, in another terminal:

```bash
curl http://localhost:8080/api/orders
```

`[]` (or a list) means the API is up and its migration against SQL Server
succeeded. There is no `/health` endpoint — see *Notes* below.

---

## Step 6 — Tear down

Delete the workloads but keep the data volumes:

```bash
kubectl delete -k k8s/
```

Delete everything including the PVCs (SQL Server and RabbitMQ data):

```bash
kubectl delete namespace orderoo
```

---

## Redeploying after a code change

```bash
docker build -t orderoo/orderapi:latest ./OrderApi && kind load docker-image orderoo/orderapi:latest --name desktop && kubectl -n orderoo rollout restart deployment/orderapi
```

The `rollout restart` is required: the tag is unchanged, so Kubernetes has no
reason to notice the new image on its own.

---

## Notes and deliberate choices

**Replicas are pinned to 1.** `Program.cs` calls
`dbContext.Database.Migrate()` at startup. EF Core migrations are not safe to
run concurrently — two pods booting together can deadlock or double-apply. To
scale out, move the migration to a one-shot `Job` and drop the `Migrate()` call.

**Probes are TCP, not HTTP.** The app exposes no `/health` endpoint, so an
`httpGet` probe would hit a 404 and be read as unhealthy. Adding
`builder.Services.AddHealthChecks()` and `app.MapHealthChecks("/health")` would
let you switch to real HTTP probes that actually verify SQL and RabbitMQ.

**initContainers replace `depends_on`.** Kubernetes has no equivalent of
compose's `condition: service_healthy`, so `orderapi` blocks on two busybox
initContainers that wait for `sqlserver:1433` and `rabbitmq:5672`.

**`Recreate` strategy on the stateful services.** Their PVCs are
`ReadWriteOnce`; a rolling update would deadlock with the new pod unable to
attach the volume the old pod still holds.

**Credentials are in plain text in git.** `01-secrets.yaml` carries the same
hardcoded dev values as `docker-compose.yml`. Kubernetes Secrets are only
base64-encoded, not encrypted. Do not reuse this file on a shared cluster —
that file's header lists the alternatives.

**`OrderProcessor` is not deployed.** It has a Dockerfile but was never in
`docker-compose.yml`, and its `appsettings.json` still points at Kafka
(`BootstrapServers`, `Topic`) while the rest of the system moved to RabbitMQ.
Porting it to RabbitMQ is a prerequisite for giving it a manifest.

---

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| `ImagePullBackOff` on `orderapi` | Step 3 skipped or cluster name wrong. Re-run `kind load docker-image` with the name from `kind get clusters`. |
| `orderapi` stuck at `Init:0/2` | A dependency never became reachable. `kubectl -n orderoo logs <pod> -c wait-for-sqlserver` shows which one. |
| `sqlserver` in `CrashLoopBackOff` | Almost always memory. `kubectl -n orderoo logs <pod>` — if it mentions RAM, raise Docker Desktop's allocation. |
| `Pending` pods, `FailedScheduling` | Not enough CPU/memory in the cluster to satisfy requests. Same fix. |
| `PersistentVolumeClaim is not bound` | No default StorageClass. Check `kubectl get storageclass` for one marked `(default)`. |
| Odd `kubectl` errors, fields ignored | Version skew. Go back to Step 0. |
| `connection refused` on localhost | The `port-forward` died. It does not survive pod restarts — re-run it. |

Useful when digging:

```bash
kubectl -n orderoo describe pod -l app.kubernetes.io/name=orderapi
```

```bash
kubectl -n orderoo logs -l app.kubernetes.io/name=orderapi --tail=100
```
