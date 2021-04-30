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
        public void LoadAccounts_DifferentImportConfigurations_ViewModelsBuildCorrect()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            var sut = new AccountsViewModel(windowManager, projectData);
            var accounts = new List<AccountingDataAccountGroup>
            {
                new AccountingDataAccountGroup
                {
                    Name = "Group",
                    Account = new List<AccountDefinition>
                    {
                        new AccountDefinition { ID = 1, Name = "1", ImportMapping = null },
                        new AccountDefinition
                        {
                            ID = 2, Name = "2", ImportMapping = Samples.SimpleImportConfiguration
                        },
                        new AccountDefinition { ID = 3, Name = "3", ImportMapping = Samples.SimpleImportConfiguration }
                    }
                }
            };
            accounts.Last().Account.Last().ImportMapping.Patterns = new List<AccountDefinitionImportMappingPattern>
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

            sut.LoadAccounts(accounts);

            var group = accounts.Last();
            sut.AccountList.Should().BeEquivalentTo(
                new object[]
                {
                    new { Identifier = 1, Name = "1", Group = group, Groups = accounts },
                    new
                    {
                        Identifier = 2,
                        Name = "2",
                        IsImportActive = true,
                        ImportDateSource = "Date",
                        ImportValueSource = "Value"
                    },
                    new
                    {
                        Identifier = 3,
                        Name = "3",
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
    }
}
