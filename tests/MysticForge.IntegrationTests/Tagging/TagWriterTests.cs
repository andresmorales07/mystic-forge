using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Persistence;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Tagging;

[Collection("postgres")]
public sealed class TagWriterTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;
    private Guid _oracleId;
    private long _hookId;
    private long _eventId;

    public TagWriterTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        await using (var ctx = _db.NewContext())
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE card_roles, card_synergy_hooks, card_synergy_hook_ancestors, card_mechanics, card_tribal_interest, card_oracle_events, cards, synergy_hooks RESTART IDENTITY CASCADE");
        }

        _oracleId = Guid.NewGuid();
        await using var ctx2 = _db.NewContext();
        ctx2.Cards.Add(new Card
        {
            OracleId = _oracleId, Name = "Test", Layout = CardLayout.Normal,
            OracleText = "x", TypeLine = "Artifact",
            ColorIdentity = Array.Empty<string>(),
            OracleHash = OracleHasher.HashSingleFace("x"),
            LastOracleChange = DateTimeOffset.UtcNow,
        });
        var hook = new SynergyHook
        {
            Path = "test_root", Name = "test_root", Depth = 1,
            Description = "", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };
        ctx2.SynergyHooks.Add(hook);
        await ctx2.SaveChangesAsync();
        _hookId = hook.Id;

        var evt = new CardOracleEvent
        {
            OracleId = _oracleId,
            EventType = OracleEventType.Created,
            NewHash = new byte[] { 1 },
            ObservedAt = DateTimeOffset.UtcNow,
            ClaimedAt = DateTimeOffset.UtcNow,
            ClaimedBy = "test",
            ClaimAttempts = 1,
        };
        ctx2.CardOracleEvents.Add(evt);
        await ctx2.SaveChangesAsync();
        _eventId = evt.EventId;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ResolvedTagSet MakeTagSet(string roleValue)
    {
        var now = DateTimeOffset.UtcNow;
        return new ResolvedTagSet(
            RoleRows: [ new CardRole { OracleId = _oracleId, Role = roleValue, ModelVersion = "m", TaxonomyVersion = "v1", TaggedAt = now, Source = "llm" } ],
            HookRows: [ new CardSynergyHook { OracleId = _oracleId, HookId = _hookId, ModelVersion = "m", TaxonomyVersion = "v1", TaggedAt = now, Source = "llm" } ],
            AncestorRows: [],
            MechanicRows: [],
            TribalRows: [ new CardTribalInterest { OracleId = _oracleId, CreatureType = "Demon", ModelVersion = "m", TaxonomyVersion = "v1", TaggedAt = now, Source = "llm" } ]);
    }

    [Fact]
    public async Task WritesTags_AndMarksEventConsumed()
    {
        var writer = new TagWriter(_db.NewContext);
        var claimed = new ClaimedEvent(_eventId, _oracleId, OracleEventType.Created, 1);

        await writer.WriteAsync(claimed, MakeTagSet(Role.Ramp), default);

        await using var verify = _db.NewContext();
        (await verify.CardRoles.CountAsync()).Should().Be(1);
        (await verify.CardSynergyHooks.CountAsync()).Should().Be(1);
        (await verify.CardTribalInterest.CountAsync()).Should().Be(1);
        var evt = await verify.CardOracleEvents.SingleAsync();
        evt.ConsumedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RewriteIsClean_DeletesOldRowsBeforeInsertingNew()
    {
        var writer = new TagWriter(_db.NewContext);
        var claimed = new ClaimedEvent(_eventId, _oracleId, OracleEventType.Created, 1);

        await writer.WriteAsync(claimed, MakeTagSet(Role.Ramp), default);
        // Reset consumed_at so we can re-write.
        await using (var ctx = _db.NewContext())
        {
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE card_oracle_events SET consumed_at = NULL WHERE event_id = {_eventId}");
        }
        await writer.WriteAsync(claimed, MakeTagSet(Role.Draw), default);

        await using var verify = _db.NewContext();
        var roles = await verify.CardRoles.Select(r => r.Role).ToListAsync();
        roles.Should().BeEquivalentTo([Role.Draw]);
    }
}
