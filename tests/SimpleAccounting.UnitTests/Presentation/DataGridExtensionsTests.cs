// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Windows;
using System.Windows.Controls;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class DataGridExtensionsTests
{
    [WpfFact]
    public void ResetDataGridColumnSizes_NestedDataGrids_NonAutoColumnsConvertedToPixel()
    {
        var starColumn = new DataGridTextColumn
        {
            Width = new DataGridLength(2, DataGridLengthUnitType.Star, 80, 80)
        };
        var autoColumn = new DataGridTextColumn { Width = DataGridLength.Auto };
        var directDataGrid = new DataGrid();
        directDataGrid.Columns.Add(starColumn);
        directDataGrid.Columns.Add(autoColumn);
        var nestedColumn = new DataGridTextColumn
        {
            Width = new DataGridLength(1, DataGridLengthUnitType.Star, 40, 40)
        };
        var nestedDataGrid = new DataGrid();
        nestedDataGrid.Columns.Add(nestedColumn);
        var nestedPanel = new StackPanel();
        nestedPanel.Children.Add(new TextBlock());
        nestedPanel.Children.Add(nestedDataGrid);
        var root = new Grid();
        root.Children.Add(directDataGrid);
        root.Children.Add(nestedPanel);

        root.ResetDataGridColumnSizes();

        using var _ = new AssertionScope();
        starColumn.Width.UnitType.Should().Be(DataGridLengthUnitType.Pixel);
        starColumn.Width.Value.Should().Be(80);
        autoColumn.Width.IsAuto.Should().BeTrue();
        nestedColumn.Width.UnitType.Should().Be(DataGridLengthUnitType.Pixel);
        nestedColumn.Width.Value.Should().Be(40);
    }

    [WpfFact]
    public void ResetDataGridColumnSizes_Null_NothingHappens()
    {
        DependencyObject sut = null!;

        var action = sut.ResetDataGridColumnSizes;

        action.Should().NotThrow();
    }
}
