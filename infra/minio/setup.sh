#!/bin/sh
# ItWorksOnMyResume — MinIO bucket bootstrap
# Runs once via minio-setup service in docker-compose
# File: infra/minio/setup.sh

set -e

MC="mc"
ALIAS="local"
ENDPOINT="http://minio:9000"

echo "Waiting for MinIO to be ready..."
until $MC alias set $ALIAS $ENDPOINT "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" 2>/dev/null; do
  sleep 2
done

echo "MinIO ready. Creating buckets..."

# ── Avatars ──────────────────────────────────────────────
# Public read — profile pictures served directly by URL
$MC mb --ignore-existing $ALIAS/iwomr-avatars
$MC anonymous set download $ALIAS/iwomr-avatars

# ── Documents ────────────────────────────────────────────
# Private — any future attachments (skill proof, etc.)
$MC mb --ignore-existing $ALIAS/iwomr-documents

echo "Buckets created:"
$MC ls $ALIAS

echo "MinIO setup complete."
