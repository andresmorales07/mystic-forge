using FluentAssertions;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Tagging;
using Xunit;

namespace MysticForge.UnitTests.Tagging;

public sealed class TaxonomyCacheTests
{
    private static SynergyHook H(long id, string path, long? parentId, short depth) =>
        new() { Id = id, Path = path, Name = path.Split('/').Last(), ParentId = parentId, Depth = depth, Description = "" };

    [Fact]
    public void ResolvesPath_WhenPresent()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting(
            taxonomyVersion: "v1",
            hooks: [
                H(1, "graveyard_value", null, 1),
                H(2, "graveyard_value/reanimate", 1, 2),
            ]);

        cache.TryResolveHook("graveyard_value/reanimate", out var id).Should().BeTrue();
        id.Should().Be(2);
    }

    [Fact]
    public void Returns_AllAncestors_ExcludingSelf()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting(
            taxonomyVersion: "v1",
            hooks: [
                H(1, "a", null, 1),
                H(2, "a/b", 1, 2),
                H(3, "a/b/c", 2, 3),
            ]);

        cache.AncestorsOf(3).Should().BeEquivalentTo([1L, 2L]);
        cache.AncestorsOf(2).Should().BeEquivalentTo([1L]);
        cache.AncestorsOf(1).Should().BeEmpty();
    }

    [Fact]
    public void RoleEnum_IsClosed()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1", []);

        cache.IsValidRole(Role.Ramp).Should().BeTrue();
        cache.IsValidRole("ramp").Should().BeTrue();
        cache.IsValidRole("nonsense").Should().BeFalse();
    }

    [Fact]
    public void UnknownPath_ReturnsFalse()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1", [H(1, "alpha", null, 1)]);

        cache.TryResolveHook("missing", out var id).Should().BeFalse();
        id.Should().Be(0);
    }
}
