namespace MysticForge.Application.Tagging;

/// <summary>JSON-serialized payload for the LLM user message. Mirrors Domain.Card without persistence noise.</summary>
public sealed record CardForTagging(
    string Name,
    string? ManaCost,
    string? TypeLine,
    string? OracleText,
    IReadOnlyList<CardFaceForTagging>? Faces);

public sealed record CardFaceForTagging(
    string Name,
    string? ManaCost,
    string? TypeLine,
    string? OracleText);
