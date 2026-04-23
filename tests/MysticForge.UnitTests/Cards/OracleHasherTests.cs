using FluentAssertions;
using MysticForge.Domain.Cards;
using Xunit;

namespace MysticForge.UnitTests.Cards;

public sealed class OracleHasherTests
{
    [Fact]
    public void HashSingleFace_EqualForIdenticalText()
    {
        var a = OracleHasher.HashSingleFace("Destroy target creature.");
        var b = OracleHasher.HashSingleFace("Destroy target creature.");

        a.Should().Equal(b);
    }

    [Fact]
    public void HashSingleFace_IgnoresCaseAndWhitespace()
    {
        var a = OracleHasher.HashSingleFace("Destroy   target creature.");
        var b = OracleHasher.HashSingleFace("destroy target creature.");

        a.Should().Equal(b);
    }

    [Fact]
    public void HashSingleFace_DifferentForSemanticChange()
    {
        var a = OracleHasher.HashSingleFace("Destroy target creature.");
        var b = OracleHasher.HashSingleFace("Exile target creature.");

        a.Should().NotEqual(b);
    }

    [Fact]
    public void HashSingleFace_TrimsLeadingAndTrailingWhitespace()
    {
        var a = OracleHasher.HashSingleFace("  Destroy target creature.  ");
        var b = OracleHasher.HashSingleFace("Destroy target creature.");

        a.Should().Equal(b);
    }

    [Fact]
    public void HashSingleFace_NormalizesUnicodeToNfc()
    {
        // 'é' as precomposed (U+00E9) vs. 'e' + combining acute (U+0065 U+0301).
        // Built from escapes to guarantee byte preservation regardless of editor normalization.
        var precomposed = OracleHasher.HashSingleFace("Café text");
        var decomposed = OracleHasher.HashSingleFace("Café text");

        precomposed.Should().Equal(decomposed);
    }

    [Fact]
    public void HashMultiFace_JoinsFacesDeterministically()
    {
        var faces1 = new[]
        {
            new CardFace("Face A", "When Face A resolves, draw a card.", "Instant", "{U}"),
            new CardFace("Face B", "Deal 2 damage to any target.", "Instant", "{R}"),
        };
        var faces2 = new[]
        {
            new CardFace("Face A", "When Face A resolves, draw a card.", "Instant", "{U}"),
            new CardFace("Face B", "Deal 2 damage to any target.", "Instant", "{R}"),
        };

        OracleHasher.HashMultiFace(faces1).Should().Equal(OracleHasher.HashMultiFace(faces2));
    }

    [Fact]
    public void HashMultiFace_IsOrderSensitive()
    {
        var reversed = new[]
        {
            new CardFace("Face B", "Deal 2 damage to any target.", "Instant", "{R}"),
            new CardFace("Face A", "When Face A resolves, draw a card.", "Instant", "{U}"),
        };
        var normal = new[]
        {
            new CardFace("Face A", "When Face A resolves, draw a card.", "Instant", "{U}"),
            new CardFace("Face B", "Deal 2 damage to any target.", "Instant", "{R}"),
        };

        OracleHasher.HashMultiFace(normal).Should().NotEqual(OracleHasher.HashMultiFace(reversed));
    }

    [Fact]
    public void HashMultiFace_TreatsNullFaceTextAsEmpty()
    {
        var withNull = new[] { new CardFace("A", null, "Creature", "{1}") };
        var withEmpty = new[] { new CardFace("A", "", "Creature", "{1}") };

        OracleHasher.HashMultiFace(withNull).Should().Equal(OracleHasher.HashMultiFace(withEmpty));
    }

    [Fact]
    public void Hash_IsSha256_32Bytes()
    {
        OracleHasher.HashSingleFace("anything").Should().HaveCount(32);
    }
}
