#!/usr/bin/env bash
set -euo pipefail

# Quick-start helper for development
# - Starts required infrastructure with docker-compose
# - Runs EF migrations for each service (if dotnet-ef is available)
# - Prints example commands to run services locally

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
echo "Repo root: $ROOT_DIR"

echo "1) Starting infrastructure (docker-compose)"
docker-compose -f "$ROOT_DIR/docker-compose.yml" up -d

echo "Waiting a few seconds for infra to start..."
sleep 6

echo "2) Running EF migrations (if available)"
if command -v dotnet >/dev/null 2>&1; then
  if dotnet tool list -g | grep -q ef; then
    dotnet ef database update --project src/Services/OrderService/Api || true
    dotnet ef database update --project src/Services/InventoryService/Api || true
    dotnet ef database update --project src/Services/PaymentService/Api || true
  else
    echo "dotnet-ef not installed globally. Skipping automatic migrations."
    echo "You can run: dotnet ef database update --project src/Services/OrderService/Api"
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
