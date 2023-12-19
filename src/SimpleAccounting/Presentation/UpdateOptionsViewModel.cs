// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.Generic;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Infrastructure;

public class UpdateOptionsViewModel : Screen
{
    public string Text { get; set; }

    public IList<OptionItem> Options { get; } = [];

    public class OptionItem
    {
        public string Text { get; set; }

        public IAsyncCommand Command { get; set; }
    }
}
