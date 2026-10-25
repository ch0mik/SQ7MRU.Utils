using System.Globalization;
using System.Reflection;

namespace SQ7MRU.Utils;

/// <summary>Reads length-delimited ADI documents or a backwards-compatible QSO projection.</summary>
/// <param name="adifContent">Complete input retained until disposal; parsed input is limited to 32 MiB of UTF-16 characters.</param>
/// <exception cref="ArgumentNullException">Input is null.</exception>
/// <seealso href="https://www.adif.org/318/ADIF_318.htm">Reviewed ADIF specification.</seealso>
/// <remarks>No network or file I/O is performed. ADX is unsupported. GetAdifRows preserves provider compatibility; ReadDocument retains field data under an explicit profile.</remarks>
public sealed class AdifReader(string adifContent) : IDisposable
{
    private static readonly IReadOnlyDictionary<string, PropertyInfo> Properties = typeof(AdifRow).GetProperties()
        .Where(p => p.CanWrite && p.PropertyType == typeof(string)).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    private string? content = adifContent ?? throw new ArgumentNullException(nameof(adifContent));

    /// <summary>Gets whether the retained input has been released.</summary>
    public bool IsDisposed => content is null;

    /// <summary>Reads older/provider ADI into typed rows using the explicit eQSL compatibility projection.</summary>
    /// <param name="cancellationToken">Cancels parsing without returning a partial list.</param>
    /// <returns>Records with nonblank CALL; recognized values are trimmed and CALL/BAND casing is normalized.</returns>
    /// <exception cref="ObjectDisposedException">The reader is disposed.</exception>
    /// <exception cref="FormatException">Structural input, limits or record termination are invalid.</exception>
    /// <exception cref="OperationCanceledException">Cancellation is requested.</exception>
    /// <remarks>Accepts older headers, deprecated values and provider Unicode without current semantic restrictions. Duplicates use the last occurrence; unknown fields/header metadata are omitted only from this projection. Use ReadDocument to retain them. Do not parse concurrently with Dispose.</remarks>
    public List<AdifRow> GetAdifRows(CancellationToken cancellationToken = default)
    {
        var document = ReadDocument(AdifProfile.EqslLegacy, cancellationToken);
        var rows = new List<AdifRow>();
        foreach (var record in document.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = new AdifRow();
            foreach (var field in record.Fields)
                if (Properties.TryGetValue(field.Name, out var property)) property.SetValue(row, field.Value.Trim());
            if (!string.IsNullOrWhiteSpace(row.CALL)) rows.Add(AdifHelper.FixRecord(row));
        }
        return rows;
    }

    /// <summary>Reads an ADI document preserving exact values, empty fields, header metadata and extensions.</summary>
    /// <param name="profile">Standard validates supported current ADI rules while accepting deprecated import values; EqslLegacy omits semantic validation for older/provider data.</param>
    /// <param name="cancellationToken">Cancels scanning and validation without returning partial records.</param>
    /// <returns>A document retaining declared ADIF_VER and type indicators without CALL filtering or trimming.</returns>
    /// <exception cref="ObjectDisposedException">The reader is disposed.</exception>
    /// <exception cref="FormatException">Grammar, profile validation or limits fail.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The profile is unknown.</exception>
    /// <exception cref="OperationCanceledException">Cancellation is requested.</exception>
    /// <remarks>Limits: 32 MiB characters, 100,000 fields total and 4,096 fields per header/record. Strict mode rejects duplicates and requires ADI framing. Compatibility mode accepts legacy tagged headers and character lengths for older Unicode providers. Text outside fields is ignored, not retained. No ADX or binary parsing is attempted.</remarks>
    public AdifDocument ReadDocument(AdifProfile profile = AdifProfile.Standard, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        cancellationToken.ThrowIfCancellationRequested();
        var text = content!;
        if (text.Length > 32 * 1024 * 1024) throw new FormatException("ADIF input exceeds the character limit.");
        var header = new List<AdifField>();
        var records = new List<AdifRecord>();
        var fields = new List<AdifField>();
        var position = text.StartsWith('\uFEFF') ? 1 : 0;
        var expectsHeader = profile == AdifProfile.Standard && position < text.Length && text[position] != '<';
        var headerSeen = false;
        var fieldCount = 0;
        while (position < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var open = text.IndexOf('<', position);
            if (open < 0) break;
            var close = text.IndexOf('>', open + 1);
            if (close < 0) throw new FormatException("Unterminated ADIF tag.");
            var size = close - open - 1;
            if (size is <= 0 or > 512) throw new FormatException("Invalid or oversized ADIF tag.");
            var raw = text.Substring(open + 1, size);
            position = close + 1;
            if (raw.Equals("EOH", StringComparison.OrdinalIgnoreCase))
            {
                if (headerSeen || records.Count != 0 || profile == AdifProfile.Standard && !expectsHeader)
                    throw new FormatException("Unexpected ADIF header terminator.");
                header.AddRange(fields); fields.Clear(); headerSeen = true; expectsHeader = false;
                continue;
            }
            if (raw.Equals("EOR", StringComparison.OrdinalIgnoreCase))
            {
                if (expectsHeader || fields.Count == 0) throw new FormatException("Invalid or empty ADIF record.");
                records.Add(new(fields)); fields.Clear();
                continue;
            }
            var tag = raw.Split(':');
            if (expectsHeader && tag.Length == 1) continue;
            if (tag.Length is < 2 or > 3 || !AdifValidation.ValidName(tag[0]) ||
                !int.TryParse(tag[1], NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length > text.Length - position)
                throw new FormatException("Invalid ADIF data specifier or field length.");
            if (++fieldCount > 100_000 || fields.Count >= 4096) throw new FormatException("ADIF field-count limit exceeded.");
            fields.Add(new(tag[0], text.Substring(position, length), tag.Length == 3 ? tag[2] : null));
            position += length;
        }
        if (expectsHeader) throw new FormatException("ADIF header is missing EOH.");
        if (fields.Count != 0) throw new FormatException("ADIF record is missing EOR.");
        if (profile == AdifProfile.Standard && records.Count == 0) throw new FormatException("ADI input has no records.");
        var document = new AdifDocument(header, records);
        AdifValidation.Document(document, profile, exporting: false, cancellationToken);
        return document;
    }

    /// <summary>Releases the input reference and prevents later parsing; repeated calls are harmless.</summary>
    /// <remarks>Previously returned documents/rows remain usable; disposal does not erase immutable strings.</remarks>
    public void Dispose() => content = null;
}
