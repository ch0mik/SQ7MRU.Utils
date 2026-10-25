using HtmlAgilityPack;

namespace SQ7MRU.Utils.Infrastructure;

internal static class EpcProtocol
{
    internal static IReadOnlyList<(string Title, string Url)> Groups(HtmlDocument doc, ServiceHttpSession http)
    {
        var groups = new List<(string, string)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // The membership menu item is a stable entry point to the certificate
        // accordion. Enumerate that list dynamically, not every Joomla menu.
        var certificateMenu = doc.DocumentNode.SelectSingleNode(
            "//li[contains(concat(' ',normalize-space(@class),' '),' item-133 ')]/ancestor::ul[1]");
        if (certificateMenu is null)
        {
            // Older markup may omit item-* classes but retain the membership URL.
            foreach (var anchor in ServiceHtml.Nodes(doc.DocumentNode, "//a[@href]"))
            {
                if (!Uri.TryCreate(http.BaseAddress, ServiceHtml.Required(anchor, "href"), out var uri) || uri.Host != http.BaseAddress.Host) continue;
                var parts = uri.Query.TrimStart('?').Split('&');
                if (parts.Contains("option=com_content") && parts.Contains("view=article") && parts.Contains("id=12"))
                {
                    certificateMenu = anchor.Ancestors("ul").FirstOrDefault();
                    if (certificateMenu is not null) break;
                }
            }
        }
        // Fail closed if a structured menu no longer has our entry point.
        // Flat legacy markup remains supported without assuming translated labels.
        if (certificateMenu is null && ServiceHtml.Nodes(doc.DocumentNode,
            "//ul[contains(concat(' ',normalize-space(@class),' '),' menu ')]").Any()) return groups;
        foreach (var anchor in ServiceHtml.Nodes(certificateMenu ?? doc.DocumentNode, ".//a[@href]"))
        {
            // Inspect navigation links rather than unrelated links embedded in article content.
            var menuTitle = ServiceHtml.Nodes(anchor, ".//span[contains(concat(' ',normalize-space(@class),' '),' item-title ')]").FirstOrDefault();
            if (menuTitle is null && !anchor.Ancestors().Any(node => node.Name is "nav" or "ul" ||
                node.GetAttributeValue("role", "").Equals("navigation", StringComparison.OrdinalIgnoreCase))) continue;
            var href = ServiceHtml.Required(anchor, "href");
            if (!Uri.TryCreate(http.BaseAddress, href, out var uri) || uri.Host != http.BaseAddress.Host) continue;
            var query = uri.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2))
                .Where(part => part.Length == 2).GroupBy(part => part[0], StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => Uri.UnescapeDataString(group.First()[1]), StringComparer.OrdinalIgnoreCase);
            if (!query.TryGetValue("id", out var id) || query.GetValueOrDefault("option") != "com_content" ||
                query.GetValueOrDefault("view") != "article" || query.ContainsKey("task")) continue;
            var title = ServiceHtml.Text(menuTitle ?? anchor);
            if (title.Length > 0 && seen.Add(id)) groups.Add((title, href));
        }
        return groups;
    }
    internal static IEnumerable<(string Name, string Action, Dictionary<string, string> Fields)> Awards(HtmlDocument doc, string title, Uri page)
    {
        var number = 0;
        foreach (var form in ServiceHtml.Nodes(doc.DocumentNode, "//form[@action]"))
        {
            var action = ServiceHtml.Required(form, "action");
            var format = ServiceHtml.Nodes(form, ".//select").FirstOrDefault(select =>
                select.GetAttributeValue("name", "").Contains("fileformat", StringComparison.OrdinalIgnoreCase));
            if (format is null && !new[] { "award", "member", "cert", "contest" }.Any(word => action.Contains(word, StringComparison.OrdinalIgnoreCase))) continue;
            if (ServiceHtml.Nodes(form, ".//input").Any(input => input.GetAttributeValue("type", "").Equals("password", StringComparison.OrdinalIgnoreCase))) continue;
            var fields = ServiceHtml.Nodes(form, ".//input").Where(input =>
                input.GetAttributeValue("type", "text").Equals("hidden", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(input.GetAttributeValue("name", "")))
                .GroupBy(input => ServiceHtml.Required(input, "name"))
                .ToDictionary(group => group.Key, group => HtmlEntity.DeEntitize(group.Last().GetAttributeValue("value", "")));
            if (fields.GetValueOrDefault("option", "").Equals("com_users", StringComparison.OrdinalIgnoreCase) ||
                fields.GetValueOrDefault("task", "").StartsWith("user.", StringComparison.OrdinalIgnoreCase)) continue;
            if (format is not null) fields[ServiceHtml.Required(format, "name")] = "jpg";
            var submit = ServiceHtml.Nodes(form, ".//input | .//button").FirstOrDefault(input =>
                input.GetAttributeValue("type", input.Name == "button" ? "submit" : "text").Equals("submit", StringComparison.OrdinalIgnoreCase));
            if (submit is not null && !string.IsNullOrEmpty(submit.GetAttributeValue("name", "")))
                fields[ServiceHtml.Required(submit, "name")] = HtmlEntity.DeEntitize(submit.GetAttributeValue("value", "Submit"));
            var selectors = ServiceHtml.Nodes(form, ".//select").Where(select => select != format).ToArray();
            if (selectors.Length > 1)
                throw new ServiceException(FailureKind.InvalidResponse, "EPC certificate form has multiple award selectors; its field combination is ambiguous.");
            var target = new Uri(page, action).ToString();
            if (selectors.Length == 0)
            {
                number++;
                yield return (number == 1 ? title : $"{title}-{number}", target, fields);
                continue;
            }
            foreach (var option in ServiceHtml.Nodes(selectors[0], ".//option[not(@disabled)]"))
            {
                var name = ServiceHtml.Text(option);
                var value = HtmlEntity.DeEntitize(option.GetAttributeValue("value", name));
                if (name.Length == 0 || value.Length == 0 || name.Contains("select", StringComparison.OrdinalIgnoreCase)) continue;
                var values = new Dictionary<string, string>(fields) { [ServiceHtml.Required(selectors[0], "name")] = value };
                yield return (name, target, values);
            }
        }
    }
}
