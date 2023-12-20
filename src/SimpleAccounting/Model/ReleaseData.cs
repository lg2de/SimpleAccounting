// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

/// <summary>
///     Defines the data to describe available releases.
/// </summary>
/// <remarks>
///     The class is public only for technical reasons.
/// </remarks>
public class ReleaseData
{
    [XmlElement("Release")]
    [SuppressMessage(
        "Major Code Smell", "S4004:Collection properties should be readonly",
        Justification = "Required for serialization")]
    public List<ReleaseItem>? Releases { get; set; }

    public static ReleaseData Deserialize(string xml)
    {
        var serializer = new XmlSerializer(typeof(ReleaseData));
        StringReader? stringReader = null;
        try
        {
            stringReader = new StringReader(xml);
            return (serializer.Deserialize(XmlReader.Create(stringReader)) as ReleaseData)!;
        }
        finally
        {
            stringReader?.Dispose();
        }
    }

    public class ReleaseItem
    {
        [XmlAttribute]
        public string? FileName { get; set; }

        public string? EnglishDescription { get; set; }

        public string? GermanDescription { get; set; }
    }
}
