// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using lg2de.SimpleAccounting.Model;

    internal sealed class CloseYearDesignViewModel : CloseYearViewModel
    {
        public CloseYearDesignViewModel()
            : base(new AccountingDataJournal { Year = "2020" })
        {
            this.Accounts.Add(new AccountDefinition { ID = 990, Name = "My CarryForward" });
            this.OnInitialize();
        }
    }
}