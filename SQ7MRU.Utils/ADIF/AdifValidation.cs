using System.Globalization;
using System.Text.RegularExpressions;

namespace SQ7MRU.Utils;

internal static class AdifValidation
{
    private static readonly Dictionary<string, string> IndicatorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B"] = "Boolean", ["D"] = "Date", ["E"] = "Enumeration", ["G"] = "IntlMultilineString",
        ["I"] = "IntlString", ["L"] = "Location", ["M"] = "MultilineString", ["N"] = "Number",
        ["S"] = "String", ["T"] = "Time", ["C"] = "String"
    };

    internal static void Document(AdifDocument document, AdifProfile profile, bool exporting, CancellationToken token)
    {
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        if (exporting && document.Records.Count == 0) throw new FormatException("ADI export requires at least one nonempty record.");
        var definitions = new Dictionary<string, (string Type, string? Constraint)>(StringComparer.OrdinalIgnoreCase);
        var applicationTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CheckDuplicates(document.Header, profile);
        foreach (var field in document.Header)
        {
            token.ThrowIfCancellationRequested();
            Basic(field);
            if (profile == AdifProfile.Standard && exporting && field.TypeIndicator?.Equals("C", StringComparison.OrdinalIgnoreCase) == true)
                Fail(field.Name, "legacy C indicator is import-only; use S for current strings");
            if (field.Name.StartsWith("USERDEF", StringComparison.OrdinalIgnoreCase))
            {
                if (profile == AdifProfile.Standard && !Match(field.Name, "^USERDEF[1-9][0-9]*$")) Fail(field.Name, "invalid USERDEF identifier");
                var parts = field.Value.Split(',', 2);
                var name = parts[0];
                if (profile == AdifProfile.Standard && (!ValidName(name) || AdifMetadata.Fields.ContainsKey(name.ToUpperInvariant()) || definitions.ContainsKey(name)))
                    Fail(field.Name, "invalid or duplicate user-defined field name");
                var type = TypeFor(field.TypeIndicator, field.Name);
                if (profile == AdifProfile.Standard)
                {
                    Value(field.Name, field.Value, "String", null, exporting);
                    if (type.StartsWith("Intl", StringComparison.Ordinal)) Fail(field.Name, "international USERDEF types require ADX");
                    if (parts.Length == 2) ValidateConstraintDefinition(field.Name, parts[1]);
                }
                definitions[name] = (type, parts.Length == 2 ? parts[1] : null);
            }
            else if (profile == AdifProfile.Standard)
            {
                if (field.TypeIndicator is not null && TypeFor(field.TypeIndicator, field.Name) != "String")
                    Fail(field.Name, "header metadata requires string data");
                if (exporting && field.Name.ToUpperInvariant() is not ("ADIF_VER" or "CREATED_TIMESTAMP" or "PROGRAMID" or "PROGRAMVERSION"))
                    Fail(field.Name, "unsupported header field");
                Value(field.Name, field.Value, "String", null, exporting);
                if (field.Name.Equals("ADIF_VER", StringComparison.OrdinalIgnoreCase) &&
                    (!Match(field.Value, "^[1-3]\\.[0-9]{1,2}(?:\\.[0-9])?$") || !Version.TryParse(field.Value, out var version) || version > new Version(AdifMetadata.Revision)))
                    Fail(field.Name, "unsupported declared ADIF revision");
                if (exporting && field.Name.Equals("ADIF_VER", StringComparison.OrdinalIgnoreCase) && field.Value != AdifMetadata.Revision)
                    Fail(field.Name, "standard export targets the reviewed revision; use an explicit compatibility profile to preserve older export metadata");
                if (field.Name.Equals("CREATED_TIMESTAMP", StringComparison.OrdinalIgnoreCase) &&
                    (!DateTime.TryParseExact(field.Value, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var created) || created.Year < 1930))
                    Fail(field.Name, "invalid UTC creation timestamp");
            }
        }
        foreach (var record in document.Records)
        {
            token.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(record);
            if (record.Fields.Count == 0) throw new FormatException("An ADI record must contain data fields.");
            CheckDuplicates(record.Fields, profile);
            foreach (var field in record.Fields)
            {
                token.ThrowIfCancellationRequested();
                Basic(field);
                if (profile == AdifProfile.EqslLegacy) continue;
                var name = field.Name.ToUpperInvariant();
                var rule = AdifMetadata.Fields.GetValueOrDefault(name);
                definitions.TryGetValue(name, out var definition);
                var indicatedType = field.TypeIndicator is null ? null : TypeFor(field.TypeIndicator, name);
                var type = rule?.Type ?? definition.Type ?? indicatedType ?? applicationTypes.GetValueOrDefault(name) ?? "MultilineString";
                if (name.StartsWith("APP_", StringComparison.Ordinal))
                {
                    if (name.Length < 7 || !name[4..].Contains('_') || name.EndsWith('_')) Fail(name, "APP fields require program and field names");
                    if (applicationTypes.TryGetValue(name, out var previousType) && previousType != type) Fail(name, "application field type changed within the document");
                    applicationTypes[name] = type;
                }
                if (exporting && field.TypeIndicator?.Equals("C", StringComparison.OrdinalIgnoreCase) == true) Fail(name, "legacy C indicator is import-only");
                if ((rule is not null || definition.Type is not null) && indicatedType is not null && !CompatibleType(type, indicatedType)) Fail(name, "type indicator conflicts with the field definition");
                if (exporting && name is "GUEST_OP" or "VE_PROV") Fail(name, "import-only field; use OPERATOR or STATE explicitly");
                if (exporting && rule is null && definition.Type is null && !name.StartsWith("APP_", StringComparison.Ordinal))
                    Fail(name, "unknown field requires an APP_ name or a USERDEF header definition");
                if (indicatedType is not null && indicatedType.StartsWith("Intl", StringComparison.Ordinal)) Fail(name, "international types require ADX");
                Value(name, field.Value, type, rule, exporting);
                if (field.Value.Length == 0) continue;
                if (definition.Constraint is not null) Constraint(name, field.Value, definition.Constraint);
                if (rule?.Enumeration is { } enumeration && AdifMetadata.Enumerations.TryGetValue(enumeration, out var values) &&
                    (type == "Enumeration" || name == "SUBMODE" && values.Any(v => v.Value.Equals(field.Value, StringComparison.OrdinalIgnoreCase))))
                {
                    var entry = values.FirstOrDefault(v => v.Value.Equals(field.Value, StringComparison.OrdinalIgnoreCase));
                    if (entry is null || exporting && entry.ImportOnly) Fail(name, "unsupported or import-only enumeration value");
                    if (name == "SUBMODE" && !string.Equals(Get(record, "MODE"), entry!.Related, StringComparison.OrdinalIgnoreCase))
                        Fail(name, "submode does not match MODE");
                    var date = Get(record, "QSO_DATE");
                    if (exporting && string.IsNullOrEmpty(date) && (!string.IsNullOrEmpty(entry!.FromDate) || !string.IsNullOrEmpty(entry.ToDate)))
                        Fail(name, "date-limited enumeration requires QSO_DATE for export");
                    if (entry!.Dxcc is { Length: > 0 } && Get(record, name.StartsWith("MY_", StringComparison.Ordinal) ? "MY_DXCC" : "DXCC") is { } country && entry.Dxcc != country)
                        Fail(name, "enumeration does not match DXCC");
                    if (date is not null && ((!string.IsNullOrEmpty(entry!.FromDate) && string.CompareOrdinal(date, entry.FromDate) < 0) ||
                        (!string.IsNullOrEmpty(entry.ToDate) && string.CompareOrdinal(date, entry.ToDate) > 0)))
                        Fail(name, "enumeration is outside its QSO-date validity interval");
                }
            }
            if (profile == AdifProfile.Standard) Relationships(record, exporting);
        }
    }

    internal static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 256 && name == name.Trim() &&
        name.All(c => c is >= ' ' and <= '~' && !",:<>{}".Contains(c)) &&
        !name.Equals("EOH", StringComparison.OrdinalIgnoreCase) && !name.Equals("EOR", StringComparison.OrdinalIgnoreCase);

    private static void Basic(AdifField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!ValidName(field.Name) || field.Value is null) throw new FormatException("Invalid ADIF field name or null data.");
        if (field.TypeIndicator is not null && !IndicatorTypes.ContainsKey(field.TypeIndicator)) Fail(field.Name, "invalid type indicator");
    }

    private static void CheckDuplicates(IReadOnlyList<AdifField> fields, AdifProfile profile)
    {
        if (profile != AdifProfile.Standard) return;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            Basic(field);
            if (!seen.Add(field.Name)) Fail(field.Name, "duplicate fields are not supported by the strict document profile");
        }
    }

    private static string TypeFor(string? indicator, string name) => indicator is null ? "String" :
        IndicatorTypes.TryGetValue(indicator, out var type) ? type : throw new FormatException($"{name}: invalid type indicator.");

    private static bool CompatibleType(string type, string indicated) => indicated == type ||
        indicated == "Number" && type is "Integer" or "PositiveInteger" ||
        indicated == "String" && type is "GridSquare" or "GridSquareExt" or "GridSquareList" or "SOTARef" or "WWFFRef" or
            "POTARefList" or "SecondarySubdivisionList" or "SecondaryAdministrativeSubdivisionListAlt" or "CreditList" or "SponsoredAwardList" or "IOTARefNo";

    private static void Value(string name, string value, string type, AdifMetadata.FieldRule? rule, bool exporting)
    {
        if (type.StartsWith("Intl", StringComparison.Ordinal)) Fail(name, "international fields require ADX");
        if (value.Any(c => c is < ' ' or > '~' && c is not ('\r' or '\n'))) Fail(name, "ADI requires ASCII field data");
        var multiline = type is "MultilineString";
        if (!multiline && value.Any(c => c is '\r' or '\n')) Fail(name, "line breaks are not permitted in this type");
        if (multiline && (value.Replace("\r\n", "", StringComparison.Ordinal).Contains('\r') || value.Replace("\r\n", "", StringComparison.Ordinal).Contains('\n')))
            Fail(name, "multiline breaks must use CRLF");
        if (value.Length == 0) return;
        if (type is "Number" or "Integer" or "PositiveInteger")
        {
            var pattern = type == "Number" ? @"^-?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)$" : @"^-?[0-9]+$";
            if (!Match(value, pattern) || !decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
                Fail(name, "invalid or unsupported numeric precision");
            else if (type == "PositiveInteger" && number <= 0 || rule?.Minimum is { } minimum && number < minimum && (exporting || name is not ("ANT_AZ" or "ANT_EL")) ||
                rule?.Maximum is { } maximum && number > maximum && (exporting || name is not ("ANT_AZ" or "ANT_EL"))) Fail(name, "numeric value is out of range");
            else if (name is "FREQ" or "FREQ_RX" && number <= 0) Fail(name, "frequency must be positive MHz");
        }
        else if (type == "Date" && (!DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date.Year < 1930)) Fail(name, "invalid UTC date");
        else if (type == "Time" && !TimeOnly.TryParseExact(value, ["HHmm", "HHmmss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail(name, "invalid UTC time");
        else if (type == "Boolean" && !Match(value, "^[YNyn]$")) Fail(name, "expected Y or N");
        else if (type == "GridSquare" && !Match(value, "^[A-Ra-r]{2}(?:[0-9]{2}(?:[A-Xa-x]{2}(?:[0-9]{2})?)?)?$")) Fail(name, "invalid Maidenhead locator");
        else if (type == "GridSquareExt" && !Match(!exporting && value.Length > 4 ? value[..4] : value, "^[A-Xa-x]{2}(?:[0-9]{2})?$")) Fail(name, "invalid grid extension");
        else if (type == "GridSquareList")
        {
            var grids = value.Split(',');
            if (grids.Length is not (2 or 4) || grids.Any(g => !Match(g, "^[A-Ra-r]{2}[0-9]{2}(?:[A-Xa-x]{2})?$"))) Fail(name, "expected two or four 4/6-character locators");
        }
        else if (type == "Location")
        {
            if (!Match(value, "^[NSEWnsew][0-9]{3} [0-9]{2}\\.[0-9]{3}$")) Fail(name, "invalid location");
            var degrees = decimal.Parse(value.AsSpan(1, 3), CultureInfo.InvariantCulture);
            var minutes = decimal.Parse(value.AsSpan(5), CultureInfo.InvariantCulture);
            var latitude = name.EndsWith("LAT", StringComparison.Ordinal);
            var directions = latitude ? "NSns" : name.EndsWith("LON", StringComparison.Ordinal) ? "EWew" : "NSEWnsew";
            if (!directions.Contains(value[0]) || minutes >= 60 || degrees + minutes / 60 > (latitude ? 90 : 180)) Fail(name, "location is out of range");
        }
        else if (type == "IOTARefNo" && (!Match(value, "^(?:AF|AN|AS|EU|NA|OC|SA)-[0-9]{3}$", true) || value.EndsWith("-000", StringComparison.Ordinal))) Fail(name, "invalid IOTA reference");
        else if (type == "POTARefList" && value.Split(',').Any(v => !Match(v, "^[A-Z0-9]{1,4}-[0-9]{4,5}(?:@[A-Z0-9-]{4,6})?$", true))) Fail(name, "invalid POTA reference list");
        else if (type == "SOTARef" && !Match(value, "^[A-Z0-9]+/[A-Z0-9]+-[0-9]+$", true)) Fail(name, "invalid SOTA reference");
        else if (type == "WWFFRef" && !Match(value, "^[A-Z0-9]{1,4}FF-[0-9]{4}$", true)) Fail(name, "invalid WWFF reference");
        else if (type == "CreditList") Credit(name, value, exporting);
        else if (type == "SponsoredAwardList" && value.Split(',').Any(v => !AdifMetadata.Enumerations["SponsoredAward_Enumeration"].Any(p => v.StartsWith(p.Value, StringComparison.OrdinalIgnoreCase)))) Fail(name, "invalid sponsored award prefix");
        else if (type == "SecondarySubdivisionList" && value.Split(':').Any(v => !Match(v, "^[^,:]+,[^,:]+$"))) Fail(name, "invalid county list");
        else if (type == "SecondaryAdministrativeSubdivisionListAlt" && value.Split(';').Any(v => !Match(v, "^[^:;]+:[^:;]+$"))) Fail(name, "invalid alternate subdivision list");
    }

    private static void Credit(string name, string value, bool exporting)
    {
        foreach (var item in value.Split(','))
        {
            var parts = item.Split(':');
            var known = AdifMetadata.Enumerations["Credit_Enumeration"].Any(v => v.Value.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
            var legacy = !exporting && AdifMetadata.Enumerations["Award_Enumeration"].Any(v => v.Value.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
            if ((!known && !legacy) || parts.Length > 2 || parts.Length == 2 && parts[1].Split('&').Any(v => !new[] { "CARD", "EQSL", "LOTW" }.Contains(v, StringComparer.OrdinalIgnoreCase)))
                Fail(name, "invalid or import-only credit list");
        }
    }

    private static void Constraint(string name, string value, string constraint)
    {
        if (!constraint.StartsWith('{') || !constraint.EndsWith('}')) Fail(name, "invalid USERDEF constraint");
        var body = constraint[1..^1];
        var range = body.Split(':');
        if (range.Length == 2)
        {
            if (!decimal.TryParse(range[0], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var low) ||
                !decimal.TryParse(range[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var high) || low >= high ||
                !decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) || number < low || number > high)
                Fail(name, "USERDEF value is outside its range");
        }
        else if (range.Length != 1 || !body.Split(',').Contains(value, StringComparer.OrdinalIgnoreCase)) Fail(name, "USERDEF value is outside its enumeration");
    }

    private static void ValidateConstraintDefinition(string name, string constraint)
    {
        if (!constraint.StartsWith('{') || !constraint.EndsWith('}')) Fail(name, "invalid USERDEF constraint");
        var body = constraint[1..^1];
        var range = body.Split(':');
        if (range.Length == 2)
        {
            if (!decimal.TryParse(range[0], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var low) ||
                !decimal.TryParse(range[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var high) || low >= high)
                Fail(name, "invalid USERDEF numeric range");
        }
        else if (range.Length != 1 || body.Split(',').Any(v => v.Length == 0)) Fail(name, "invalid USERDEF enumeration");
    }

    private static void Relationships(AdifRecord record, bool exporting)
    {
        foreach (var prefix in new[] { "", "MY_" })
        {
            var grid = Get(record, prefix + "GRIDSQUARE");
            if (Get(record, prefix + "GRIDSQUARE_EXT") is { Length: > 0 } && grid?.Length != 8) Fail(prefix + "GRIDSQUARE_EXT", "requires an 8-character base locator");
            var dxcc = Get(record, prefix + "DXCC");
            var state = Get(record, prefix == "" ? "STATE" : "MY_STATE");
            if (state is { Length: > 0 } && dxcc is not null && AdifMetadata.Subdivisions.TryGetValue(dxcc, out var states) &&
                !states.Any(s => s.Value.Equals(state, StringComparison.OrdinalIgnoreCase) && (!exporting || !s.ImportOnly))) Fail(prefix + "STATE", "unknown or import-only subdivision for DXCC");
        }
        foreach (var prefix in new[] { "", "EQSL_", "LOTW_", "DCL_" })
        {
            var received = Get(record, prefix + "QSLRDATE");
            var sent = Get(record, prefix + "QSLSDATE");
            if (received is { Length: > 0 } && !new[] { "Y", "I", "V" }.Contains(Get(record, prefix + "QSL_RCVD"), StringComparer.OrdinalIgnoreCase)) Fail(prefix + "QSLRDATE", "requires a matching received status");
            if (sent is { Length: > 0 } && !new[] { "Y", "Q", "I" }.Contains(Get(record, prefix + "QSL_SENT"), StringComparer.OrdinalIgnoreCase)) Fail(prefix + "QSLSDATE", "requires a matching sent status");
        }
    }

    private static string? Get(AdifRecord record, string name) => record.Fields.LastOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
    private static bool Match(string value, string pattern, bool ignoreCase = false)
    {
        try { return Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None), TimeSpan.FromSeconds(1)); }
        catch (RegexMatchTimeoutException) { throw new FormatException("ADIF value validation exceeded its time limit."); }
    }
    private static void Fail(string name, string reason) => throw new FormatException($"{name}: {reason}.");
}
