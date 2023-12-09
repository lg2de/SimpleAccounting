// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FluentAssertions;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class EditBookingAccountControlTests
{
    [WpfFact]
    public void SingleRowVisibility_Default_Visible()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = true };

        sut.SingleRowVisibility.Should().Be(Visibility.Visible);
    }

    [WpfFact]
    public void SplitRowsVisibility_Default_NotVisible()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = true };

        sut.SplitRowsVisibility.Should().NotBe(Visibility.Visible);
    }

    [WpfFact]
    public void SplitButtonVisibility_SplittingNotAllowed_NotVisible()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = false };

        sut.SplitButtonVisibility.Should().NotBe(Visibility.Visible);
    }

    [WpfFact]
    public void SplitButtonVisibility_SplittingAllowed_Visible()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = true };

        sut.SplitButtonVisibility.Should().Be(Visibility.Visible);
    }

    [WpfFact]
    public void SplitButtonVisibility_SplittingChanged_VisibilityChanged()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = false };
        using var monitor = sut.Monitor();

        sut.AllowSplitting = true;

        monitor.Should().RaisePropertyChangeFor(x => x.SplitButtonVisibility);
    }

    [WpfFact]
    public void AccountSelectionSpan_SplittingNotAllowed_TwoColumns()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = false };

        sut.AccountSelectionSpan.Should().Be(2);
    }

    [WpfFact]
    public void AccountSelectionSpan_SplittingAllowed_OneColumn()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = true };

        sut.AccountSelectionSpan.Should().Be(1);
    }

    [WpfFact]
    public void AccountSelectionSpan_SplittingChanged_Changed()
    {
        var sut = new EditBookingAccountControl(false) { AllowSplitting = false };
        using var monitor = sut.Monitor();

        sut.AllowSplitting = true;

        monitor.Should().RaisePropertyChangeFor(x => x.AccountSelectionSpan);
    }

    [WpfFact]
    public void SplitCommand_SplittingNotAllowed_CannotExecute()
    {
        var sut = new EditBookingAccountControl(false);

        sut.SplitCommand.CanExecute(null).Should().BeFalse();
    }

    [WpfFact]
    public void SplitCommand_Execute_NewSplitEntry()
    {
        var sut = new EditBookingAccountControl(false)
        {
            AllowSplitting = true,
            SplitEntries = new ObservableCollection<SplitBookingViewModel>()
        };
        using var monitor = sut.Monitor();

        sut.SplitCommand.Execute(null);

        sut.SplitEntries.Should().HaveCount(1);
        monitor.Should().RaisePropertyChangeFor(x => x.SingleRowVisibility);
        sut.SingleRowVisibility.Should().NotBe(Visibility.Visible);
        monitor.Should().RaisePropertyChangeFor(x => x.SplitRowsVisibility);
        sut.SplitRowsVisibility.Should().Be(Visibility.Visible);
    }

    [WpfFact]
    public void AddSplitEntryCommand_SplittingNotAllowed_CannotExecute()
    {
        var sut = new EditBookingAccountControl(false);

        sut.AddSplitEntryCommand.CanExecute(null).Should().BeFalse();
    }

    [WpfFact]
    public void AddSplitEntryCommand_ExecuteWithOneExisting_NewSplitEntry()
    {
        var sut = new EditBookingAccountControl(false)
        {
            AllowSplitting = true,
            SplitEntries = new ObservableCollection<SplitBookingViewModel> { new SplitBookingViewModel() }
        };

        sut.AddSplitEntryCommand.Execute(sut.SplitEntries.Single());

        sut.SplitEntries.Should().HaveCount(2);
    }

    [WpfFact]
    public void RemoveSplitEntryCommand_SplittingNotAllowed_CannotExecute()
    {
        var sut = new EditBookingAccountControl(false);

        sut.RemoveSplitEntryCommand.CanExecute(null).Should().BeFalse();
    }

    [WpfFact]
    public void RemoveSplitEntryCommand_ExecuteWithOneExisting_SingleViewReactivated()
    {
        var sut = new EditBookingAccountControl(false)
        {
            AllowSplitting = true,
            SplitEntries = new ObservableCollection<SplitBookingViewModel> { new SplitBookingViewModel() }
        };
        using var monitor = sut.Monitor();

        sut.RemoveSplitEntryCommand.Execute(sut.SplitEntries.Single());

        sut.SplitEntries.Should().HaveCount(0);
        monitor.Should().RaisePropertyChangeFor(x => x.SingleRowVisibility);
        sut.SingleRowVisibility.Should().Be(Visibility.Visible);
        monitor.Should().RaisePropertyChangeFor(x => x.SplitRowsVisibility);
        sut.SplitRowsVisibility.Should().NotBe(Visibility.Visible);
    }
}
