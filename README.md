# Distributed Sensor Monitoring

A fault-tolerant, secure **microservice platform** for collecting and validating sensor data in real time. The example use case is temperature monitoring in a safety-critical setting, where exactly five sensors must always be active.

Built as the project for the **Supervisory Control Systems Software** course (*Softver nadzorno upravljačkih sistema*). It implements automatic fault tolerance, a simplified Byzantine-fault-tolerant consensus, end-to-end message encryption and signing, and a containerised deployment with Docker Compose and Kubernetes.

---

## 👥 Authors

- [**Vukašin Vitomirović**](https://github.com/Kakodane)
- [**Miloš Damjanović**](https://github.com/pifchina)
- [**Ognjen Vujović**](https://github.com/ognjenvujovic04)

---

## What the system does

| Capability | How it's implemented |
| ---------- | -------------------- |
| **Real-time value tracking** | Simulated sensors push temperature readings every 1–10 s; readings, alarms and consensus values are persisted to PostgreSQL. |
| **Fault tolerance** | The system maintains exactly 5 active sensors. A background worker detects silent sensors (>10 s) and promotes standby sensors to replace them. |
| **Byzantine fault tolerance** | Every minute a consensus value is computed from the previous minute's readings. A statistical outlier detector (MAD / modified z-score) flags malicious sensors and excludes them from consensus. |
| **Secure communication** | Each message is AES-256-GCM encrypted (per-message key, RSA-wrapped) and ECDSA-signed, with replay protection (timestamp window + monotonic message IDs) and per-sensor rate limiting. |
| **Real-time alerting** | Threshold-crossing readings raise priority-coded alarms broadcast to clients over SignalR. |
| **Single entry point** | A YARP reverse-proxy gateway fronts the system with path-based routing and WebSocket forwarding. |

---

## Architecture

Independently deployable ASP.NET Core services sharing common contract and infrastructure libraries, communicating over HTTP/REST and SignalR, behind a single gateway.

```
                       ┌──────────────────────────────┐
   Sensor clients ───► │   GatewayService (YARP)      │  ◄── single public entry point
   (encrypted +        │   /api/ingest/**   /hub/**   │
    signed envelopes)  └───────┬───────────────┬──────┘
                               │               │
                     /api/ingest               /hub (WebSocket)
                               ▼               ▼
                  ┌──────────────────────┐   ┌──────────────────────┐
                  │  IngestionService    │   │ NotificationService  │
                  │  • decrypt + verify  │──►│ • SignalR hub        │
                  │  • replay / rate     │   │ • broadcasts alarms  │
                  │    limiting          │   └──────────────────────┘
                  │  • sensor pool worker│
                  └──────────┬───────────┘
                             │ writes
                             ▼
                  ┌─────────────────────┐        ┌────────────────────────┐
                  │   PostgreSQL (EF)   │ ◄───── │  ConsensusService      │
                  │  readings / alarms  │        │ • per-minute consensus │
                  │  consensus / sensors│        │ • BFT outlier detection│
                  └─────────────────────┘        │ • CQRS queries         │
                                                 └────────────────────────┘
```

### Services & libraries

| Project | Responsibility |
| ------- | -------------- |
| **GatewayService** | YARP reverse proxy / ingress — single public entry point with config-driven path routing. |
| **IngestionService** | REST ingestion endpoint; decrypts & verifies envelopes, enforces replay/rate-limit rules, persists readings, runs the fault-tolerance sensor-pool worker. |
| **ConsensusService** | Background worker computing per-minute consensus and detecting malicious sensors, structured around the CQRS pattern. |
| **NotificationService** | Receives alarms and broadcasts them in real time over SignalR. |
| **SensorSimulator** | Console client that simulates sensors (and several attack modes) and generates cryptographic keys. |
| **SensorMonitoring.Contracts** | Shared DTOs / enums (`SensorMessage`, `SecureEnvelope`, `AlarmPayload`, …). |
| **SensorMonitoring.Data** | EF Core entities, `DbContext`, migrations and seed data. |
| **SensorMonitoring.Security** | Reusable AES-256-GCM + RSA encryption and ECDSA signing, shared by client and server. |

---

## Tech Stack

**Backend & runtime**
* **C# / .NET 8** — all services.
* **ASP.NET Core** — minimal-API ingestion & notification endpoints, background `Worker` services.
* **Entity Framework Core** — code-first ORM with migrations over **PostgreSQL 16**.
* **SignalR** — server-to-client push for real-time alarms over WebSockets.
* **YARP** — Microsoft's reverse-proxy library powering the API gateway.

**Security**
* **AES-256-GCM** authenticated encryption (fresh key per message), **RSA** key wrapping, **ECDSA** per-sensor signatures.
* Replay protection (timestamp window + monotonic message IDs) and per-sensor rate limiting. Threat model documented in [docs/SECURITY.md](docs/SECURITY.md).

**Distributed-systems concepts**
* Fault tolerance (automatic sensor failover), simplified Byzantine fault tolerance (MAD-based outlier detection), and CQRS in the consensus pipeline.

**Infrastructure**
* **Docker & Docker Compose** — every service containerised; one-command local stack.
* **Kubernetes (Minikube)** — manifests for `Deployment`, `Service`, `ConfigMap`, `Secret`, `PersistentVolumeClaim`, a migration `Job`, and an `Ingress`.

---

## Quick start

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), [Docker Desktop](https://www.docker.com/products/docker-desktop/), and the EF Core CLI (`dotnet tool install --global dotnet-ef`).

```bash
# 1. Build the solution
dotnet build

# 2. Start PostgreSQL and apply the schema
docker compose up -d postgres
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService

# 3. Generate cryptographic keys (one-time)
dotnet run --project src/SensorSimulator -- --keygen

# 4. Launch the full stack behind the gateway
docker compose up --build
```

Drive the system with one or more simulated sensors:

```bash
# Normal sensor
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-001

# A "malicious" sensor that the consensus service detects and quarantines
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-005 --malicious
```

The gateway exposes everything on `http://localhost:8080` (`/health`, `/api/ingest/**`, `/hub/alarms`).
