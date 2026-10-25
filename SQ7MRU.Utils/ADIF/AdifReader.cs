using System.Globalization;
using System.Reflection;

namespace SQ7MRU.Utils;

/// <summary>Parses ADIF text using declared field lengths instead of splitting values at angle brackets.</summary>
/// <param name="adifContent">Complete ADIF text held in memory; a header is optional.</param>
/// <exception cref="ArgumentNullException">The supplied text is null.</exception>
/// <remarks>Parsing is synchronous CPU work. Field names are case-insensitive; recognized string fields are assigned to AdifRow and unknown fields are skipped by their declared length. Dispose releases the reader's reference to the input; it does not erase strings from memory.</remarks>
/// <seealso href="https://www.adif.org/100/adif_100.htm">ADIF 1.0 physical format, field types and field definitions.</seealso>
public sealed class AdifReader(string adifContent) : IDisposable
{
    private static readonly IReadOnlyDictionary<string, PropertyInfo> Properties = typeof(AdifRow).GetProperties()
        .Where(p => p.CanWrite && p.PropertyType == typeof(string)).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    private string? content = adifContent ?? throw new ArgumentNullException(nameof(adifContent));
    /// <summary>Gets whether the input reference has been released and parsing is no longer available.</summary>
    public bool IsDisposed => content is null;
    /// <summary>Parses the retained ADIF text into QSO records and normalizes callsign/band casing.</summary>
    /// <param name="cancellationToken">Checked during parsing; cancellation throws rather than returning partial records.</param>
    /// <returns>A new list containing terminated records with a nonblank CALL; header fields and records without CALL are omitted.</returns>
    /// <exception cref="ObjectDisposedException">This reader has been disposed.</exception>
    /// <exception cref="FormatException">A tag is unterminated, a declared field length is invalid/out of bounds, or a final QSO with CALL lacks EOR.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Each call parses the input again without consuming it. Values are trimmed and lengths use .NET string characters, not UTF-8 bytes. Numeric/date ADIF fields remain strings and are not schema-validated. Do not parse concurrently with Dispose.</remarks>
    /// <remarks>Optional type indicators are accepted but not interpreted; zero-length fields and text between fields are accepted. Requiring CALL, trimming values and normalizing casing are library policies, not requirements of the ADIF 1.0 physical format. This string-based API does not support arbitrary binary ADIF payloads.</remarks>
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
    /// <summary>Releases the retained input reference and prevents subsequent parsing.</summary>
    /// <remarks>Repeated calls are harmless. Existing rows returned earlier are not modified.</remarks>
    public void Dispose() => content = null;
}
