// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class CloseYearViewModelTests
{
    [Fact]
    public void CloseYearCommand_AccountSelected_CanExecute()
    {
        var sut = new CloseYearViewModel(new AccountingDataJournal());
        sut.RemoteAccount = new AccountDefinition();

        sut.CloseYearCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CloseYearCommand_NoAccountSelected_CannotExecute()
    {
        var sut = new CloseYearViewModel(new AccountingDataJournal());

        sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task OnInitialize_AccountsAvailable_AccountSelected()
    {
        var sut = new CloseYearViewModel(new AccountingDataJournal());
        sut.Accounts.Add(new AccountDefinition { ID = 1, Name = "CF", Type = AccountDefinitionType.Carryforward });

        await ((IActivate)sut).ActivateAsync(TestContext.Current.CancellationToken);

        sut.RemoteAccount.Should().Be(sut.Accounts.Single());
    }

    [Fact]
    public async Task OnInitialize_NoAccountAvailable_NoAccountSelected()
    {
        var sut = new CloseYearViewModel(new AccountingDataJournal());

        await ((IActivate)sut).ActivateAsync(TestContext.Current.CancellationToken);

        sut.RemoteAccount.Should().BeNull();
    }
}
