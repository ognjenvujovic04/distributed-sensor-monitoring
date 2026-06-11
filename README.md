# Distributed Sensor Monitoring

Distributed sensor monitoring system built with ASP.NET Core 8 microservices, PostgreSQL, and Docker.

## Overview

This project implements a fault-tolerant sensor data ingestion pipeline with consensus calculation, alarm handling, and real-time notifications. The solution is organized as multiple services sharing common contracts and data access libraries.


| Service / Library              | Description                                                        |
| ------------------------------ | ------------------------------------------------------------------ |
| **IngestionService**           | Receives sensor readings via REST API, fault-tolerance pool worker |
| **SensorSimulator**            | Console client that simulates sensor readings and client-side alarms |
| **ConsensusService**           | Background worker: per-minute consensus calculation and malicious-sensor (BFT) detection |
| **NotificationService**        | Real-time alarm and status notifications (SignalR in Phase 3+)     |
| **SensorMonitoring.Contracts** | Shared DTOs and enums (`SensorMessage`, `AlarmPayload`, etc.)      |
| **SensorMonitoring.Data**      | EF Core entities, DbContext, and migrations                        |


## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (for applying migrations):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Quick start

From the repository root:

```bash
# 1. Clone the repository
git clone <repository-url>
cd distributed-sensor-monitoring

# 2. Build the solution
dotnet build

# 3. Start PostgreSQL
docker compose up -d postgres

# 4. Apply database migrations
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService

# 5. Start all services
docker compose up --build
```

Verify containers are running:

```bash
docker compose ps
```

Check health endpoints:

```bash
curl http://localhost:5001/health   # IngestionService → 200 OK
curl http://localhost:5003/health   # NotificationService → 200 OK
```

On Windows PowerShell, use `Invoke-WebRequest` instead of `curl`:

```powershell
(Invoke-WebRequest -Uri http://localhost:5001/health -UseBasicParsing).StatusCode
(Invoke-WebRequest -Uri http://localhost:5003/health -UseBasicParsing).StatusCode
```

## Local development

Run PostgreSQL in Docker, then start a service on the host machine:

```bash
docker compose up -d postgres
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService
dotnet run --project src/IngestionService
```

IngestionService listens on `http://localhost:5288` by default (see `src/IngestionService/Properties/launchSettings.json`). Swagger UI is available at `/swagger` in Development.

To run other services locally:

```bash
dotnet run --project src/NotificationService
dotnet run --project src/ConsensusService
```

## Phase 2: Sensor simulation & fault tolerance

Phase 2 implements the sensor data pipeline: simulated clients send readings to IngestionService, which persists them and maintains a pool of exactly 5 active sensors.

### Flow

1. **SensorSimulator** generates a random temperature every 1–10 seconds, detects alarm thresholds locally, and `POST`s a `SensorMessage` to IngestionService.
2. **IngestionService** validates the message, writes a `SensorReading` (and `AlarmEvent` when an alarm is present), and updates `Sensor.LastMessageAt`.
3. **SensorPoolWorker** runs every 2 seconds: sensors silent for more than 10 seconds are marked inactive; standby sensors are promoted to keep 5 active in the database.

### IngestionService API

| Method | Endpoint | Description |
| ------ | -------- | ----------- |
| `GET` | `/health` | Health check → `200 OK` |
| `POST` | `/api/readings` | Accept a `SensorMessage` → `202 Accepted` |
| `POST` | `/api/sensors/{id}/block` | Block a sensor for 30 seconds (testing) → `200 OK` |

Example `SensorMessage` body:

```json
{
  "sensorId": "SENSOR-001",
  "temperatureValue": 22.5,
  "timestamp": "2026-06-08T12:00:00Z",
  "messageId": 1,
  "dataQuality": "Good",
  "alarmPriority": null
}
```

Swagger UI (`/swagger`) is available when running IngestionService locally in Development.

### Sensor simulator

Run one simulator process per sensor ID (separate terminals):

```bash
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-001
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-002
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-003
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-004
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-005
```

The simulator reads `IngestionBaseUrl` from `src/SensorSimulator/appsettings.json` (default `http://localhost:5001` for Docker). If you run IngestionService locally on port `5288`, change that URL to `http://localhost:5288`.

Successful sends print `OK (202)`. Alarm readings are color-coded in the console: yellow (priority 1), orange (priority 2), red (priority 3).

Sensor metadata (temperature range, thresholds) is defined in `src/SensorSimulator/SensorConfig.cs` and matches the seeded data in the database (`SENSOR-001`–`007`; 5 active, 2 standby).

### Fault tolerance

| Setting | Default | Config key |
| ------- | ------- | ---------- |
| Inactivity timeout | 10 s | `FaultTolerance:InactivityTimeoutSeconds` |
| Pool worker interval | 2 s | hardcoded in `SensorPoolWorker` |
| Target active sensors | 5 | hardcoded in `SensorPoolService` |
| Manual block duration | 30 s | `POST /api/sensors/{id}/block` |

A sensor is deactivated only after it has sent at least one message and then goes silent for longer than the inactivity timeout. Promoting a standby updates `IsActive` in the database only — you must start a simulator for the promoted sensor ID (e.g. `SENSOR-006`) in a new terminal if you want it to send data.

### Phase 2 quick test

From the repository root:

```bash
docker compose up -d postgres
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService
docker compose up --build ingestion-service
```

In new terminals:

```bash
# Health check (PowerShell)
(Invoke-WebRequest -Uri http://localhost:5001/health -UseBasicParsing).StatusCode   # → 200

# Start simulators (one per terminal)
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-001
```

Block a sensor (PowerShell):

```powershell
Invoke-WebRequest -Uri http://localhost:5001/api/sensors/SENSOR-001/block -Method POST -UseBasicParsing
```

Failover test: stop one simulator, wait ~12 seconds, then start a standby:

```bash
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-006
```

## Phase 3: Consensus & BFT

Phase 3 adds the **ConsensusService** worker: once per minute it reads the previous minute's `GOOD`-quality readings, detects malicious sensors (statistical outliers), marks them `BAD` (`Sensor.DataQuality`), and writes a consensus value to the `ConsensusValues` table.

### Migration required

This phase adds `SensorCount` / `SampleCount` columns and a unique index to `ConsensusValues`. Apply the latest migrations before running:

```bash
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService
```

### Running

Start PostgreSQL and IngestionService (see Phase 2), run at least 3 sensor simulators, then start the worker:

```bash
dotnet run --project src/ConsensusService
```

The worker uses the connection string in `src/ConsensusService/appsettings.json` (local dev) or the `ConnectionStrings__Default` env var (Docker). In Docker it runs automatically as the `consensus-service` container. It logs one line per completed minute, e.g.:

```
Consensus 19.84°C for [12:04-12:05) from 5 sensors / 41 samples
```

### Configuration

Outlier-detection thresholds are in the `Consensus` section of `src/ConsensusService/appsettings.json`:

| Setting | Default | Meaning |
| ------- | ------- | ------- |
| `MinAbsoluteDeviation` | `10.0` | Min deviation (°C) from the population median to consider a sensor an outlier |
| `MadZScoreThreshold` | `2.5` | Modified z-score (MAD-based) above which a sensor is flagged malicious |
| `MinSensorsForDetection` | `3` | Minimum reporting sensors before detection runs (below this, consensus is still computed) |
| `GraceSeconds` | `5` | Delay after each minute boundary before processing, to tolerate late readings |

### Testing malicious-sensor detection

Run a simulator with the `--malicious` flag — it sends pinned near-maximum values (a clean outlier):

```bash
dotnet run --project src/SensorSimulator -- --sensor-id SENSOR-005 --malicious
```

Within 1–2 cycles the worker logs `Outliers detected: SENSOR-005` and `marked MALICIOUS`; that sensor's `DataQuality` becomes `BAD` and it is dropped from consensus. The mark is **permanent** — reset it to re-run the demo:

```bash
# PowerShell (pipe SQL via stdin so quoted identifiers survive)
'UPDATE "Sensors" SET "DataQuality"=''Good'' WHERE "Id"=''SENSOR-005'';' | docker exec -i git-postgres-1 psql -U snus -d sensordb
```

## Database

PostgreSQL 16 runs in Docker with these defaults:


| Setting               | Value       |
| --------------------- | ----------- |
| Host (local dev)      | `localhost` |
| Host (inside compose) | `postgres`  |
| Port                  | `5432`      |
| Database              | `sensordb`  |
| Username              | `snus`      |
| Password              | `snus`      |


### Connection strings


| Context                       | Connection string                                                        |
| ----------------------------- | ------------------------------------------------------------------------ |
| Local dev (`dotnet run`)      | `Host=localhost;Port=5432;Database=sensordb;Username=snus;Password=snus` |
| Inside Docker Compose network | `Host=postgres;Port=5432;Database=sensordb;Username=snus;Password=snus`  |


The local connection string is configured in `src/IngestionService/appsettings.json` and `src/ConsensusService/appsettings.json`. Docker services receive the compose-network string via the `ConnectionStrings__Default` environment variable.

### Schema and seed data

EF Core migrations create four tables: `Sensors`, `SensorReadings`, `ConsensusValues`, and `AlarmEvents`. The initial migration seeds 7 sensors (5 active, 2 standby).

Apply or update the schema:

```bash
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService
```

Add a new migration after entity changes:

```bash
dotnet ef migrations add <MigrationName> --project src/SensorMonitoring.Data --startup-project src/IngestionService
```

## Docker

### Service ports


| Service             | Host port                                      | Health check  |
| ------------------- | ---------------------------------------------- | ------------- |
| PostgreSQL          | 5432                                           | —             |
| IngestionService    | [http://localhost:5001](http://localhost:5001) | `GET /health` |
| NotificationService | [http://localhost:5003](http://localhost:5003) | `GET /health` |
| ConsensusService    | — (background worker, no public port)          | —             |


### Common commands

```bash
# Start only the database
docker compose up -d postgres

# Build and start the full stack (foreground)
docker compose up --build

# Build and start the full stack (detached)
docker compose up --build -d

# Stop all containers
docker compose down

# Stop containers and remove the database volume
docker compose down -v
```

