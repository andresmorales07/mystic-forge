using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MysticForge.Domain.Cards;

public static class OracleHasher
{
    private const string FaceDelimiter = "\n---FACE---\n";
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    public static byte[] HashSingleFace(string oracleText)
    {
        var normalized = Normalize(oracleText);
        return SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
    }

    public static byte[] HashMultiFace(IReadOnlyList<CardFace> faces)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < faces.Count; i++)
        {
            if (i > 0) sb.Append(FaceDelimiter);
            sb.Append(Normalize(faces[i].OracleText ?? string.Empty));
        }
        return SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static string Normalize(string input)
    {
        var nfc = input.Normalize(NormalizationForm.FormC);
        var lowered = nfc.ToLower(CultureInfo.InvariantCulture);
        var whitespaceCollapsed = WhitespaceRun.Replace(lowered, " ");
        return whitespaceCollapsed.Trim();
    }
}
