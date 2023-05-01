// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions;

using System;
using System.Globalization;
using System.Xml;

/// <summary>
///     Implements extensions on <see cref="XmlNode" />.
/// </summary>
internal static class XmlExtensions
{
    /// <summary>
    ///     Adds a new XML element with node named "td" with specified text as element content.
    /// </summary>
    /// <param name="parent">The XML node that will get new child node.</param>
    /// <param name="text">The text to be added as table node.</param>
    /// <returns>Returns the new created node for fluent extensions.</returns>
    public static XmlNode AddTableNode(this XmlNode parent, string text)
    {
        var node = parent.OwnerDocument!.CreateElement("td");
        node.InnerText = text;
        parent.AppendChild(node);
        return node;
    }

    /// <summary>
    ///     Set or adds the an attribute with initial value.
    /// </summary>
    /// <param name="node">The node to receive the attribute.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The attribute value.</param>
    public static void SetAttribute(this XmlNode node, string name, object value)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        XmlAttribute attr = node.OwnerDocument!.CreateAttribute(name);
        attr.Value = value.ToString();
        node.Attributes!.SetNamedItem(attr);
    }

    // switch to "nullable" to allow use of "default" for all types
#nullable disable

    /// <summary>
    ///     Gets the specified attribute converted to the type argument <typeparamref name="T" />.
    /// </summary>
    /// <param name="node">The node providing the attributes.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="defaultValue">Optional default value, <c>default</c> by default.</param>
    /// <typeparam name="T">The type the argument is converted to.</typeparam>
    /// <returns>
    ///     The value converted to the generic type or the default value.
    ///     The value may be <c>null</c> for reference types.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if the generic type is not supported.</exception>
    public static T GetAttribute<T>(this XmlNode node, string name, T defaultValue = default)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var attribute = node.Attributes?.GetNamedItem(name);
        if (attribute == null)
        {
            return defaultValue!;
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
