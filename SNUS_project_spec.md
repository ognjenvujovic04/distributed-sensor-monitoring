# Project Specification: Distributed Sensor Data Collection System

## Overview

The goal is to develop a robust distributed system for collecting, processing, and storing data from sensor nodes. The example use case is temperature monitoring in a critical industrial setting, such as a nuclear power plant core.

At any given time, **exactly 5 sensors** must be active and participating in temperature monitoring. The system has a larger pool of sensors deployed at various locations.

---

## System Requirements

The system must provide:

1. **Value tracking** — real-time values, historical access, and event logging
2. **Fault tolerance** — always maintain exactly 5 active sensors
3. **Consistency** — consensus-based data validation with BFT approach
4. **Reliable and secure client-server communication** — encrypted, signed, replay-protected

---

## Feature Specifications

### 1. Value Tracking

- Before simulation starts, each sensor is assigned:
  - A unique **ID**
  - A **temperature range** for value generation
  - A **data quality** flag: `GOOD`, `BAD`, or `UNCERTAIN`
  - **Alarm thresholds** with priorities 1, 2, and 3

- Current values are tracked by printing the measured value and timestamp to the console.

- **Alarm logic** (sensor-side):
  - The sensor registers an alarm when a measured value crosses a defined threshold.
  - If a **priority 3** alarm is triggered, priority 1 and 2 alarms must **not** additionally activate.
  - Console output color by alarm priority:
    - Priority 1 → **yellow**
    - Priority 2 → **orange**
    - Priority 3 → **red**
  - After alarm detection, the sensor sends a message to the server.

- **Server-side alarm handling**:
  - Prints sensor ID and the triggering value, using the corresponding alarm color.

- Values without an active alarm use **priority 0** for simpler database storage.

- **All data is written to the server-side database.**

---

### 2. Fault Tolerance

- The system must ensure **exactly 5 active sensors** at all times.
- A sensor is considered **inactive** if the server has not received its message within **10 seconds**.
- For testing purposes, it must be possible to **temporarily block a sensor for 30 seconds**.
- The server must maintain a record of the **last received message timestamp** for every sensor that has ever been part of the system.

---

### 3. Consistency

- Active sensors measure temperature (generate a random value) **every 1–10 seconds** and send it to the server.

- Received values are written to a **PostgreSQL** database using **Entity Framework**.

- Every **1 minute**, a **consensus value** is calculated from the previous minute's data and written to the database.

- Each stored value must have a **flag** indicating whether it is a consensus value.

- **Malicious sensor assumptions**:
  - Sensors may stop responding, respond late, or send incorrect data.
  - A **BFT (Byzantine Fault Tolerant)** approach must be researched and a simplified version of a chosen algorithm implemented.
  - When a sensor is determined to be malicious, its data quality is set to **`BAD`**.
  - Only data with quality **`GOOD`** participates in consensus calculation.

---

### 4. Reliable Communication

- All messages from clients to the server must be **encrypted** and **digitally signed** to ensure confidentiality and sender authentication.
  - Recommended: **AES** (encryption) + **RSA/ECDSA** (signing)

- **Replay attack protection**: Each message includes a timestamp and a unique, incrementing message ID.

- **DoS protection**: If the same sensor ID sends more than **10 messages per second**, the server temporarily blocks it.
  - Suggested library: `AspNetCoreRateLimit`

- Messages must be exchanged over a **real network address** (not just `localhost`).
  - Security risks of this approach must be **analyzed, mitigated, and documented**.

- Client-server communication protocol: **HTTP/REST**

---

## Microservices / Components

| Service                 | Description                                                                                                                                                                                                                                                |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **IngestionService**    | Receives data from sensors and forwards it for further processing. Must be fast and scalable to handle high message throughput.                                                                                                                            |
| **ConsensusService**    | Worker service that reads raw data from the database, calculates a consensus value every minute, and writes the result to a separate table. Uses the **CQRS** (Command Query Responsibility Segregation) pattern to decouple data processing from the API. |
| **NotificationService** | Handles alarm monitoring. Notified by IngestionService upon alarm detection. Uses **SignalR** for real-time client notifications.                                                                                                                          |
| **Ingress**             | Single entry point that routes traffic to appropriate services (e.g., `/api/ingest` → IngestionService, `/api/reports` → ReportingService).                                                                                                                |

---

## Technology Stack

| Concern                 | Technology               |
| ----------------------- | ------------------------ |
| Backend framework       | ASP.NET Core             |
| Containerization        | Docker, docker-compose   |
| Orchestration           | Kubernetes (Minikube)    |
| Database                | PostgreSQL or SQL Server |
| ORM                     | Entity Framework         |
| Real-time notifications | SignalR                  |
| Encryption              | AES                      |
| Digital signatures      | RSA / ECDSA              |
| Rate limiting           | AspNetCoreRateLimit      |
| Consensus               | Simplified BFT algorithm |

---

## GitHub Repository Requirements

The repository must contain:

- Source code
- `.yaml` configuration files (Kubernetes manifests)
- Setup/run instructions
- Description of applied security measures
- Screenshots of the running system

**Defense requirement**: The system must be demonstrated on at least **two computers**, with servers and clients running on separate machines. Account for hardware availability when forming teams.
