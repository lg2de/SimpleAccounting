// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

/// <summary>
///     Implements the main view of the application.
/// </summary>
public partial class ShellView
{
    [ExcludeFromCodeCoverage]
    public ShellView()
    {
        this.InitializeComponent();

        this.Loaded += (s, a) => this.ResetDataGridColumnSizes();
    }

    [ExcludeFromCodeCoverage]
    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!(sender is DataGrid grid) || grid.SelectedItem == null)
        {
            return;
        }

        grid.ScrollIntoView(grid.SelectedItem);
    }
}
