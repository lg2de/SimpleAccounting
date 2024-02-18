// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

/// <summary>
///     Implements data for file reservation information.
/// </summary>
public class ReservationData
{
    private static XmlSerializer? serializer;

    public string UserName { get; init; } = string.Empty;

    public string MachineName { get; init; } = string.Empty;

    private static XmlSerializer Serializer
    {
        get
        {
            return serializer ??= new XmlSerializer(typeof(ReservationData));
        }
    }

    public static ReservationData Deserialize(string xml)
    {
        StringReader? stringReader = null;
        try
        {
            stringReader = new StringReader(xml);
            return (ReservationData)(Serializer.Deserialize(XmlReader.Create(stringReader)))!;
        }
        finally
        {
            stringReader?.Dispose();
        }
    }

    public string Serialize()
    {
        StreamReader? streamReader = null;
        MemoryStream? memoryStream = null;
        try
        {
            memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, this);
            memoryStream.Seek(0, SeekOrigin.Begin);
            streamReader = new StreamReader(memoryStream);
            return streamReader.ReadToEnd();
        }
        finally
        {
            streamReader?.Dispose();
            memoryStream?.Dispose();
        }
    }
}
