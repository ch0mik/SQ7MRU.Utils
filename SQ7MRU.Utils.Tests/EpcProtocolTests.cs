using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public class EpcProtocolTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AccordionDiscoveryStaysInsideCertificateMenuIncludingFutureCategories(bool membershipClass)
    {
        using var scheduler = new ServiceRequestScheduler(TimeProvider.System);
        using var http = new ServiceHttpSession(new HttpClient(), QslService.Epc, new(), scheduler, TimeProvider.System);
        var page = ServiceHtml.Parse("""
            <div class='accordionbody'><ul class='menu'>
              <li><a href='index.php?option=com_content&amp;view=article&amp;id=900'>Change password</a></li>
            </ul></div>
            <div class='accordionbody current-body'><ul class='menu'>
              <li class='item-133 item-first'><a href='index.php?option=com_content&amp;view=article&amp;id=12&amp;lang=pl'>Certyfikat członkostwa</a></li>
              <li class='item-232'><a href='index.php?option=com_content&amp;view=article&amp;id=61&amp;lang=pl'>Dyplomy EPC</a></li>
              <li class='item-9999'><a href='index.php?option=com_content&amp;view=article&amp;id=8888&amp;lang=pl'>Nowy klub</a></li>
              <li class='item-236 item-last'><a href='index.php?option=com_content&amp;view=article&amp;id=65&amp;lang=pl'>Certyfikaty zawodów</a></li>
            </ul></div>
            <div class='accordionbody'><ul class='menu'>
              <li><a href='index.php?option=com_content&amp;view=article&amp;id=901'>Account settings</a></li>
            </ul></div>
            """);
        if (!membershipClass)
            foreach (var node in ServiceHtml.Nodes(page.DocumentNode, "//li[@class='item-133 item-first']")) node.SetAttributeValue("class", "item-first");
        Assert.Equal(new[] { "Certyfikat członkostwa", "Dyplomy EPC", "Nowy klub", "Certyfikaty zawodów" },
            EpcProtocol.Groups(page, http).Select(group => group.Title));
    }

    [Fact]
    public void MenuDiscoveryUsesNeitherCategoryIdsNorLanguageOrOrdering()
    {
        using var scheduler = new ServiceRequestScheduler(TimeProvider.System);
        using var http = new ServiceHttpSession(new HttpClient(), QslService.Epc, new(), scheduler, TimeProvider.System);
        var page = ServiceHtml.Parse("""
            <nav><ul>
              <li><a href='/index.php?option=com_content&amp;view=article&amp;id=9999&amp;lang=pl'><b>Nowa grupa</b></a></li>
              <li><a href='/index.php?option=com_content&amp;view=article&amp;id=65&amp;lang=pl'>Certyfikaty zawodów</a></li>
              <li><a href='/index.php?option=com_content&amp;view=article&amp;id=12&amp;lang=pl'>Certyfikat członkostwa</a></li>
              <li><a href='/index.php?option=com_content&amp;view=article&amp;id=9999&amp;lang=en'>Duplicate</a></li>
              <li><a href='https://example.com/index.php?option=com_content&amp;view=article&amp;id=1'>External</a></li>
              <li><a href='/index.php?option=com_users&amp;task=user.logout'>Log out</a></li>
              <li><a href='/index.php?option=com_content&amp;view=article&amp;id=2&amp;task=edit'>Edit</a></li>
            </ul></nav>
            <article><a href='/index.php?option=com_content&amp;view=article&amp;id=3'>Unrelated content</a></article>
            """);
        var groups = EpcProtocol.Groups(page, http);
        Assert.Equal(new[] { "Nowa grupa", "Certyfikaty zawodów", "Certyfikat członkostwa" }, groups.Select(group => group.Title));
    }

    [Fact]
    public void EveryListedCategoryIsDiscoveredWithoutEnglishBoundaryLabels()
    {
        using var scheduler = new ServiceRequestScheduler(TimeProvider.System);
        using var http = new ServiceHttpSession(new HttpClient(), QslService.Epc, new(), scheduler, TimeProvider.System);
        var ids = new[] { 12, 61, 64, 63, 206, 209, 211, 212, 214, 215, 65 };
        var html = string.Concat(ids.Select(id => $"<a href='index.php?option=com_content&amp;view=article&amp;id={id}&amp;lang=pl'><span class='item-title'>Kategoria {id}</span></a>"));
        Assert.Equal(11, EpcProtocol.Groups(ServiceHtml.Parse(html), http).Count);
    }

    [Fact]
    public void AllFormsButtonsAndOptionGroupsAreInspected()
    {
        var doc = ServiceHtml.Parse("""
            <form action='award-new.php'><input type='hidden' name='token' value='a&amp;b'>
              <select name='award'><option value=''>Wybierz</option><optgroup label='Group'>
                <option value='first'>First</option><option value='disabled' disabled>Unavailable</option>
              </optgroup></select>
              <select name='fileformat'><option>jpg</option></select><button name='download' value='yes'>Download</button>
            </form>
            <form action='certificate.php'><input type='hidden' name='kind' value='membership'>
              <button name='get' value='1'>Download</button></form>
            <form action='index.php'><input type='hidden' name='task' value='user.logout'></form>
            <form action='membership.php'><input type='hidden' name='task' value='user.logout'></form>
            """);
        var awards = EpcProtocol.Awards(doc, "Membership", new("https://epc-mc.eu/index.php")).ToArray();
        Assert.Equal(2, awards.Length);
        Assert.Equal("First", awards[0].Name);
        Assert.Equal("a&b", awards[0].Fields["token"]);
        Assert.Equal("yes", awards[0].Fields["download"]);
        Assert.Equal("membership", awards[1].Fields["kind"]);
    }
}
