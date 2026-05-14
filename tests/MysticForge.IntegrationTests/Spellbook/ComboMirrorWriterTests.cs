using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Spellbook;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class ComboMirrorWriterTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public ComboMirrorWriterTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE spellbook_ingest_runs RESTART IDENTITY CASCADE");
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE cards RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ComboMirrorWriter NewWriter(MysticForgeDbContext db) =>
        new(db, new CardNameResolver(db), TimeProvider.System);

    // -------------------------------------------------------------------------

    [Fact]
    public async Task Upserts_a_new_combo_with_cards_features_and_templates()
    {
        long runId;
        long featureId = 901_001L;
        long templateId = 902_001L;
        const string comboId = "cmw-new-combo-full";

        await using (var db = _db.NewContext())
        {
            runId = await SpellbookSeedHelpers.OpenRunAsync(db);

            // Seed the feature and template so FK constraints are satisfied.
            await SpellbookSeedHelpers.UpsertFeatureAsync(db, featureId,  "Infinite Mana",  runId);
            await SpellbookSeedHelpers.UpsertTemplateAsync(db, templateId, "Basic Template", runId);
        }

        RawCombo rawCombo = MakeRawCombo(
            id:          comboId,
            cards:       [new RawComboCard(1, "Bloodghast",      1, false, null),
                          new RawComboCard(2, "Never-Seen Card", 1, false, null)],
            featureIds:  [featureId],
            templateRefs:[new RawComboTemplateRef(templateId, 1)]);

        WrittenComboCounts counts;
        await using (var db = _db.NewContext())
        {
            counts = await NewWriter(db).UpsertVariantsAsync([rawCombo], runId, CancellationToken.None);
        }

        counts.Inserted.Should().Be(1);
        counts.Updated.Should().Be(0);

        await using var verify = _db.NewContext();
        (await verify.Combos    .CountAsync(c => c.Id == comboId))            .Should().Be(1);
        (await verify.ComboCards.CountAsync(c => c.ComboId == comboId))       .Should().Be(2);
        (await verify.ComboFeatures .CountAsync(c => c.ComboId == comboId))   .Should().Be(1);
        (await verify.ComboTemplates.CountAsync(c => c.ComboId == comboId))   .Should().Be(1);
    }

    [Fact]
    public async Task Upserts_an_existing_combo_replacing_its_card_set()
    {
        long runId;
        const string comboId = "cmw-replace-cards";

        await using (var db = _db.NewContext())
        {
            runId = await SpellbookSeedHelpers.OpenRunAsync(db);
        }

        // First upsert — 3 cards.
        RawCombo first = MakeRawCombo(
            id:    comboId,
            cards: [new RawComboCard(1, "Card A", 1, false, null),
                    new RawComboCard(2, "Card B", 1, false, null),
                    new RawComboCard(3, "Card C", 1, false, null)]);

        await using (var db = _db.NewContext())
            await NewWriter(db).UpsertVariantsAsync([first], runId, CancellationToken.None);

        // Second upsert — 2 different cards.
        RawCombo second = MakeRawCombo(
            id:    comboId,
            cards: [new RawComboCard(1, "Card X", 1, false, null),
                    new RawComboCard(2, "Card Y", 1, false, null)]);

        WrittenComboCounts counts;
        await using (var db = _db.NewContext())
        {
            counts = await NewWriter(db).UpsertVariantsAsync([second], runId, CancellationToken.None);
        }

        counts.Inserted.Should().Be(0);
        counts.Updated.Should().Be(1);

        await using var verify = _db.NewContext();
        var cards = await verify.ComboCards.Where(c => c.ComboId == comboId).ToListAsync();
        cards.Should().HaveCount(2);
        cards.Select(c => c.CardName).Should().BeEquivalentTo(["Card X", "Card Y"]);
    }

    [Fact]
    public async Task Resolves_oracle_id_when_card_exists()
    {
        long runId;
        Guid oracleId;
        const string comboId = "cmw-resolve-oracle-id";

        await using (var db = _db.NewContext())
        {
            runId    = await SpellbookSeedHelpers.OpenRunAsync(db);
            (oracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Bloodghast");
        }

        RawCombo rawCombo = MakeRawCombo(
            id:    comboId,
            cards: [new RawComboCard(1, "Bloodghast", 1, false, null)]);

        await using (var db = _db.NewContext())
            await NewWriter(db).UpsertVariantsAsync([rawCombo], runId, CancellationToken.None);

        await using var verify = _db.NewContext();
        var card = await verify.ComboCards.SingleAsync(c => c.ComboId == comboId && c.CardPosition == 1);
        card.OracleId.Should().Be(oracleId);
    }

    [Fact]
    public async Task Leaves_oracle_id_null_when_card_does_not_exist()
    {
        long runId;
        const string comboId = "cmw-unresolved-oracle-id";

        await using (var db = _db.NewContext())
        {
            runId = await SpellbookSeedHelpers.OpenRunAsync(db);
        }

        RawCombo rawCombo = MakeRawCombo(
            id:    comboId,
            cards: [new RawComboCard(1, "Never-Seen Card", 1, false, null)]);

        await using (var db = _db.NewContext())
            await NewWriter(db).UpsertVariantsAsync([rawCombo], runId, CancellationToken.None);

        await using var verify = _db.NewContext();
        var card = await verify.ComboCards.SingleAsync(c => c.ComboId == comboId && c.CardPosition == 1);
        card.OracleId.Should().BeNull();
    }

    [Fact]
    public async Task Re_resolution_picks_up_card_that_landed_later()
    {
        long runId;
        Guid oracleId;
        const string comboId = "cmw-re-resolve";

        await using (var db = _db.NewContext())
        {
            runId = await SpellbookSeedHelpers.OpenRunAsync(db);
        }

        // First upsert — card not yet in DB.
        RawCombo rawCombo = MakeRawCombo(
            id:    comboId,
            cards: [new RawComboCard(1, "Bloodghast", 1, false, null)]);

        await using (var db = _db.NewContext())
            await NewWriter(db).UpsertVariantsAsync([rawCombo], runId, CancellationToken.None);

        // Insert the card AFTER the first upsert.
        await using (var db = _db.NewContext())
        {
            (oracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Bloodghast");
        }

        // Second upsert — card is now resolvable.
        await using (var db = _db.NewContext())
            await NewWriter(db).UpsertVariantsAsync([rawCombo], runId, CancellationToken.None);

        await using var verify = _db.NewContext();
        var card = await verify.ComboCards.SingleAsync(c => c.ComboId == comboId && c.CardPosition == 1);
        card.OracleId.Should().Be(oracleId);
    }

    [Fact]
    public async Task SoftMarkUnseen_marks_combos_features_templates_with_older_run_id()
    {
        long run1, run2;
        const string combo1Id = "cmw-soft-mark-combo-1";
        const string combo2Id = "cmw-soft-mark-combo-2";
        long feature1Id = 910_001L;
        long feature2Id = 910_002L;
        long template1Id = 911_001L;
        long template2Id = 911_002L;

        await using (var db = _db.NewContext())
        {
            run1 = await SpellbookSeedHelpers.OpenRunAsync(db);
            run2 = await SpellbookSeedHelpers.OpenRunAsync(db);
        }

        // Insert items in run1.
        await using (var db = _db.NewContext())
        {
            await SpellbookSeedHelpers.UpsertFeatureAsync(db,  feature1Id,  "Feature Run1",  run1);
            await SpellbookSeedHelpers.UpsertTemplateAsync(db, template1Id, "Template Run1", run1);
        }

        await using (var db = _db.NewContext())
        {
            var c1 = MakeRawCombo(id: combo1Id, cards: [new RawComboCard(1, "Card X", 1, false, null)]);
            await NewWriter(db).UpsertVariantsAsync([c1], run1, CancellationToken.None);
        }

        // Insert items in run2.
        await using (var db = _db.NewContext())
        {
            await SpellbookSeedHelpers.UpsertFeatureAsync(db,  feature2Id,  "Feature Run2",  run2);
            await SpellbookSeedHelpers.UpsertTemplateAsync(db, template2Id, "Template Run2", run2);
        }

        await using (var db = _db.NewContext())
        {
            var c2 = MakeRawCombo(id: combo2Id, cards: [new RawComboCard(1, "Card Y", 1, false, null)]);
            await NewWriter(db).UpsertVariantsAsync([c2], run2, CancellationToken.None);
        }

        // SoftMarkUnseen with run2 — only run1 items should be marked.
        int total;
        await using (var db = _db.NewContext())
        {
            total = await NewWriter(db).SoftMarkUnseenAsync(run2, CancellationToken.None);
        }

        // 1 combo + 1 feature + 1 template from run1.
        total.Should().Be(3);

        await using var verify = _db.NewContext();
        (await verify.Features .SingleAsync(f => f.Id == feature1Id))   .DeletedAt.Should().NotBeNull();
        (await verify.Features .SingleAsync(f => f.Id == feature2Id))   .DeletedAt.Should().BeNull();
        (await verify.Templates.SingleAsync(t => t.Id == template1Id))  .DeletedAt.Should().NotBeNull();
        (await verify.Templates.SingleAsync(t => t.Id == template2Id))  .DeletedAt.Should().BeNull();
        (await verify.Combos   .SingleAsync(c => c.Id == combo1Id))     .DeletedAt.Should().NotBeNull();
        (await verify.Combos   .SingleAsync(c => c.Id == combo2Id))     .DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task SoftMarkUnseen_clears_deleted_at_when_row_re_emerges()
    {
        long run1, run2;
        long featureId = 920_001L;

        await using (var db = _db.NewContext())
        {
            run1 = await SpellbookSeedHelpers.OpenRunAsync(db);
            run2 = await SpellbookSeedHelpers.OpenRunAsync(db);
        }

        // Insert feature in run1, then soft-mark it.
        await using (var db = _db.NewContext())
        {
            await SpellbookSeedHelpers.UpsertFeatureAsync(db, featureId, "Re-emerge Feature", run1);
        }

        await using (var db = _db.NewContext())
        {
            await NewWriter(db).SoftMarkUnseenAsync(run2, CancellationToken.None);
        }

        // Verify it's soft-deleted.
        await using (var verify = _db.NewContext())
        {
            var f = await verify.Features.SingleAsync(f => f.Id == featureId);
            f.DeletedAt.Should().NotBeNull();
        }

        // Re-upsert the feature in run2 — DeletedAt should be cleared.
        var raw = new RawFeature(featureId, "Re-emerge Feature", "ok", false);
        await using (var db = _db.NewContext())
        {
            await NewWriter(db).UpsertFeaturesAsync([raw], run2, CancellationToken.None);
        }

        await using var verify2 = _db.NewContext();
        var feature = await verify2.Features.SingleAsync(f => f.Id == featureId);
        feature.DeletedAt.Should().BeNull();
        feature.LastSeenRunId.Should().Be(run2);
    }

    [Fact]
    public async Task PurgeStaleCache_deletes_cache_rows_older_than_given_run_id()
    {
        long run1, run2;

        await using (var db = _db.NewContext())
        {
            run1 = await SpellbookSeedHelpers.OpenRunAsync(db);
            run2 = await SpellbookSeedHelpers.OpenRunAsync(db);
        }

        byte[] oldHash = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                          17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32];
        byte[] newHash = [32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17,
                          16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];

        await using (var db = _db.NewContext())
        {
            await SpellbookSeedHelpers.InsertCacheEntryAsync(db, oldHash, run1);
            await SpellbookSeedHelpers.InsertCacheEntryAsync(db, newHash, run2);
        }

        await using (var db = _db.NewContext())
        {
            await NewWriter(db).PurgeStaleCacheAsync(run2, CancellationToken.None);
        }

        await using var verify = _db.NewContext();
        (await verify.FindMyCombosCache.CountAsync(c => c.IngestRunId == run1)).Should().Be(0);
        (await verify.FindMyCombosCache.CountAsync(c => c.IngestRunId == run2)).Should().Be(1);
    }

    // -------------------------------------------------------------------------
    // Helpers

    private static RawCombo MakeRawCombo(
        string                             id,
        IReadOnlyList<RawComboCard>        cards,
        IReadOnlyList<long>?               featureIds   = null,
        IReadOnlyList<RawComboTemplateRef>? templateRefs = null) =>
        new(
            Id:                 id,
            Identity:           "wubrg",
            ManaNeeded:         null,
            ManaValueNeeded:    null,
            OtherPrerequisites: null,
            Description:        "Test combo",
            Notes:              null,
            Status:             "preview",
            Spoiler:            false,
            LegalitiesJson:     null,
            BracketTag:         null,
            Popularity:         null,
            Cards:              cards,
            FeatureIds:         featureIds   ?? [],
            TemplateRefs:       templateRefs ?? []);
}
