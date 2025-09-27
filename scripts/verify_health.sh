#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_ROOT"

echo "[info] Building API image (tag: youtube-downloader-api:latest)"

if ! command -v docker >/dev/null 2>&1; then
  echo "[error] Docker is not installed or not in PATH" >&2
  exit 1
fi

DOCKER_COMPOSE="docker compose"
if ! $DOCKER_COMPOSE version >/dev/null 2>&1; then
  if command -v docker-compose >/dev/null 2>&1; then
    DOCKER_COMPOSE="docker-compose"
  else
    echo "[error] docker compose plugin or docker-compose not found" >&2
    exit 1
  fi
fi

docker build -t youtube-downloader-api:latest .

echo "[info] Starting only the api service via compose"
$DOCKER_COMPOSE up -d api

CONTAINER_NAME="youtube-downloader-api"

echo "[info] Waiting for container health (timeout: 120s)"
DEADLINE=$((SECONDS+120))
STATUS="starting"
while [ $SECONDS -lt $DEADLINE ]; do
  STATUS=$(docker inspect -f '{{.State.Health.Status}}' "$CONTAINER_NAME" 2>/dev/null || echo "unknown")
  if [ "$STATUS" = "healthy" ]; then
    break
  fi
  sleep 3
done

if [ "$STATUS" != "healthy" ]; then
  echo "[error] Container did not become healthy (status: $STATUS)" >&2
  echo "[hint] Last 100 lines of logs:" >&2
  docker logs --tail 100 "$CONTAINER_NAME" || true
  $DOCKER_COMPOSE down || true
  exit 2
fi

echo "[info] Container is healthy. Probing /api/health from host"
if command -v curl >/dev/null 2>&1; then
  set +e
  RESP=$(curl -fsS --max-time 10 http://localhost:8080/api/health)
  RC=$?
  set -e
  if [ $RC -ne 0 ]; then
    echo "[error] HTTP probe failed with rc=$RC" >&2
    $DOCKER_COMPOSE down || true
    exit 3
  fi
  echo "[info] Response: $RESP"
  echo "$RESP" | grep -qi 'healthy' && echo "[ok] Health endpoint responded with 'healthy'" || echo "[warn] Health endpoint did not contain 'healthy'"
else
  echo "[warn] curl is not available on host; skipping HTTP probe"
fi

echo "[info] Stopping and removing containers"
$DOCKER_COMPOSE down

echo "[done] Local build and health verification completed"

