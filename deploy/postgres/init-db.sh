#!/bin/bash
# Creates the one database of the system, its login and one schema per service.
# Runs only when the pgdata volume is empty.
set -e
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<EOSQL
CREATE USER app_svc WITH PASSWORD '${SERVICE_DB_PASSWORD}';
CREATE DATABASE parking_system OWNER app_svc;
REVOKE ALL ON DATABASE parking_system FROM PUBLIC;
EOSQL
for schema in identity parking booking payment notification; do
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname parking_system \
  -c "CREATE SCHEMA ${schema} AUTHORIZATION app_svc"
done
