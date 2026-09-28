# Parking System

Smart parking finder and management platform with a 3D lot view and AI recommendations (OJT project). Drivers find the lot most likely to have a free spot when they arrive, reserve and pay; lot owners manage many lots on one platform.

> Status: project skeleton. Services start, expose `/health` and pass one sample event (camera → parking → ai). The Docker Compose stack has not been run end-to-end yet.

## Planned features

- Lot search and AI recommendation by arrival-time availability, occupancy forecast
- 3D lot map (Three.js) with real-time slot status for drivers, owners and staff
- Two reservation types (hold while driving, scheduled up to 7 days) with a VNPay deposit
- Parking sessions from gate cameras (plate recognition) or staff entry; fees per visit or per time block
- Real payments through VNPay or cash at the exit

## Tech stack

| Part | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10) microservices, YARP gateway, SignalR |
| AI | Python 3.12, FastAPI |
| Messaging | RabbitMQ with MassTransit 8 (outbox/inbox); RabbitMQ MQTT plugin for cameras |
| Data | PostgreSQL 17 (one database per service), Redis |
| Observability | Serilog, OpenTelemetry, Aspire Dashboard |
| Frontend | React + Three.js (to be scaffolded in `frontend/`) |
| Run | Docker Compose; CI on GitHub Actions |

## Requirements

- .NET SDK 10
- Docker Desktop
- Python 3.10+ to run the AI tests outside Docker

## Run locally

```
cd deploy
cp ../.env.example .env
docker compose up --build
```

Fill in `deploy/.env` before starting. Add `--profile sim` to `docker compose up` to start the camera simulator.

| URL | What |
|---|---|
| http://localhost:8080/health | Gateway health |
| http://localhost:8080/api/ai/events/recent | Last slot events received by the AI service (sample flow) |
| http://localhost:18888 | Aspire Dashboard: logs, traces, metrics |
| http://localhost:15672 | RabbitMQ management |

## Configuration (`deploy/.env`)

| Variable | Meaning |
|---|---|
| `POSTGRES_PASSWORD` | PostgreSQL superuser password |
| `SERVICE_DB_PASSWORD` | Password of the per-service database logins |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | RabbitMQ user, also used by MQTT clients |
| `JWT_SIGNING_KEY` | JWT signing key, at least 32 characters |
| `VNPAY_TMN_CODE`, `VNPAY_HASH_SECRET` | VNPay merchant credentials (payment service only) |
| `PUBLIC_BASE_URL` | Frontend URL used for the VNPay return page |
| `SMTP_HOST`, `SMTP_USER`, `SMTP_PASSWORD` | Email sending (notification service) |

Never commit `.env`; only `.env.example` is tracked.

## Tests

```
dotnet test ParkingSystem.slnx
```

AI service:

```
cd services/ai
pip install -r requirements-dev.txt
pytest
```

## Repository layout

| Path | Content |
|---|---|
| `contracts/` | Events shared between services |
| `shared/` | Common setup for every .NET service (logging, tracing, health, JWT, messaging) |
| `gateway/` | YARP API gateway |
| `services/<name>/` | identity, parking, booking, payment, notification (`*.Api`, `*.Tests`, `Dockerfile`) and `ai` (Python) |
| `edge/camera-simulator/` | Publishes fake zone-camera readings over MQTT |
| `deploy/` | Docker Compose stack, database init script, RabbitMQ plugins |
| `docs/` | Setup guide and team workflow (Vietnamese) |
| `.github/` | CI workflows and pull request templates |

## Team workflow

Branches follow the mentor's guide: `main` (tested sprint releases), `develop`, `features/Implementation_<UserStory>`, `features/Design_<UserStory>`, `hotfix/Bug_<UserStory>`, `release/sprint_x`. Commits are in English, one change per commit; pull requests go to `develop` using the template. Details: `docs/workflow.md`.

## Team

To be added.
