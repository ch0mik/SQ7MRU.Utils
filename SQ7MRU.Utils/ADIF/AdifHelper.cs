using System.Globalization;
using System.Reflection;

namespace SQ7MRU.Utils;

/// <summary>Exports typed QSO rows and provides reviewed band lookup and explicit legacy case normalization.</summary>
/// <seealso href="https://www.adif.org/318/ADIF_318.htm">Reviewed current ADIF specification.</seealso>
public static class AdifHelper
{
    /// <summary>Exports rows as ADI using the reviewed current standard profile.</summary>
    /// <param name="rows">Non-null records; at least one nonempty record is required.</param>
    /// <returns>ADI declaring the reviewed protocol revision with valid framing and invariant UTC creation time.</returns>
    /// <exception cref="ArgumentNullException">The array or a row is null.</exception>
    /// <exception cref="FormatException">Values or export eligibility fail the standard profile.</exception>
    /// <remarks>Null properties are omitted; empty strings remain empty fields. International/import-only data is rejected, not silently dropped. Select the compatibility overload for older provider output.</remarks>
    public static string ExportAsADIF(AdifRow[] rows) => ExportAsADIF(rows, AdifProfile.Standard);

    /// <summary>Exports typed rows under the current standard or explicit older eQSL compatibility profile.</summary>
    /// <param name="rows">Non-null records; at least one nonempty record is required.</param>
    /// <param name="profile">Standard declares the reviewed revision; EqslLegacy retains the previous helper's 3.0.6 declaration without current semantic restrictions.</param>
    /// <returns>ADI with a readable prefix, header and EOR-delimited records.</returns>
    /// <exception cref="ArgumentNullException">The array or a row is null.</exception>
    /// <exception cref="FormatException">The profile cannot represent supplied content.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The profile is unknown.</exception>
    /// <remarks>No network request occurs. The compatibility revision is not evidence that eQSL supports every supplied field. Use AdifDocument/AdifWriter for explicit header versions and extension preservation.</remarks>
    public static string ExportAsADIF(AdifRow[] rows, AdifProfile profile)
    {
        ArgumentNullException.ThrowIfNull(rows);
        AdifField[] header = [new("ADIF_VER", profile == AdifProfile.EqslLegacy ? "3.0.6" : AdifWriter.SupportedRevision),
            new("PROGRAMID", "SQ7MRU.Utils"), new("CREATED_TIMESTAMP", DateTime.UtcNow.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture))];
        return AdifWriter.Export(new(header, rows.Select(ToRecord)), profile);
    }

    /// <summary>Exports one typed row as a current-profile headerless ADI record.</summary>
    /// <param name="row">Non-null record; null properties are omitted and empty strings are retained.</param>
    /// <returns>One validated record including EOR and trailing CRLF.</returns>
    /// <exception cref="ArgumentNullException">The row is null.</exception>
    /// <exception cref="FormatException">The record is empty or contains invalid/ineligible values.</exception>
    public static string ConvertToString(AdifRow row) => AdifWriter.Export(new([], [ToRecord(row)]));

    private static AdifRecord ToRecord(AdifRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new(typeof(AdifRow).GetRuntimeProperties().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Where(p => p.GetValue(row) is string).Select(p => new AdifField(p.Name, (string)p.GetValue(row)!)));
    }

    /// <summary>Maps MHz to the reviewed ADIF band table, including both endpoints.</summary>
    /// <param name="freq">Frequency in megahertz.</param>
    /// <returns>The band label, or null for unmatched, nonfinite or negative values.</returns>
    /// <remarks>This is an interchange label mapping, not a jurisdiction-specific permission to transmit.</remarks>
    public static string? FreqToBand(double freq) => AdifMetadata.Bands.FirstOrDefault(b => freq >= b.Lower && freq <= b.Upper)?.Name;

    /// <summary>Applies only backwards-compatible CALL/BAND case normalization in place.</summary>
    /// <param name="row">Non-null record; null values remain null.</param>
    /// <returns>The same instance with invariant uppercase CALL and lowercase BAND.</returns>
    /// <exception cref="ArgumentNullException">The row is null.</exception>
    /// <remarks>No trimming, validation or field inference occurs here. Document parsing preserves original casing.</remarks>
    public static AdifRow FixRecord(AdifRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        row.BAND = row.BAND?.ToLowerInvariant();
        row.CALL = row.CALL?.ToUpperInvariant();
        return row;
    }
}
