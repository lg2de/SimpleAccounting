// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Caliburn.Micro;
using JetBrains.Annotations;

/// <summary>
///     Extends the <see cref="WindowManager" /> so that dialogs do not appear separately in task bar and task switcher.
/// </summary>
/// <remarks>
///     A modal dialog belongs to its owner window. Switching to the owner window brings the dialog to the front.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "This view function cannot be tested.")]
[UsedImplicitly]
internal class DialogWindowManager : WindowManager
{
    protected override Window EnsureWindow(object model, object view, bool isDialog)
    {
        var window = base.EnsureWindow(model, view, isDialog);
        if (isDialog && window.Owner != null)
        {
            window.ShowInTaskbar = false;
        }

        return window;
    }
}
