namespace MysticForge.Application.Scryfall;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
