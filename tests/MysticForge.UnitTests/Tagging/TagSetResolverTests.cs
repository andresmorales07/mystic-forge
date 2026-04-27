using FluentAssertions;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Tagging;
using NSubstitute;
using Xunit;

namespace MysticForge.UnitTests.Tagging;

public sealed class TagSetResolverTests
{
    private static SynergyHook H(long id, string path, long? parentId, short depth) =>
        new() { Id = id, Path = path, Name = path.Split('/').Last(), ParentId = parentId, Depth = depth, Description = "" };

    private static (TaxonomyCache cache, IMechanicsRegistry mechanics) Setup()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1",
        [
            H(10, "graveyard_value", null, 1),
            H(11, "graveyard_value/reanimate", 10, 2),
            H(20, "tokens", null, 1),
        ]);
        var mechanics = Substitute.For<IMechanicsRegistry>();
        mechanics.ResolveOrInsertAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .Returns(ci => Task.FromResult(99L + ci.Arg<string>().Length));
        return (cache, mechanics);
    }

    [Fact]
    public async Task ResolvesValidRolesHooksMechanicsTribal()
    {
        var (cache, mechanics) = Setup();
        var resolver = new TagSetResolver(cache, mechanics);
        var raw = new RawTagSet(
            Roles: [Role.Ramp, Role.Draw],
            SynergyHookPaths: [ "graveyard_value/reanimate" ],
            Mechanics: [ "Flashback" ],
            TribalInterest: [ "Demon" ]);

        var result = await resolver.ResolveAsync(Guid.NewGuid(), raw, "test-model", default);

        result.RoleRows.Select(r => r.Role).Should().BeEquivalentTo([Role.Ramp, Role.Draw]);
        result.HookRows.Select(h => h.HookId).Should().BeEquivalentTo([11L]);
        result.AncestorRows.Select(a => a.AncestorHookId).Should().BeEquivalentTo([10L]);
        result.MechanicRows.Should().HaveCount(1);
        result.TribalRows.Select(t => t.CreatureType).Should().BeEquivalentTo(["Demon"]);
    }

    [Fact]
    public async Task DropsUnknownRoles_AndUnknownHookPaths()
    {
        var (cache, mechanics) = Setup();
        var resolver = new TagSetResolver(cache, mechanics);
        var raw = new RawTagSet(
            Roles: [Role.Ramp, "garbage"],
            SynergyHookPaths: [ "graveyard_value/reanimate", "nonsense/path" ],
            Mechanics: [],
            TribalInterest: []);

        var result = await resolver.ResolveAsync(Guid.NewGuid(), raw, "test-model", default);

        result.RoleRows.Select(r => r.Role).Should().BeEquivalentTo([Role.Ramp]);
        result.HookRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeduplicatesAncestors_AcrossMultipleLeavesUnderSameParent()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1",
        [
            H(1, "root", null, 1),
            H(2, "root/leaf_a", 1, 2),
            H(3, "root/leaf_b", 1, 2),
        ]);
        var mechanics = Substitute.For<IMechanicsRegistry>();
        var resolver = new TagSetResolver(cache, mechanics);

        var raw = new RawTagSet([], ["root/leaf_a", "root/leaf_b"], [], []);
        var result = await resolver.ResolveAsync(Guid.NewGuid(), raw, "m", default);

        result.AncestorRows.Where(a => a.AncestorHookId == 1).Should().HaveCount(1);
    }
}
