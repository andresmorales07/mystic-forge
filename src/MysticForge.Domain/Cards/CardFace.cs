namespace MysticForge.Domain.Cards;

public sealed record CardFace(
    string Name,
    string? OracleText,
    string? TypeLine,
    string? ManaCost);
