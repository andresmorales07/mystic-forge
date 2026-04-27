using FluentAssertions;
using MysticForge.Infrastructure.Seeding;
using Xunit;

namespace MysticForge.UnitTests.Tagging;

public sealed class TaxonomyV1YamlParserTests
{
    [Fact]
    public void Parses_VersionAndAllHooks_FromV1Yaml()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "MysticForge.Infrastructure", "Seeding", "taxonomy-v1.yaml");
        var yaml = File.ReadAllText(path);

        var parser = new TaxonomyV1YamlParser();
        var doc = parser.Parse(yaml);

        doc.TaxonomyVersion.Should().NotBeNullOrEmpty();
        doc.Hooks.Should().NotBeEmpty();
        doc.Hooks.Should().HaveCountGreaterThan(100);                                          // 118 in v1
        doc.Hooks.Select(h => h.Path).Should().OnlyHaveUniqueItems();
        doc.Hooks.Should().Contain(h => h.Path == "graveyard_matters/reanimator_effect");      // sanity check
    }

    [Fact]
    public void Path_IsSlashJoinedFromRoot()
    {
        var yaml = """
                   version: "test-1"
                   synergy_hooks:
                     - name: alpha
                       parent: null
                       description: "root"
                       sort_order: 0
                     - name: beta
                       parent: alpha
                       description: "child"
                       sort_order: 0
                     - name: gamma
                       parent: beta
                       description: "leaf"
                       sort_order: 0
                   """;

        var doc = new TaxonomyV1YamlParser().Parse(yaml);

        doc.Hooks.Should().HaveCount(3);
        doc.Hooks.Should().Contain(h => h.Path == "alpha"       && h.Depth == 1 && h.ParentPath == null);
        doc.Hooks.Should().Contain(h => h.Path == "alpha/beta"  && h.Depth == 2 && h.ParentPath == "alpha");
        doc.Hooks.Should().Contain(h => h.Path == "alpha/beta/gamma" && h.Depth == 3 && h.ParentPath == "alpha/beta");
    }
}
