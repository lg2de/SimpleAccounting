// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

[SuppressMessage("ReSharper", "ObjectCreationAsStatement", Justification = "These tests are better readable that way.")]
[SuppressMessage(
    "Performance", "CA1806:Do not ignore method results", Justification = "We are testing the constructor only.")]
public class DesignViewModelTests
{
    [Fact]
    public void AccountDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new AccountDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void AddBookingDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new EditBookingDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void BusyDesignControlModel_ConstructorSucceed()
    {
        Action action = () => new BusyDesignControlModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void CloseYearDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new CloseYearDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void ErrorMessageDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new ErrorMessageDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void ImportBookingsDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new ImportBookingsDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void ProjectOptionsDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new ProjectOptionsDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void ShellDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new ShellDesignViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void UpdateOptionsDesignViewModel_ConstructorSucceed()
    {
        Action action = () => new UpdateOptionsDesignViewModel();
        action.Should().NotThrow();
    }
}
