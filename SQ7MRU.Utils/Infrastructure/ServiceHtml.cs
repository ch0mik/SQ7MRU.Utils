using System.Net;
using HtmlAgilityPack;

namespace SQ7MRU.Utils.Infrastructure;

internal static class ServiceHtml
{
    public static HtmlDocument Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }
    public static IEnumerable<HtmlNode> Nodes(HtmlNode node, string xpath) =>
        node.SelectNodes(xpath)?.AsEnumerable() ?? [];
    public static string Required(HtmlNode? node, string attribute) =>
        node?.GetAttributeValue(attribute, "") is { Length: > 0 } value ? WebUtility.HtmlDecode(value)
            : throw new ServiceException(FailureKind.InvalidResponse, "Service page format has changed (required field missing).");
    public static string Text(HtmlNode node) => WebUtility.HtmlDecode(node.InnerText).Trim();
    public static void CheckAuthentication(string html, bool checkLoginForms = true)
    {
        if (html.Contains("incorrect password", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("invalid password", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("login failed", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("Username and password do not match", StringComparison.OrdinalIgnoreCase))
            throw new ServiceException(FailureKind.Authentication, "Login failed. Check your credentials.");
        var doc = Parse(html);
        // Only an interactive password field indicates a returned login form.
        // eQSL account-selection/switching forms also include EnteredPassword, but as type=hidden.
        if (checkLoginForms && (doc.GetElementbyId("login-form") is not null ||
            Nodes(doc.DocumentNode, "//input").Any(input =>
                input.GetAttributeValue("name", "").Equals("EnteredPassword", StringComparison.OrdinalIgnoreCase) &&
                input.GetAttributeValue("type", "text").Equals("password", StringComparison.OrdinalIgnoreCase))))
            throw new ServiceException(FailureKind.Authentication, "The service returned a login form; the session is not authenticated.");
    }
    public static string Query(string value) => Uri.EscapeDataString(value);
}

