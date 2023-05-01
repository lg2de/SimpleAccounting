// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Globalization;
using FluentAssertions;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class NullableValueConverterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("X")]
    public void Convert_AllValues_JustReturned(string value)
    {
        var sut = new NullableValueConverter();

        var result = sut.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(value);
    }
        
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("X", "X")]
    public void ConvertBack_InputValues_NullForEmpty(string input, string expected)
    {
        var sut = new NullableValueConverter();

        var result = sut.ConvertBack(input, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(expected);
    }
}
