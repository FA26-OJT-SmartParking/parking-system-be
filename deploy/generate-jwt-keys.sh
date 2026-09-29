#!/usr/bin/env bash
# Prints an RS256 key pair, each key as one base64 line: JWT_PUBLIC_KEY goes into deploy/.env (every service checks tokens with it);
# JWT_PRIVATE_KEY is kept for the service that will sign tokens.
# Needs openssl (it comes with Git for Windows). Nothing is written to disk.
set -euo pipefail
key=$(openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048)
echo "JWT_PRIVATE_KEY=$(echo "$key" | openssl pkcs8 -topk8 -nocrypt -outform DER | base64 -w0)"
echo "JWT_PUBLIC_KEY=$(echo "$key" | openssl rsa -pubout -outform DER 2>/dev/null | base64 -w0)"
