// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Windows.Controls;

namespace lg2de.SimpleAccounting.Presentation
{
    /// <summary>
    ///     Implements the main view of the application.
    /// </summary>
    public partial class ShellView : UserControl
    {
        public ShellView()
        {
            this.InitializeComponent();

            this.Loaded += (s, a) => this.ResetDataGridColumnSizes();
        }
    }
}
