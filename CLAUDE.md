# Mystic Forge — Claude Code Guide

Personal MTG Commander/EDH deck advisor. ASP.NET Core + PostgreSQL + Hangfire + pgvector.

## Planning lives in Anytype, not this repo

Design docs, phase breakdowns, research summaries, and per-phase specs are **not in this repo**. They live as linked notes in the "Mystic Forge" project in Anytype:

- Project note — overview and tech stack
- Phased Build Plan (2026-04-22) — the 7-phase breakdown (Phase 0 skeleton → Phase 6 frontend)
- Per-phase design specs (Phase 0+1 is the active one)

The `docs/` directory is gitignored. When adding to planning, create or update Anytype notes; do not write design docs into the repo. Skills with a default "write spec to `docs/…`" behavior should be redirected to Anytype.

## Architecture — Clean Architecture (lean)

```
src/
  MysticForge.Domain          entities with behavior, value objects; zero dependencies
  MysticForge.Application     use cases, workflows, interfaces Infrastructure implements
  MysticForge.Infrastructure  EF DbContext, Scryfall / EDHRec / Spellbook clients, Hangfire jobs
  MysticForge.Api             ASP.NET Core host, DI composition root, Hangfire dashboard
tests/
  MysticForge.UnitTests
  MysticForge.IntegrationTests
```

Dependency rules: Domain → nothing; Application → Domain; Infrastructure → Application + Domain; Api → all three. Tests may reference any production project.

## Conventions that aren't obvious from the code

- **.NET 10 LTS.** Nullable reference types on. Implicit usings on.
- **No `IRepository<T>` generic abstractions.** If a persistence seam is needed, make it purpose-built (`ICardWriter`, `IScryfallBulkClient`).
- **EF Core entities live in `Infrastructure`, not `Domain`.** Domain entities are POCOs with behavior; map at the persistence boundary.
- **Postgres: snake_case** tables and columns via `EFCore.NamingConventions`. C# remains PascalCase.
- **All timestamps `TIMESTAMPTZ`; all app code uses UTC** (`DateTime.UtcNow` / `DateTimeOffset.UtcNow`).
- **Explicit migrations.** `dotnet ef database update` in dev; dedicated migration command in prod. Never auto-apply on startup.
- **Hangfire storage** colocated in the same Postgres under schema `hangfire`.
- **Serilog everywhere.** Console + rolling file sinks, request-ID enricher.
- **No AutoMapper** until there's a real reason for one. Direct mapping first.
- **Anemic Domain is worse than no Domain project.** Put real behavior on entities (invariants, hash computation, errata comparison).

## Running locally

```bash
docker compose up -d        # Postgres + pgvector
dotnet ef database update --project src/MysticForge.Infrastructure --startup-project src/MysticForge.Api
dotnet run --project src/MysticForge.Api
```

Hangfire dashboard at `/hangfire` (dev-only by default). Health check at `/healthz`.

## Testing

xUnit + NSubstitute + AwesomeAssertions.

- `UnitTests` are fast (no I/O, no containers) and run on every build.
- `IntegrationTests` use Testcontainers (real Postgres + pgvector) and WireMock.NET (fake Scryfall) and run on every push in CI.

```bash
dotnet test tests/MysticForge.UnitTests
dotnet test tests/MysticForge.IntegrationTests
```

Avoid these assertion libraries:
- **FluentAssertions v8+** — commercial license.
- **Moq** — SponsorLink history; default to NSubstitute.

## Config & secrets

- **Local dev:** `dotnet user-secrets` on `MysticForge.Api` for anything sensitive.
- **Local dev via compose:** `.env` file (gitignored). Use `.env.example` as the template.
- **Prod:** env vars injected by the deployment environment; read via `IConfiguration` with no code branching on environment.

No secrets in git, ever. `appsettings.Production.json` is not committed.

## LLM access — OpenRouter only

All LLM calls go through **OpenRouter** (OpenAI-compatible API at `https://openrouter.ai/api/v1`). Do **not** add the Anthropic .NET SDK. Model id is a config value (e.g. `anthropic/claude-haiku-4-5`) and is recorded on `card_tags.model_version` when a card is tagged.

## Navigating the codebase — prefer Serena MCP

When exploring or editing this codebase, prefer the **Serena MCP server**'s semantic tools over `Read`-whole-file approaches. Serena understands C# symbols via LSP and only returns the structure/bodies you actually ask for, which keeps the context budget lean on a multi-project .NET solution.

- Start with `get_symbols_overview` on a file or `list_dir` on a folder before reading anything in full.
- Use `find_symbol` with a `name_path` (and `include_body` only when you need the body) rather than `Read`-ing an entire file.
- Use `find_referencing_symbols` to trace a method's callers instead of grepping.
- For edits, `insert_before_symbol` / `insert_after_symbol` / `replace_symbol_body` are surgical and cheap; fall back to `Edit` only when the change doesn't fit a single symbol.
- `search_for_pattern` is fine for free-text searches the symbolic tools can't express.

Reading a whole file is a last resort, not the default.

## Branch naming

| Prefix | When to use |
|---|---|
| `feature/*` | New capabilities or user-facing behaviour |
| `fix/*` | Bug fixes |
| `chore/*` | CI changes, config tweaks, manual dependency bumps — maintenance that isn't a feature or bug |

Slugs: lowercase, hyphen-separated, brief (e.g. `feature/scryfall-bulk-ingest`). Dependabot branches (`dependabot/nuget/…`) are exempt from this convention.

## Commit and deployment guardrails

- Ask before committing. The user approves commits explicitly; don't pre-stage or commit on their behalf.
- Never force-push to `main`. Public repo.
- Deployment to Proxmox is deferred until end of Phase 1. `docker-compose.prod.yml` is committed but nothing is running.

## What's deferred

- Auth, public API surface beyond `/healthz`, deck persistence → Phase 5.
- Frontend → Phase 6.
- Tagging pipeline, taxonomy, LLM prompts → Phase 2.
- Scoring, recommendations, archetype detection → Phase 4.

See the Phased Build Plan in Anytype for the full roadmap.
