using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace SQ7MRU.Build;

internal static partial class AdifMetadataGenerator
{
    private const string Revision = "3.1.8";

    internal static async Task WriteAsync(string sourcePath, string targetPath, CancellationToken cancellationToken = default)
    {
        if (new FileInfo(sourcePath).Length > 16 * 1024 * 1024) throw new InvalidDataException("Specification exceeds the input limit.");
        var source = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var catalog = Extract(source, cancellationToken);
        if (((JsonObject)catalog["fields"]!).Count != 181 || ((JsonArray)catalog["bands"]!).Count != 33)
            throw new InvalidDataException("Unexpected ADIF field or band inventory; review before updating.");
        var target = Path.GetFullPath(targetPath);
        var temporary = Path.Combine(Path.GetDirectoryName(target)!, $".metadata-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, catalog.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n", cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, target, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }

    // Kept separate to test extraction with small synthetic HTML, without network access.
    internal static JsonObject Extract(string source, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!source.Contains($"Released ADIF Version {Revision}", StringComparison.Ordinal))
            throw new InvalidDataException("Review the generator before using a different ADIF revision.");
        var document = new HtmlDocument();
        document.LoadHtml(source);
        var ids = document.DocumentNode.Descendants().Where(n => n.Attributes["id"] is not null)
            .GroupBy(n => n.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
        var fields = new JsonObject();
        var enums = new JsonObject();
        foreach (var (id, row) in ids)
        {
            token.ThrowIfCancellationRequested();
            if (!id.StartsWith("QSO_Field_", StringComparison.Ordinal) || row.Name != "tr") continue;
            var columns = Cells(row);
            if (columns.Length != 4) throw new InvalidDataException($"Unexpected field row: {id}");
            var dtype = columns[1].Descendants("a").FirstOrDefault() ?? throw new InvalidDataException($"Missing field type: {id}");
            var links = columns[2].Descendants("a").Select(a => a.GetAttributeValue("href", ""))
                .Where(h => h.StartsWith('#')).Select(h => h[1..]).ToArray();
            var rule = new JsonObject { ["type"] = Text(dtype), ["enumeration"] = links.FirstOrDefault() };
            foreach (var span in columns[3].Descendants("span"))
            {
                var title = span.GetAttributeValue("title", "");
                if (title is "Minimum" or "Maximum") rule[title.ToLowerInvariant()] = Text(span);
            }
            fields[id[10..]] = rule;
            foreach (var link in links)
            {
                if (enums.ContainsKey(link) || link is "Primary_Administrative_Subdivision" or "Secondary_Administrative_Subdivision" or "Deprecation") continue;
                var table = Table(link);
                if (table is null) continue;
                var values = new JsonArray();
                foreach (var tr in table.Descendants("tr"))
                {
                    var cs = Cells(tr);
                    if (cs.Length == 0) continue;
                    var code = Clean(Text(cs[0]));
                    if (code is "" or "Credit For" or "Enumeration") continue;
                    values.Add(new JsonObject
                    {
                        ["value"] = code, ["importOnly"] = Text(cs[0]).Contains("import-only", StringComparison.Ordinal),
                        ["related"] = link == "Submode_Enumeration" && cs.Length > 1 ? Text(cs[1]) : null,
                        ["dxcc"] = link == "ARRL_Section_Enumeration" && cs.Length > 2 ? Text(cs[2]) : null,
                        ["fromDate"] = link == "ARRL_Section_Enumeration" && cs.Length > 3 ? Text(cs[3]).Replace("/", "", StringComparison.Ordinal) : null,
                        ["toDate"] = link == "ARRL_Section_Enumeration" && cs.Length > 4 ? Text(cs[4]).Replace("/", "", StringComparison.Ordinal) : null
                    });
                }
                enums[link] = values;
            }
        }
        var subdivisions = new JsonObject();
        const string prefix = "Enumeration_Primary_Administrative_Subdivision_";
        foreach (var (id, table) in ids.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal) && pair.Value.Name == "table"))
        {
            token.ThrowIfCancellationRequested();
            var values = new JsonArray();
            foreach (var row in table.Descendants("tr"))
            {
                var cells = Cells(row);
                if (cells.Length > 0) values.Add(new JsonObject { ["value"] = Clean(Text(cells[0])), ["importOnly"] = Text(row).Contains("import-only", StringComparison.Ordinal) });
            }
            subdivisions[id[prefix.Length..]] = values;
        }
        var bands = new JsonArray();
        foreach (var row in Table("Band_Enumeration")?.Descendants("tr") ?? [])
        {
            var cells = Cells(row);
            if (cells.Length == 3) bands.Add(new JsonObject
            {
                ["name"] = Text(cells[0]), ["lower"] = double.Parse(Text(cells[1]), CultureInfo.InvariantCulture),
                ["upper"] = double.Parse(Text(cells[2]), CultureInfo.InvariantCulture)
            });
        }
        return new JsonObject { ["revision"] = Revision, ["source"] = "https://www.adif.org/318/ADIF_318.htm",
            ["fields"] = fields, ["enumerations"] = enums, ["subdivisions"] = subdivisions, ["bands"] = bands };

        HtmlNode? Table(string id) => !ids.TryGetValue(id, out var node) ? null : node.Name == "table" ? node : node.SelectSingleNode("following::table[1]");
    }

    private static HtmlNode[] Cells(HtmlNode row) => row.ChildNodes.Where(n => n.Name == "td").ToArray();
    private static string Text(HtmlNode node) => Whitespace().Replace(HtmlEntity.DeEntitize(node.InnerText), " ").Trim();
    private static string Clean(string value) => ImportOnly().Replace(value, "").Trim();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
    [GeneratedRegex(@"\s*\(import-only\)", RegexOptions.CultureInvariant)]
    private static partial Regex ImportOnly();
}
