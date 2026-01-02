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

        updatedViewModel?.ImportRemoteAccounts.Should().BeEquivalentTo(new[] { new { ID = 2 } });
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
        projectData.Storage.Accounts[0].Account.Should().BeEquivalentTo(new[] { new { ID = 1 } });
        projectData.Storage.Accounts[1].Account.Should().BeEquivalentTo(new[] { new { ID = 2 }, new { ID = 3 } });
    }
}
