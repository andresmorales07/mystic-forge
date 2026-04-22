# Mystic Forge

MTG Commander/EDH deck-building advisor. Combines Scryfall, EDHRec, and Commander Spellbook with an LLM-driven tag pipeline to offer explainable, synergy-aware suggestions.

## Status

In active development. Phase 0 + 1 (project skeleton + Scryfall bulk ingest) is the current work.

Planning, design specs, and phase breakdowns live in Anytype, not in this repo.

## Stack

ASP.NET Core 10 · EF Core 10 · PostgreSQL 16 + pgvector · Hangfire · Serilog · xUnit + Testcontainers.

## Prerequisites

- .NET 10 SDK
- Docker Desktop (Windows/macOS) or Docker Engine (Linux)

## Running locally

```bash
# 1. Start Postgres + pgvector
docker compose up -d

# 2. Install EF tooling if not already installed
dotnet tool install --global dotnet-ef

# 3. Apply migrations
dotnet ef database update \
  --project src/MysticForge.Infrastructure \
  --startup-project src/MysticForge.Api

# 4. Set your Scryfall contact email (required by Scryfall API guidelines)
dotnet user-secrets set "Scryfall:ContactEmail" "you@example.com" \
  --project src/MysticForge.Api

# 5. Run the API
dotnet run --project src/MysticForge.Api
```

Health check: `http://localhost:5000/healthz`
Hangfire dashboard (dev only): `http://localhost:5000/hangfire`

## Testing

```bash
dotnet test tests/MysticForge.UnitTests
dotnet test tests/MysticForge.IntegrationTests
```

Integration tests use Testcontainers — Docker must be running.

## License

See `LICENSE`.
