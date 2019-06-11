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
    }
}
