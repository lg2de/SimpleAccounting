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

    private static AccountingData CreateTemplateData()
    {
        var data = new AccountingData
        {
            Setup =
            {
                BookingTemplates = new AccountingDataSetupBookingTemplates
                {
                    Template =
                    [
                        new AccountingDataSetupBookingTemplatesTemplate
                        {
                            Text = "Fee",
                            Value = 5000,
                            ValueSpecified = true,
                            Debit = 100,
                            DebitSpecified = true,
                            Credit = 400,
                            CreditSpecified = true
                        },
                        new AccountingDataSetupBookingTemplatesTemplate
                        {
                            Text = "Old", Credit = 900, CreditSpecified = true
                        }
                    ]
                }
            },
            Accounts =
            [
                new AccountingDataAccountGroup
                {
                    Name = "Default",
                    Account =
                    [
                        new AccountDefinition { ID = 100, Name = "Bank", Type = AccountDefinitionType.Asset },
                        new AccountDefinition { ID = 400, Name = "Fees", Type = AccountDefinitionType.Income },
                        new AccountDefinition
                        {
                            ID = 800, Name = "Unused inactive", Type = AccountDefinitionType.Expense, Active = false
                        },
                        new AccountDefinition
                        {
                            ID = 900, Name = "Used inactive", Type = AccountDefinitionType.Expense, Active = false
                        },
                        new AccountDefinition
                        {
                            ID = 990, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
                        }
                    ]
                }
            ]
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

    [Fact]
    public void Ctor_BookingTemplates_TemplatesLoaded()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());

        sut.BookingTemplates.Should().BeEquivalentTo(
            new object[]
            {
                new { Text = "Fee", Value = 50.0, DebitAccount = new { ID = 100 }, CreditAccount = new { ID = 400 } },
                new { Text = "Old", Value = (double?)null, DebitAccount = new { ID = 0 }, CreditAccount = new { ID = 900 } }
            });
    }

    [Fact]
    public void Ctor_BookingTemplates_ActiveAndReferencedAccountsAvailable()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());

        sut.TemplateAccounts.Select(x => x.ID).Should().Equal(0UL, 100UL, 400UL, 900UL);
    }

    [Fact]
    public void OnSave_BookingTemplatesUnchanged_ReturnsFalse()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());

        sut.OnSave().Should().BeFalse();
    }

    [Fact]
    public void OnSave_BookingTemplateAdded_TemplateStored()
    {
        var data = CreateTemplateData();
        var sut = new ProjectOptionsViewModel(data);
        sut.BookingTemplates.Add(
            new BookingTemplateViewModel
            {
                Text = " New ", Value = 12.34, DebitAccount = sut.TemplateAccounts.Single(x => x.ID == 400)
            });

        sut.OnSave().Should().BeTrue();

        data.Setup.BookingTemplates.Template[^1].Should().BeEquivalentTo(
            new
            {
                Text = "New",
                Value = 1234,
                ValueSpecified = true,
                Debit = 400,
                DebitSpecified = true,
                Credit = 0,
                CreditSpecified = false
            });
    }

    [Fact]
    public void OnSave_BookingTemplateValueRemoved_ValueNotSpecified()
    {
        var data = CreateTemplateData();
        var sut = new ProjectOptionsViewModel(data);
        sut.BookingTemplates[0].Value = null;

        sut.OnSave().Should().BeTrue();

        data.Setup.BookingTemplates.Template[0].ValueSpecified.Should().BeFalse();
    }

    [Fact]
    public void OnSave_AllBookingTemplatesRemoved_TemplatesRemoved()
    {
        var data = CreateTemplateData();
        var sut = new ProjectOptionsViewModel(data);
        sut.BookingTemplates.Clear();

        sut.OnSave().Should().BeTrue();

        data.Setup.BookingTemplates.Should().BeNull();
    }

    [Fact]
    public void SaveCommand_BookingTemplateWithoutText_CannotExecute()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());
        sut.BookingTemplates.Add(new BookingTemplateViewModel { Text = " " });

        sut.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_BookingTemplateWithSameAccounts_CannotExecute()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());
        sut.BookingTemplates[0].CreditAccount = sut.BookingTemplates[0].DebitAccount;

        sut.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void MoveTemplateUpCommand_SecondTemplate_Moved()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());

        sut.MoveTemplateUpCommand.Execute(sut.BookingTemplates[1]);

        sut.BookingTemplates.Select(x => x.Text).Should().Equal("Old", "Fee");
    }

    [Fact]
    public void MoveTemplateDownCommand_LastTemplate_Unchanged()
    {
        var sut = new ProjectOptionsViewModel(CreateTemplateData());

        sut.MoveTemplateDownCommand.Execute(sut.BookingTemplates[^1]);

        sut.BookingTemplates.Select(x => x.Text).Should().Equal("Fee", "Old");
    }
}
