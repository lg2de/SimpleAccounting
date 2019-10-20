// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Diagnostics.CodeAnalysis;

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
    }
}
