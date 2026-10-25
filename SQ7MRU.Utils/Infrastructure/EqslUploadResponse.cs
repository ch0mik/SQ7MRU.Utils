using System.Text.RegularExpressions;

namespace SQ7MRU.Utils.Infrastructure;

internal static partial class EqslUploadResponse
{
    public static bool IsSuccess(string html)
    {
        var document = ServiceHtml.Parse(html);
        foreach (var node in ServiceHtml.Nodes(document.DocumentNode, "//script|//style|//comment()").ToArray()) node.Remove();
        var text = ServiceHtml.Text(document.DocumentNode);
        // Negative evidence always wins, even if a success marker appears elsewhere.
        if (Failure().IsMatch(text)) return false;
        return SuccessMarker().IsMatch(text) || AddedRecords().IsMatch(text);
    }

    [GeneratedRegex(@"\b(?:error\s*[:=]|(?:failed|failure|unsuccessful|rejected)\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Failure();
    [GeneratedRegex(@"(?<![\w])RESULT\s*=\s*OK(?![\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SuccessMarker();
    [GeneratedRegex(@"\b[1-9]\d*\s+records?\s+added\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AddedRecords();
}
