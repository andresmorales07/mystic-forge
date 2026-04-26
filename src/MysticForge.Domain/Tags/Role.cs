namespace MysticForge.Domain.Tags;

// Closed list of Tier 1 roles. Order matches the JSON-schema enum sent to the LLM.
public static class Role
{
    public const string Ramp = "ramp";
    public const string Draw = "draw";
    public const string Tutor = "tutor";
    public const string Removal = "removal";
    public const string Counterspell = "counterspell";
    public const string Wipe = "wipe";
    public const string Protection = "protection";
    public const string WinCon = "win_con";
    public const string Stax = "stax";
    public const string LockPiece = "lock_piece";
    public const string Utility = "utility";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Ramp, Draw, Tutor, Removal, Counterspell, Wipe, Protection, WinCon, Stax, LockPiece, Utility,
    };
}
