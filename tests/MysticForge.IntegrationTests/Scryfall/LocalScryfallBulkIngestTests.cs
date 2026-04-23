using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Scryfall;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Scryfall;
using MysticForge.IntegrationTests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace MysticForge.IntegrationTests.Scryfall;

/// <summary>
/// Offline diagnostic tests that ingest from a cached Scryfall bulk file rather than
/// hitting the Scryfall API every time. Run once manually:
///   curl -sS -L -H "User-Agent: MysticForge-Dev/0.1 (+you@example.com)" \
///        https://data.scryfall.io/default-cards/default-cards-YYYYMMDDHHMMSS.json \
///        -o tests/MysticForge.IntegrationTests/.cache/default-cards.json
/// Then the tests below work offline against the cached file.
/// </summary>
[Collection("postgres")]
public sealed class LocalScryfallBulkIngestTests : IAsyncLifetime
{
    private const int DefaultRowLimit = 5_000;

    private static readonly string CacheDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", ".cache");

    private readonly PostgresContainerFixture _pg;
    private readonly ITestOutputHelper _output;
    private DatabaseFixture _db = null!;

    public LocalScryfallBulkIngestTests(PostgresContainerFixture pg, ITestOutputHelper output)
    {
        _pg = pg;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        // Fresh slate for each test in this class — shared container, but isolated per-test data.
        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE card_oracle_events, printings, cards, scryfall_ingest_runs RESTART IDENTITY CASCADE;");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task IngestsFirstNRowsOfCachedBulkFile_WithoutExceptions()
        => await IngestAsync(DefaultRowLimit);

    [Fact]
    public async Task IngestsEntireCachedBulkFile_WithoutExceptions()
        => await IngestAsync(rowLimit: int.MaxValue);

    private async Task IngestAsync(int rowLimit)
    {
        var bulkFile = Path.GetFullPath(Path.Combine(CacheDir, "default-cards.json"));
        if (!File.Exists(bulkFile))
        {
            _output.WriteLine(
                $"Skipping — cached bulk file not present at {bulkFile}. " +
                "See class-level doc comment for how to seed it.");
            return;
        }

        var subsetJson = await BuildSubsetAsync(bulkFile, rowLimit);

        using var mock = new WireMockScryfall();
        mock.GivenBulkMetadata(new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero));
        mock.GivenBulkFileRaw("/bulk.json", subsetJson);

        var job = BuildJob(mock);

        try
        {
            await job.RunAsync("default_cards", default);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"INGEST FAILED: {ex}");
            throw;
        }

        await using var ctx = _db.NewContext();
        var run = await ctx.ScryfallIngestRuns.OrderByDescending(r => r.RunId).FirstAsync();
        _output.WriteLine(
            $"Run outcome={run.Outcome}, cardsInserted={run.CardsInserted}, " +
            $"printingsInserted={run.PrintingsInserted}, errata={run.ErrataEmitted}, " +
            $"error={run.ErrorMessage}");

        run.Outcome.Should().Be("success");
        run.ErrorMessage.Should().BeNull();

        var printingCount = await ctx.Printings.CountAsync();
        var cardCount = await ctx.Cards.CountAsync();
        _output.WriteLine($"Persisted {cardCount} cards, {printingCount} printings.");

        printingCount.Should().BeGreaterThan(0);
        cardCount.Should().BeGreaterThan(0);
        cardCount.Should().BeLessThanOrEqualTo(printingCount, "cards is oracle-level; at most one per printing");

        // Sanity-check the oracle-level / printing-level magnitudes when running the full file.
        if (run.CardsInserted > 20_000)
        {
            run.CardsInserted.Should().BeInRange(20_000, 50_000, "Scryfall's canonical oracle card count is ~30k as of 2026");
            run.PrintingsInserted.Should().BeGreaterThan(run.CardsInserted.GetValueOrDefault());
        }
    }

    private static async Task<string> BuildSubsetAsync(string bulkFile, int rowLimit)
    {
        // The bulk file is a single top-level JSON array. We stream until we've captured `rowLimit`
        // elements, then wrap them into a new JSON array string. This keeps the test fast and
        // avoids loading 500MB into memory at once.
        await using var fs = File.OpenRead(bulkFile);
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(fs);

        var rows = doc.RootElement.EnumerateArray()
            .Take(rowLimit)
            .Select(el => el.GetRawText());

        return "[" + string.Join(",", rows) + "]";
    }

    private ScryfallIngestJob BuildJob(WireMockScryfall mock)
    {
        var httpClient = new HttpClient { BaseAddress = mock.BaseAddress };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MysticForge-Tests/1.0");

        var client = new ScryfallBulkClient(httpClient);
        var parser = new ScryfallCardStreamParser();

        var writerCtx = _db.NewContext();
        var cards = new CardWriter(writerCtx);
        var printings = new PrintingWriter(writerCtx);
        var emitter = new OracleEventEmitter(writerCtx);

        var clock = new Clock();
        var tracker = new IngestRunTracker(_db.ContextFactory, clock);

        return new ScryfallIngestJob(client, parser, cards, printings, emitter, tracker, clock);
    }
}
