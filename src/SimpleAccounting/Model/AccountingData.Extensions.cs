// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    public partial class AccountingData
    {
        [XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string xsiSchemaLocation
        {
            get => "https://lg2.de/SimpleAccounting/AccountingSchema https://lg2de.github.io/SimpleAccounting/AccountingData.xsd";
            set
            {
                // ignored
            }
        }

        internal IEnumerable<AccountDefinition> AllAccounts => this.Accounts.SelectMany(g => g.Account);

        internal AccountingData Clone()
        {
            var xml = this.Serialize();
            return AccountingData.Deserialize(xml);
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
