// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using NSubstitute;
using Xunit;

public class AccountsViewModelTests
{
    [Fact]
    public void OnDataLoaded_DifferentImportConfigurations_ViewModelsBuildCorrect()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition { ID = 1, Name = "No import mapping", ImportMapping = null },
                    new AccountDefinition
                    {
                        ID = 2, Name = "Simple import mapping", ImportMapping = Samples.SimpleImportConfiguration
                    },

                    new AccountDefinition
                    {
                        ID = 3,
                        Name = "Incomplete import mapping",
                        ImportMapping = new AccountDefinitionImportMapping()
                    },

                    new AccountDefinition
                    {
                        ID = 4, Name = "Full import mapping", ImportMapping = Samples.SimpleImportConfiguration
                    }
                ]
            }
        ];
        projectData.Storage.Accounts[^1].Account[^1].ImportMapping.Patterns =
        [
            new AccountDefinitionImportMappingPattern { Expression = "OnlyAccount", AccountID = 1 },
            new AccountDefinitionImportMappingPattern
            {
                Expression = "ValueSpecified", ValueSpecified = true, AccountID = 2
            },

            new AccountDefinitionImportMappingPattern
            {
                Expression = "ValueSet", Value = 5, ValueSpecified = true, AccountID = 3
            }
        ];

        sut.OnDataLoaded();

        var group = projectData.Storage.Accounts[^1];
        sut.AccountList.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 1,
                    Name = "No import mapping",
                    Group = group,
                    Groups = projectData.Storage.Accounts
                },
                new
                {
                    Identifier = 2,
                    Name = "Simple import mapping",
                    IsImportActive = true,
                    ImportDateSource = "Date",
                    ImportValueSource = "Value",
                    ImportNameSource = (string)null,
                    ImportTextSource = (string)null
                },
                new
                {
                    Identifier = 3,
                    Name = "Incomplete import mapping",
                    IsImportActive = true,
                    ImportDateSource = (string)null,
                    ImportValueSource = (string)null
                },
                new
                {
                    Identifier = 4,
                    Name = "Full import mapping",
                    IsImportActive = true,
                    ImportPatterns = new object[]
                    {
                        new { AccountId = 1, Expression = "OnlyAccount", Value = (double?)null },
                        new { AccountId = 2, Expression = "ValueSpecified", Value = 0.0 },
                        new { AccountId = 3, Expression = "ValueSet", Value = 0.05 }
                    }
                }
            }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task OnEditAccount_ImportPatternsConfigured_ImportPatternsBuilt()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        AccountViewModel updatedViewModel = null;
        await windowManager.ShowDialogAsync(Arg.Do<object>(o => updatedViewModel = o as AccountViewModel));
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition { ID = 1, Name = "Asset", Type = AccountDefinitionType.Asset },
                    new AccountDefinition { ID = 2, Name = "Income", Type = AccountDefinitionType.Income },
                    new AccountDefinition { ID = 3, Name = "CarryForward", Type = AccountDefinitionType.Carryforward }
                ]
            }
        ];
        projectData.Storage.Accounts[0].Account[0].ImportMapping = new AccountDefinitionImportMapping
        {
            Patterns = [new AccountDefinitionImportMappingPattern { Expression = "Expression", AccountID = 2 }]
        };
        sut.OnDataLoaded();

        var accountViewModel = sut.AccountList[0];
        accountViewModel.ImportRemoteAccounts.Should().BeEmpty();
        await sut.OnEditAccountAsync(accountViewModel);

#pragma warning disable FAA0001 // false-positive
        updatedViewModel?.ImportRemoteAccounts.Should().BeEquivalentTo([new { ID = 2 }]);
#pragma warning restore FAA0001
    }

    [Fact]
    public async Task OnEditAccount_IdChanged_AccountGroupReordered()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        windowManager.ShowDialogAsync(Arg.Do<object>(o => ((AccountViewModel)o).Identifier = 5)).Returns(true);
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition { ID = 1, Name = "Asset", Type = AccountDefinitionType.Asset },
                    new AccountDefinition { ID = 2, Name = "Income", Type = AccountDefinitionType.Income },
                    new AccountDefinition { ID = 3, Name = "CarryForward", Type = AccountDefinitionType.Carryforward }
                ]
            }
        ];
        sut.OnDataLoaded();

        var accountViewModel = sut.AccountList[1];
        await sut.OnEditAccountAsync(accountViewModel);

        projectData.Storage.Accounts.SelectMany(g => g.Account.Select(a => a.ID))
            .Should().Equal(1, 3, 5);
    }

    [Fact]
    public async Task OnEditAccount_IdChanged_BookingTemplatesUpdated()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition { ID = 100, Name = "Asset", Type = AccountDefinitionType.Asset },
                    new AccountDefinition { ID = 200, Name = "Income", Type = AccountDefinitionType.Income }
                ]
            }
        ];
        projectData.Storage.Setup.BookingTemplates = new AccountingDataSetupBookingTemplates
        {
            Template =
            [
                new AccountingDataSetupBookingTemplatesTemplate { Text = "Debit", Debit = 200, Credit = 100 },
                new AccountingDataSetupBookingTemplatesTemplate { Text = "Credit", Debit = 100, Credit = 200 },
                new AccountingDataSetupBookingTemplatesTemplate { Text = "Other", Debit = 100 }
            ]
        };
        sut.OnDataLoaded();
        windowManager
            .ShowDialogAsync(Arg.Do<object>(o => ((AccountViewModel)o).Identifier = 101))
            .Returns(true);

        await sut.OnEditAccountAsync(sut.AccountList.Single(x => x.Identifier == 100));

        projectData.Storage.Setup.BookingTemplates.Template.Should().BeEquivalentTo(
            new object[]
            {
                new { Text = "Debit", Debit = 200, Credit = 101 },
                new { Text = "Credit", Debit = 101, Credit = 200 },
                new { Text = "Other", Debit = 101, Credit = 0 }
            }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task OnEditAccount_IdChanged_ImportPatternsUpdated()
    {
        var windowManager = Substitute.For<IWindowManager>();
        ulong? newIdentifier = null;
        windowManager.ShowDialogAsync(
                Arg.Do<object>(o =>
                {
                    if (newIdentifier.HasValue)
                    {
                        ((AccountViewModel)o).Identifier = newIdentifier.Value;
                    }
                }))
            .Returns(true);
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition
                    {
                        ID = 1,
                        Name = "Bank",
                        Type = AccountDefinitionType.Asset,
                        ImportMapping = Samples.SimpleImportConfiguration
                    },
                    new AccountDefinition { ID = 2, Name = "Income", Type = AccountDefinitionType.Income },
                    new AccountDefinition { ID = 3, Name = "Expense", Type = AccountDefinitionType.Expense }
                ]
            }
        ];
        projectData.Storage.Accounts[0].Account[0].ImportMapping.Patterns =
        [
            new AccountDefinitionImportMappingPattern { Expression = "Salary", AccountID = 2 },
            new AccountDefinitionImportMappingPattern { Expression = "Shop", AccountID = 3 }
        ];
        sut.OnDataLoaded();

        // change identifier of income account
        newIdentifier = 5;
        await sut.OnEditAccountAsync(sut.AccountList.Single(x => x.Identifier == 2));

        projectData.Storage.Accounts[0].Account.Single(x => x.ID == 1).ImportMapping.Patterns
            .Select(x => x.AccountID).Should().Equal(5UL, 3UL);

        // edit bank account unchanged, import configuration must be kept
        newIdentifier = null;
        await sut.OnEditAccountAsync(sut.AccountList.Single(x => x.Identifier == 1));

        projectData.Storage.Accounts[0].Account.Single(x => x.ID == 1).ImportMapping.Patterns
            .Select(x => x.AccountID).Should().Equal(5UL, 3UL);
    }

    [Fact]
    public async Task OnEditAccount_IdChanged_LastCarryForwardUpdated()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        windowManager.ShowDialogAsync(Arg.Do<object>(o => ((AccountViewModel)o).Identifier = 999)).Returns(true);
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition { ID = 1, Name = "Asset", Type = AccountDefinitionType.Asset },
                    new AccountDefinition { ID = 990, Name = "CarryForward", Type = AccountDefinitionType.Carryforward }
                ]
            }
        ];
        projectData.Storage.Setup.Behavior.LastCarryForward = 990;
        projectData.Storage.Setup.Behavior.LastCarryForwardSpecified = true;
        sut.OnDataLoaded();

        await sut.OnEditAccountAsync(sut.AccountList.Single(x => x.Identifier == 990));

        projectData.Storage.Setup.Behavior.LastCarryForward.Should().Be(999);
    }

    [Fact]
    public async Task OnEditAccount_OtherIdChanged_LastCarryForwardUnchanged()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        windowManager.ShowDialogAsync(Arg.Do<object>(o => ((AccountViewModel)o).Identifier = 5)).Returns(true);
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group",
                Account =
                [
                    new AccountDefinition { ID = 1, Name = "Asset", Type = AccountDefinitionType.Asset },
                    new AccountDefinition { ID = 990, Name = "CarryForward", Type = AccountDefinitionType.Carryforward }
                ]
            }
        ];
        projectData.Storage.Setup.Behavior.LastCarryForward = 990;
        projectData.Storage.Setup.Behavior.LastCarryForwardSpecified = true;
        sut.OnDataLoaded();

        await sut.OnEditAccountAsync(sut.AccountList.Single(x => x.Identifier == 1));

        projectData.Storage.Setup.Behavior.LastCarryForward.Should().Be(990);
    }

    [Fact]
    public async Task OnEditAccount_GroupChanged_StorageUpdated()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var clock = Substitute.For<IClock>();
        windowManager.ShowDialogAsync(
            Arg.Do<object>(
                o =>
                {
                    var vm = ((AccountViewModel)o);
                    vm.Group = vm.Groups.ToArray()[1];
                })).Returns(true);
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        var sut = new AccountsViewModel(windowManager, projectData);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Group1",
                Account =
                [
                    new AccountDefinition { ID = 1, Name = "Asset", Type = AccountDefinitionType.Asset },
                    new AccountDefinition { ID = 2, Name = "Income", Type = AccountDefinitionType.Income }
                ]
            },
            new AccountingDataAccountGroup
            {
                Name = "Group2",
                Account =
                [
                    new AccountDefinition { ID = 3, Name = "CarryForward", Type = AccountDefinitionType.Carryforward }
                ]
            }
        ];
        sut.OnDataLoaded();

        var accountViewModel = sut.AccountList[1];
        await sut.OnEditAccountAsync(accountViewModel);

        using var _ = new AssertionScope();
        projectData.Storage.Accounts[0].Account.Should().BeEquivalentTo([new { ID = 1 }]);
        projectData.Storage.Accounts[1].Account.Should().BeEquivalentTo([new { ID = 2 }, new { ID = 3 }]);
    }
}
