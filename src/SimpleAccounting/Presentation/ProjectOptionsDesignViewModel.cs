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
                Reports = { TotalsAndBalancesReport = ["Treasurer", "Auditor 1", "Auditor 2"] }
            }
        };
        return data;
    }
}
