# Build context: repository root (see deploy/docker-compose.yml), because the gRPC contract lives in grpc_proto/
FROM python:3.12-slim AS proto
WORKDIR /gen
RUN pip install --no-cache-dir grpcio-tools==1.84.0
COPY grpc_proto/ grpc_proto/
COPY services/ai/gen_proto.py services/ai/gen_proto.py
RUN python services/ai/gen_proto.py

FROM python:3.12-slim
WORKDIR /app
COPY services/ai/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY services/ai/app/ app/
COPY --from=proto /gen/services/ai/app/generated app/generated
USER 65534
EXPOSE 8080 8081
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8080"]
