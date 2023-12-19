// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Threading.Tasks;
using lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Implements the designer view model for <see cref="UpdateOptionsViewModel"/>.
/// </summary>
internal class UpdateOptionsDesignViewModel : UpdateOptionsViewModel
{
    public UpdateOptionsDesignViewModel() : base("Asking for update options...")
    {
        this.Options.Add(new OptionItem("Option 1", new AsyncCommand(() => Task.CompletedTask)));
        this.Options.Add(new OptionItem("Option 2", new AsyncCommand(() => Task.CompletedTask)));
    }
}
