// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using lg2de.SimpleAccounting.Infrastructure;

    public class MenuViewModel
    {
        public MenuViewModel(string header, IAsyncCommand command)
        {
            this.Header = header;
            this.Command = command;
        }

        public string Header { get; }

        public IAsyncCommand Command { get; }
    }
}
