
#!/usr/bin/env bash
set -euo pipefail

# Quick-start helper for development
# - Starts required infrastructure with docker compose
# - Waits for key services to become healthy (postgres, rabbitmq)
# - Runs EF migrations for each service (if dotnet-ef is available)
# - Prints example commands to run services locally

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"

echo "Repo root: $ROOT_DIR"

function wait_for_healthy() {
  local svc="$1"
  local timeout=${2:-120}
  local start=$(date +%s)

  echo "Waiting for service '$svc' to be healthy (timeout ${timeout}s)..."

  while true; do
    cid=$(docker compose -f "$COMPOSE_FILE" ps -q "$svc" 2>/dev/null || true)
    if [ -z "$cid" ]; then
      # Service not created yet
      sleep 1
    else
      status=$(docker inspect --format='{{.State.Health.Status}}' "$cid" 2>/dev/null || true)
      if [ "$status" = "healthy" ]; then
        echo "- $svc is healthy"
        return 0
      fi
      # Fallback: if no health info, consider 'running' as acceptable
      state=$(docker inspect --format='{{.State.Status}}' "$cid" 2>/dev/null || true)
      if [ -z "$status" ] && [ "$state" = "running" ]; then
        echo "- $svc is running (no healthcheck defined)"
        return 0
      fi
    fi

    now=$(date +%s)
    if [ $((now - start)) -gt $timeout ]; then
      echo "Timed out waiting for $svc to be healthy"
      docker compose -f "$COMPOSE_FILE" logs --no-color --tail 200 "$svc" || true
      return 1
    fi
    sleep 2
  done
}

echo "1) Starting infrastructure (docker compose)"
docker compose -f "$COMPOSE_FILE" up -d --build

echo "2) Wait for critical services"
wait_for_healthy postgres 120
wait_for_healthy rabbitmq 120

echo "3) Running EF migrations (if available)"
if command -v dotnet >/dev/null 2>&1; then
  if dotnet tool list -g | grep -q ef; then
    dotnet ef database update --project src/Services/OrderService/Api || true
    dotnet ef database update --project src/Services/InventoryService/Api || true
    dotnet ef database update --project src/Services/PaymentService/Api || true
  else
    echo "dotnet-ef not installed globally. Skipping automatic migrations."
    echo "Run manually if needed: dotnet ef database update --project <service>"
  fi
else
  echo "dotnet not found on PATH. Skipping migrations."
fi

cat <<'EOF'
Quick-start done. To run services locally (separate terminals):

# OrderService
dotnet run --project src/Services/OrderService/Api --urls "http://localhost:5001"

# InventoryService
dotnet run --project src/Services/InventoryService/Api --urls "http://localhost:5002"

# PaymentService
dotnet run --project src/Services/PaymentService/Api --urls "http://localhost:5003"

Check health endpoints after services start:
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
EOF

echo "Script finished."
