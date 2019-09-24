// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Windows.Controls;

    /// <summary>
    ///     Implements the view to import bookings from file.
    /// </summary>
    public partial class ImportBookingsView : UserControl
    {
        public ImportBookingsView()
        {
            this.InitializeComponent();

            this.Loaded += (s, a) => this.ResetDataGridColumnSizes();
        }
    }
}
