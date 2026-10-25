namespace SQ7MRU.Utils;

/// <summary>Separates standards-aware ADI behavior from tolerant provider compatibility.</summary>
public enum AdifProfile
{
    /// <summary>Validate supported ADI types and current export eligibility against the reviewed ADIF metadata.</summary>
    Standard,
    /// <summary>Accept older eQSL-style data without applying current semantic/export restrictions; this is not a conformance claim.</summary>
    EqslLegacy
}

/// <summary>Stores one length-delimited field without trimming its value or losing its optional type indicator.</summary>
/// <param name="Name">Case-insensitive field name, including legal APP or USERDEF names.</param>
/// <param name="Value">Exact field value; empty is distinct from an absent field.</param>
/// <param name="TypeIndicator">Optional ADIF type indicator, preserved independently of the value.</param>
/// <remarks>Construction does not validate values. Parsing and export enforce the selected profile.</remarks>
public sealed record AdifField(string Name, string Value, string? TypeIndicator = null);

/// <summary>Stores one complete QSO record, including fields absent from the typed row projection.</summary>
public sealed class AdifRecord
{
    /// <summary>Creates an immutable field-list snapshot without normalizing field data.</summary>
    /// <param name="fields">Non-null sequence of non-null fields; validated on export.</param>
    /// <exception cref="ArgumentNullException">The sequence is null.</exception>
    /// <exception cref="ArgumentException">A field entry is null.</exception>
    public AdifRecord(IEnumerable<AdifField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var snapshot = fields.ToArray();
        if (snapshot.Any(field => field is null)) throw new ArgumentException("Field entries cannot be null.", nameof(fields));
        Fields = Array.AsReadOnly(snapshot);
    }

    /// <summary>Gets ordered fields, retaining exact values and empty fields.</summary>
    public IReadOnlyList<AdifField> Fields { get; }
}

/// <summary>Represents an ADI document independently of the service-specific AdifRow projection.</summary>
public sealed class AdifDocument
{
    /// <summary>Creates immutable snapshots of header fields and records.</summary>
    /// <param name="header">Header metadata and USERDEF definitions; an empty sequence denotes no declared header fields.</param>
    /// <param name="records">Ordered records, which may include records without CALL.</param>
    /// <exception cref="ArgumentNullException">Either sequence is null.</exception>
    /// <exception cref="ArgumentException">A header field or record entry is null.</exception>
    /// <remarks>Construction does not validate the document. Arbitrary text outside fields is not retained. Export creates its own readable header prefix when header fields exist.</remarks>
    public AdifDocument(IEnumerable<AdifField> header, IEnumerable<AdifRecord> records)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(records);
        var headers = header.ToArray();
        var recordsSnapshot = records.ToArray();
        if (headers.Any(field => field is null) || recordsSnapshot.Any(record => record is null))
            throw new ArgumentException("Document entries cannot be null.");
        Header = Array.AsReadOnly(headers);
        Records = Array.AsReadOnly(recordsSnapshot);
    }

    /// <summary>Gets header metadata, including the originally declared ADIF_VER rather than a silently upgraded version.</summary>
    public IReadOnlyList<AdifField> Header { get; }

    /// <summary>Gets complete records in their original order without CALL filtering or casing changes.</summary>
    public IReadOnlyList<AdifRecord> Records { get; }
}
