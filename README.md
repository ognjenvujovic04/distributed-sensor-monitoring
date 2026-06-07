# Distributed Sensor Monitoring

Distributed sensor monitoring system built with ASP.NET Core 8 microservices, PostgreSQL, and Docker.

## Overview

This project implements a fault-tolerant sensor data ingestion pipeline with consensus calculation, alarm handling, and real-time notifications. The solution is organized as multiple services sharing common contracts and data access libraries.

| Service | Description |
|---------|-------------|
| **IngestionService** | Receives sensor readings via REST API |
| **ConsensusService** | Background worker for consensus value calculation |
| **NotificationService** | Real-time alarm and status notifications (SignalR in later phases) |
| **SensorMonitoring.Contracts** | Shared DTOs and enums |
| **SensorMonitoring.Data** | EF Core entities, DbContext, and migrations |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Local Development

```bash
# Clone the repository
git clone <repository-url>
cd distributed-sensor-monitoring

# Build the solution
dotnet build

# Run a service (example: IngestionService)
dotnet run --project src/IngestionService
```

## Database

Start PostgreSQL with Docker Compose:

```bash
docker compose up -d postgres
```

Verify the container is running:

```bash
docker compose ps
```

### Connection strings

| Context | Connection string |
|---------|-------------------|
| Local dev (`dotnet run`) | `Host=localhost;Port=5432;Database=sensordb;Username=snus;Password=snus` |
| Inside Docker Compose network | `Host=postgres;Port=5432;Database=sensordb;Username=snus;Password=snus` |

## Docker

```bash
# Start PostgreSQL
docker compose up -d postgres

# Apply database migrations (after EF setup)
dotnet ef database update --project src/SensorMonitoring.Data --startup-project src/IngestionService

# Start all services
docker compose up --build
```

### Service URLs (Docker)

| Service | URL |
|---------|-----|
| IngestionService | http://localhost:5001 |
| NotificationService | http://localhost:5003 |

