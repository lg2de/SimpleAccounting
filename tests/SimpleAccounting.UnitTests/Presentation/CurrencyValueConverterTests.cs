// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Globalization;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class CurrencyValueConverterTests
{
    [Theory]
    [InlineData("de", 12.5, null, "12,50")]
    [InlineData("en", 12.5, null, "12.50")]
    [InlineData("de", 1234.5, "€", "1.234,50 €")]
    [InlineData("de", -12.5, "€", "-12,50 €")]
    [InlineData("en", 12.5, "€", "€12.50")]
    [InlineData("en", -12.5, "€", "-€12.50")]
    [InlineData("fr", 12.5, "€", "12,50 €")]
    [InlineData("de", 12.5, "", "12,50")]
    public void Convert_ValueAndCurrency_FormattedByCulture(
        string cultureName, double value, string currency, string expected)
    {
        var sut = new CurrencyValueConverter();

        var result = sut.Convert(
            [value, currency], typeof(string), null, CultureInfo.GetCultureInfo(cultureName));

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("€")]
    public void Convert_ZeroWithHideZero_Empty(string currency)
    {
        var sut = new CurrencyValueConverter();

        var result = sut.Convert(
            [0.0, currency], typeof(string), CurrencyValueConverter.HideZeroParameter, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_ZeroWithoutHideZero_Formatted()
    {
        var sut = new CurrencyValueConverter();

        var result = sut.Convert([0.0, null], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("0.00");
    }

    [Fact]
    public void Convert_ValueNotAvailable_Empty()
    {
        var sut = new CurrencyValueConverter();

        var result = sut.Convert(
            [System.Windows.DependencyProperty.UnsetValue, "€"], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }
}
