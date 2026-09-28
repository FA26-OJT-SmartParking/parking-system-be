"""Reads events that the .NET services publish through MassTransit."""
import asyncio
import json
import logging
from typing import Callable

import aio_pika

logger = logging.getLogger(__name__)

# MassTransit publishes each event type to a fanout exchange named "<namespace>:<type>"
# (see contracts/ParkingSystem.Contracts/Events.cs).
SLOT_STATUS_EXCHANGE = "ParkingSystem.Contracts:SlotStatusChanged"
QUEUE_NAME = "ai-slot-status"


def extract_message(body: bytes) -> dict:
    """Return the event carried in a MassTransit JSON envelope."""
    return json.loads(body)["message"]


async def consume_slot_events(rabbitmq_url: str, on_event: Callable[[dict], None]) -> None:
    """Consume SlotStatusChanged forever, reconnecting when RabbitMQ is not reachable."""
    while True:
        try:
            connection = await aio_pika.connect_robust(rabbitmq_url)
            async with connection:
                channel = await connection.channel()
                exchange = await channel.declare_exchange(
                    SLOT_STATUS_EXCHANGE, aio_pika.ExchangeType.FANOUT, durable=True
                )
                queue = await channel.declare_queue(QUEUE_NAME, durable=True)
                await queue.bind(exchange)
                async with queue.iterator() as messages:
                    async for message in messages:
                        async with message.process():
                            try:
                                on_event(extract_message(message.body))
                            except (KeyError, ValueError):
                                logger.warning("Ignoring malformed SlotStatusChanged message")
        except asyncio.CancelledError:
            raise
        except Exception:
            logger.exception("RabbitMQ consumer stopped, retrying in 5 seconds")
            await asyncio.sleep(5)
