// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Xml;

    /// <summary>
    ///     Defines the abstraction for a printing framework based on an XML document.
    /// </summary>
    internal interface IXmlPrinter
    {
        XmlDocument Document { get; }

        void LoadDocument(string resourceName);
        
        void PrintDocument(string documentName);
    }
}
