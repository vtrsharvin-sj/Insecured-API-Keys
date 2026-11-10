# Insecured API Keys

A platform for discovering, validating, and tracking exposed API keys found in public code repositories.

> ⚠️ **Educational and security-awareness project. Use responsibly and in accordance with applicable laws.**

## What it does

Insecured API Keys continuously scans public GitHub repositories for accidentally committed API keys. When keys are found, they are validated against their respective providers. Users can browse discovered keys, filter by type, report them to the original repository owners, and contribute their own GitHub tokens to help the scraper run faster.

## Stack

| Layer | Technology |
|---|---|
| Backend API | .NET 9 (ASP.NET Core) |
| Frontend | Next.js 14 (static export) |
| Database | PostgreSQL 15 |
| Scraper bot | .NET 9 worker |
| Verifier bot | .NET 9 worker |
| Reverse proxy | nginx |
| Container runtime | Docker Compose |

## Quick start

```bash
git clone https://github.com/vtrsharvin-sj/Insecured-API-Keys.git
cd Insecured-API-Keys

cp .env.example .env
# Edit .env — set POSTGRES_PASSWORD and ALLOWED_REFERRERS at minimum

docker compose up -d --build
```

The API applies all database migrations automatically on first boot.

| Service | URL | Notes |
|---|---|---|
| UI | `http://localhost:8080` | nginx static export + API proxy |
| API | `http://localhost:8081` | Direct API access |
| Health | `http://localhost:8081/health` | Used by healthchecks |

## Configuration

All configuration is done via environment variables in `.env`:

| Variable | Required | Description |
|---|---|---|
| `POSTGRES_PASSWORD` | ✅ | Database password — change from default |
| `POSTGRES_USER` | | Database user (default: `postgres`) |
| `UI_PORT` | | Host port for the UI (default: `8080`) |
| `API_PORT` | | Host port for direct API access (default: `8081`) |
| `ALLOWED_REFERRERS` | | Comma-separated domains allowed to call the API |
| `PRODUCTION_DOMAIN` | | Your domain — adds it to CORS allowed origins |
| `ASPNETCORE_ENVIRONMENT` | | `Production` or `Development` (Swagger at `/scalar/v1`) |
| `NEXT_PUBLIC_GA_MEASUREMENT_ID` | | Google Analytics ID — leave blank to disable GA |
| `SCRAPER_CONTINUOUS_MODE` | | `true` to loop the scraper every 10 min (default) |
| `VERIFIER_CONTINUOUS_MODE` | | `true` to loop the verifier every 5 min (default) |

## Adding a GitHub token for the scraper

The scraper requires at least one GitHub personal access token with `public_repo` scope. Without it, no keys will be found.

**Option 1 — via the UI:** Go to the About page and click "Contribute a GitHub Token".

**Option 2 — via the database directly:**
```bash
docker exec -i api-db psql -U postgres -d UnsecuredAPIKeys -c \
  "INSERT INTO \"SearchProviderTokens\" (\"Token\", \"SearchProvider\", \"IsEnabled\", \"LastUsedUTC\") VALUES ('your_pat_here', 1, true, NULL);"
```

## API endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/API/GetAllValidKeys` | Paginated key listing (`?page=1&pageSize=50&type=130&statusFilter=valid`) |
| GET | `/API/GetKeyStatistics` | Total/valid/new today/fixed counts |
| GET | `/API/GetKeyTypes` | Available key types with counts |
| POST | `/API/TrackIssueSubmission` | Record that a user flagged a key |
| POST | `/API/SubmitToken` | Contribute a GitHub PAT for the scraper |
| GET | `/API/GetSnitchLeaderboard` | Top reporters |
| GET | `/health` | Healthcheck endpoint |

## Supported API key types

OpenAI · Anthropic Claude · Google AI · Cohere · HuggingFace · Stability AI · Mistral AI · Replicate · Together AI · OpenRouter · Perplexity · Groq · DeepSeek · ElevenLabs · RunwayML · AssemblyAI · Pinecone · Weaviate · AWS · Azure · GCP · GitHub · GitLab · Stripe · SendGrid · Twilio · MongoDB · Firebase

## Running without Docker

1. Install .NET 9 SDK and PostgreSQL 15
2. Set the `CONNECTION_STRING` environment variable
3. Run migrations: `dotnet ef database update --project UnsecuredAPIKeys.Data`
4. Start the API: `dotnet run --project UnsecuredAPIKeys.WebAPI`
5. Start the bots: `dotnet run --project UnsecuredAPIKeys.Bots.Scraper` / `UnsecuredAPIKeys.Bots.Verifier`
6. Build the UI: `cd UnsecuredAPIKeys.UI && npm install && npm run build:simple`

## Contributing

Pull requests are welcome. For major changes, open an issue first.

1. Fork the repository
2. Create a branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m 'Add my feature'`
4. Push: `git push origin feature/my-feature`
5. Open a Pull Request

## Support

If this project is useful to you, consider [buying me a coffee](https://buymeacoffee.com/sharvinsj) ☕

## License

MIT — see [LICENSE](LICENSE).

> This project is built on top of [UnsecuredAPIKeys-OpenSource](https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource) by TSCarterJr. Attribution is required by the original license.
