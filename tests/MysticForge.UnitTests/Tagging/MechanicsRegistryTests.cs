using FluentAssertions;
using MysticForge.Application.Tagging;
using Xunit;

namespace MysticForge.UnitTests.Tagging;

public sealed class MechanicsRegistryTests
{
    [Theory]
    [InlineData("Flashback",     "flashback")]
    [InlineData("Partner with",  "partner_with")]
    [InlineData("  Madness  ",   "madness")]
    [InlineData("Lieutenant's",  "lieutenants")]
    [InlineData("Cycling — Tap", "cycling_tap")]
    [InlineData("UPPERCASE",     "uppercase")]
    [InlineData("snake_already", "snake_already")]
    [InlineData("",              "")]
    public void Normalize_ProducesCanonicalName(string input, string expected)
    {
        IMechanicsRegistry.Normalize(input).Should().Be(expected);
    }
}
