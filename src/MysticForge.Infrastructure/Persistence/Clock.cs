using MysticForge.Application.Scryfall;

namespace MysticForge.Infrastructure.Persistence;

public sealed class Clock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
