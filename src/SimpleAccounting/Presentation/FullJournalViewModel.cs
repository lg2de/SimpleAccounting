// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    public class FullJournalViewModel : JournalBaseViewModel
    {
        public double Value { get; set; }

        public string CreditAccount { get; set; } = string.Empty;

        public string DebitAccount { get; set; } = string.Empty;

        internal FullJournalViewModel Clone()
        {
            return (FullJournalViewModel)this.MemberwiseClone();
        }
    }
}
