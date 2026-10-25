using System.Buffers.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace SQ7MRU.Utils.Infrastructure;

// Structural download validation, not PDF rendering or active-content sanitization.
internal static class PdfCertificateValidator
{
    internal static bool IsValid(byte[] content)
    {
        if (content.Length is < 64 or > 32 * 1024 * 1024 || !content.AsSpan().StartsWith("%PDF-"u8)) return false;
        if (!((content[5] == '1' && content[6] == '.' && content[7] is >= (byte)'0' and <= (byte)'7') ||
            (content[5] == '2' && content[6] == '.' && content[7] == '0'))) return false;
        var span = content.AsSpan();
        var end = span.Length;
        while (end > 0 && IsWhitespace(span[end - 1])) end--;
        if (end < 5 || !span[..end].EndsWith("%%EOF"u8)) return false;
        var marker = span[..(end - 5)].LastIndexOf("startxref"u8);
        if (marker < 0) return false;
        var value = span[(marker + 9)..(end - 5)];
        var start = 0;
        while (start < value.Length && IsWhitespace(value[start])) start++;
        if (!Utf8Parser.TryParse(value[start..], out long offset, out var consumed) || offset < 8 || offset >= marker) return false;
        for (var i = start + consumed; i < value.Length; i++) if (!IsWhitespace(value[i])) return false;
        var xref = span[(int)offset..marker];
        if (xref.StartsWith("xref"u8)) return xref.IndexOf("trailer"u8) >= 0 && xref.IndexOf("/Root"u8) >= 0;
        // PDF 1.5+ may use an XRef stream instead of a traditional xref table.
        var header = Encoding.Latin1.GetString(xref[..Math.Min(xref.Length, 2048)]);
        return Regex.IsMatch(header, @"\A\d+\s+\d+\s+obj\b", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking) &&
            Regex.IsMatch(header, @"/Type\s*/XRef\b", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    }

    private static bool IsWhitespace(byte value) => value is 0 or 9 or 10 or 12 or 13 or 32;
}
