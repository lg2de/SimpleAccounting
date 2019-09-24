// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Xml;

    internal static class XmlExtensions
    {
        public static void SetAttribute(this XmlNode node, string name, string value)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            XmlAttribute attr = node.OwnerDocument.CreateAttribute(name);
            attr.Value = value;
            node.Attributes.SetNamedItem(attr);
        }

        public static T GetAttribute<T>(this XmlNode node, string name, T defaultValue = default)
        {
            var attribute = node.Attributes.GetNamedItem(name);
            if (attribute == null)
            {
                return defaultValue;
            }

            Type returnType = typeof(T);
            if (returnType == typeof(int))
            {
                return (T)(object)Convert.ToInt32(attribute.Value);
            }

            throw new ArgumentException($"The type {returnType.Name} is not supported.");
        }
    }
}
