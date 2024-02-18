// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

/// <summary>
///     Implements the main view of the application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "The view class will not be tested.")]
public partial class ShellView
{
    public ShellView()
    {
        this.InitializeComponent();

        this.Loaded += (_, _) => this.ResetDataGridColumnSizes();
    }

    [SuppressMessage(
        "Minor Code Smell", "S2325:Methods and properties that don\'t access instance data should be static",
        Justification = "FP")]
    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!(sender is DataGrid grid) || grid.SelectedItem == null)
        {
            return;
        }

        grid.ScrollIntoView(grid.SelectedItem);
    }
}
