<div align="center">

# 🔑 Insecured API Keys

**Discover, validate, and track exposed API keys found in public repositories.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com)
[![Next.js](https://img.shields.io/badge/Next.js-14-black.svg)](https://nextjs.org)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue.svg)](https://docs.docker.com/compose)
[![GitHub](https://img.shields.io/badge/GitHub-vtrsharvin--sj-black?logo=github)](https://github.com/vtrsharvin-sj/Insecured-API-Keys)

[**Live Demo**](https://8080-agq36dxutmi62gju3bzvftrayu.instabox.tech) · [**Buy Me a Coffee**](https://buymeacoffee.com/sharvinsj) · [**Report a Bug**](https://github.com/vtrsharvin-sj/Insecured-API-Keys/issues)

</div>

---

## What is this?

Insecured API Keys is a full-stack security platform that continuously scans public GitHub repositories for accidentally committed API keys. When keys are found, they are validated against their respective providers (OpenAI, Anthropic, Google, Stripe, and 25+ more). Users can browse discovered keys, filter by provider, copy them, report them to the original repository owners, and contribute their own GitHub tokens to help the scraper discover more.

> ⚠️ **For educational and security awareness purposes only. Use responsibly and in accordance with applicable laws.**

---

## Features

- 🔍 **Continuous scraping** — searches GitHub code using rotating API tokens across 10+ search patterns
- ✅ **Automatic validation** — verifies each key against its provider's API in real time
- 🔄 **Smart token rotation** — when a GitHub token hits rate limits, it's automatically parked until reset and the next token takes over
- 🤝 **Community tokens** — users can contribute their own GitHub PATs via the UI to boost scraping speed
- 📊 **Live statistics** — total indexed, valid now, new today, fixed — auto-refreshing every 30 seconds without flicker
- 🏆 **Leaderboard** — ranks users who report exposed keys to repo owners
- 🌐 **Single-URL deployment** — nginx serves the UI and proxies the API on one port, one origin
- 📱 **Responsive** — dark mode, mobile-friendly

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        nginx (UI + proxy)                    │
│   /          → Next.js static export                         │
│   /apiproxy/ → WebAPI (ASP.NET Core 9)                       │
└──────────────────────────┬──────────────────────────────────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
         WebAPI        Scraper      Verifier
         (.NET 9)      (.NET 9)     (.NET 9)
              │            │            │
              └────────────┼────────────┘
                           │
                     PostgreSQL 15
```

| Container | Purpose |
|---|---|
| `api-ui` | nginx serving the Next.js static export + reverse proxy to the API |
| `api` | ASP.NET Core 9 REST API — migrations, endpoints, SignalR stats hub |
| `api-scraper` | .NET worker — searches GitHub for exposed keys every 10 minutes |
| `api-verifier` | .NET worker — validates keys against provider APIs every 5 minutes |
| `api-db` | PostgreSQL 15 — persisted in a named Docker volume |

---

## Quick Start

**Prerequisites:** Linux, Docker, Docker Compose plugin.

```bash
git clone https://github.com/vtrsharvin-sj/Insecured-API-Keys.git
cd Insecured-API-Keys

# 1. Configure
cp .env.example .env
nano .env   # set POSTGRES_PASSWORD and ALLOWED_REFERRERS at minimum

# 2. Build and start everything
docker compose up -d --build

# 3. Verify
curl http://localhost:8081/health        # → {"status":"healthy"}
curl http://localhost:8080               # → HTML (UI)
```

The API automatically applies all database migrations on first boot. No manual setup needed.

---

## Configuration

All configuration lives in `.env`. Copy `.env.example` to get started.

| Variable | Required | Default | Description |
|---|---|---|---|
| `POSTGRES_PASSWORD` | ✅ | `postgres` | **Change this.** Database password. |
| `POSTGRES_USER` | | `postgres` | Database user |
| `UI_PORT` | | `8080` | Host port for the full site (port-forward this one) |
| `API_PORT` | | `8081` | Host port for direct API access |
| `ALLOWED_REFERRERS` | | — | Comma-separated domains that may call the API (e.g. `mysite.com`) |
| `PRODUCTION_DOMAIN` | | — | Your domain — adds it to CORS allowed origins |
| `ASPNETCORE_ENVIRONMENT` | | `Production` | Set to `Development` to enable Swagger at `/scalar/v1` |
| `FORCE_HTTPS_REDIRECT` | | unset | Set to `1` if you terminate TLS in front of this stack |
| `SCRAPER_CONTINUOUS_MODE` | | `true` | Loop the scraper every 10 minutes |
| `VERIFIER_CONTINUOUS_MODE` | | `true` | Loop the verifier every 5 minutes |
| `NEXT_PUBLIC_GA_MEASUREMENT_ID` | | — | Google Analytics 4 ID — leave blank to disable GA entirely |

---

## Adding GitHub Tokens for the Scraper

The scraper needs at least one GitHub personal access token with `public_repo` scope. Without it, nothing gets scraped.

**Option 1 — via the UI (recommended):**
Go to the **About** page → click **"Contribute a GitHub Token"** → paste your PAT → Submit.
The token is validated against GitHub before saving. Invalid tokens are rejected with a clear error.

**Option 2 — directly in the database:**
```bash
docker exec api-db psql -U postgres -d UnsecuredAPIKeys \
  -c "INSERT INTO \"SearchProviderTokens\" (\"Token\", \"SearchProvider\", \"IsEnabled\", \"LastUsedUTC\") VALUES ('your_pat_here', 1, true, NULL);"
```

**Token rotation:** When a token hits GitHub's rate limit, the scraper automatically parks it until the reset time and picks the next available token. No restarts needed.

**Creating a GitHub PAT:** Go to [github.com/settings/tokens/new](https://github.com/settings/tokens/new?scopes=public_repo&description=InsecuredAPIKeys+Scraper), select `public_repo` scope, generate, and paste.

---

## API Reference

Base URL: `http://your-server:8081` or via proxy at `http://your-server:8080/apiproxy`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/health` | None | Health check — returns `{"status":"healthy"}` |
| GET | `/API/GetAllValidKeys` | Referrer | Paginated key listing. Params: `page`, `pageSize`, `type` (int), `statusFilter=valid` |
| GET | `/API/GetKeyStatistics` | None | Counts: total, valid, new today, fixed |
| GET | `/API/GetKeyTypes` | None | Available key types with counts |
| GET | `/API/GetRateLimitInfo` | None | Current rate limit settings |
| GET | `/API/GetSnitchLeaderboard` | None | Top reporters (param: `count`) |
| GET | `/API/GetLeaderboardStats` | None | Aggregate leaderboard statistics |
| GET | `/API/GetKeyRotations` | None | Recent key rotation events |
| GET | `/API/GetPatternEffectiveness` | None | Search pattern success rates |
| GET | `/API/GetInvalidationStats` | None | Key invalidation statistics |
| POST | `/API/TrackIssueSubmission` | Referrer | Record that a user flagged a key. Body: `{ApiKeyId, ApiType, RepoUrl}` |
| POST | `/API/SubmitToken` | None | Contribute a GitHub PAT. Body: `{Token}` — validated against GitHub before saving |
| POST | `/API/webhook/key-fixed` | None | Mark a key as fixed. Body: `{KeyId, Reason}` |

**Rate limiting:** `GetAllValidKeys` allows 500 requests/hour per IP. The API forwards real client IPs through the nginx proxy.

---

## Supported API Key Types

| Category | Providers |
|---|---|
| AI / LLM | OpenAI, Anthropic Claude, Google AI, Cohere, HuggingFace, Stability AI, Mistral AI, Replicate, Together AI, OpenRouter, Perplexity AI, Groq, DeepSeek, RunwayML, AssemblyAI |
| Cloud | AWS, Azure, Google Cloud Platform |
| Vector / ML Infra | Pinecone, Weaviate, ChromaDB, LangChain |
| Source Control | GitHub, GitLab, BitBucket |
| Payments & Comms | Stripe, SendGrid, Twilio, MongoDB, Firebase |

---

## Running Without Docker

1. Install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) and PostgreSQL 15
2. Set `CONNECTION_STRING` environment variable:
   ```bash
   export CONNECTION_STRING="Host=localhost;Database=UnsecuredAPIKeys;Username=postgres;Password=your_password"
   ```
3. Start the API (applies migrations automatically):
   ```bash
   dotnet run --project UnsecuredAPIKeys.WebAPI
   ```
4. Start the bots:
   ```bash
   dotnet run --project UnsecuredAPIKeys.Bots.Scraper
   dotnet run --project UnsecuredAPIKeys.Bots.Verifier
   ```
5. Build and serve the UI:
   ```bash
   cd UnsecuredAPIKeys.UI
   npm install
   NEXT_PUBLIC_API_URL=http://localhost:5000 npm run build:simple
   npx serve out
   ```

---

## Deployment Tips

**Single public URL:** port-forward `UI_PORT` (default `8080`). The nginx container serves the UI and proxies all `/apiproxy/*` calls to the API internally. You never need to expose the API port publicly.

**TLS:** Terminate TLS in front of the nginx container (Cloudflare, your own nginx, Caddy, etc.) and set `FORCE_HTTPS_REDIRECT=1` in `.env`.

**Domain:** Set `ALLOWED_REFERRERS=yourdomain.com` and `PRODUCTION_DOMAIN=yourdomain.com` in `.env`, then restart the API container.

**Monitoring:** `GET /health` returns `{"status":"healthy","utc":"..."}` — wire this into your uptime monitor.

---

## Project Structure

```
Insecured-API-Keys/
├── UnsecuredAPIKeys.WebAPI/        # ASP.NET Core 9 REST API
├── UnsecuredAPIKeys.UI/            # Next.js 14 frontend (static export)
├── UnsecuredAPIKeys.Bots.Scraper/  # GitHub scraper worker
├── UnsecuredAPIKeys.Bots.Verifier/ # Key validation worker
├── UnsecuredAPIKeys.Data/          # EF Core models + migrations
├── UnsecuredAPIKeys.Providers/     # Provider validation logic
├── deploy/
│   ├── nginx-ui.conf               # nginx config (UI + proxy + security headers)
│   └── deploy.sh                   # Helper script (setup/build/logs/stop)
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## Contributing

Pull requests are welcome. For major changes, open an issue first to discuss what you'd like to change.

```bash
# Fork and clone
git clone https://github.com/vtrsharvin-sj/Insecured-API-Keys.git
cd Insecured-API-Keys

# Create a branch
git checkout -b feature/my-feature

# Make changes, then commit
git commit -m "Add my feature"

# Push and open a PR
git push origin feature/my-feature
```

---

## Support

If this project is useful to you, consider [buying me a coffee](https://buymeacoffee.com/sharvinsj) ☕

---

## License

MIT — see [LICENSE](LICENSE).

> This project builds on [UnsecuredAPIKeys-OpenSource](https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource) by TSCarterJr. Attribution is required by the original license.
