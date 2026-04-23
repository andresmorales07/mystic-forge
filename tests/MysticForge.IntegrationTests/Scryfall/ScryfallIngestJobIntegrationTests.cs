using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Scryfall;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Scryfall;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Scryfall;

[Collection("postgres")]
public sealed class ScryfallIngestJobIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public ScryfallIngestJobIntegrationTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        // Per-test isolation: the shared container retains data across tests in the
        // [Collection("postgres")] collection. Truncate the ingest-pipeline tables so
        // each test sees a clean slate. FK cascade handles child rows.
        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE card_oracle_events, printings, cards, scryfall_ingest_runs RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HappyPath_IngestsCardsAndEmitsCreatedEvents()
    {
        using var mock = new WireMockScryfall();
        mock.GivenBulkMetadata(new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero));
        mock.GivenBulkFile("/bulk.json",
        [
            await LoadFixtureAsync("single-face-card.json"),
            await LoadFixtureAsync("dfc-card.json"),
        ]);

        var job = BuildJob(mock);
        await job.RunAsync("default_cards", default);

        await using var ctx = _db.NewContext();
        (await ctx.Cards.CountAsync()).Should().Be(2);
        (await ctx.Printings.CountAsync()).Should().Be(2);
        (await ctx.CardOracleEvents.CountAsync(e => e.EventType == "created")).Should().Be(2);
        (await ctx.ScryfallIngestRuns.CountAsync(r => r.Outcome == "success")).Should().Be(1);
    }

    [Fact]
    public async Task IdempotentRerun_WhenUpdatedAtUnchanged_SkipsDownload()
    {
        using var mock = new WireMockScryfall();
        var updatedAt = new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero);
        mock.GivenBulkMetadata(updatedAt);
        mock.GivenBulkFile("/bulk.json", [await LoadFixtureAsync("single-face-card.json")]);

        var job = BuildJob(mock);
        await job.RunAsync("default_cards", default);
        await job.RunAsync("default_cards", default);

        mock.GetDownloadCallCount().Should().Be(1, "second run must skip because updated_at is unchanged");

        await using var ctx = _db.NewContext();
        (await ctx.ScryfallIngestRuns.CountAsync(r => r.Outcome == "skipped")).Should().Be(1);
        (await ctx.CardOracleEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ErrataFlow_EmitsErrataEventAndUpdatesHash()
    {
        using var mock = new WireMockScryfall();

        // First ingest
        var firstUpdatedAt = new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero);
        mock.GivenBulkMetadata(firstUpdatedAt);
        mock.GivenBulkFile("/bulk.json", [await LoadFixtureAsync("single-face-card.json")]);

        var job = BuildJob(mock);
        await job.RunAsync("default_cards", default);

        // Second ingest, with errata
        using var mock2 = new WireMockScryfall();
        var secondUpdatedAt = firstUpdatedAt.AddDays(1);
        mock2.GivenBulkMetadata(secondUpdatedAt);
        mock2.GivenBulkFile("/bulk.json", [await LoadFixtureAsync("single-face-card-errata.json")]);

        var job2 = BuildJob(mock2);
        await job2.RunAsync("default_cards", default);

        await using var ctx = _db.NewContext();
        (await ctx.CardOracleEvents.CountAsync(e => e.EventType == "errata")).Should().Be(1);
        (await ctx.CardOracleEvents.CountAsync(e => e.EventType == "created")).Should().Be(1);

        var card = await ctx.Cards.SingleAsync(c => c.Name == "Sol Ring");
        card.OracleText.Should().Contain("Add {C}. Add {C}.");
    }

    [Fact]
    public async Task MultiplePrintingsOfSameOracleId_YieldOneCardAndManyPrintings()
    {
        using var mock = new WireMockScryfall();
        mock.GivenBulkMetadata(new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero));

        var solRing = await LoadFixtureAsync("single-face-card.json");
        var solRing2 = solRing.Replace("\"00000000-0000-0000-0000-000000000001\"", "\"00000000-0000-0000-0000-000000000011\"")
                              .Replace("\"263\"", "\"555\"");
        var solRing3 = solRing.Replace("\"00000000-0000-0000-0000-000000000001\"", "\"00000000-0000-0000-0000-000000000021\"")
                              .Replace("\"263\"", "\"777\"");

        mock.GivenBulkFile("/bulk.json", [solRing, solRing2, solRing3]);

        var job = BuildJob(mock);
        await job.RunAsync("default_cards", default);

        await using var ctx = _db.NewContext();
        (await ctx.Cards.CountAsync()).Should().Be(1);
        (await ctx.Printings.CountAsync()).Should().Be(3);
        (await ctx.CardOracleEvents.CountAsync(e => e.EventType == "created")).Should().Be(1);
        (await ctx.ScryfallIngestRuns.CountAsync(r => r.Outcome == "success")).Should().Be(1);
    }

    [Fact]
    public async Task DownloadFailure_RecordsFailedRun()
    {
        using var mock = new WireMockScryfall();
        mock.GivenBulkMetadata(new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero));
        // Note: no GivenBulkFile — bulk.json will 404.

        var job = BuildJob(mock);

        await FluentActions.Invoking(() => job.RunAsync("default_cards", default))
            .Should().ThrowAsync<HttpRequestException>();

        await using var ctx = _db.NewContext();
        var lastRun = await ctx.ScryfallIngestRuns.OrderByDescending(r => r.RunId).FirstAsync();
        lastRun.Outcome.Should().Be("failed");
        lastRun.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    private ScryfallIngestJob BuildJob(WireMockScryfall mock)
    {
        var httpClient = new HttpClient { BaseAddress = mock.BaseAddress };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MysticForge-Tests/1.0");

        var client = new ScryfallBulkClient(httpClient);
        var parser = new ScryfallCardStreamParser();

        var ctxForWriters = _db.NewContext();
        var cards = new CardWriter(ctxForWriters);
        var printings = new PrintingWriter(ctxForWriters);
        var emitter = new OracleEventEmitter(ctxForWriters);

        var clock = new Clock();
        var tracker = new IngestRunTracker(_db.ContextFactory, clock);

        return new ScryfallIngestJob(client, parser, cards, printings, emitter, tracker, clock);
    }

    private static Task<string> LoadFixtureAsync(string filename)
        => File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", filename));
}
