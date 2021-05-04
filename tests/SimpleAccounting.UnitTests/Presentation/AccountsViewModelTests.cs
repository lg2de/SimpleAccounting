// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System.Collections.Generic;
    using System.Linq;
    using Caliburn.Micro;
    using FluentAssertions;
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
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            var sut = new AccountsViewModel(windowManager, projectData);
            projectData.Storage.Accounts = new List<AccountingDataAccountGroup>
            {
                new AccountingDataAccountGroup
                {
                    Name = "Group",
                    Account = new List<AccountDefinition>
                    {
                        new AccountDefinition { ID = 1, Name = "No import mapping", ImportMapping = null },
                        new AccountDefinition
                        {
                            ID = 2,
                            Name = "Simple import mapping",
                            ImportMapping = Samples.SimpleImportConfiguration
                        },
                        new AccountDefinition
                        {
                            ID = 3,
                            Name = "Incomplete import mapping",
                            ImportMapping = new AccountDefinitionImportMapping()
                        },
                        new AccountDefinition
                        {
                            ID = 4,
                            Name = "Full import mapping",
                            ImportMapping = Samples.SimpleImportConfiguration
                        }
                    }
                }
            };
            projectData.Storage.Accounts.Last().Account.Last().ImportMapping.Patterns =
                new List<AccountDefinitionImportMappingPattern>
                {
                    new AccountDefinitionImportMappingPattern { Expression = "OnlyAccount", AccountID = 1 },
                    new AccountDefinitionImportMappingPattern
                    {
                        Expression = "ValueSpecified", ValueSpecified = true, AccountID = 2
                    },
                    new AccountDefinitionImportMappingPattern
                    {
                        Expression = "ValueSet", Value = 5, ValueSpecified = true, AccountID = 3
                    }
                };

            sut.OnDataLoaded();

            var group = projectData.Storage.Accounts.Last();
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
        public void OnEditAccount_ImportPatternsConfigured_ImportPatternsBuilt()
        {
            var windowManager = Substitute.For<IWindowManager>();
            AccountViewModel updatedViewModel = null;
            windowManager.ShowDialog(Arg.Do<object>(o => updatedViewModel = o as AccountViewModel), null, null);
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            var sut = new AccountsViewModel(windowManager, projectData);
            projectData.Storage.Accounts = new List<AccountingDataAccountGroup>
            {
                new AccountingDataAccountGroup
                {
                    Name = "Group",
                    Account = new List<AccountDefinition>
                    {
                        new AccountDefinition
                        {
                            ID = 1, Name = "Asset", Type = AccountDefinitionType.Asset
                        },
                        new AccountDefinition
                        {
                            ID = 2, Name = "Income", Type = AccountDefinitionType.Income
                        },
                        new AccountDefinition
                        {
                            ID = 3, Name = "CarryForward", Type = AccountDefinitionType.Carryforward
                        }
                    }
                }
            };
            projectData.Storage.Accounts.First().Account.First().ImportMapping = new AccountDefinitionImportMapping
            {
                Patterns = new List<AccountDefinitionImportMappingPattern>
                {
                    new AccountDefinitionImportMappingPattern { Expression = "Expression", AccountID = 2 }
                }
            };
            sut.OnDataLoaded();

            var accountViewModel = sut.AccountList.First();
            accountViewModel.ImportRemoteAccounts.Should().BeEmpty();
            sut.OnEditAccount(accountViewModel);

            updatedViewModel?.ImportRemoteAccounts.Should().BeEquivalentTo(new { ID = 2 });
        }
    }
}
