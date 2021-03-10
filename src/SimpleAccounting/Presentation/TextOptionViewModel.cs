// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using lg2de.SimpleAccounting.Infrastructure;

    /// <summary>
    ///     Implements the view model to show option for Opening Booking.
    /// </summary>
    public class TextOptionViewModel
    {
        public TextOptionViewModel(OpeningTextOption option, string name)
        {
            this.Option = option;
            this.Name = name;
        }

        public OpeningTextOption Option { get; }

        public string Name { get; }
    }
}
