import asyncio

import grpc

from app.generated import ai_pb2, ai_pb2_grpc
from app.grpc_server import start_grpc_server

EVENTS = [
    {"lotId": "00000000-0000-0000-0000-000000000001", "slotCode": "A-01", "status": "Occupied", "at": "2026-09-28T15:15:00.392931+00:00"},
    {"lotId": "00000000-0000-0000-0000-000000000001", "slotCode": "A-02", "status": "Available", "at": "2026-09-28T15:15:02.393731+00:00"},
    {"lotId": "00000000-0000-0000-0000-000000000001", "slotCode": "A-03", "status": "Occupied", "at": "2026-09-28T15:15:04.394486+00:00"},
]


async def _call(limit: int) -> ai_pb2.GetRecentSlotEventsResponse:
    server, port = await start_grpc_server(EVENTS, 0)
    try:
        async with grpc.aio.insecure_channel(f"localhost:{port}") as channel:
            stub = ai_pb2_grpc.AiServiceStub(channel)
            return await stub.GetRecentSlotEvents(ai_pb2.GetRecentSlotEventsRequest(limit=limit))
    finally:
        await server.stop(grace=None)


def test_get_recent_slot_events_returns_all_when_limit_is_zero():
    response = asyncio.run(_call(0))

    assert [event.slot_code for event in response.events] == ["A-01", "A-02", "A-03"]
    assert response.events[0].status == "Occupied"
    assert response.events[0].at.ToDatetime().year == 2026


def test_get_recent_slot_events_returns_only_the_newest_when_limited():
    response = asyncio.run(_call(2))

    assert [event.slot_code for event in response.events] == ["A-02", "A-03"]
