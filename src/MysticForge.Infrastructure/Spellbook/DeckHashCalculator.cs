using System.Security.Cryptography;

namespace MysticForge.Infrastructure.Spellbook;

public static class DeckHashCalculator
{
    /// <summary>
    /// Order-independent SHA-256 over (sorted main ids) + 0xFF + (sorted commander ids).
    /// The 0xFF separator ensures the commander/main boundary affects the hash even when both lists
    /// contain the same ids.
    /// </summary>
    public static byte[] Compute(IReadOnlyList<Guid> main, IReadOnlyList<Guid> commanders)
    {
        var mainSorted       = main.OrderBy(g => g).ToArray();
        var commandersSorted = commanders.OrderBy(g => g).ToArray();

        using var sha    = SHA256.Create();
        using var stream = new MemoryStream(capacity: (mainSorted.Length + commandersSorted.Length) * 16 + 1);

        Span<byte> guidBytes = stackalloc byte[16];
        foreach (var g in mainSorted)
        {
            g.TryWriteBytes(guidBytes);
            stream.Write(guidBytes);
        }
        stream.WriteByte(0xFF);
        foreach (var g in commandersSorted)
        {
            g.TryWriteBytes(guidBytes);
            stream.Write(guidBytes);
        }

        stream.Position = 0;
        return sha.ComputeHash(stream);
    }
}
