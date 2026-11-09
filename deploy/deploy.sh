#!/usr/bin/env bash
# Deploy the Insecured API Keys stack on a Linux host (requires Docker + Docker Compose).
# Usage:
#   ./deploy.sh setup   # first time: copy .env if missing
#   ./deploy.sh build   # build and start/recreate all services
#   ./deploy.sh logs    # tail logs
#   ./deploy.sh stop    # stop the stack
set -euo pipefail

cd "$(dirname "$0")/.."

case "${1:-build}" in
  setup)
    if [[ ! -f .env ]]; then
      cp .env.example .env
      echo ".env created from .env.example - edit the passwords before building."
    else
      echo ".env already exists - leaving it alone."
    fi
    ;;
  build)
    [[ -f .env ]] || { echo "Run ./deploy.sh setup first."; exit 1; }
    docker compose --env-file .env up -d --build
    echo
    echo "Stack is up. API is listening on http://localhost:${API_PORT:-8080}"
    echo "Check status with:  curl http://localhost:${API_PORT:-8080}/api/status"
    ;;
  logs)
    docker compose --env-file .env logs -f --tail=100
    ;;
  stop)
    docker compose --env-file .env down
    ;;
  *)
    echo "Usage: ./deploy.sh {setup|build|logs|stop}"
    exit 1
    ;;
esac
