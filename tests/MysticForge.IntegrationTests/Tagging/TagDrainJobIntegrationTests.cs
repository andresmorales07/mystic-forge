using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Tagging;

// End-to-end TagDrainJob regression test. Scaffolded but not active.
//
// The intent is to feed each of the 53 Phase 2a fixtures (Fixtures/taxonomy-v1-fixtures.yaml) back
// through the pipeline via a stubbed IOpenRouterTaggingClient that returns each card's expected
// raw tag set, then assert the database reflects them after one drain tick.
//
// Why this is skipped, not implemented:
//   1. Vocabulary divergence. The Phase 2a fixture file uses Phase 2a's role names
//      (`card_draw`, `card_filtering`, `mana_fix`, `recursion`, `win_condition`). Phase 2b's
//      closed Tier 1 Role enum uses `draw`, `win_con`, plus `stax`/`lock_piece`/`utility` that
//      Phase 2a didn't have. ~5 of the 11 Phase 2b roles map cleanly; the rest don't. Until
//      these vocabularies are reconciled (either by updating the fixture or expanding/aligning
//      Role.All), strict 1:1 assertions would fail on ~half the cards.
//   2. The fixture file omits `oracle_id`, `oracle_text`, and `type_line` — those are LLM
//      *input* data that Phase 2a didn't need to capture. The test would have to synthesize
//      placeholder values, which is reasonable but adds noise.
//   3. Field naming differs from the plan: `structural_roles` (vs `roles`), `synergy_hooks`
//      (vs `synergy_hook_paths`), `mechanic_tags` (vs `mechanics`).
//
// Coverage in the meantime: TagSetResolverTests covers the unknown-role-drop and ancestor-dedup
// paths in isolation; TagWriterTests covers the per-card transactional write semantics;
// OutboxClaimerTests covers the claim/reclaim contract; the smoke test in Task 25 exercises
// the actual API end-to-end against real OpenRouter.
[Collection("postgres")]
public sealed class TagDrainJobIntegrationTests
{
    [Fact(Skip = "Phase 2a/2b vocabulary alignment + fixture LLM-input fields needed; tracked separately.")]
    public void DrainsAllFixtureCards_ProducingExpectedTagRows() { }
}
