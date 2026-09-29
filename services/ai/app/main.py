"""AI service. Recommendation and occupancy forecast come later; for now it records SlotStatusChanged events."""
import asyncio
import os
from collections import deque
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.events import consume_slot_events
from app.grpc_server import start_grpc_server

recent_events: deque[dict] = deque(maxlen=50)


@asynccontextmanager
async def lifespan(_: FastAPI):
    grpc_server, _port = await start_grpc_server(recent_events, int(os.environ.get("GRPC_PORT", "8081")))
    rabbitmq_url = os.environ.get("RABBITMQ_URL")
    consumer = asyncio.create_task(consume_slot_events(rabbitmq_url, recent_events.append)) if rabbitmq_url else None
    yield
    if consumer:
        consumer.cancel()
    await grpc_server.stop(grace=1)


app = FastAPI(title="ai-service", lifespan=lifespan)


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/events/recent")
def events_recent() -> list[dict]:
    """Last SlotStatusChanged events received, newest last. Used to check the camera -> parking -> ai flow."""
    return list(recent_events)
