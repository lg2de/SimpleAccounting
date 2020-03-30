// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model
{
    using System.Collections.Generic;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
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

            sut.Migrate();

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
            sut.Should().BeEquivalentTo(expectation);
        }

        [Fact]
        public void Migrate_Empty_Unchanged()
        {
            var sut = new AccountingData();

            sut.Migrate();

            sut.Should().BeEquivalentTo(new AccountingData());
        }

        [Fact]
        public void Migrate_EmptyYears_YearsNodeRemoved()
        {
            var sut = new AccountingData { Years = new List<AccountingDataYear>() };

            sut.Migrate();

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

            sut.Migrate();

            var expectation = new AccountingData
            {
                Journal = new List<AccountingDataJournal>
                {
                    new AccountingDataJournal
                    {
                        Year = "2001", DateStart = 20010101, DateEnd = 20011231, Closed = true
                    },
                    new AccountingDataJournal
                    {
                        Year = "2002", DateStart = 20020101, DateEnd = 20021231, Closed = false
                    }
                }
            };
            sut.Should().BeEquivalentTo(expectation);
        }

        [Fact]
        public void XsiSchemaLocation_DefaultContructor_DefaultValue()
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
