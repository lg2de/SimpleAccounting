// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Serialization;

    public partial class AccountingData
    {
        internal const string DefaultXsiSchemaLocation = DefaultSchemaNamespacee + " " + DefaultSchemaLocation;

        private const string DefaultSchemaNamespacee = "https://lg2.de/SimpleAccounting/AccountingSchema";
        private const string DefaultSchemaLocation = "https://lg2de.github.io/SimpleAccounting/AccountingData.xsd";

        private string schema = DefaultXsiSchemaLocation;

        [XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        [SuppressMessage("Minor Code Smell", "S100:Methods and properties should be named in PascalCase", Justification = "fixed name")]
        public string xsiSchemaLocation
        {
            get => this.schema;
            set
            {
                this.schema = value;
                if (this.schema != DefaultXsiSchemaLocation)
                {
                    this.schema = DefaultXsiSchemaLocation;
                }
            }
        }

        internal IEnumerable<AccountDefinition> AllAccounts => this.Accounts?.SelectMany(g => g.Account);

        internal AccountingData Clone()
        {
            var xml = this.Serialize();
            return Deserialize(xml);
        }

        public bool Migrate()
        {
            var result = false;
            result |= this.MergeYearsIntoJournal();
            result |= this.RemoveEmptyElements();
            return result;
        }

        private bool MergeYearsIntoJournal()
        {
            if (this.Years == null)
            {
                return false;
            }

            var result = false;
            foreach (var year in this.Years)
            {
                this.Journal ??= new List<AccountingDataJournal>();

                string oldYearName = year.Name.ToString(CultureInfo.InvariantCulture);
                var journal = this.Journal.SingleOrDefault(x => x.Year == oldYearName);
                if (journal == null)
                {
                    journal = new AccountingDataJournal { Year = oldYearName };
                    this.Journal.Add(journal);
                }

                journal.DateStart = year.DateStart;
                journal.DateEnd = year.DateEnd;
                journal.Closed = year.Closed;

                result = true;
            }

            this.Years = null;

            return result;
        }

        private bool RemoveEmptyElements()
        {
            if (this.Accounts == null)
            {
                return false;
            }

            var result = false;
            foreach (var group in this.Accounts)
            {
                foreach (var account in group.Account)
                {
                    if (account.ImportMapping == null)
                    {
                        continue;
                    }

                    if (!(account.ImportMapping.Columns?.Any() ?? false)
                        && !(account.ImportMapping.Patterns?.Any() ?? false))
                    {
                        account.ImportMapping = null;
                        result = true;
                    }
                }
            }

            return result;
        }
    }

    public partial class AccountDefinition
    {
        internal string FormatName()
        {
            return $"{this.ID} ({this.Name})";
        }
    }

    public partial class BookingValue
    {
        internal BookingValue Clone()
        {
            return this.MemberwiseClone() as BookingValue;
        }
    }
}
