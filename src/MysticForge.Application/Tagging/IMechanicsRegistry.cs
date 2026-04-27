using System.Text;

namespace MysticForge.Application.Tagging;

public interface IMechanicsRegistry
{
    /// <summary>
    /// Normalizes the raw mechanic name (lowercase, underscores) and returns its id.
    /// Inserts a new row with reviewed=false if not yet known.
    /// </summary>
    Task<long> ResolveOrInsertAsync(string rawName, CancellationToken ct);

    /// <summary>Pure function: lowercase, replace whitespace and hyphens with underscores, strip non-alphanum/underscore, collapse runs.</summary>
    static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var lowered = raw.Trim().ToLowerInvariant();
        var buf = new StringBuilder(lowered.Length);
        foreach (var ch in lowered)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') { buf.Append(ch); continue; }
            if (char.IsWhiteSpace(ch) || ch == '-') { buf.Append('_'); continue; }
            // Drop other punctuation (e.g. apostrophes in "Lieutenant's").
        }
        var s = buf.ToString();
        while (s.Contains("__")) s = s.Replace("__", "_");
        return s.Trim('_');
    }
}
