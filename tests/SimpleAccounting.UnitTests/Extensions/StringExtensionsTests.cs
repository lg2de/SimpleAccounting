// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Extensions
{
    using FluentAssertions;
    using lg2de.SimpleAccounting.Extensions;
    using Xunit;

    public class StringExtensionsTests
    {
        [Fact]
        public void Wrap_EmptyString_InputReturnedUnchanged()
        {
            var result = string.Empty.Wrap(50);

            result.Should().BeEmpty();
        }
        
        [Theory]
        [InlineData("A", 50)]
        [InlineData("A123", 50)]
        [InlineData("A123456789B123456789C123456789D123456789", 200)]
        public void Wrap_ShortString_InputReturnedUnchanged(string input, int maxWidth)
        {
            var result = input.Wrap(maxWidth);

            result.Should().Be(input);
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
            var result = input.Wrap(19);

            result.Should().Be(expected);
        }
    }
}
