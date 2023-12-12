// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Implements the view to import bookings from file.
/// </summary>
public partial class ImportBookingsView
{
    [ExcludeFromCodeCoverage]
    public ImportBookingsView()
    {
        this.InitializeComponent();

        this.Loaded += (_, _) => this.ResetDataGridColumnSizes();
    }
}
