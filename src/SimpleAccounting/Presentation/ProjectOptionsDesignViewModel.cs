// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the designer view model for <see cref="ProjectOptionsViewModel"/>.
/// </summary>
public class ProjectOptionsDesignViewModel : ProjectOptionsViewModel
{
    public ProjectOptionsDesignViewModel() : base(CreateDesignData())
    {
    }

    private static AccountingData CreateDesignData()
    {
        var data = new AccountingData
        {
            Setup =
            {
                Name = "My Club",
                Location = "Hometown",
                Reports = { TotalsAndBalancesReport = ["Treasurer", "Auditor 1", "Auditor 2"] },
                BookingTemplates = new AccountingDataSetupBookingTemplates
                {
                    Template =
                    [
                        new AccountingDataSetupBookingTemplatesTemplate
                        {
                            Text = "Membership fee",
                            Value = 5000,
                            ValueSpecified = true,
                            Debit = 100,
                            DebitSpecified = true,
                            Credit = 400,
                            CreditSpecified = true
                        },
                        new AccountingDataSetupBookingTemplatesTemplate
                        {
                            Text = "Bank fee", Debit = 600, DebitSpecified = true, Credit = 100, CreditSpecified = true
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
                        new AccountDefinition { ID = 100, Name = "Bank account", Type = AccountDefinitionType.Asset },
                        new AccountDefinition { ID = 400, Name = "Fees", Type = AccountDefinitionType.Income },
                        new AccountDefinition { ID = 600, Name = "Bank fees", Type = AccountDefinitionType.Expense }
                    ]
                }
            ]
        };
        return data;
    }
}
