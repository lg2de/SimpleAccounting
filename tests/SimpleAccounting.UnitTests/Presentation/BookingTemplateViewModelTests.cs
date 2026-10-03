// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.ComponentModel;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class BookingTemplateViewModelTests
{
    [Fact]
    public void IsValid_TextAndDifferentAccounts_True()
    {
        var sut = new BookingTemplateViewModel
        {
            Text = "Fee", DebitAccount = new AccountDefinition { ID = 1 }, CreditAccount = new AccountDefinition { ID = 2 }
        };

        sut.IsValid.Should().BeTrue();
        ((IDataErrorInfo)sut).Error.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_TextWithoutAccounts_True()
    {
        var sut = new BookingTemplateViewModel { Text = "Fee" };

        sut.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_EmptyText_False()
    {
        var sut = new BookingTemplateViewModel { Text = " " };

        using var _ = new AssertionScope();
        sut.IsValid.Should().BeFalse();
        ((IDataErrorInfo)sut)[nameof(sut.Text)].Should().NotBeEmpty();
        ((IDataErrorInfo)sut)[nameof(sut.DebitAccount)].Should().BeEmpty();
    }

    [Fact]
    public void IsValid_SameAccounts_False()
    {
        var account = new AccountDefinition { ID = 1 };
        var sut = new BookingTemplateViewModel { Text = "Fee", DebitAccount = account, CreditAccount = account };

        using var _ = new AssertionScope();
        sut.IsValid.Should().BeFalse();
        ((IDataErrorInfo)sut)[nameof(sut.Text)].Should().BeEmpty();
        ((IDataErrorInfo)sut)[nameof(sut.DebitAccount)].Should().NotBeEmpty();
        ((IDataErrorInfo)sut)[nameof(sut.CreditAccount)].Should().NotBeEmpty();
    }

    [Fact]
    public void DebitAccount_Changed_BothAccountsNotified()
    {
        var sut = new BookingTemplateViewModel();
        using var monitor = sut.Monitor();

        sut.DebitAccount = new AccountDefinition { ID = 1 };

        using var _ = new AssertionScope();
        monitor.Should().RaisePropertyChangeFor(x => x.DebitAccount);
        monitor.Should().RaisePropertyChangeFor(x => x.CreditAccount);
    }
}
