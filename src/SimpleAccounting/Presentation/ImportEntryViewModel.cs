// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Windows.Input;
    using lg2de.SimpleAccounting.Model;

    public class ImportEntryViewModel : JournalBaseViewModel
    {
        private AccountDefinition remoteAccount;

        public IEnumerable<AccountDefinition> Accounts { get; set; }

        public string Name { get; set; }

        public double Value { get; set; }

        public AccountDefinition RemoteAccount
        {
            get => this.remoteAccount;
            set
            {
                this.remoteAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ICommand ResetRemoteAccountCommand => new RelayCommand(
            _ => this.RemoteAccount = null,
            _ => this.RemoteAccount != null);
    }
}
