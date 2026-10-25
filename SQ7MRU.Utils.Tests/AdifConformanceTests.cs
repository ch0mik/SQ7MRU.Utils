using System.Globalization;

namespace SQ7MRU.Utils.Tests;

public sealed class AdifConformanceTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void IndependentCurrentFixtureRetainsHeaderExtensionsEmptyAndSignificantValues()
    {
        using var reader = new AdifReader(Fixture("current-profile.adi"));
        var document = reader.ReadDocument();
        var record = Assert.Single(document.Records);
        Assert.Equal("3.1.8", document.Header.Single(f => f.Name == "ADIF_VER").Value);
        Assert.Contains(record.Fields, f => f.Name == "NAME" && f.Value == "");
        Assert.Contains(record.Fields, f => f.Name == "APP_FIXTURE_NOTE" && f.Value == " a<eor>b " && f.TypeIndicator == "S");
        var exported = AdifWriter.Export(document);
        Assert.StartsWith("Generated", exported);
        Assert.Contains("<COUNT:1:N>3", exported);
        using var reimport = new AdifReader(exported);
        Assert.Equal(record.Fields, Assert.Single(reimport.ReadDocument().Records).Fields);
    }

    [Theory]
    [InlineData("1.00")]
    [InlineData("2.2.7")]
    [InlineData("3.0.6")]
    [InlineData("3.1.8")]
    public void StandardImportAcceptsOlderVersionsAndImportOnlyValues(string revision)
    {
        var text = $"Legacy export\r\n<ADIF_VER:{revision.Length}>{revision}<EOH>" +
            "<CALL:5>K1ABC<MODE:5>PSK31<GUEST_OP:5>K2ABC<VE_PROV:2>ON<QSL_RCVD:1>V<EOR>";
        using var reader = new AdifReader(text);
        var document = reader.ReadDocument();
        Assert.Equal(revision, Assert.Single(document.Header).Value);
        Assert.Contains(Assert.Single(document.Records).Fields, f => f.Name == "VE_PROV" && f.Value == "ON");
        Assert.Contains($"<ADIF_VER:{revision.Length}>{revision}", AdifWriter.Export(document, AdifProfile.EqslLegacy));
        Assert.Throws<FormatException>(() => AdifWriter.Export(document));
    }

    [Fact]
    public void EqslProjectionKeepsTaggedHeaderUnicodeAndDeprecatedFields()
    {
        using var reader = new AdifReader(Fixture("legacy-eqsl.adi"));
        var row = Assert.Single(reader.GetAdifRows());
        Assert.Equal("K1ABC", row.CALL);
        Assert.Equal("Łukasz", row.NAME);
        Assert.Equal("PSK31", row.MODE);
        Assert.Equal("ON", row.VE_PROV);
        Assert.Contains(Assert.Single(reader.ReadDocument(AdifProfile.EqslLegacy).Records).Fields, f => f.Name == "APP_OLD_NOTE" && f.TypeIndicator == "C");
        Assert.Throws<FormatException>(() => reader.ReadDocument());
    }

    [Fact]
    public void OriginalAdifOneExampleIsReadableWithoutUpgradingItsValues()
    {
        // Headerless example from ADIF 1.0's physical-format section.
        const string text = "<call:6>WN4AZY<band:3>20M<mode:4>RTTY<qso_date:8>19960513<time_on:4>1305<eor>";
        using var reader = new AdifReader(text);
        Assert.Contains(Assert.Single(reader.ReadDocument().Records).Fields, f => f.Name == "band" && f.Value == "20M");
        Assert.Equal("WN4AZY", Assert.Single(reader.GetAdifRows()).CALL);
    }

    [Theory]
    [InlineData("<QSO_DATE:8:N>20261025<EOR>")]
    [InlineData("<IOTA:6>EU-000<EOR>")]
    [InlineData("<APP_TEST_VALUE:1:N>1<EOR><APP_TEST_VALUE:1:S>x<EOR>")]
    [InlineData("<SUBMODE:5>PSK31<MODE:4>RTTY<EOR>")]
    [InlineData("<GRIDSQUARE_EXT:2>BQ<EOR>")]
    public void StrictImportReportsTypeAndRelationshipErrors(string text)
    {
        using var reader = new AdifReader(text);
        Assert.Throws<FormatException>(() => reader.ReadDocument());
        Assert.NotEmpty(reader.ReadDocument(AdifProfile.EqslLegacy).Records);
    }

    [Fact]
    public void UserDefinedRangesAndApplicationTypeContinuationAreValidated()
    {
        using var range = new AdifReader("Generated<USERDEF1:11:N>Count,{0:5}<EOH><Count:1>9<EOR>");
        Assert.Throws<FormatException>(() => range.ReadDocument());
        using var invalidDefinition = new AdifReader("Generated<USERDEF1:11:N>Count,{5:0}<EOH><CALL:5>K1ABC<EOR>");
        Assert.Throws<FormatException>(() => invalidDefinition.ReadDocument());
        using var continuation = new AdifReader("<APP_TEST_VALUE:1:N>1<EOR><APP_TEST_VALUE:1>2<EOR>");
        Assert.Equal(2, continuation.ReadDocument().Records.Count);
    }

    [Theory]
    [InlineData("20221025", true)]
    [InlineData("20241025", false)]
    public void HistoricalSectionDatesAreCheckedAgainstTheQso(string date, bool valid)
    {
        using var reader = new AdifReader($"<CALL:5>K1ABC<ARRL_SECT:3>GTA<DXCC:1>1<QSO_DATE:8>{date}<EOR>");
        if (valid) Assert.Single(reader.ReadDocument().Records);
        else Assert.Throws<FormatException>(() => reader.ReadDocument());
        Assert.Single(reader.GetAdifRows());
    }

    [Fact]
    public void LegacyProjectionKeepsLastDuplicateWhileDocumentRetainsBoth()
    {
        using var reader = new AdifReader("<CALL:5>K1ABC<NAME:3>old<NAME:3>new<EOR>");
        Assert.Equal("new", Assert.Single(reader.GetAdifRows()).NAME);
        Assert.Equal(3, Assert.Single(reader.ReadDocument(AdifProfile.EqslLegacy).Records).Fields.Count);
        Assert.Throws<FormatException>(() => reader.ReadDocument());
    }

    [Theory]
    [InlineData("<COMMENT:1>x")]
    [InlineData("<CALL:5>K1ABC<EOR:0>")]
    [InlineData("<CALL:5>K1ABC<EOH:0><EOR>")]
    [InlineData("<CALL:5:S:extra>K1ABC<EOR>")]
    [InlineData("<CALL:-1>K1ABC<EOR>")]
    [InlineData("<CALL:999>short<EOR>")]
    [InlineData("<EOR>")]
    public void BothProfilesRejectMalformedStructure(string text)
    {
        using var reader = new AdifReader(text);
        Assert.Throws<FormatException>(() => reader.ReadDocument());
        Assert.Throws<FormatException>(() => reader.GetAdifRows());
    }

    [Theory]
    [InlineData("QSO_DATE", "20260230")]
    [InlineData("QSO_DATE", "19291231")]
    [InlineData("TIME_ON", "2400")]
    [InlineData("TIME_ON", "123460")]
    [InlineData("TX_PWR", "-1")]
    [InlineData("RX_PWR", "NaN")]
    [InlineData("FREQ", "14,074")]
    [InlineData("AGE", "121")]
    [InlineData("CQZ", "0")]
    [InlineData("ITUZ", "91")]
    [InlineData("GRIDSQUARE", "ZZ99")]
    [InlineData("BAND", "imaginary")]
    [InlineData("MODE", "imaginary")]
    [InlineData("NAME_INTL", "Łukasz")]
    [InlineData("NAME", "Łukasz")]
    [InlineData("NAME", "line\nbreak")]
    [InlineData("GUEST_OP", "K2ABC")]
    [InlineData("VE_PROV", "ON")]
    [InlineData("QSL_RCVD", "V")]
    [InlineData("QSL_RCVD_VIA", "M")]
    public void StandardExportRejectsInvalidOrImportOnlyData(string name, string value)
    {
        var document = new AdifDocument([], [new([new("CALL", "K1ABC"), new(name, value)])]);
        Assert.Throws<FormatException>(() => AdifWriter.Export(document));
        Assert.Contains(value, AdifWriter.Export(document, AdifProfile.EqslLegacy));
    }

    [Theory]
    [InlineData("TX_PWR", "0")]
    [InlineData("RX_PWR", "0")]
    [InlineData("POTA_REF", "K-5033,VE-5082@CA-AB")]
    [InlineData("WWFF_REF", "3DAFF-0002")]
    [InlineData("TIME_ON", "235959")]
    [InlineData("VUCC_GRIDS", "IO83UG,IO83UF")]
    [InlineData("QSLMSG", "first\r\nsecond")]
    public void StandardExportAcceptsReviewedCurrentValues(string name, string value)
    {
        var exported = AdifWriter.Export(new([], [new([new("CALL", "K1ABC"), new(name, value)])]));
        using var reader = new AdifReader(exported);
        Assert.Contains(Assert.Single(reader.ReadDocument().Records).Fields, f => f.Name == name && f.Value == value);
    }

    [Theory]
    [InlineData(40, "8m")]
    [InlineData(45, "8m")]
    [InlineData(54, "6m")]
    [InlineData(54.000001, "5m")]
    [InlineData(69.9, "5m")]
    [InlineData(119980, "2.5mm")]
    [InlineData(123000, "2.5mm")]
    [InlineData(134000, "2mm")]
    [InlineData(149000, "2mm")]
    [InlineData(300000, "submm")]
    [InlineData(7500000, "submm")]
    [InlineData(14, "20m")]
    [InlineData(14.35, "20m")]
    [InlineData(46, null)]
    [InlineData(double.NaN, null)]
    public void BandLookupUsesReviewedIntervalsAndEndpoints(double frequency, string? expected) =>
        Assert.Equal(expected, AdifHelper.FreqToBand(frequency));

    [Theory]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("tr-TR")]
    public void ExportUsesInvariantGregorianUtc(string culture)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new(culture);
            using var reader = new AdifReader(AdifHelper.ExportAsADIF([new() { CALL = "K1ABC", FREQ = "14.074", TX_PWR = "0" }]));
            var header = reader.ReadDocument().Header;
            Assert.Equal(AdifWriter.SupportedRevision, header.Single(f => f.Name == "ADIF_VER").Value);
            Assert.StartsWith(DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture), header.Single(f => f.Name == "CREATED_TIMESTAMP").Value);
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void ExtensionsCannotBeSilentlyDroppedAndGenericRecordsNeedNotHaveCall()
    {
        using var reader = new AdifReader("<COMMENT:5> abc <NAME:0><NEW_FIELD:3>raw<EOR>");
        var document = reader.ReadDocument();
        Assert.Equal(" abc ", Assert.Single(document.Records).Fields[0].Value);
        Assert.Empty(reader.GetAdifRows());
        Assert.Throws<FormatException>(() => AdifWriter.Export(document));
        Assert.Contains("<NEW_FIELD:3>raw", AdifWriter.Export(document, AdifProfile.EqslLegacy));
    }

    [Fact]
    public void AllReviewedFieldsHaveTypedPropertiesAndXmlDescriptions()
    {
        var properties = typeof(AdifRow).GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();
        Assert.Equal(181, properties.Length);
        Assert.Equal(AdifMetadata.Fields.Keys.Order(), properties.Select(p => p.Name).Order());
        var xml = System.Xml.Linq.XDocument.Load(Path.ChangeExtension(typeof(AdifRow).Assembly.Location, ".xml"));
        foreach (var property in properties)
        {
            var member = xml.Descendants("member").Single(m => (string?)m.Attribute("name") == $"P:SQ7MRU.Utils.AdifRow.{property.Name}");
            Assert.False(string.IsNullOrWhiteSpace(member.Element("summary")?.Value));
            Assert.Contains(AdifMetadata.Fields[property.Name].Type, member.Element("value")!.Value);
        }
    }

    [Fact]
    public void CancellationDisposalAndLimitsAreExplicit()
    {
        using var reader = new AdifReader("<CALL:5>K1ABC<EOR>");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => reader.ReadDocument(cancellationToken: cancelled.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => AdifWriter.Export(new([], []), cancellationToken: cancelled.Token));
        using var excessive = new AdifReader(string.Concat(Enumerable.Repeat("<APP_TEST_ITEM:0>", 4097)) + "<EOR>");
        Assert.Throws<FormatException>(() => excessive.ReadDocument(AdifProfile.EqslLegacy));
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.ReadDocument());
        Assert.Throws<FormatException>(() => AdifHelper.ExportAsADIF([]));
        Assert.Throws<FormatException>(() => AdifHelper.ExportAsADIF([new()]));
    }
}
