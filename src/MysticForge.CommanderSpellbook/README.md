# MysticForge.CommanderSpellbook

Kiota-generated typed client for the Commander Spellbook backend
(https://backend.commanderspellbook.com).

## Regenerating

The generated client is committed to the repo. To regenerate after an
upstream schema change:

    pwsh ./scripts/regen-spellbook-client.ps1   # Windows / cross-platform
    ./scripts/regen-spellbook-client.sh         # bash

CI verifies that `Generated/` matches a fresh regeneration against the
live `/schema/` endpoint. PRs with a non-empty diff after regen will fail.

## What lives where

- `.openapi/schema.json` — snapshot of the upstream OpenAPI document
- `Generated/`           — Kiota-emitted typed client (do not hand-edit)
- `kiota-lock.json`      — Kiota tooling version pin

Infrastructure code consumes the generated `SpellbookApiClient` via DI;
adapter classes in `MysticForge.Infrastructure/Spellbook/` translate
between Kiota DTOs and Application DTOs.
