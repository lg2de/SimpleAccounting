// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Windows.Input;

namespace lg2de.SimpleAccounting.Presentation
{
    public class MenuViewModel
    {
        public MenuViewModel(string header, ICommand command)
        {
            this.Header = header;
            this.Command = command;
        }

        public string Header { get; }

        public ICommand Command { get; }
    }
}
