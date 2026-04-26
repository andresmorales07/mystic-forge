namespace MysticForge.Application.Tagging;

public interface ITaxonomyV1YamlParser
{
    TaxonomyDocument Parse(string yaml);
}

public sealed record TaxonomyDocument(string TaxonomyVersion, IReadOnlyList<HookNode> Hooks);

public sealed record HookNode(
    string Path,
    string Name,
    string? ParentPath,
    short Depth,
    string Description,
    int SortOrder);
