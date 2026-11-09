# Insecured API Keys (Lite, Linux Edition)

A platform for discovering, validating, and tracking unsecured API keys across public code repositories. Educational and security-awareness project — please use responsibly.

**This edition is Linux-first:** the whole stack runs with Docker Compose on any Linux host (or as systemd services), and the Windows-only PowerShell tooling has been removed.

## ⚠️ Educational Purpose Only

This project is designed for educational and security awareness purposes. It demonstrates common security vulnerabilities in API key management. Please use responsibly and in accordance with applicable laws and regulations.

## 🏗️ Architecture

- **WebAPI** (.NET 9): Core backend providing REST endpoints and real-time SignalR stats
- **UI** (Next.js): Frontend with API key discovery browser
- **Data layer** (EF Core + PostgreSQL): schema migrations applied automatically at API startup
- **Providers library**: extensible validation framework per API provider
- **Verifier bot** (.NET worker): validates discovered keys
- **Scraper bot** (.NET worker): discovers new keys across Git platforms

> ⚠️ **Attentive on the scale of the stack: the WebAPI is the only service that publicly exposes keys; the bots are background workers.**

## What's included / removed

Included: core API, UI, scraper + verifier workers, real-time stats, rate limiting.

Removed from the previous version ("legacy UI"):

- ❌ Discord OAuth login, Discord role service and member rate-limit tier
- ❌ PayPal donation buttons, IPN webhook, donation tracking endpoints and related UI pages
- ❌ Windows PowerShell deploy/analysis scripts, Claude bot workflows
- ❌ Google Analytics/other snippets tied to the removed flows

The Discord user table remains in the database schema for compatibility with existing deployments, but nothing in the code reads from it anymore.

## 🚀 Quick Start (Linux, Docker Compose)

Prerequisites: **Linux** (Debian, Ubuntu, Fedora, ...), **Docker** and the **compose** plugin.

```bash
git clone https://github.com/vtrsharvin-sj/Insecured-API-Keys.git
cd Insecured-API-Keys

# 1. Configure
./deploy/deploy.sh setup        # copies .env.example -> .env
$EDITOR .env                     # set POSTGRES_PASSWORD etc.

# 2. Build & run everything (postgres + api + verifier + scraper)
./deploy/deploy.sh build

# 3. Verify
curl -s http://localhost:8080 > /dev/null        # UI (nginx)  - port-forward THIS to your URL
curl -s http://localhost:8081/Status/GetStatus   # API direct
```

The API applies all EF Core migrations on first boot; PostgreSQL data is persisted in a Docker volume (`postgres-data`).

| Service   | Container name | Notes |
|-----------|----------------|-------|
| db        | `api-db`       | PostgreSQL 15, health-checked |
| ui        | `api-ui`       | Static Next.js export via nginx on port `UI_PORT` (default **8080**) |
| api       | `api`          | WebAPI on host port `API_PORT` (default **8081**) |
| verifier  | `api-verifier` | background key validator (continuous mode) |
| scraper   | `api-scraper`  | background key discovery worker (continuous mode) |

**Single-URL deployment:** the `ui` service serves the whole site on one port and reverse-proxies the API at `/apiproxy/*` on the same origin (including the `/hubs/stats` SignalR websocket). Port-forward `UI_PORT` and you have the full URL; the API always accepts same-origin requests regardless of the final domain. If you call the API directly (not through the proxy) from another domain, add it to `ALLOWED_REFERRERS`.

Useful commands:

```bash
./deploy/deploy.sh logs        # tail all services
./deploy/deploy.sh stop        # stop everything
docker compose ps              # status overview
```

## 🐧 Running without Docker (systemd)

1. Install .NET 9 SDK (see [Microsoft's Ubuntu/Debian install guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux)) and PostgreSQL.
2. Publish: `dotnet publish UnsecuredAPIKeys.WebAPI -c Release -o /opt/insecured-api-keys/UnsecuredAPIKeys.WebAPI` (same for the two bots).
3. Copy `deploy/systemd/*.service` to `/etc/systemd/system/`, edit the `User=`, `WorkingDirectory=` and `CONNECTION_STRING` entries.
4. `sudo systemctl daemon-reload && sudo systemctl enable --now insecured-api-keys-api insecured-api-keys-verifier insecured-api-keys-scraper`

## 🔧 Configuration

All runtime configuration is environment-variable based:

| Variable | Used by | Default | Purpose |
|---|---|---|---|
| `CONNECTION_STRING` | api / bots | built-in localhost string | PostgreSQL connection |
| `POSTGRES_PASSWORD` | compose db/api/bots | `postgres` | database password inside the compose stack |
| `UI_PORT` | compose | `8080` | host port for the full site (UI + API proxy) — **port-forward this one** |
| `API_PORT` | compose | `8081` | host port for direct access to the raw API |
| `ASPNETCORE_ENVIRONMENT` | api | `Production` | `Development` enables Swagger at `/scalar/v1` |
| `SCRAPER_SERVICE_NAME` | api | `api-scraper` | container name used by the /status endpoint |
| `VERIFIER_SERVICE_NAME` | api | `api-verifier` | container name used by the /status endpoint |
| `ALLOWED_REFERRERS` | api | – | extra domains allowed to call the API directly (same-origin always allowed) |
| `PRODUCTION_DOMAIN` | api | – | adds CORS origin `https://<domain>` (only needed for direct cross-origin browser use) |
| `FORCE_HTTPS_REDIRECT` | api | unset | set to enable HTTPS redirection (behind your own TLS proxy) |

Rate limiting is per-IP, configurable through the `RateLimit` row in the `ApplicationSettings` table (default 5 requests/hour).

## 🔑 API endpoints (high level)

- `GET /api/GetRandomKey` — fetch an exposed key (rate limited; over-limit requests get a fallback key instead of a hard block)
- `GET /api/GetKeyStatistics`, `/api/GetPatternEffectiveness`, `/api/GetInvalidationStats`, ...
- `POST /api/webhook/key-fixed` — mark a key as fixed
- SignalR hub at `/hubs/stats` (`DisplayCountUpdated`, `ActiveUserUpdated`, ...)

Live OpenAPI docs are available at `/scalar/v1` in Development mode.

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push: `git push origin feature/amazing-feature`
5. Open a Pull Request (CI builds the backend and the UI on `ubuntu-latest`)

## 📝 License

Custom attribution-required license based on MIT — see [LICENSE](LICENSE).

### ⚠️ Attribution requirement

This codebase derives from [UnsecuredAPIKeys-OpenSource](https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource) by TSCarterJr. Any public-facing use of this code requires a visible UI attribution link to that original repository. Removing attribution violates the license.

## ⚖️ Legal and Ethical Considerations

- **Educational purpose**: demonstrates insecure key handling for security awareness
- **Responsible use**: users are responsible for compliance with applicable laws
- **No warranty**: the software is provided as-is

---

Built on [UnsecuredAPIKeys-OpenSource](https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource) · Linux edition maintained by [@vtrsharvin-sj](https://github.com/vtrsharvin-sj)
