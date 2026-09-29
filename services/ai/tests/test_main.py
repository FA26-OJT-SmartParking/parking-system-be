import json

from fastapi.testclient import TestClient

from app.events import extract_message
from app.main import app


def test_health_returns_ok():
    # Without "with TestClient(...)" the lifespan (RabbitMQ consumer) does not start
    response = TestClient(app).get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_extract_message_reads_masstransit_envelope():
    body = json.dumps({
        "messageType": ["urn:message:ParkingSystem.Contracts:SlotStatusChanged"],
        "message": {
            "lotId": "00000000-0000-0000-0000-000000000001",
            "slotCode": "A-01",
            "status": "Occupied",
            "at": "2026-09-28T10:00:00+00:00",
        },
    }).encode()

    event = extract_message(body)

    assert event["slotCode"] == "A-01"
    assert event["status"] == "Occupied"
