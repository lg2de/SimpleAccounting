// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Globalization;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class DateConverterTests
{
    [Fact]
    public void Convert_NullInput_NullReturned()
    {
        var sut = new DateConverter();

        var result = sut.Convert(null, typeof(string), null, CultureInfo.GetCultureInfo("de"));

        result.Should().BeNull();
    }

    [Fact]
    public void Convert_MinInput_NullReturned()
    {
        var sut = new DateConverter();

        var result = sut.Convert(DateTime.MinValue, typeof(string), null, CultureInfo.GetCultureInfo("de"));

        result.Should().BeNull();
    }
    
    [Theory]
    [InlineData("de", "01.01.2023")]
    [InlineData("en", "1/1/2023")]
    public void Convert_ValidInput_FormattedReturned(string culture, string expectedResult)
    {
        var sut = new DateConverter();

        var result = sut.Convert(
            new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local), typeof(string), null,
            CultureInfo.GetCultureInfo(culture));

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var sut = new DateConverter();
        
        sut.Invoking(x => x.ConvertBack(null, typeof(DateTime), null, CultureInfo.CurrentCulture)).Should()
            .Throw<NotSupportedException>();
    }
}
