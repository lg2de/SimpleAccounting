// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System.Globalization;
using lg2de.SimpleAccounting.Infrastructure;
using Xunit;

public class ImportFormatDetectorTests
{
    [Theory]
    [InlineData("31.12.2000", "-1.234,56", "en", "de-DE")]
    [InlineData("31/12/2000", "-1234,56", "de", "fr-FR")]
    [InlineData("12/31/2000", "-1,234.56", "de", "en-US")]
    [InlineData("2000-12-31", "-1234.5", "de", "en-US")]
    [InlineData("2000-12-31", "-1234,56", "de", "de-DE")]
    [InlineData("2000-12-31", "-1234,56", "fr", "fr-FR")]
    [InlineData("2000-12-31", "-1234,56", "en", "de-DE")]
    [InlineData("2000-12-31", "-1234", "en", "en-US")]
    [InlineData("2000-12-31", "-1234", "fr", "fr-FR")]
    public void Detect_SupportedFormat_CultureDetected(
        string date, string value, string preferredCulture, string expectedCulture)
    {
        var result = ImportFormatDetector.Detect(
            ["2000-01-01", date], ["0", value], CultureInfo.GetCultureInfo(preferredCulture));

        result.Should().NotBeNull();
        result!.Name.Should().Be(expectedCulture);
    }

    [Theory]
    [InlineData("31/12/2000", "-1234.56")]
    [InlineData("31.12.2000", "-1234.56")]
    [InlineData("12/31/2000", "-1234,56")]
    public void Detect_UnsupportedFormat_Null(string date, string value)
    {
        var result = ImportFormatDetector.Detect([date], [value], CultureInfo.GetCultureInfo("de"));

        result.Should().BeNull();
    }
}
