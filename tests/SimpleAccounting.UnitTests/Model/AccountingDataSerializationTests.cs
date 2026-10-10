// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using lg2de.SimpleAccounting.Model;
using Xunit;

/// <summary>
///     Ensures the stable file format of the project files independent of the code generator for the model.
/// </summary>
public class AccountingDataSerializationTests
{
    private const string VirtualAddress = "https://lg2.de/SimpleAccounting/AccountingSchema";
    private const string ActualAddress = "https://lg2de.github.io/SimpleAccounting/AccountingData.xsd";

    private const string CompleteProjectXml =
        $"""
         <?xml version="1.0" encoding="utf-8"?>
         <AccountingData
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            xsi:schemaLocation="{
                VirtualAddress
            } {
                ActualAddress
            }">
           <Setup>
             <Name>Organization</Name>
             <Location>Location</Location>
             <Currency Show="true">$</Currency>
             <BookingTemplates>
               <Template Text="Full template" Value="1234" Credit="400" Debit="100" />
               <Template Text="Text only" />
             </BookingTemplates>
             <Reports>
               <AccountJournalReport PageBreakBetweenAccounts="true" />
               <TotalsAndBalancesReport>
                 <Signature>Treasurer</Signature>
                 <Signature>Auditor</Signature>
               </TotalsAndBalancesReport>
             </Reports>
             <Behavior>
               <LastBookingImportFolder>C:\Import</LastBookingImportFolder>
               <OpeningTextPattern>AccountName</OpeningTextPattern>
               <LastCarryForward>990</LastCarryForward>
             </Behavior>
           </Setup>
           <Accounts>
             <AccountGroup Name="Group 1">
               <Account ID="100" Name="Bank" Type="Asset">
                 <ImportMapping>
                   <Columns>
                     <Column Target="Date" Source="Date" />
                     <Column Target="Text" Source="Text" IgnorePattern="ignore" />
                   </Columns>
                   <Patterns>
                     <Pattern Expression="Salary" Value="5000" AccountID="400" />
                     <Pattern Expression="Any" AccountID="600" />
                   </Patterns>
                 </ImportMapping>
               </Account>
               <Account ID="400" Name="Income" Type="Income" />
               <Account ID="600" Name="Inactive" Type="Expense" Active="false" />
             </AccountGroup>
             <AccountGroup Name="Empty group" />
             <AccountGroup Name="Group 2">
               <Account ID="5000" Name="Credit" Type="Credit" />
               <Account ID="6000" Name="Debit" Type="Debit" />
               <Account ID="990" Name="Carryforward" Type="Carryforward" />
             </AccountGroup>
           </Accounts>
           <Journal Year="2025" DateStart="20250101" DateEnd="20251231" Closed="true">
             <Booking Date="20250101" ID="1" Opening="true">
               <Debit Value="1000" Account="100" Text="Opening" />
               <Credit Value="1000" Account="990" Text="Opening" />
             </Booking>
           </Journal>
           <Journal Year="2026" DateStart="20260101" DateEnd="20261231">
             <Booking Date="20260215" ID="2" Followup="true">
               <Debit Value="5000" Account="100" Text="Salary" />
               <Credit Value="3000" Account="400" Text="Salary 1" />
               <Credit Value="2000" Account="400" Text="Salary 2" />
             </Booking>
           </Journal>
           <OpenIssues>
             <Entry Year="2026" Date="20260301" ID="3" Value="999" Account="100" Text="Unclear" />
           </OpenIssues>
         </AccountingData>
         """;

    [Fact]
    public void Serialize_CompleteProject_ExpectedXml()
    {
        var sut = CreateCompleteProject();

        var xml = sut.Serialize();

        XDocument.Parse(xml).Should().BeEquivalentTo(XDocument.Parse(CompleteProjectXml));
    }

    [Fact]
    public void Serialize_NewProject_ExpectedXml()
    {
        var sut = new AccountingData();

        var xml = sut.Serialize();

        const string expectedXml =
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <AccountingData
               xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xsi:schemaLocation="{VirtualAddress} {ActualAddress}">
               <Setup>
                 <Currency>€</Currency>
                 <Reports />
                 <Behavior />
               </Setup>
               <Accounts />
             </AccountingData>
             """;
        XDocument.Parse(xml).Should().BeEquivalentTo(
            XDocument.Parse(
                expectedXml));
    }

    [Fact]
    public void Serialize_CompleteProject_ValidAgainstSchema()
    {
        var sut = CreateCompleteProject();

        var xml = sut.Serialize();

        ValidateAgainstSchema(xml).Should().BeEmpty();
    }

    [Fact]
    public void Serialize_EmptyPatterns_ValidAgainstSchema()
    {
        var sut = CreateCompleteProject();
        sut.AllAccounts.First().ImportMapping.Patterns.Clear();

        var xml = sut.Serialize();

        ValidateAgainstSchema(xml).Should().BeEmpty("empty lists must not be serialized as empty elements");
    }

    [Fact]
    public void Deserialize_CompleteXml_AllValuesLoaded()
    {
        // The application always migrates the project after loading.
        var sut = AccountingData.Deserialize(CompleteProjectXml);
        sut.Migrate();

        sut.Should().BeEquivalentTo(CreateCompleteProject());
    }

    [Fact]
    public void Deserialize_CompleteXml_SerializedUnchanged()
    {
        // The application always migrates the project after loading.
        var sut = AccountingData.Deserialize(CompleteProjectXml);
        sut.Migrate();

        var xml = sut.Serialize();

        XDocument.Parse(xml).Should().BeEquivalentTo(XDocument.Parse(CompleteProjectXml));
    }

    [Fact]
    public void Deserialize_MinimalXml_DefaultsApplied()
    {
        var sut = AccountingData.Deserialize(
            """
            <AccountingData>
              <Setup>
                <Name>Organization</Name>
                <Location>Location</Location>
                <Currency>€</Currency>
              </Setup>
              <Accounts>
                <AccountGroup Name="Group">
                  <Account ID="100" Name="Bank" Type="Asset" />
                </AccountGroup>
              </Accounts>
              <Journal Year="2026" DateStart="20260101" DateEnd="20261231">
                <Booking Date="20260215" ID="1">
                  <Debit Value="5000" Account="100" Text="Debit" />
                  <Credit Value="5000" Account="100" Text="Credit" />
                </Booking>
              </Journal>
            </AccountingData>
            """);

        using var _ = new AssertionScope();
        sut.Setup.Currency.Show.Should().BeFalse();
        sut.Accounts[0].Account[0].Active.Should().BeTrue();
        sut.Journal[0].Closed.Should().BeFalse();
        sut.Journal[0].Booking[0].Followup.Should().BeFalse();
        sut.Journal[0].Booking[0].Opening.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_SampleFile_ValidAndLoaded()
    {
        var xml = File.ReadAllText(
            Path.Combine(AccountingDataSchemaTests.FindRepositoryRoot(), "samples", "sample.acml"));

        var sut = AccountingData.Deserialize(xml);

        using var _ = new AssertionScope();
        ValidateAgainstSchema(xml).Should().BeEmpty();
        sut.Setup.Name.Should().Be("Demo");
        sut.AllAccounts.Should().HaveCount(5);
        sut.Journal.Should().ContainSingle().Which.Booking.Should().HaveCount(3);
    }

    private static List<string> ValidateAgainstSchema(string xml)
    {
        var schemaFile = Path.Combine(
            AccountingDataSchemaTests.FindRepositoryRoot(), "src", "SimpleAccounting", "Model", "AccountingData.xsd");
        var errors = new List<string>();
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
        settings.Schemas.Add(null, schemaFile);
        settings.ValidationEventHandler += (_, args) => errors.Add(args.Message);
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        while (reader.Read())
        {
            // just read to validate
        }

        return errors;
    }

    private static AccountingData CreateCompleteProject()
    {
        return new AccountingData
        {
            Setup = new AccountingDataSetup
            {
                Name = "Organization",
                Location = "Location",
                Currency = new AccountingDataSetupCurrency { Value = "$", Show = true },
                BookingTemplates =
                [
                    new AccountingDataSetupBookingTemplatesTemplate
                    {
                        Text = "Full template",
                        Value = 1234,
                        ValueSpecified = true,
                        Debit = 100,
                        DebitSpecified = true,
                        Credit = 400,
                        CreditSpecified = true
                    },
                    new AccountingDataSetupBookingTemplatesTemplate { Text = "Text only" }
                ],
                Reports = new AccountingDataSetupReports
                {
                    AccountJournalReport =
                        new AccountingDataSetupReportsAccountJournalReport { PageBreakBetweenAccounts = true },
                    TotalsAndBalancesReport = ["Treasurer", "Auditor"]
                },
                Behavior = new AccountingDataSetupBehavior
                {
                    LastBookingImportFolder = "C:\\Import",
                    OpeningTextPattern = "AccountName",
                    LastCarryForward = 990,
                    LastCarryForwardSpecified = true
                }
            },
            Accounts =
            [
                new AccountingDataAccountsAccountGroup
                {
                    Name = "Group 1",
                    Account =
                    [
                        new AccountDefinition
                        {
                            ID = 100,
                            Name = "Bank",
                            Type = AccountDefinitionType.Asset,
                            ImportMapping = new AccountDefinitionImportMapping
                            {
                                Columns =
                                [
                                    new AccountDefinitionImportMappingColumnsColumn
                                    {
                                        Target = AccountDefinitionImportMappingColumnsColumnTarget.Date,
                                        Source = "Date"
                                    },
                                    new AccountDefinitionImportMappingColumnsColumn
                                    {
                                        Target = AccountDefinitionImportMappingColumnsColumnTarget.Text,
                                        Source = "Text",
                                        IgnorePattern = "ignore"
                                    }
                                ],
                                Patterns =
                                [
                                    new AccountDefinitionImportMappingPatternsPattern
                                    {
                                        Expression = "Salary", AccountID = 400, Value = 5000, ValueSpecified = true
                                    },
                                    new AccountDefinitionImportMappingPatternsPattern
                                    {
                                        Expression = "Any", AccountID = 600
                                    }
                                ]
                            }
                        },
                        new AccountDefinition { ID = 400, Name = "Income", Type = AccountDefinitionType.Income },
                        new AccountDefinition
                        {
                            ID = 600, Name = "Inactive", Type = AccountDefinitionType.Expense, Active = false
                        }
                    ]
                },
                new AccountingDataAccountsAccountGroup { Name = "Empty group", Account = [] },
                new AccountingDataAccountsAccountGroup
                {
                    Name = "Group 2",
                    Account =
                    [
                        new AccountDefinition { ID = 5000, Name = "Credit", Type = AccountDefinitionType.Credit },
                        new AccountDefinition { ID = 6000, Name = "Debit", Type = AccountDefinitionType.Debit },
                        new AccountDefinition
                        {
                            ID = 990, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
                        }
                    ]
                }
            ],
            Journal =
            [
                new AccountingDataJournal
                {
                    Year = "2025",
                    DateStart = 2025_0101,
                    DateEnd = 2025_1231,
                    Closed = true,
                    Booking =
                    [
                        new AccountingDataJournalBooking
                        {
                            Date = 2025_0101,
                            ID = 1,
                            Opening = true,
                            Debit = [new BookingValue { Value = 1000, Account = 100, Text = "Opening" }],
                            Credit = [new BookingValue { Value = 1000, Account = 990, Text = "Opening" }]
                        }
                    ]
                },
                new AccountingDataJournal
                {
                    Year = "2026",
                    DateStart = 2026_0101,
                    DateEnd = 2026_1231,
                    Booking =
                    [
                        new AccountingDataJournalBooking
                        {
                            Date = 2026_0215,
                            ID = 2,
                            Followup = true,
                            Debit = [new BookingValue { Value = 5000, Account = 100, Text = "Salary" }],
                            Credit =
                            [
                                new BookingValue { Value = 3000, Account = 400, Text = "Salary 1" },
                                new BookingValue { Value = 2000, Account = 400, Text = "Salary 2" }
                            ]
                        }
                    ]
                }
            ],
            OpenIssues =
            [
                new AccountingDataOpenIssuesEntry
                {
                    Year = "2026",
                    Date = 2026_0301,
                    ID = 3,
                    Value = 999,
                    Account = 100,
                    Text = "Unclear"
                }
            ]
        };
    }
}
