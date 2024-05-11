// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Implements the view to visualize an application error.
/// </summary>
public partial class ErrorMessageView
{
    [ExcludeFromCodeCoverage(Justification = "The view class will not be tested.")]
    public ErrorMessageView()
    {
        this.InitializeComponent();
    }
}
