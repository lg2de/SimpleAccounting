// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    public class JournalViewModel : JournalBaseViewModel
    {
        public double Value { get; set; }

        public string CreditAccount { get; set; }

        public string DebitAccount { get; set; }

        internal JournalViewModel Clone()
        {
            return this.MemberwiseClone() as JournalViewModel;
        }
    }
}
