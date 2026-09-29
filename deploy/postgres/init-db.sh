#!/bin/bash
# Creates one database and one login per service; runs only when the pgdata volume is empty.
set -e
for svc in identity parking booking payment notification ai; do
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<EOSQL
CREATE USER ${svc}_svc WITH PASSWORD '${SERVICE_DB_PASSWORD}';
CREATE DATABASE ${svc}_db OWNER ${svc}_svc;
REVOKE ALL ON DATABASE ${svc}_db FROM PUBLIC;
EOSQL
done
