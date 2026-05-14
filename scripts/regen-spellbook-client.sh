#!/usr/bin/env bash
# Regenerate the Commander Spellbook typed client from the live OpenAPI spec.
# Requires Kiota CLI 2.x+ (the runtime packages are pinned at 2.0.0 in Directory.Packages.props).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SCHEMA_PATH="$ROOT/src/MysticForge.CommanderSpellbook/.openapi/schema.json"
GENERATED_DIR="$ROOT/src/MysticForge.CommanderSpellbook/Generated"
SCHEMA_URL="https://backend.commanderspellbook.com/schema/"

if ! command -v kiota >/dev/null 2>&1; then
    echo "kiota CLI not found. Install with: dotnet tool install --global Microsoft.OpenApi.Kiota (requires Kiota CLI 2.x+)" >&2
    exit 1
fi

echo "Downloading OpenAPI spec from $SCHEMA_URL ..."
curl --fail --silent --show-error "$SCHEMA_URL" -o "$SCHEMA_PATH"

echo "Wiping previous generated output ..."
rm -rf "$GENERATED_DIR"
mkdir -p "$GENERATED_DIR"

echo "Running kiota generate ..."
kiota generate \
    --openapi "$SCHEMA_PATH" \
    --language csharp \
    --output "$GENERATED_DIR" \
    --namespace-name "MysticForge.CommanderSpellbook.Generated" \
    --class-name "SpellbookApiClient" \
    --clean-output \
    --clear-cache

echo "Codegen complete. Review the diff and commit if intended."
