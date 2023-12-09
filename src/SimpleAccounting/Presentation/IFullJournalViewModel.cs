// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
///     Defines abstraction for <see cref="FullJournalViewModel"/>.
/// </summary>
internal interface IFullJournalViewModel : INotifyPropertyChanged
{
    ObservableCollection<FullJournalItemViewModel> Items { get; }
        
    FullJournalItemViewModel? SelectedItem { get; set; }
        
    void Rebuild();
        
    void Select(ulong bookingId);
}
