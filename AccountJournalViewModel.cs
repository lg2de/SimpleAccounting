namespace lg2de.SimpleAccounting
{
    public class AccountJournalViewModel : JournalBaseViewModel
    {
        public double CreditValue { get; set; }

        public double DebitValue { get; set; }

        public string RemoteAccount { get; set; }
    }
}
