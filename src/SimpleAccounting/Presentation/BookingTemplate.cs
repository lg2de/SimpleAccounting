// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

/// <summary>
///     Implements the view model for a single booking template.
/// </summary>
internal class BookingTemplate
{
    public string Text { get; set; } = string.Empty;

    public ulong Credit { get; set; }

    public ulong Debit { get; set; }

    public double Value { get; set; }
}
