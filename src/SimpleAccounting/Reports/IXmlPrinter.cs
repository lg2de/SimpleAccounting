// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Xml;

    internal interface IXmlPrinter
    {
        XmlDocument Document { get; }

        void LoadDocument(string resourceName);
        void PrintDocument(string documentName);
    }
}
