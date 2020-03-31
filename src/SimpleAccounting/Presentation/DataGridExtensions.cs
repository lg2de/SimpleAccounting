// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    internal static class DataGridExtensions
    {
        public static void ResetDataGridColumnSizes(this DependencyObject dependencyObject)
        {
            var dataGrids = FindVisualChildren<DataGrid>(dependencyObject);
            foreach (var dataGrid in dataGrids)
            {
                foreach (DataGridColumn column in dataGrid.Columns.Where(x => !x.Width.IsAuto))
                {
                    column.Width = new DataGridLength(column.Width.DisplayValue, DataGridLengthUnitType.Pixel);
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj)
            where T : DependencyObject
        {
            if (depObj == null)
            {
                yield break;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T childOfExpectedType)
                {
                    yield return childOfExpectedType;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}
