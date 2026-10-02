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

public class ProjectOptionsViewModelTests
{
    private static AccountingData CreateData()
    {
        var data = new AccountingData
        {
            Setup =
            {
                Name = "My Club",
                Location = "Hometown",
                Reports =
                {
                    AccountJournalReport =
                        new AccountingDataSetupReportsAccountJournalReport
                        {
                            PageBreakBetweenAccounts = true
                        },
                    TotalsAndBalancesReport = ["Treasurer", "Auditor"]
                }
            }
        };
        return data;
    }

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

    [Fact]
    public void Ctor_ExistingSetup_AllValuesLoaded()
    {
        var data = CreateData();

        var sut = new ProjectOptionsViewModel(data);

        using var _ = new AssertionScope();
        sut.Organization.Should().Be("My Club");
        sut.Location.Should().Be("Hometown");
        sut.Currency.Should().Be("€");
        sut.PageBreakBetweenAccounts.Should().BeTrue();
        sut.Signatures.Select(x => x.Text).Should().Equal("Treasurer", "Auditor");
    }

    [Fact]
    public void Ctor_MissingOptionalValues_DefaultsLoaded()
    {
        var data = new AccountingData { Setup = { Name = null, Location = null, Reports = null } };

        var sut = new ProjectOptionsViewModel(data);

        using var _ = new AssertionScope();
        sut.Organization.Should().BeEmpty();
        sut.Location.Should().BeEmpty();
        sut.PageBreakBetweenAccounts.Should().BeFalse();
        sut.Signatures.Should().BeEmpty();
    }

    [Fact]
    public void OnSave_ExistingSetupUnchanged_ReturnsFalse()
    {
        var data = CreateData();
        var sut = new ProjectOptionsViewModel(data);

        sut.OnSave().Should().BeFalse();
    }

    [Fact]
    public void OnSave_MissingOptionalValuesUnchanged_ReturnsFalse()
    {
        var data = new AccountingData { Setup = { Name = null, Location = null, Reports = null } };
        var sut = new ProjectOptionsViewModel(data);

        sut.OnSave().Should().BeFalse();

        using var _ = new AssertionScope();
        data.Setup.Reports.AccountJournalReport.Should().BeNull();
        data.Setup.Reports.TotalsAndBalancesReport.Should().BeNull();
    }

    [Fact]
    public void OnSave_GeneralChanged_ValuesTrimmedAndStored()
    {
        var data = CreateData();
        var sut = new ProjectOptionsViewModel(data) { Organization = " New Club ", Location = " New Town " };

        sut.OnSave().Should().BeTrue();

        using var _ = new AssertionScope();
        data.Setup.Name.Should().Be("New Club");
        data.Setup.Location.Should().Be("New Town");
    }

    [Fact]
    public void OnSave_PageBreakActivated_ReportSettingCreated()
    {
        var data = new AccountingData();
        var sut = new ProjectOptionsViewModel(data) { PageBreakBetweenAccounts = true };

        sut.OnSave().Should().BeTrue();

        data.Setup.Reports.AccountJournalReport.PageBreakBetweenAccounts.Should().BeTrue();
    }

    [Fact]
    public void OnSave_SignaturesChanged_EmptyLinesRemoved()
    {
        var data = CreateData();
        var sut = new ProjectOptionsViewModel(data);
        sut.Signatures[0].Text = " Chairman ";
        sut.Signatures.Add(new SignatureViewModel { Text = "  " });
        sut.Signatures.Add(new SignatureViewModel { Text = "Treasurer" });

        sut.OnSave().Should().BeTrue();

        data.Setup.Reports.TotalsAndBalancesReport.Should().Equal("Chairman", "Auditor", "Treasurer");
    }

    [Fact]
    public void OnSave_AllSignaturesRemoved_ReportSettingRemoved()
    {
        var data = CreateData();
        var sut = new ProjectOptionsViewModel(data);
        sut.Signatures.Clear();

        sut.OnSave().Should().BeTrue();

        data.Setup.Reports.TotalsAndBalancesReport.Should().BeNull();
    }

    [Fact]
    public void MoveSignatureUpCommand_SecondSignature_Moved()
    {
        var sut = new ProjectOptionsViewModel(CreateData());

        sut.MoveSignatureUpCommand.Execute(sut.Signatures[1]);

        sut.Signatures.Select(x => x.Text).Should().Equal("Auditor", "Treasurer");
    }

    [Fact]
    public void MoveSignatureUpCommand_FirstSignature_Unchanged()
    {
        var sut = new ProjectOptionsViewModel(CreateData());

        sut.MoveSignatureUpCommand.Execute(sut.Signatures[0]);

        sut.Signatures.Select(x => x.Text).Should().Equal("Treasurer", "Auditor");
    }

    [Fact]
    public void MoveSignatureDownCommand_FirstSignature_Moved()
    {
        var sut = new ProjectOptionsViewModel(CreateData());

        sut.MoveSignatureDownCommand.Execute(sut.Signatures[0]);

        sut.Signatures.Select(x => x.Text).Should().Equal("Auditor", "Treasurer");
    }

    [Fact]
    public void MoveSignatureDownCommand_LastSignature_Unchanged()
    {
        var sut = new ProjectOptionsViewModel(CreateData());

        sut.MoveSignatureDownCommand.Execute(sut.Signatures[^1]);

        sut.Signatures.Select(x => x.Text).Should().Equal("Treasurer", "Auditor");
    }

    [Fact]
    public void MoveSignatureDownCommand_NoSignature_Ignored()
    {
        var sut = new ProjectOptionsViewModel(CreateData());

        sut.MoveSignatureDownCommand.Execute(null);

        sut.Signatures.Select(x => x.Text).Should().Equal("Treasurer", "Auditor");
    }
}
