using System.Globalization;
using System.Reflection;

namespace SQ7MRU.Utils;

/// <summary>Length-aware reader; field values may contain line breaks or angle brackets.</summary>
public sealed class AdifReader(string adifContent) : IDisposable
{
    private static readonly IReadOnlyDictionary<string, PropertyInfo> Properties = typeof(AdifRow).GetProperties()
        .Where(p => p.CanWrite && p.PropertyType == typeof(string)).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    private string? content = adifContent ?? throw new ArgumentNullException(nameof(adifContent));
    public bool IsDisposed => content is null;
    public List<AdifRow> GetAdifRows(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var text = content!;
        var rows = new List<AdifRow>();
        var row = new AdifRow();
        var hasFields = false;
        for (var position = 0; position < text.Length;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var open = text.IndexOf('<', position);
            if (open < 0) break;
            var close = text.IndexOf('>', open + 1);
            if (close < 0) throw new FormatException("Unterminated ADIF tag.");
            var tag = text[(open + 1)..close].Split(':');
            position = close + 1;
            if (tag[0].Equals("EOH", StringComparison.OrdinalIgnoreCase))
            { row = new(); hasFields = false; continue; }
            if (tag[0].Equals("EOR", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(row.CALL)) rows.Add(AdifHelper.FixRecord(row));
                row = new(); hasFields = false; continue;
            }
            if (tag.Length < 2 || !int.TryParse(tag[1], NumberStyles.None, CultureInfo.InvariantCulture, out var length)
                || length > text.Length - position) throw new FormatException("Invalid ADIF field length.");
            if (Properties.TryGetValue(tag[0], out var property)) property.SetValue(row, text.Substring(position, length).Trim());
            position += length;
            hasFields = true;
        }
        if (hasFields && !string.IsNullOrWhiteSpace(row.CALL)) throw new FormatException("ADIF record is missing EOR.");
        return rows;
    }
    public void Dispose() => content = null;
}
