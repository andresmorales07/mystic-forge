using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;
using MysticForge.Infrastructure.Persistence;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Tagging;

[Collection("postgres")]
public sealed class OutboxClaimerTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public OutboxClaimerTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        // Postgres container is shared; reset state to avoid cross-test contamination.
        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE card_oracle_events, cards RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedCardAsync()
    {
        var oracleId = Guid.NewGuid();
        await using var ctx = _db.NewContext();
        ctx.Cards.Add(new Card
        {
            OracleId = oracleId,
            Name = $"Card_{oracleId:N}",
            Layout = CardLayout.Normal,
            OracleText = "T",
            TypeLine = "Artifact",
            ColorIdentity = Array.Empty<string>(),
            OracleHash = OracleHasher.HashSingleFace("T"),
            LastOracleChange = DateTimeOffset.UtcNow,
        });
        await ctx.SaveChangesAsync();
        return oracleId;
    }

    private async Task<long> SeedEventAsync(Guid oracleId, string eventType = OracleEventType.Created)
    {
        await using var ctx = _db.NewContext();
        var evt = new CardOracleEvent
        {
            OracleId = oracleId,
            EventType = eventType,
            NewHash = new byte[] { 1, 2, 3 },
            ObservedAt = DateTimeOffset.UtcNow,
        };
        ctx.CardOracleEvents.Add(evt);
        await ctx.SaveChangesAsync();
        return evt.EventId;
    }

    [Fact]
    public async Task ClaimsUnconsumedEvents_AndIncrementsAttempts()
    {
        var card = await SeedCardAsync();
        await SeedEventAsync(card);

        var claimer = new OutboxClaimer(_db.ContextFactory);

        var claimed = await claimer.ClaimBatchAsync("test-1", batchSize: 10, default);

        claimed.Should().HaveCount(1);
        claimed[0].ClaimAttempts.Should().Be(1);

        await using var verify = _db.NewContext();
        var row = await verify.CardOracleEvents.SingleAsync();
        row.ClaimedBy.Should().Be("test-1");
        row.ClaimAttempts.Should().Be(1);
        row.ClaimedAt.Should().NotBeNull();
        row.ConsumedAt.Should().BeNull();
    }

    [Fact]
    public async Task DoesNotClaimRecentlyClaimedEvents()
    {
        var card = await SeedCardAsync();
        await SeedEventAsync(card);

        var claimer = new OutboxClaimer(_db.ContextFactory);
        await claimer.ClaimBatchAsync("test-1", 10, default);
        var second = await claimer.ClaimBatchAsync("test-2", 10, default);

        second.Should().BeEmpty();
    }

    [Fact]
    public async Task ReclaimsAfterExpiry()
    {
        var card = await SeedCardAsync();
        var eventId = await SeedEventAsync(card);

        await using (var ctx = _db.NewContext())
        {
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE card_oracle_events SET claimed_at = now() - interval '11 minutes', claimed_by = 'crashed', claim_attempts = 1 WHERE event_id = {eventId}");
        }

        var claimer = new OutboxClaimer(_db.ContextFactory);
        var claimed = await claimer.ClaimBatchAsync("test-recovered", 10, default);

        claimed.Should().HaveCount(1);
        claimed[0].ClaimAttempts.Should().Be(2);
    }

    [Fact]
    public async Task ExcludesEvents_BeyondClaimAttemptsCeiling()
    {
        var card = await SeedCardAsync();
        var eventId = await SeedEventAsync(card);

        await using (var ctx = _db.NewContext())
        {
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE card_oracle_events SET claimed_at = now() - interval '11 minutes', claim_attempts = 6 WHERE event_id = {eventId}");
        }

        var claimer = new OutboxClaimer(_db.ContextFactory);
        var claimed = await claimer.ClaimBatchAsync("test-x", 10, default);
        claimed.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentClaimers_SeeDisjointBatches()
    {
        var card = await SeedCardAsync();
        for (var i = 0; i < 5; i++) await SeedEventAsync(card);

        var claimer = new OutboxClaimer(_db.ContextFactory);
        var both = await Task.WhenAll(
            claimer.ClaimBatchAsync("a", 3, default),
            claimer.ClaimBatchAsync("b", 3, default));

        var ids = both.SelectMany(b => b.Select(e => e.EventId)).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().HaveCount(5);
    }
}
