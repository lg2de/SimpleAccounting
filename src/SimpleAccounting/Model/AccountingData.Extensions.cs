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
        internal const string DefaultXsiSchemaLocation = DefaultSchemaNamespacee + " " + DefaultSchemaLocation;

        private const string DefaultSchemaNamespacee = "https://lg2.de/SimpleAccounting/AccountingSchema";
        private const string DefaultSchemaLocation = "https://lg2de.github.io/SimpleAccounting/AccountingData.xsd";

        private string schema = DefaultXsiSchemaLocation;

        [XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
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
