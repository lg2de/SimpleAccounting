// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Globalization;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class InverseBooleanConverterTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_AllPossibleInputs_Converted(bool input, bool expected)
    {
        var sut = new InverseBooleanConverter();

        var result = sut.Convert(input, typeof(bool), 0, CultureInfo.InvariantCulture);

        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_WrongDataType_ExceptionThrown()
    {
        var sut = new InverseBooleanConverter();

        sut.Invoking(x => x.Convert(true, typeof(int), 0, CultureInfo.InvariantCulture)).Should()
            .Throw<ArgumentException>();
    }
}
