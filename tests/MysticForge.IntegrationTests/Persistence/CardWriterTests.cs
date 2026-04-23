using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Domain.Cards;
using MysticForge.Infrastructure.Persistence;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Persistence;

[Collection("postgres")]
public sealed class CardWriterTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public CardWriterTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InsertsNewCardAndReportsItAsNew()
    {
        await using var ctx = _db.NewContext();
        var writer = new CardWriter(ctx);
        var card = MakeCard("{T}: Add {C}{C}.");

        var result = await writer.UpsertAsync([card], default);

        result.Inserted.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Changes.Should().ContainSingle(c => c.OracleId == card.OracleId && c.IsNew && c.PreviousHash == null);
    }

    [Fact]
    public async Task RepeatingTheSameCard_DoesNothing()
    {
        var oracleId = Guid.NewGuid();

        await using (var ctx1 = _db.NewContext())
        {
            await new CardWriter(ctx1).UpsertAsync([MakeCard("{T}: Add {C}{C}.", oracleId)], default);
        }

        await using var ctx2 = _db.NewContext();
        var result = await new CardWriter(ctx2).UpsertAsync([MakeCard("{T}: Add {C}{C}.", oracleId)], default);

        result.Inserted.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatedOracleText_RegistersAsChange()
    {
        var oracleId = Guid.NewGuid();
        await using (var ctx1 = _db.NewContext())
        {
            await new CardWriter(ctx1).UpsertAsync([MakeCard("{T}: Add {C}{C}.", oracleId)], default);
        }

        await using var ctx2 = _db.NewContext();
        var result = await new CardWriter(ctx2).UpsertAsync([MakeCard("{T}: Add {C}.", oracleId)], default);

        result.Inserted.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Changes.Should().ContainSingle(c => c.OracleId == oracleId && !c.IsNew && c.PreviousHash != null);
    }

    [Fact]
    public async Task CheckConstraint_BothOracleTextAndFacesPopulated_ThrowsOnInsert()
    {
        await using var ctx = _db.NewContext();
        var bad = new Card
        {
            OracleId = Guid.NewGuid(),
            Name = "Invalid Card",
            Layout = CardLayout.Normal,
            OracleText = "Do a thing.",
            TypeLine = "Artifact",
            Faces = new[] { new CardFace("Face", "Text", "Instant", "{U}") },
            ColorIdentity = Array.Empty<string>(),
            OracleHash = OracleHasher.HashSingleFace("Do a thing."),
            LastOracleChange = DateTimeOffset.UtcNow,
        };
        ctx.Cards.Add(bad);

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private static Card MakeCard(string oracleText, Guid? oracleId = null)
    {
        var id = oracleId ?? Guid.NewGuid();
        return new Card
        {
            OracleId = id,
            Name = $"Test Card {id:N}",
            Layout = CardLayout.Normal,
            OracleText = oracleText,
            TypeLine = "Artifact",
            ManaCost = "{1}",
            Cmc = 1m,
            ColorIdentity = Array.Empty<string>(),
            OracleHash = OracleHasher.HashSingleFace(oracleText),
            LastOracleChange = DateTimeOffset.UtcNow,
        };
    }
}
