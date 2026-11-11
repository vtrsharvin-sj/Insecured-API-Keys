# Insecured API Keys — Find exposed API keys before attackers do

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Next.js](https://img.shields.io/badge/Next.js-14-black.svg)
![Docker](https://img.shields.io/badge/Docker-Compose-blue.svg)

> A full-stack platform that continuously scans public GitHub repositories for accidentally committed API keys, validates them against their providers, and makes them discoverable so developers can be notified and rotate their credentials before damage is done.

- [Introduction](#introduction)
- [How it Works](#how-it-works)
- [Tech Stack](#tech-stack)
- [Quick Start](#quick-start)
  - [Configuration](#configuration)
  - [Adding GitHub Tokens](#adding-github-tokens)
- [API Reference](#api-reference)
- [Contributing](#contributing)
- [License](#license)

# Introduction

Every day, developers accidentally push API keys to public GitHub repositories — OpenAI keys, Stripe secrets, AWS credentials, and dozens more. Most of them don't realise until something breaks or a bill arrives.

**Insecured API Keys** runs a scraper that searches GitHub code 24/7, extracts candidate keys, and runs each one through the matching provider's API to confirm whether it is live. Valid keys appear on the site with the repository they came from, so security researchers and community members can open GitHub issues to notify the owners.

The platform also exposes a leaderboard for users who actively report keys, a token contribution system so the community can donate GitHub PATs to increase scraping throughput, and automatic token rotation — when a token hits GitHub's rate limit the scraper parks it, picks the next available one, and keeps going without any manual intervention.

# How it Works

```
GitHub Code Search
       │
       ▼
  Scraper Bot  ──►  PostgreSQL  ◄──  Verifier Bot
  (finds keys)       (stores)        (validates keys
                                      against providers)
       │
       ▼
   Web API  ──►  nginx  ──►  Next.js UI
  (REST + SignalR)
```

1. **Scraper** — a .NET 9 worker runs search queries against the GitHub Code Search API every 10 minutes using a rotating pool of personal access tokens. Each result is parsed for API key patterns and stored in PostgreSQL.

2. **Verifier** — a second .NET 9 worker picks up unverified keys in batches, calls the matching provider's API (OpenAI, Anthropic, Stripe, etc.), and marks each key as valid, invalid, or error. It uses a batch locking system so multiple instances can run without double-processing.

3. **API** — an ASP.NET Core 9 REST API serves the frontend, handles community submissions, and runs a SignalR hub for real-time statistics. It applies all database migrations automatically on startup.

4. **UI** — a Next.js 14 static export served by nginx. The same nginx instance reverse-proxies the API under `/apiproxy/`, so the entire site runs on a single port and origin with no cross-origin complexity.

## Supported Key Types

| Category | Providers |
|---|---|
| AI / LLM | OpenAI, Anthropic Claude, Google AI, Cohere, HuggingFace, Stability AI, Mistral AI, Replicate, Together AI, OpenRouter, Perplexity AI, Groq, DeepSeek, RunwayML, AssemblyAI |
| Cloud | AWS, Azure, Google Cloud Platform |
| Vector / ML Infra | Pinecone, Weaviate, ChromaDB, LangChain |
| Source Control | GitHub, GitLab, BitBucket |
| Payments & Comms | Stripe, SendGrid, Twilio, MongoDB, Firebase |

# Tech Stack

- [ASP.NET Core 9](https://dotnet.microsoft.com) — REST API, SignalR hub, EF Core migrations, rate limiting, referrer-based access control
- [Next.js 14](https://nextjs.org) — static export frontend, served by nginx
- [PostgreSQL 15](https://www.postgresql.org) — primary datastore, persisted in a Docker named volume
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) — ORM with automatic migrations on startup
- [Octokit](https://github.com/octokit/octokit.net) — GitHub API client used by the scraper
- [nginx](https://nginx.org) — static file serving, API reverse proxy, security headers, CORS
- [Docker Compose](https://docs.docker.com/compose/) — orchestrates all five services

# Quick Start

**Prerequisites:** Linux, Docker, and the Docker Compose plugin.

```bash
git clone https://github.com/vtrsharvin-sj/Insecured-API-Keys.git
cd Insecured-API-Keys

# 1. Configure
cp .env.example .env
nano .env   # set POSTGRES_PASSWORD at minimum

# 2. Build and start
docker compose up -d --build

# 3. Verify
curl http://localhost:8081/health   # → {"status":"healthy"}
curl http://localhost:8080          # → HTML
```

The API applies all database migrations on first boot. No manual database setup is needed.

The UI is served on `UI_PORT` (default `8080`). That is the only port you need to expose publicly — the nginx container handles everything else internally.

## Configuration

All runtime configuration is done through environment variables in `.env`. Copy `.env.example` to get started.

| Variable | Required | Default | Description |
|---|---|---|---|
| `POSTGRES_PASSWORD` | ✅ | `postgres` | **Change this.** Database password. |
| `POSTGRES_USER` | | `postgres` | Database user |
| `UI_PORT` | | `8080` | Host port for the full site — port-forward this one |
| `API_PORT` | | `8081` | Host port for direct API access |
| `ALLOWED_REFERRERS` | | — | Comma-separated domains allowed to call the API (e.g. `mysite.com`) |
| `PRODUCTION_DOMAIN` | | — | Your domain — adds it to CORS allowed origins |
| `ASPNETCORE_ENVIRONMENT` | | `Production` | Set to `Development` to enable Swagger at `/scalar/v1` |
| `FORCE_HTTPS_REDIRECT` | | unset | Set to `1` if you terminate TLS in front of this stack |
| `SCRAPER_CONTINUOUS_MODE` | | `true` | Loop the scraper every 10 minutes |
| `VERIFIER_CONTINUOUS_MODE` | | `true` | Loop the verifier every 5 minutes |
| `NEXT_PUBLIC_GA_MEASUREMENT_ID` | | — | Google Analytics 4 ID — leave blank to disable tracking |

## Adding GitHub Tokens

The scraper requires at least one GitHub personal access token with `public_repo` scope. Without it nothing gets scraped.

**Option 1 — via the UI (recommended):**
Go to the **About** page and click **Contribute a GitHub Token**. The token is validated against GitHub before saving. Invalid tokens are rejected immediately with a specific error message.

**Option 2 — directly in the database:**
```bash
docker exec api-db psql -U postgres -d UnsecuredAPIKeys \
  -c "INSERT INTO \"SearchProviderTokens\" (\"Token\", \"SearchProvider\", \"IsEnabled\", \"LastUsedUTC\") \
      VALUES ('your_pat_here', 1, true, NULL);"
```

**Token rotation:** When a token hits GitHub's rate limit, the scraper records the exact reset time on that token and skips it on subsequent cycles until the reset time passes. No restarts or manual intervention needed.

Create a token at [github.com/settings/tokens](https://github.com/settings/tokens/new?scopes=public_repo&description=InsecuredAPIKeys+Scraper) with `public_repo` scope.

# API Reference

Base URL when going through nginx: `http://your-server:8080/apiproxy`  
Direct API: `http://your-server:8081`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check — `{"status":"healthy","utc":"..."}` |
| GET | `/API/GetAllValidKeys` | Paginated key listing. Params: `page`, `pageSize`, `type` (int), `statusFilter=valid` |
| GET | `/API/GetKeyStatistics` | Counts: total indexed, valid now, new today, fixed |
| GET | `/API/GetKeyTypes` | Available key types with key counts |
| GET | `/API/GetSnitchLeaderboard` | Top reporters. Param: `count` |
| GET | `/API/GetLeaderboardStats` | Aggregate leaderboard statistics |
| GET | `/API/GetPatternEffectiveness` | Search pattern success rates |
| GET | `/API/GetInvalidationStats` | Key invalidation statistics |
| GET | `/API/GetKeyRotations` | Recent key rotation events |
| POST | `/API/TrackIssueSubmission` | Record a user flagging a key. Body: `{ApiKeyId, ApiType, RepoUrl, Timestamp}` |
| POST | `/API/SubmitToken` | Contribute a GitHub PAT. Body: `{Token}` — validated before saving |
| POST | `/API/webhook/key-fixed` | Mark a key as fixed. Body: `{KeyId, Reason}` |

**Rate limiting:** `GetAllValidKeys` allows 500 requests/hour per IP. The nginx proxy forwards real client IPs so each user gets their own limit.

# Contributing

Contributions are welcome. If you want to add a new provider validator, improve the scraper patterns, or fix a bug:

1. Fork the repository and create a feature branch (`git checkout -b feature/your-idea`).
2. Make your changes. For new provider support, add a validator in `UnsecuredAPIKeys.Providers` and register the new `ApiTypeEnum` value in `CommonEnums.cs`.
3. Test locally with `docker compose up -d --build`.
4. Commit with a clear message and open a pull request describing what you changed and why.

Bug reports and feature ideas are equally welcome — open an issue with as much detail as possible.

# License

This project is released under the [MIT License](LICENSE). You are free to use, modify, and distribute it; please keep the copyright notice intact.

> Built on top of [UnsecuredAPIKeys-OpenSource](https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource) by TSCarterJr. Attribution is required by the original license.
