// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage(
    "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
    Justification = "This is test code only.")]
internal class ErrorMessageDesignViewModel : ErrorMessageViewModel
{
    public ErrorMessageDesignViewModel() : base(null!)
    {
        this.Introduction =
            "This is the sample error text"
            + Environment.NewLine
            + "including new line, and also a very long second line to check"
            + " the handling line wrapping maximum size.";
        this.ErrorMessage = "The is the error message.";
    }
}
