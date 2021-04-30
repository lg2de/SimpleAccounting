// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Presentation;
    using Xunit;

    public class ImportPatternViewModelTests
    {
        [Fact]
        public void Expression_SetEmpty_Throws()
        {
            var sut = new ImportPatternViewModel();

            sut.Invoking(x => x.Expression = string.Empty).Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void Expression_SetInvalidRegex_Throws()
        {
            var sut = new ImportPatternViewModel();

            sut.Invoking(x => x.Expression = "(").Should().Throw<ArgumentException>();
        }
    }
}
