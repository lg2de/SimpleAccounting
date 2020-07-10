// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using Caliburn.Micro;

    public class JournalBaseViewModel : PropertyChangedBase
    {
        protected JournalBaseViewModel()
        {
        }

        public ulong Identifier { get; set; }

        public DateTime Date { get; set; }

        public string Text { get; set; } = string.Empty;

        public bool IsFollowup { get; set; }
    }
}
