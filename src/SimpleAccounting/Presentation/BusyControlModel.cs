// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using Caliburn.Micro;
using lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Implements the model for the busy control.
/// </summary>
public class BusyControlModel : Screen, IBusy
{
    private bool isBusy;

    public bool IsBusy
    {
        get => this.isBusy;
        set
        {
            if (value == this.isBusy)
            {
                return;
            }

            this.isBusy = value;
            this.NotifyOfPropertyChange();
        }
    }
}
