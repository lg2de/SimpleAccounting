// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;

    public class ImportEntryViewModel : JournalBaseViewModel
    {
        private AccountDefinition? remoteAccount;

        public ImportEntryViewModel()
            : this(Enumerable.Empty<AccountDefinition>())
        {
            this.IsExisting = true;
        }

        public ImportEntryViewModel(IEnumerable<AccountDefinition> accounts)
        {
            this.Accounts = accounts;
        }

        public IEnumerable<AccountDefinition> Accounts { get; }

        public string Name { get; set; } = string.Empty;

        public double Value { get; set; }

        public AccountDefinition? RemoteAccount
        {
            get => this.remoteAccount;
            set
            {
                this.remoteAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public bool IsExisting { get; }

        public ICommand ResetRemoteAccountCommand => new RelayCommand(
            _ => this.RemoteAccount = null,
            _ => this.RemoteAccount != null);

        internal string BuildText()
        {
            // build booking text from name and/or text
            if (string.IsNullOrWhiteSpace(this.Text))
            {
                return this.Name;
            }

            if (string.IsNullOrWhiteSpace(this.Name))
            {
                return this.Text;
            }

            return $"{this.Name} - {this.Text}";
        }
    }
}
