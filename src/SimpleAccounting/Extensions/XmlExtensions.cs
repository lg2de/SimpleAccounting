// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Globalization;
    using System.Xml;

    internal static class XmlExtensions
    {
        public static void AddTableNode(this XmlNode parent, string text)
        {
            var node = parent.OwnerDocument.CreateElement("td");
            node.InnerText = text;
            parent.AppendChild(node);
        }

        public static void SetAttribute(this XmlNode node, string name, object value)
        {
            if (node?.OwnerDocument == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            XmlAttribute attr = node.OwnerDocument.CreateAttribute(name);
            attr.Value = value.ToString();
            node.Attributes.SetNamedItem(attr);
        }

        public static T GetAttribute<T>(this XmlNode node, string name, T defaultValue = default)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var attribute = node.Attributes.GetNamedItem(name);
            if (attribute == null)
            {
                return defaultValue;
            }

            Type returnType = typeof(T);
            if (returnType == typeof(int) || returnType == typeof(int?))
            {
                return (T)(object)Convert.ToInt32(attribute.Value, CultureInfo.InvariantCulture);
            }

            if (returnType == typeof(float) || returnType == typeof(float?))
            {
                return (T)(object)Convert.ToSingle(attribute.Value, CultureInfo.InvariantCulture);
            }

            if (returnType == typeof(bool) || returnType == typeof(bool?))
            {
                return (T)(object)bool.Parse(attribute.Value);
            }

            if (returnType == typeof(string))
            {
                return (T)(object)attribute.Value;
            }

            throw new ArgumentException($"The type {returnType.Name} is not supported.");
        }
    }
}
