// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

/// <summary>
///     Defines abstraction for <see cref="AccountsViewModel"/>.
/// </summary>
internal interface IAccountsViewModel : INotifyPropertyChanged
{
    ObservableCollection<AccountViewModel> AccountList { get; }

    AccountViewModel? SelectedAccount { get; set; }

    ICommand AccountSelectionCommand { get; }

    bool ShowInactiveAccounts { get; set; }

    void ShowNewAccountDialog();

    void OnEditAccount(object? commandParameter);

    void SelectFirstAccount();
    void OnDataLoaded();
}
