// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Globalization;
using Caliburn.Micro;
using JetBrains.Annotations;

/// <summary>
///     Implements the base view model for items in all journals.
/// </summary>
public class JournalItemBaseViewModel : PropertyChangedBase, IJournalItem
{
    private bool isEvenRow;

    protected JournalItemBaseViewModel()
    {
    }

    public ulong Identifier { get; set; }

    public string IdentifierText =>
        this.Identifier > 0 ? this.Identifier.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public DateTime Date { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsFollowup { get; set; }

    public bool IsEvenRow
    {
        [UsedImplicitly] get => this.isEvenRow;
        set
        {
            if (value == this.isEvenRow)
            {
                return;
            }

            this.isEvenRow = value;
            this.NotifyOfPropertyChange();
        }
    }

    public int StorageIndex { get; protected init; } = -1;
}
