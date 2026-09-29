"""Generates the Python gRPC code for grpc_proto/ai.proto into app/generated (git-ignored).

Run from anywhere: python gen_proto.py   (needs grpcio-tools, see requirements-dev.txt)
"""
import sys
from importlib.resources import files
from pathlib import Path

from grpc_tools import protoc

ROOT = Path(__file__).resolve().parent
PROTO_DIR = ROOT.parents[1] / "grpc_proto"
OUT_DIR = ROOT / "app" / "generated"


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "__init__.py").write_text("", encoding="utf-8")
    # protoc.main does not add the well-known types (google/protobuf/timestamp.proto) that ship with grpcio-tools
    well_known_types = files("grpc_tools") / "_proto"
    exit_code = protoc.main([
        "grpc_tools.protoc",
        f"-I{PROTO_DIR}",
        f"-I{well_known_types}",
        f"--python_out={OUT_DIR}",
        f"--grpc_python_out={OUT_DIR}",
        str(PROTO_DIR / "ai.proto"),
    ])
    if exit_code != 0:
        sys.exit(exit_code)

    # protoc writes "import ai_pb2"; inside the app package it must be a relative import
    grpc_module = OUT_DIR / "ai_pb2_grpc.py"
    grpc_module.write_text(
        grpc_module.read_text(encoding="utf-8").replace("import ai_pb2 as", "from . import ai_pb2 as"),
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
