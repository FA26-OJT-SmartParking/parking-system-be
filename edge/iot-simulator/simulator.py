"""Publishes fake slot-sensor readings over MQTT until real IoT devices are available."""
import json
import os
import random
import time
from datetime import datetime, timezone

import paho.mqtt.client as mqtt

HOST = os.environ.get("MQTT_HOST", "localhost")
USER = os.environ.get("MQTT_USER", "guest")
PASSWORD = os.environ.get("MQTT_PASSWORD", "guest")
LOT_ID = os.environ.get("LOT_ID", "00000000-0000-0000-0000-000000000001")
INTERVAL_SECONDS = float(os.environ.get("INTERVAL_SECONDS", "2"))
SLOTS = [f"A-{number:02d}" for number in range(1, 11)]


def main() -> None:
    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id="iot-simulator")
    client.username_pw_set(USER, PASSWORD)
    client.connect(HOST, 1883)
    client.loop_start()
    while True:
        topic = f"lot/{LOT_ID}/slot/{random.choice(SLOTS)}"
        reading = {
            "status": random.choice(["Occupied", "Available"]),
            "confidence": round(random.uniform(0.8, 1.0), 2),
            "at": datetime.now(timezone.utc).isoformat(),
        }
        client.publish(topic, json.dumps(reading), qos=1)
        print(topic, reading, flush=True)
        time.sleep(INTERVAL_SECONDS)


if __name__ == "__main__":
    main()
