// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    public class FullJournalViewModel : JournalBaseViewModel
    {
        public double Value { get; set; }

        public string CreditAccount { get; set; }

        public string DebitAccount { get; set; }

        internal FullJournalViewModel Clone()
        {
            return this.MemberwiseClone() as FullJournalViewModel;
        }
    }
}
