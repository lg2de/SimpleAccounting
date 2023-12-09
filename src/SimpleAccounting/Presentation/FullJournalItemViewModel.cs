// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

/// <summary>
///     Implements the view model for a single entry in the full journal.
/// </summary>
public class FullJournalItemViewModel : JournalItemBaseViewModel
{
    public FullJournalItemViewModel(int storageIndex)
    {
        this.StorageIndex = storageIndex;
    }

    public double Value { get; set; }

    public string CreditAccount { get; set; } = string.Empty;

    public string DebitAccount { get; set; } = string.Empty;

    internal FullJournalItemViewModel Clone()
    {
        return (FullJournalItemViewModel)this.MemberwiseClone();
    }
}
