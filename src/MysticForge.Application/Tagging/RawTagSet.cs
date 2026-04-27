using System.Text.Json.Serialization;

namespace MysticForge.Application.Tagging;

/// <summary>Raw structured response from the LLM, validated by response_format JSON schema.</summary>
public sealed record RawTagSet(
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("synergy_hook_paths")] IReadOnlyList<string> SynergyHookPaths,
    [property: JsonPropertyName("mechanics")] IReadOnlyList<string> Mechanics,
    [property: JsonPropertyName("tribal_interest")] IReadOnlyList<string> TribalInterest);
