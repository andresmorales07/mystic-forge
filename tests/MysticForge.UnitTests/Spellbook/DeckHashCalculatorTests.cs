using FluentAssertions;
using MysticForge.Infrastructure.Spellbook;
using Xunit;

namespace MysticForge.UnitTests.Spellbook;

public sealed class DeckHashCalculatorTests
{
    private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid C = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Hash_is_deterministic_for_same_input()
    {
        var h1 = DeckHashCalculator.Compute(new[] { A, B }, new[] { C });
        var h2 = DeckHashCalculator.Compute(new[] { A, B }, new[] { C });
        h1.Should().Equal(h2);
    }

    [Fact]
    public void Hash_is_order_independent_within_main()
    {
        var h1 = DeckHashCalculator.Compute(new[] { A, B }, new[] { C });
        var h2 = DeckHashCalculator.Compute(new[] { B, A }, new[] { C });
        h1.Should().Equal(h2);
    }

    [Fact]
    public void Hash_distinguishes_commander_role()
    {
        var h1 = DeckHashCalculator.Compute(new[] { A, B }, new[] { C });   // C is commander
        var h2 = DeckHashCalculator.Compute(new[] { A, C }, new[] { B });   // B is commander
        h1.Should().NotEqual(h2);
    }

    [Fact]
    public void Hash_is_32_bytes()
    {
        var h = DeckHashCalculator.Compute(new[] { A }, Array.Empty<Guid>());
        h.Length.Should().Be(32);
    }

    [Fact]
    public void Empty_inputs_produce_well_defined_hash()
    {
        var h = DeckHashCalculator.Compute(Array.Empty<Guid>(), Array.Empty<Guid>());
        h.Length.Should().Be(32);
    }
}
