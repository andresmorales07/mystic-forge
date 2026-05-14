namespace MysticForge.Domain.Spellbook;

public sealed class ComboTemplate
{
    public required string ComboId    { get; init; }
    public required long   TemplateId { get; init; }
    public          short  Quantity   { get; set; } = 1;

    public Combo?    Combo    { get; init; }
    public Template? Template { get; init; }
}
