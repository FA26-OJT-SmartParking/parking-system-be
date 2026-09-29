# Parking System — Backend

Smart parking finder and management platform with a 3D lot view and AI recommendations (OJT project). Drivers find the lot most likely to have a free spot when they arrive, reserve and pay; lot owners manage many lots on one platform.

This repository holds the backend. The web app is in [parking-system-fe](https://github.com/FA26-OJT-SmartParking/parking-system-fe).

> Status: project skeleton. Each service is split into Domain, Application, Infrastructure, API and Tests layers. Two sample flows exist: camera simulator → parking → RabbitMQ → ai, and booking → parking over gRPC (`GET /api/booking/lots/{lotId}/availability`). Some business and technical choices wait for the team to confirm the source-of-truth document, see `docs/huong-dan-setup-microservices.md` section 12.

## Planned features

- Lot search and AI recommendation by arrival-time availability, occupancy forecast
- 3D lot map (Three.js) with real-time slot status for drivers, owners and staff
- Two reservation types (hold while driving, scheduled up to 7 days) with a VNPay deposit
- Parking sessions from gate cameras (plate recognition) or staff entry; fees per visit or per time block
- Real payments through VNPay or cash at the exit

## Tech stack

| Part | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10) microservices, one solution per service in the layout of the mentor's `Project.CleanArchitecture` template (Domain, Application, Infrastructure, Persistence, WebAPI, Tests), MediatR and FluentValidation, YARP gateway, SignalR |
| AI | Python 3.12, FastAPI |
| Service calls | gRPC (contracts in `grpc_proto/`) |
| Messaging | RabbitMQ with MassTransit 8 (outbox/inbox); RabbitMQ MQTT plugin for cameras |
| Data | PostgreSQL 17: one database, one schema per service |
| Observability | Serilog, OpenTelemetry, Aspire Dashboard (optional) |
| Frontend | Next.js + React + Three.js, separate repository [parking-system-fe](https://github.com/FA26-OJT-SmartParking/parking-system-fe) |
| Run | Docker Compose |

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

Fill in `deploy/.env` before starting. By default only the core stack runs (postgres, rabbitmq, gateway, parking, booking). Add `--profile sim` for the camera simulator, `--profile ai` for the AI service, `--profile observability` for the Aspire Dashboard (also set `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889` in `.env`), or `--profile full` for everything.

| URL | What |
|---|---|
| http://localhost:8088/health | Gateway health |
| http://localhost:8088/api/ai/events/recent | Last slot events received by the AI service (profiles `ai` and `sim`) |
| http://localhost:8088/api/booking/lots/{lotId}/availability | Free and occupied slots of a lot (public) |
| http://localhost:18888 | Aspire Dashboard: logs, traces, metrics (profile `observability`) |
| http://localhost:15672 | RabbitMQ management |

## Configuration (`deploy/.env`)

| Variable | Meaning |
|---|---|
| `POSTGRES_PASSWORD` | PostgreSQL superuser password |
| `SERVICE_DB_PASSWORD` | Password of the database login `app_svc` |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | RabbitMQ user, also used by MQTT clients |
| `JWT_PUBLIC_KEY` | RS256 public key (base64 DER, one line); the gateway and every service check tokens with it. `deploy/generate-jwt-keys.sh` prints a key pair |
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
| `grpc_proto/` | gRPC contracts (`.proto`) and the project that generates the C# code |
| `shared/` | `ParkingSystem.Contracts` (RabbitMQ events) and `ParkingSystem.ServiceDefaults` (logging, tracing, health, JWT, messaging for every .NET service) |
| `tests/` | Integration tests that run real gRPC between services in memory |
| `gateway/` | YARP API gateway |
| `services/<name>/` | identity, parking, booking, payment, notification (`*.Domain`, `*.Application`, `*.Infrastructure`, `*.Persistence`, `*.WebAPI`, `*.Tests`, `Dockerfile`) and `ai` (Python, FastAPI + gRPC) |
| `edge/camera-simulator/` | Publishes fake zone-camera readings over MQTT |
| `deploy/` | Docker Compose stack, database init script, RabbitMQ plugins |
| `docs/` | Start with `tong-quan-he-thong.md` (system overview for newcomers); also the setup guide, team workflow and template deviations (Vietnamese) |
| `.github/` | GitHub Actions workflows and the pull request templates from the mentor guide |

## Team workflow

Branches follow the mentor's guide: `main`, `develop`, `features/Implementation_<UserStory>`, `features/Design_<UserStory>`, `hotfix/Bug_<UserStory>`, `release/sprint_x`. Commits are in English (team choice), one change per commit; every feature is merged through a pull request that uses the guide's template. Details, and which rules come from the guide and which are the team's own: `docs/workflow.md`.

## Team

To be added.
