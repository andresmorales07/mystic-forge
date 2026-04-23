using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;
using MysticForge.Infrastructure.Persistence;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Persistence;

[Collection("postgres")]
public sealed class OracleEventEmitterTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public OracleEventEmitterTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EmitsEventsAndPersistsThem()
    {
        var oracleId = Guid.NewGuid();

        // Precondition: card exists because CardOracleEvent has an FK to cards.
        await using (var seed = _db.NewContext())
        {
            await new CardWriter(seed).UpsertAsync([new Card
            {
                OracleId = oracleId,
                Name = "Seed",
                Layout = CardLayout.Normal,
                OracleText = "Text.",
                TypeLine = "Artifact",
                ColorIdentity = Array.Empty<string>(),
                OracleHash = OracleHasher.HashSingleFace("Text."),
                LastOracleChange = DateTimeOffset.UtcNow,
            }], default);
        }

        await using var ctx = _db.NewContext();
        var emitter = new OracleEventEmitter(ctx);

        var count = await emitter.EmitAsync([new CardOracleEvent
        {
            OracleId = oracleId,
            EventType = OracleEventType.Errata,
            PreviousHash = OracleHasher.HashSingleFace("Text."),
            NewHash = OracleHasher.HashSingleFace("Text modified."),
            ObservedAt = DateTimeOffset.UtcNow,
        }], default);

        count.Should().Be(1);

        await using var verify = _db.NewContext();
        var persisted = await verify.CardOracleEvents.SingleAsync(e => e.OracleId == oracleId);
        persisted.EventType.Should().Be("errata");
        persisted.ConsumedAt.Should().BeNull();
    }
}
