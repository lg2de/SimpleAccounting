// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Presentation;
    using Xunit;

    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class DesignViewModelTests
    {
        [Fact]
        public void ShellDesignViewModel_ConstructorSucceed()
        {
            Action action = () => new ShellDesignViewModel();
            action.Should().NotThrow();
        }

        [Fact]
        public void AccountDesignViewModel_ConstructorSucceed()
        {
            Action action = () => new AccountDesignViewModel();
            action.Should().NotThrow();
        }

        [Fact]
        public void AddBookingDesignViewModel_ConstructorSucceed()
        {
            Action action = () => new AddBookingDesignViewModel();
            action.Should().NotThrow();
        }

        [Fact]
        public void ImportBookingsDesignViewModel_ConstructorSucceed()
        {
            Action action = () => new ImportBookingsDesignViewModel();
            action.Should().NotThrow();
        }
    }
}