// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Model;

    public class CloseYearViewModel : Screen
    {
        private readonly AccountingDataJournal currentYear;

        public CloseYearViewModel(AccountingDataJournal currentYear)
        {
            this.currentYear = currentYear ?? throw new ArgumentNullException(nameof(currentYear));
        }

        public string InstructionText { get; private set; }

        public List<AccountDefinition> Accounts { get; } = new List<AccountDefinition>();

        public AccountDefinition RemoteAccount { get; set; }

        public ICommand CloseYearCommand => new RelayCommand(_ => this.TryClose(true));

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this.DisplayName = "Jahresabschluss";
            this.InstructionText = $"Wollen Sie das Jahr {this.currentYear.Year} abschließen?";

            this.RemoteAccount = this.Accounts.FirstOrDefault();
        }
    }
}