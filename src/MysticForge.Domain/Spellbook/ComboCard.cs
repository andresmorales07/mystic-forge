namespace MysticForge.Domain.Spellbook;

public sealed class ComboCard
{
    public required string  ComboId          { get; init; }
    public required short   CardPosition     { get; init; }
    public required string  CardName         { get; set; }
    public          Guid?   OracleId         { get; set; }
    public          short   Quantity         { get; set; } = 1;
    public          bool    MustBeCommander  { get; set; }
    public          string? ZoneLocations    { get; set; }

    public Combo? Combo { get; init; }
}
