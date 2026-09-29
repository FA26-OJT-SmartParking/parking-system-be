import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# The gRPC code is generated from grpc_proto/ai.proto and is not committed
if not (ROOT / "app" / "generated" / "ai_pb2.py").exists():
    subprocess.run([sys.executable, str(ROOT / "gen_proto.py")], check=True)
