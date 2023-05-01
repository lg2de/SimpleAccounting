// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
///     Defines abstraction for <see cref="AccountJournalViewModel"/>.
/// </summary>
internal interface IAccountJournalViewModel : INotifyPropertyChanged
{
    ObservableCollection<AccountJournalItemViewModel> Items { get; }

    AccountJournalItemViewModel? SelectedItem { get; set; }

    void Rebuild(ulong accountNumber);

    void Select(ulong bookingId);
}
