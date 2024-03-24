// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;

internal class ErrorMessageDesignViewModel : ErrorMessageViewModel
{
    public ErrorMessageDesignViewModel() : base(null!)
    {
        this.ErrorText = "This is the sample error text" + Environment.NewLine + "including new line.";
    }
}
