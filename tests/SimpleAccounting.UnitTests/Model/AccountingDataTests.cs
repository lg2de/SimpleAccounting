﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using Xunit;

    public class AccountingDataTests
    {
        [Fact]
        public void Migrate_DataWithEmptyProperties_EmptyPropertiesRemoved()
        {
            var sut = new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition { Name = "1" },
                            new AccountDefinition
                            {
                                Name = "2", ImportMapping = new AccountDefinitionImportMapping()
                            },
                            new AccountDefinition
                            {
                                Name = "3",
                                ImportMapping = new AccountDefinitionImportMapping
                                {
                                    Columns = new List<AccountDefinitionImportMappingColumn>(),
                                    Patterns = new List<AccountDefinitionImportMappingPattern>()
                                }
                            },
                            new AccountDefinition
                            {
                                Name = "4",
                                ImportMapping = new AccountDefinitionImportMapping
                                {
                                    Columns = new List<AccountDefinitionImportMappingColumn>
                                    {
                                        new AccountDefinitionImportMappingColumn
                                        {
                                            Source = "A"
                                        }
                                    },
                                    Patterns = new List<AccountDefinitionImportMappingPattern>
                                    {
                                        new AccountDefinitionImportMappingPattern
                                        {
                                            Expression = "A"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            sut.Migrate().Should().BeTrue();

            var expectation = new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition { Name = "1" },
                            new AccountDefinition { Name = "2" },
                            new AccountDefinition { Name = "3" },
                            new AccountDefinition
                            {
                                Name = "4",
                                ImportMapping = new AccountDefinitionImportMapping
                                {
                                    Columns = new List<AccountDefinitionImportMappingColumn>
                                    {
                                        new AccountDefinitionImportMappingColumn
                                        {
                                            Source = "A"
                                        }
                                    },
                                    Patterns = new List<AccountDefinitionImportMappingPattern>
                                    {
                                        new AccountDefinitionImportMappingPattern
                                        {
                                            Expression = "A"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            sut.Should().BeEquivalentTo(
                expectation,
                o => o.Excluding(
                    info => info.SelectedMemberPath.EndsWith(nameof(AccountDefinitionImportMappingPattern.Regex))));
        }

        [Fact]
        public void Migrate_Empty_Unchanged()
        {
            var sut = new AccountingData();

            sut.Migrate().Should().BeFalse("no (relevant) changes made");

            sut.Should().BeEquivalentTo(new AccountingData());
        }

        [Fact]
        public void Migrate_EmptyYears_YearsNodeRemoved()
        {
            var sut = new AccountingData { Years = new List<AccountingDataYear>() };

            sut.Migrate().Should().BeFalse("just removing the empty not is not a relevant change");

            sut.Should().BeEquivalentTo(new AccountingData());
            sut.Years.Should().BeNull();
        }

        [Fact]
        public void Migrate_ObsoleteYears_YearsMigratedToJournal()
        {
            var sut = new AccountingData
            {
                Years = new List<AccountingDataYear>
                {
                    new AccountingDataYear
                    {
                        Name = 2001, DateStart = 20010101, DateEnd = 20011231, Closed = true
                    },
                    new AccountingDataYear
                    {
                        Name = 2002, DateStart = 20020101, DateEnd = 20021231, Closed = false
                    }
                }
            };

            sut.Migrate().Should().BeTrue();

            var expectation = new
            {
                Journal = new[]
                {
                    new { Year = "2001", DateStart = 20010101, DateEnd = 20011231, Closed = true },
                    new { Year = "2002", DateStart = 20020101, DateEnd = 20021231, Closed = false }
                }
            };
            sut.Should().BeEquivalentTo(expectation);
        }

        [CulturedFact("en")]
        public void CloseYear_SampleDataEnglish_AllRelevantDataCorrect()
        {
            var sut = Samples.SampleProject;
            var currentYear = sut.Journal.Last();
            currentYear.Booking.AddRange(Samples.SampleBookings);
            var carryforwardAccount =
                sut.AllAccounts.First(x => x.Active && x.Type == AccountDefinitionType.Carryforward);

            sut.CloseYear(currentYear, carryforwardAccount, OpeningTextOption.Numbered);

            var newYear = sut.Journal.Last();
            newYear.Should().NotBeEquivalentTo(currentYear);
            currentYear.Closed.Should().BeTrue();
            newYear.Closed.Should().BeFalse();
            newYear.Booking.Should().BeEquivalentTo(
                new
                {
                    ID = 1,
                    Opening = true,
                    Credit = new[] { new { Account = Samples.Carryforward, Text = "Opening value 1", Value = 65100 } },
                    Debit = new[] { new { Account = Samples.BankAccount, Text = "Opening value 1", Value = 65100 } }
                },
                new
                {
                    ID = 2,
                    Opening = true,
                    Credit = new[] { new { Account = Samples.BankCredit, Text = "Opening value 2", Value = 260000 } },
                    Debit = new[] { new { Account = Samples.Carryforward, Text = "Opening value 2", Value = 260000 } }
                },
                new
                {
                    ID = 3,
                    Opening = true,
                    Credit = new[] { new { Account = Samples.Carryforward, Text = "Opening value 3", Value = 9900 } },
                    Debit = new[] { new { Account = Samples.FriendsDebit, Text = "Opening value 3", Value = 9900 } }
                });
        }

        [CulturedFact("de")]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void CloseYear_SampleDataGerman_TextCorrect()
        {
            var sut = Samples.SampleProject;
            var currentYear = sut.Journal.Last();
            currentYear.Booking.AddRange(Samples.SampleBookings);
            var carryforwardAccount =
                sut.AllAccounts.First(x => x.Active && x.Type == AccountDefinitionType.Carryforward);

            sut.CloseYear(currentYear, carryforwardAccount, OpeningTextOption.Numbered);

            var newYear = sut.Journal.Last();
            newYear.Should().NotBeEquivalentTo(currentYear);
            currentYear.Closed.Should().BeTrue();
            newYear.Closed.Should().BeFalse();
            newYear.Booking.Should().BeEquivalentTo(
                new
                {
                    ID = 1,
                    Credit = new[] { new { Account = 990, Text = "Eröffnungsbetrag 1" } },
                    Debit = new[] { new { Account = 100, Text = "Eröffnungsbetrag 1" } }
                },
                new
                {
                    ID = 2,
                    Credit = new[] { new { Account = 5000, Text = "Eröffnungsbetrag 2" } },
                    Debit = new[] { new { Account = 990, Text = "Eröffnungsbetrag 2" } }
                },
                new
                {
                    ID = 3,
                    Credit = new[] { new { Account = 990, Text = "Eröffnungsbetrag 3" } },
                    Debit = new[] { new { Account = 6000, Text = "Eröffnungsbetrag 3" } }
                });
        }

        [CulturedFact("en")]
        public void CloseYear_TextOptionAccountName_TextCorrect()
        {
            var sut = Samples.SampleProject;
            var currentYear = sut.Journal.Last();
            currentYear.Booking.AddRange(Samples.SampleBookings);
            var carryforwardAccount =
                sut.AllAccounts.First(x => x.Active && x.Type == AccountDefinitionType.Carryforward);

            sut.CloseYear(currentYear, carryforwardAccount, OpeningTextOption.AccountName);

            var newYear = sut.Journal.Last();
            newYear.Should().NotBeEquivalentTo(currentYear);
            currentYear.Closed.Should().BeTrue();
            newYear.Closed.Should().BeFalse();
            newYear.Booking.Should().BeEquivalentTo(
                new
                {
                    ID = 1,
                    Credit = new[] { new { Account = 990, Text = "Opening value Bank account" } },
                    Debit = new[] { new { Account = 100, Text = "Opening value Bank account" } }
                },
                new
                {
                    ID = 2,
                    Credit = new[] { new { Account = 5000, Text = "Opening value Bank credit" } },
                    Debit = new[] { new { Account = 990, Text = "Opening value Bank credit" } }
                },
                new
                {
                    ID = 3,
                    Credit = new[] { new { Account = 990, Text = "Opening value Friends debit" } },
                    Debit = new[] { new { Account = 6000, Text = "Opening value Friends debit" } }
                });
        }

        [Fact]
        public void CloseYear_JournalWithInvalidBookings_EntryIgnored()
        {
            var sut = new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition { ID = 100, Type = AccountDefinitionType.Asset },
                            new AccountDefinition
                            {
                                ID = 999, Type = AccountDefinitionType.Carryforward
                            }
                        }
                    }
                },
                Journal = new List<AccountingDataJournal>
                {
                    new AccountingDataJournal { Booking = null, DateStart = 20200101, DateEnd = 20201231 }
                }
            };

            sut.CloseYear(sut.Journal.Last(), sut.Accounts.Last().Account.Last(), OpeningTextOption.Numbered);

            sut.Journal.First().Closed.Should().BeTrue();
            sut.Journal.Last().Closed.Should().BeFalse();
        }

        [Fact]
        public void XsiSchemaLocation_DefaultConstructor_DefaultValue()
        {
            var sut = new AccountingData();

            sut.xsiSchemaLocation.Should().Be(AccountingData.DefaultXsiSchemaLocation);
        }

        [Fact]
        public void XsiSchemaLocation_SetDifferentValue_DefaultValue()
        {
            var sut = new AccountingData { xsiSchemaLocation = "foo" };

            sut.xsiSchemaLocation.Should().Be(AccountingData.DefaultXsiSchemaLocation);
        }
    }
}
