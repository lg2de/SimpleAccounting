// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Linq;
using FluentAssertions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class ImportEntryViewModelTests
{
    [Fact]
    public void ResetRemoteAccountCommand_NoRemoteAccount_CannotExecute()
    {
        var sut = new ImportEntryViewModel(Enumerable.Empty<AccountDefinition>()) { RemoteAccount = null };

        sut.ResetRemoteAccountCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ResetRemoteAccountCommand_RemoteAccount_CanExecute()
    {
        var sut = new ImportEntryViewModel(Enumerable.Empty<AccountDefinition>())
        {
            RemoteAccount = new AccountDefinition()
        };

        sut.ResetRemoteAccountCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ResetRemoteAccountCommand_RemoteAccount_RemoteAccountReset()
    {
        var sut = new ImportEntryViewModel(Enumerable.Empty<AccountDefinition>())
        {
            RemoteAccount = new AccountDefinition()
        };

        sut.ResetRemoteAccountCommand.Execute(null);

        sut.RemoteAccount.Should().BeNull();
    }
}
