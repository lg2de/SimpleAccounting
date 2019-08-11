// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Xml;

namespace lg2de.SimpleAccounting.Reports
{
    internal interface IXmlPrinter
    {
        XmlDocument Document { get; }

        void LoadDocument(string resourceName);
        void PrintDocument(string documentName);
    }
}