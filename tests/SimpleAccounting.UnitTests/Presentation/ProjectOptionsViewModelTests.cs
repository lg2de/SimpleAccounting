// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Threading.Tasks;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class ProjectOptionsViewModelTests
{
    [Fact]
    public async Task Activate_TitleSet()
    {
        var data = new AccountingData();
        var sut = new ProjectOptionsViewModel(data);

        await sut.As<IActivate>().ActivateAsync(TestContext.Current.CancellationToken);

        sut.DisplayName.Should().NotBe(sut.GetType().FullName);
    }

    [Fact]
    public void SaveCommand_WithCurrency_CanExecute()
    {
        var data = new AccountingData();
        var sut = new ProjectOptionsViewModel(data) { Currency = "$" };

        sut.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void SaveCommand_MissingCurrency_CannotExecute()
    {
        var data = new AccountingData();
        var sut = new ProjectOptionsViewModel(data) { Currency = "" };

        sut.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void OnSave_Unchanged_ReturnsFalse()
    {
        var data = new AccountingData();
        var sut = new ProjectOptionsViewModel(data);

        sut.OnSave().Should().BeFalse();
        data.Setup.Currency.Should().Be("€");
    }

    [Fact]
    public void OnSave_Changed_ReturnsTrue()
    {
        var data = new AccountingData();
        var sut = new ProjectOptionsViewModel(data) { Currency = "$" };

        sut.OnSave().Should().BeTrue();
        data.Setup.Currency.Should().Be("$");
    }
}
