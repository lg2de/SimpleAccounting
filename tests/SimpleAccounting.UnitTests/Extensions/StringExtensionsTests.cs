// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Extensions;

using System;
using System.Drawing;
using FluentAssertions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using NSubstitute;
using Xunit;

public sealed class StringExtensionsTests : IDisposable
{
    private readonly Font testFont = new("Arial", 10);

    [Fact]
    public void Wrap_EmptyString_InputReturnedUnchanged()
    {
        var graphics = Substitute.For<IGraphics>();
        var result = string.Empty.Wrap(50, this.testFont, graphics);

        result.Should().BeEmpty();
    }
        
    [Theory]
    [InlineData("1", "1")]
    [InlineData("12", "1\n2")]
    public void Wrap_InsufficientSpace_WrappedWithOneCharacter(string input, string expected)
    {
        var graphics = Substitute.For<IGraphics>();
        graphics.MeasureString(Arg.Any<string>(), Arg.Any<Font>())
            .Returns(call => new SizeF(call.Arg<string>().Length, 0));
        var result = input.Wrap(0, this.testFont, graphics);

        result.Should().Be(expected);
    }
        
    [Theory]
    [InlineData("A", 50)]
    [InlineData("A123", 50)]
    [InlineData("A123456789B123456789C123456789D123456789", 200)]
    public void Wrap_ShortString_InputReturnedUnchanged(string input, int maxWidth)
    {
        var graphics = Substitute.For<IGraphics>();
        var result = input.Wrap(maxWidth, this.testFont, graphics);

        result.Should().Be(input);
    }

    [Theory]
    [InlineData("A123456789-B123456789", "A123456789-\nB123456789")]
    [InlineData("A123456789-B123456789-C123456789", "A123456789-\nB123456789-\nC123456789")]
    public void Wrap_LongStringWithSeparators_WrappedAtSeparator(string input, string expected)
    {
        var graphics = Substitute.For<IGraphics>();
        graphics.MeasureString(Arg.Any<string>(), Arg.Any<Font>())
            .Returns(call => new SizeF(call.Arg<string>().Length, 0));
        var result = input.Wrap(15, this.testFont, graphics);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("A123456789B123456789", "A123456789\nB123456789")]
    [InlineData("A123456789B123456789C", "A123456789\nB123456789\nC")]
    [InlineData("A123456789B123456789C123456789", "A123456789\nB123456789\nC123456789")]
    [InlineData("A123456789B123456789C123456789D", "A123456789\nB123456789\nC123456789\nD")]
    [InlineData("A123456789B123456789C123456789D123456789", "A123456789\nB123456789\nC123456789\nD123456789")]
    [InlineData("A123456789B123456789C123456789D123456789E", "A123456789\nB123456789\nC123456789\nD123456789\nE")]
    public void Wrap_LongString_WrappedExactly(string input, string expected)
    {
        var graphics = Substitute.For<IGraphics>();
        graphics.MeasureString(Arg.Any<string>(), Arg.Any<Font>())
            .Returns(call => new SizeF(call.Arg<string>().Length, 0));
        var result = input.Wrap(10, this.testFont, graphics);
        
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null, 0)]
    [InlineData(null, "", 0)]
    [InlineData("", "", 0)]
    [InlineData("A", "A", 0)]
    [InlineData("A", "AA", 1)]
    [InlineData("A", "B", 1)]
    [InlineData("A", "BB", 2)]
    public void LevenshteinDistance_Samples_ResultValidated(string input, string other, int expected)
    {
        var result = input.LevenshteinDistance(other);
        
        result.Should().Be(expected);
    }

    public void Dispose()
    {
        this.testFont?.Dispose();
    }
}
