// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Xml;

namespace lg2de.SimpleAccounting.Extensions
{
    internal static class XmlExtensions
    {
        public static void SetAttribute(this XmlNode node, string name, string value)
        {
            XmlAttribute attr = node.OwnerDocument.CreateAttribute(name);
            attr.Value = value;
            node.Attributes.SetNamedItem(attr);
        }
    }
}
