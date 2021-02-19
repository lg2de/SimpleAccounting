// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Input;
    using lg2de.SimpleAccounting.Model;

    internal interface IAccountsViewModel : INotifyPropertyChanged
    {
        IEnumerable<AccountViewModel> AllAccounts { get; }
        ObservableCollection<AccountViewModel> AccountList { get; }
        AccountViewModel? SelectedAccount { get; set; }
        ICommand AccountSelectionCommand { get; }
        bool ShowInactiveAccounts { get; set; }
        void ShowNewAccountDialog();
        void OnEditAccount(object commandParameter);
        void SelectFirstAccount();
        void LoadAccounts(IReadOnlyCollection<AccountingDataAccountGroup> accounts);
    }
}
