using FluentAssertions;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Tagging;
using Xunit;

namespace MysticForge.UnitTests.Tagging;

public sealed class PromptBuilderTests
{
    private static SynergyHook H(long id, string path, long? parentId, short depth, string desc) =>
        new() { Id = id, Path = path, Name = path.Split('/').Last(), ParentId = parentId, Depth = depth, Description = desc };

    [Fact]
    public void Preamble_IncludesAllRoles()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1", []);
        var builder = new PromptBuilder(cache);

        var preamble = builder.GetSystemPreamble();

        foreach (var role in Role.All)
        {
            preamble.Should().Contain(role);
        }
    }

    [Fact]
    public void Preamble_IncludesAllHookPaths()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1",
        [
            H(1, "graveyard_value", null, 1, "graveyard ROOT"),
            H(2, "graveyard_value/reanimate", 1, 2, "ETB from graveyard"),
        ]);
        var builder = new PromptBuilder(cache);

        var preamble = builder.GetSystemPreamble();

        preamble.Should().Contain("graveyard_value");
        preamble.Should().Contain("graveyard_value/reanimate");
        preamble.Should().Contain("graveyard ROOT");
        preamble.Should().Contain("ETB from graveyard");
    }

    [Fact]
    public void Preamble_IncludesTribalAndMultiFaceRules()
    {
        var cache = new TaxonomyCache();
        cache.LoadForTesting("v1", []);
        var builder = new PromptBuilder(cache);

        var preamble = builder.GetSystemPreamble();

        preamble.Should().Contain("tribal_interest");
        preamble.Should().Contain("faces");
        preamble.Should().Contain("rules text");
        preamble.Should().Contain("synergy_hook_paths");
        preamble.Should().Contain("mechanics");
    }
}
