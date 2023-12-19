// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;

internal class UpdateOptionsDesignViewModel : UpdateOptionsViewModel
{
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "This is text for the designer only.")]
    public UpdateOptionsDesignViewModel()
    {
        this.Text = "Asking for update options...";

        this.Options.Add(new OptionItem { Text = "Option 1" });
        this.Options.Add(new OptionItem { Text = "Option 2" });
    }
}
