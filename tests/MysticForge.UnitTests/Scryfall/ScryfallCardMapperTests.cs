using FluentAssertions;
using MysticForge.Domain.Cards;
using MysticForge.Infrastructure.Scryfall;
using Xunit;

namespace MysticForge.UnitTests.Scryfall;

public sealed class ScryfallCardMapperTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MapsSingleFaceCard()
    {
        var json = LoadFixture("single-face-card.json");

        var (card, printing) = ScryfallCardMapper.Map(json, FixedNow);

        card.OracleId.Should().Be(Guid.Parse("00000000-0000-0000-0000-0000000000A1"));
        card.Name.Should().Be("Sol Ring");
        card.Layout.Should().Be(CardLayout.Normal);
        card.OracleText.Should().Be("{T}: Add {C}{C}.");
        card.TypeLine.Should().Be("Artifact");
        card.ManaCost.Should().Be("{1}");
        card.Faces.Should().BeNull();
        card.Cmc.Should().Be(1.0m);
        card.Colors.Should().BeEmpty();
        card.ColorIdentity.Should().BeEmpty();
        card.OracleHash.Should().Equal(OracleHasher.HashSingleFace("{T}: Add {C}{C}."));
        card.LastOracleChange.Should().Be(FixedNow);

        printing.ScryfallId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        printing.OracleId.Should().Be(card.OracleId);
        printing.SetCode.Should().Be("C21");
        printing.CollectorNumber.Should().Be("263");
        printing.Rarity.Should().Be("uncommon");
        printing.PriceUsd.Should().Be(1.99m);
        printing.PriceUsdFoil.Should().Be(3.50m);
        printing.ImageUriNormal.Should().Be("https://img.example/sol-ring-normal.jpg");
        printing.ReleasedAt.Should().Be(new DateOnly(2021, 4, 23));
    }

    [Fact]
    public void MapsDfcCard()
    {
        var json = LoadFixture("dfc-card.json");

        var (card, _) = ScryfallCardMapper.Map(json, FixedNow);

        card.Layout.Should().Be(CardLayout.ModalDfc);
        card.Name.Should().Be("Valki, God of Lies // Tibalt, Cosmic Impostor");
        card.OracleText.Should().BeNull();
        card.TypeLine.Should().BeNull();
        card.ManaCost.Should().BeNull();
        card.Faces.Should().NotBeNull().And.HaveCount(2);
        card.Faces![0].Name.Should().Be("Valki, God of Lies");
        card.Faces[1].Name.Should().Be("Tibalt, Cosmic Impostor");
        card.ColorIdentity.Should().BeEquivalentTo(["B", "R"]);

        card.EnsureFaceInvariant();
    }

    [Fact]
    public void MapsSplitCard()
    {
        var json = LoadFixture("split-card.json");

        var (card, _) = ScryfallCardMapper.Map(json, FixedNow);

        card.Layout.Should().Be(CardLayout.Split);
        card.Faces.Should().NotBeNull().And.HaveCount(2);
        card.Faces![0].OracleText.Should().Contain("Fire deals 2 damage");
        card.Faces[1].OracleText.Should().Contain("Tap target permanent");
    }

    [Fact]
    public void MapsAdventureCard()
    {
        var json = LoadFixture("adventure-card.json");

        var (card, _) = ScryfallCardMapper.Map(json, FixedNow);

        card.Layout.Should().Be(CardLayout.Adventure);
        card.Faces.Should().NotBeNull().And.HaveCount(2);
        card.Keywords.Should().Contain("Flash");
    }

    [Fact]
    public void HashMatchesHasher_SingleFace()
    {
        var json = LoadFixture("single-face-card.json");
        var (card, _) = ScryfallCardMapper.Map(json, FixedNow);

        card.OracleHash.Should().Equal(OracleHasher.HashSingleFace("{T}: Add {C}{C}."));
    }

    [Fact]
    public void HashMatchesHasher_MultiFace()
    {
        var json = LoadFixture("dfc-card.json");
        var (card, _) = ScryfallCardMapper.Map(json, FixedNow);

        var expected = OracleHasher.HashMultiFace(card.Faces!);
        card.OracleHash.Should().Equal(expected);
    }

    private static string LoadFixture(string filename)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", filename);
        return File.ReadAllText(path);
    }
}
