using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lg2de.SimpleAccounting
{
    public class JournalViewModel
    {
        public ulong Identifier { get; set; }

        public DateTime Date { get; set; }

        public string Text { get; set; }

        public double Value { get; set; }

        public string CreditAccount { get; set; }

        public string DebitAccount { get; set; }

        internal JournalViewModel Clone()
        {
            return this.MemberwiseClone() as JournalViewModel;
        }

        internal void SetDate(uint date)
        {
            this.Date = new DateTime((int)date / 10000, (int)(date / 100) % 100, (int)date % 100);
        }
    }
}
