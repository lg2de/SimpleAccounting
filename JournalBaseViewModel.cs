using System;

namespace lg2de.SimpleAccounting
{
    public class JournalBaseViewModel
    {
        protected JournalBaseViewModel()
        {
        }

        public ulong Identifier { get; set; }

        public DateTime Date { get; set; }

        public string Text { get; set; }

        internal void SetDate(uint date)
        {
            this.Date = new DateTime((int)date / 10000, (int)(date / 100) % 100, (int)date % 100);
        }
    }
}
