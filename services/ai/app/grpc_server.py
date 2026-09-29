"""gRPC server (contract: grpc_proto/ai.proto), served next to the REST API on its own port."""
from collections.abc import Iterable

import grpc
from google.protobuf.timestamp_pb2 import Timestamp

from app.generated import ai_pb2, ai_pb2_grpc


class AiService(ai_pb2_grpc.AiServiceServicer):
    def __init__(self, events: Iterable[dict]):
        self._events = events

    async def GetRecentSlotEvents(self, request, context):
        events = list(self._events)
        if request.limit > 0:
            events = events[-request.limit:]

        response = ai_pb2.GetRecentSlotEventsResponse()
        for event in events:
            at = Timestamp()
            at.FromJsonString(event["at"])
            response.events.add(lot_id=event["lotId"], slot_code=event["slotCode"], status=event["status"], at=at)
        return response


async def start_grpc_server(events: Iterable[dict], port: int) -> tuple[grpc.aio.Server, int]:
    """Start serving on [::]:port (0 picks a free port) and return the server with the port it bound."""
    server = grpc.aio.server()
    ai_pb2_grpc.add_AiServiceServicer_to_server(AiService(events), server)
    bound_port = server.add_insecure_port(f"[::]:{port}")
    await server.start()
    return server, bound_port
