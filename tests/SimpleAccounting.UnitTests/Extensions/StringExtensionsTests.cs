// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Extensions
{
    using System.Drawing;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Extensions;
    using Xunit;

    public class StringExtensionsTests
    {
        private readonly Font testFont = new Font("Arial", 10);
        private const double PrintFactor = 100 / 25.4;

        [Fact]
        public void Wrap_EmptyString_InputReturnedUnchanged()
        {
            var result = string.Empty.Wrap(50, this.testFont, PrintFactor);

            result.Should().BeEmpty();
        }
        
        [Fact]
        public void Wrap_InsufficientSpace_InputReturnedUnchanged()
        {
            var result = "1".Wrap(0, this.testFont, PrintFactor);

            result.Should().Be("1");
        }
        
        [Theory]
        [InlineData("A", 50)]
        [InlineData("A123", 50)]
        [InlineData("A123456789B123456789C123456789D123456789", 200)]
        public void Wrap_ShortString_InputReturnedUnchanged(string input, int maxWidth)
        {
            var result = input.Wrap(maxWidth, this.testFont, PrintFactor);

            result.Should().Be(input);
        }

        [Theory]
        [InlineData("A123456789-B123456789", "A123456789-\nB123456789")]
        public void Wrap_LongStringWithSeparators_WrappedAtSeparator(string input, string expected)
        {
            var result = input.Wrap(25, this.testFont, PrintFactor);

            result.Should().Be(expected);
        }

        // [Theory]
        // [InlineData("A123456789B123456789", "A123456789\nB123456789")]
        // [InlineData("A123456789B123456789C", "A123456789\nB123456789\nC")]
        // [InlineData("A123456789B123456789C123456789", "A123456789\nB123456789\nC123456789")]
        // [InlineData("A123456789B123456789C123456789D", "A123456789\nB123456789\nC123456789\nD")]
        // [InlineData("A123456789B123456789C123456789D123456789", "A123456789\nB123456789\nC123456789\nD123456789")]
        // [InlineData("A123456789B123456789C123456789D123456789E", "A123456789\nB123456789\nC123456789\nD123456789\nE")]
        // public void Wrap_LongString_WrappedExactly(string input, string expected)
        // {
        //     var result = input.Wrap(24, this.testFont, PrintFactor);
        //
        //     result.Should().Be(expected);
        // }
    }
}
