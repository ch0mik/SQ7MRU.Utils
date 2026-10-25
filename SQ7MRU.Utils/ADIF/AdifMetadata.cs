using System.Text.Json;
using System.Text.Json.Serialization;

namespace SQ7MRU.Utils;

// Factual names, types, limits and enumerations from the reviewed official release.
// No network dependency: upgrades require an explicit generator run and review.
internal static class AdifMetadata
{
    internal sealed record FieldRule(string Type, string? Enumeration, decimal? Minimum, decimal? Maximum);
    internal sealed record EnumValue(string Value, bool ImportOnly, string? Related, string? Dxcc, string? FromDate, string? ToDate);
    internal sealed record Band(string Name, double Lower, double Upper);
    private sealed record Catalog(string Revision, Dictionary<string, FieldRule> Fields,
        Dictionary<string, EnumValue[]> Enumerations, Dictionary<string, EnumValue[]> Subdivisions, Band[] Bands);
    private static readonly Catalog Data = Load();
    internal static string Revision => Data.Revision;
    internal static IReadOnlyDictionary<string, FieldRule> Fields => Data.Fields;
    internal static IReadOnlyDictionary<string, EnumValue[]> Enumerations => Data.Enumerations;
    internal static IReadOnlyDictionary<string, EnumValue[]> Subdivisions => Data.Subdivisions;
    internal static IReadOnlyList<Band> Bands => Data.Bands;

    private static Catalog Load()
    {
        using var stream = typeof(AdifMetadata).Assembly.GetManifestResourceStream("SQ7MRU.Utils.ADIF.adif-metadata.json")
            ?? throw new InvalidOperationException("Missing reviewed ADIF metadata.");
        return JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        }) ?? throw new InvalidOperationException("Invalid ADIF metadata.");
    }
}
