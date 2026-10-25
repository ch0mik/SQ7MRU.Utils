using System.Globalization;
using System.Text.Json.Nodes;
using SQ7MRU.Build;

namespace SQ7MRU.Utils.Tests;

public sealed class AdifMetadataGeneratorTests
{
    private const string Synthetic = """
        Released ADIF Version 3.1.8
        <table><tr id="QSO_Field_AGE"><td>AGE</td><td><a>Number</a></td><td></td>
        <td><span title="Minimum">0</span> to <span title="Maximum">120</span></td></tr>
        <tr id="QSO_Field_MODE"><td>MODE</td><td><a>Enumeration</a></td><td><a href="#Mode_Enumeration">Mode</a></td><td></td></tr></table>
        <h4><a id="Mode_Enumeration">Mode</a></h4><div><table><tr><th>Mode</th></tr>
        <tr><td>PSK31 (import-only)</td></tr><tr><td>AM</td></tr></table></div>
        <table id="Enumeration_Primary_Administrative_Subdivision_1"><tr><td>ON</td><td>Ontario</td></tr></table>
        <h4><a id="Band_Enumeration">Band</a></h4><table><tr><th>Band</th></tr><tr><td>2190m</td><td>.1357</td><td>.1378</td></tr></table>
        """;

    [Fact]
    public void ExtractsLimitsEnumsSubdivisionAndBandIndependentlyOfCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new("pl-PL");
            var result = AdifMetadataGenerator.Extract(Synthetic);
            Assert.Equal("Number", result["fields"]!["AGE"]!["type"]!.GetValue<string>());
            Assert.Equal("120", result["fields"]!["AGE"]!["maximum"]!.GetValue<string>());
            Assert.Equal("PSK31", result["enumerations"]!["Mode_Enumeration"]![0]!["value"]!.GetValue<string>());
            Assert.True(result["enumerations"]!["Mode_Enumeration"]![0]!["importOnly"]!.GetValue<bool>());
            Assert.Equal("ON", result["subdivisions"]!["1"]![0]!["value"]!.GetValue<string>());
            Assert.Equal(.1357, result["bands"]![0]!["lower"]!.GetValue<double>());
            Assert.True(JsonNode.DeepEquals(result, AdifMetadataGenerator.Extract(Synthetic)));
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void ChangedRevisionRequiresExplicitGeneratorReview() =>
        Assert.Throws<InvalidDataException>(() => AdifMetadataGenerator.Extract(Synthetic.Replace("3.1.8", "unreviewed", StringComparison.Ordinal)));

    [Fact]
    public async Task PartialInventoryCannotReplaceReviewedCatalog()
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-metadata-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "reviewed.html");
            var target = Path.Combine(root, "metadata.json");
            await File.WriteAllTextAsync(source, Synthetic);
            await File.WriteAllTextAsync(target, "existing catalog");
            await Assert.ThrowsAsync<InvalidDataException>(() => AdifMetadataGenerator.WriteAsync(source, target));
            Assert.Equal("existing catalog", await File.ReadAllTextAsync(target));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
