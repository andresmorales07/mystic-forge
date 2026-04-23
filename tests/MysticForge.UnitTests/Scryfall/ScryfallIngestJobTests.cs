using FluentAssertions;
using MysticForge.Application.Scryfall;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MysticForge.UnitTests.Scryfall;

public sealed class ScryfallIngestJobTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Skips_WhenScryfallUpdatedAtUnchanged()
    {
        var metadata = new ScryfallBulkMetadata("default_cards", new Uri("https://example/bulk.json"), FixedNow);

        var client = Substitute.For<IScryfallBulkClient>();
        client.GetBulkMetadataAsync("default_cards", Arg.Any<CancellationToken>()).Returns(metadata);

        var tracker = Substitute.For<IIngestRunTracker>();
        tracker.GetLastSuccessfulUpdatedAtAsync("default_cards", Arg.Any<CancellationToken>()).Returns(FixedNow);

        var parser = Substitute.For<IScryfallCardStreamParser>();
        var cards = Substitute.For<ICardWriter>();
        var printings = Substitute.For<IPrintingWriter>();
        var emitter = Substitute.For<IOracleEventEmitter>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(FixedNow);

        var job = new ScryfallIngestJob(client, parser, cards, printings, emitter, tracker, clock);

        await job.RunAsync("default_cards", default);

        await tracker.Received(1).RecordSkipAsync("default_cards", FixedNow, Arg.Any<CancellationToken>());
        await client.DidNotReceive().DownloadBulkAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        await cards.DidNotReceive().UpsertAsync(Arg.Any<IReadOnlyList<Card>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HappyPath_InsertsAndEmitsCreatedEvents()
    {
        var metadata = new ScryfallBulkMetadata(
            "default_cards",
            new Uri("https://example/bulk.json"),
            FixedNow);

        var client = Substitute.For<IScryfallBulkClient>();
        client.GetBulkMetadataAsync("default_cards", Arg.Any<CancellationToken>()).Returns(metadata);
        client.DownloadBulkAsync(metadata.DownloadUri, Arg.Any<CancellationToken>())
              .Returns(new MemoryStream([1, 2, 3]));

        var parser = Substitute.For<IScryfallCardStreamParser>();
        parser.ReadCardJsonAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
              .Returns(ToAsync([
                  File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "single-face-card.json")),
                  File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dfc-card.json")),
              ]));

        var tracker = Substitute.For<IIngestRunTracker>();
        tracker.GetLastSuccessfulUpdatedAtAsync("default_cards", Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null);
        tracker.StartAsync("default_cards", FixedNow, Arg.Any<CancellationToken>()).Returns(42L);

        var cards = Substitute.For<ICardWriter>();
        cards.UpsertAsync(Arg.Any<IReadOnlyList<Card>>(), Arg.Any<CancellationToken>())
             .Returns(call =>
             {
                 var list = call.Arg<IReadOnlyList<Card>>();
                 var changes = list.Select(c => new OracleChange(c.OracleId, null, c.OracleHash, IsNew: true)).ToList();
                 return new CardUpsertResult(Inserted: list.Count, Updated: 0, Changes: changes);
             });

        var printings = Substitute.For<IPrintingWriter>();
        printings.UpsertAsync(Arg.Any<IReadOnlyList<Printing>>(), Arg.Any<CancellationToken>())
                 .Returns(new PrintingUpsertResult(Inserted: 2, Updated: 0));

        var emitter = Substitute.For<IOracleEventEmitter>();
        emitter.EmitAsync(Arg.Any<IReadOnlyList<CardOracleEvent>>(), Arg.Any<CancellationToken>())
               .Returns(call => call.Arg<IReadOnlyList<CardOracleEvent>>().Count);

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(FixedNow);

        var job = new ScryfallIngestJob(client, parser, cards, printings, emitter, tracker, clock);

        await job.RunAsync("default_cards", default);

        await tracker.Received(1).StartAsync("default_cards", FixedNow, Arg.Any<CancellationToken>());
        await cards.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<Card>>(x => x.Count == 2), Arg.Any<CancellationToken>());
        await printings.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<Printing>>(x => x.Count == 2), Arg.Any<CancellationToken>());
        await emitter.Received(1).EmitAsync(
            Arg.Is<IReadOnlyList<CardOracleEvent>>(x => x.Count == 2 && x.All(e => e.EventType == OracleEventType.Created)),
            Arg.Any<CancellationToken>());
        await tracker.Received(1).CompleteAsync(42L, "success", Arg.Is<IngestCounts>(c =>
            c.CardsInserted == 2 && c.CardsUpdated == 0 && c.ErrataEmitted == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailureDuringDownload_MarksRunFailed()
    {
        var metadata = new ScryfallBulkMetadata("default_cards", new Uri("https://example/bulk.json"), FixedNow);

        var client = Substitute.For<IScryfallBulkClient>();
        client.GetBulkMetadataAsync("default_cards", Arg.Any<CancellationToken>()).Returns(metadata);
        client.DownloadBulkAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new HttpRequestException("network"));

        var tracker = Substitute.For<IIngestRunTracker>();
        tracker.GetLastSuccessfulUpdatedAtAsync("default_cards", Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null);
        tracker.StartAsync("default_cards", FixedNow, Arg.Any<CancellationToken>()).Returns(99L);

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(FixedNow);

        var job = new ScryfallIngestJob(
            client,
            Substitute.For<IScryfallCardStreamParser>(),
            Substitute.For<ICardWriter>(),
            Substitute.For<IPrintingWriter>(),
            Substitute.For<IOracleEventEmitter>(),
            tracker,
            clock);

        await FluentActions.Invoking(() => job.RunAsync("default_cards", default))
            .Should().ThrowAsync<HttpRequestException>();

        await tracker.Received(1).FailAsync(99L, "network", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DedupesCardsByOracleId_WithinBatch()
    {
        var metadata = new ScryfallBulkMetadata("default_cards", new Uri("https://example/bulk.json"), FixedNow);

        var client = Substitute.For<IScryfallBulkClient>();
        client.GetBulkMetadataAsync("default_cards", Arg.Any<CancellationToken>()).Returns(metadata);
        client.DownloadBulkAsync(metadata.DownloadUri, Arg.Any<CancellationToken>())
              .Returns(new MemoryStream([1, 2, 3]));

        // Two printings of Sol Ring (same oracle_id) + one printing of a DFC.
        // Result should be 2 unique cards; 3 printings.
        var solRingJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "single-face-card.json"));
        var solRingJson2 = solRingJson.Replace("\"00000000-0000-0000-0000-000000000001\"", "\"00000000-0000-0000-0000-000000000011\"");
        var dfcJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dfc-card.json"));

        var parser = Substitute.For<IScryfallCardStreamParser>();
        parser.ReadCardJsonAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
              .Returns(ToAsync([solRingJson, solRingJson2, dfcJson]));

        var tracker = Substitute.For<IIngestRunTracker>();
        tracker.GetLastSuccessfulUpdatedAtAsync("default_cards", Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null);
        tracker.StartAsync("default_cards", FixedNow, Arg.Any<CancellationToken>()).Returns(77L);

        var cards = Substitute.For<ICardWriter>();
        cards.UpsertAsync(Arg.Any<IReadOnlyList<Card>>(), Arg.Any<CancellationToken>())
             .Returns(call =>
             {
                 var list = call.Arg<IReadOnlyList<Card>>();
                 var changes = list.Select(c => new OracleChange(c.OracleId, null, c.OracleHash, IsNew: true)).ToList();
                 return new CardUpsertResult(Inserted: list.Count, Updated: 0, Changes: changes);
             });

        var printings = Substitute.For<IPrintingWriter>();
        printings.UpsertAsync(Arg.Any<IReadOnlyList<Printing>>(), Arg.Any<CancellationToken>())
                 .Returns(call => new PrintingUpsertResult(Inserted: call.Arg<IReadOnlyList<Printing>>().Count, Updated: 0));

        var emitter = Substitute.For<IOracleEventEmitter>();
        emitter.EmitAsync(Arg.Any<IReadOnlyList<CardOracleEvent>>(), Arg.Any<CancellationToken>())
               .Returns(call => call.Arg<IReadOnlyList<CardOracleEvent>>().Count);

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(FixedNow);

        var job = new ScryfallIngestJob(client, parser, cards, printings, emitter, tracker, clock);

        await job.RunAsync("default_cards", default);

        // 2 unique cards (Sol Ring dedup'd from 2 -> 1 + DFC = 2 total)
        await cards.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<Card>>(x => x.Count == 2 && x.Select(c => c.OracleId).Distinct().Count() == 2),
            Arg.Any<CancellationToken>());

        // 3 printings (all unique by ScryfallId)
        await printings.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<Printing>>(x => x.Count == 3 && x.Select(p => p.ScryfallId).Distinct().Count() == 3),
            Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<string> ToAsync(IEnumerable<string> items)
    {
        foreach (var item in items) { yield return item; await Task.Yield(); }
    }
}
