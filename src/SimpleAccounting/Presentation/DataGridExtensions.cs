// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
///     Implements extensions for the <see cref="DataGrid"/>.
/// </summary>
internal static class DataGridExtensions
{
    public static void ResetDataGridColumnSizes(this DependencyObject dependencyObject)
    {
        var dataGrids = FindVisualChildren(dependencyObject).OfType<DataGrid>();
        foreach (var dataGrid in dataGrids)
        {
            foreach (DataGridColumn column in dataGrid.Columns.Where(x => !x.Width.IsAuto))
            {
                column.Width = new DataGridLength(column.Width.DisplayValue, DataGridLengthUnitType.Pixel);
            }
        }
    }

    private static IEnumerable<DependencyObject> FindVisualChildren(DependencyObject depObj)
    {
        if (depObj == null)
        {
            yield break;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
            yield return child;

            foreach (DependencyObject childOfChild in FindVisualChildren(child))
            {
                yield return childOfChild;
            }
        }
    }
}
