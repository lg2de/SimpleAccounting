// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using Caliburn.Micro;

namespace lg2de.SimpleAccounting.Presentation
{
    public class JournalBaseViewModel : PropertyChangedBase
    {
        protected JournalBaseViewModel()
        {
        }

        public ulong Identifier { get; set; }

        public DateTime Date { get; set; }

        public string Text { get; set; }
    }
}
