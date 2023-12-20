// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model;

using System.IO;
using FluentAssertions;
using lg2de.SimpleAccounting.Model;
using Xunit;

public class ReleaseDataTests
{
    [Fact]
    public void Deserialize_PublishedConfiguration_Validated()
    {
        var stream = this.GetType().Assembly.GetManifestResourceStream(
            "lg2de.SimpleAccounting.UnitTests.Model.ReleaseData.xml");
        using var reader = new StreamReader(stream!);
        var xml = reader.ReadToEnd();

        var result = ReleaseData.Deserialize(xml);

        result.Should().BeEquivalentTo(
            new ReleaseData
            {
                Releases =
                [
                    new ReleaseData.ReleaseItem
                    {
                        FileName = "SimpleAccounting.zip",
                        EnglishDescription = "Small Package - .NET 8 runtime needs to be installed",
                        GermanDescription = "Kleines Paket - .NET 8 Laufzeitumgebung muss installiert sein"
                    },
                    new ReleaseData.ReleaseItem
                    {
                        FileName = "SimpleAccounting-self-contained.zip",
                        EnglishDescription = "Larges Package - Current .NET 8 runtime is included",
                        GermanDescription = "Großes Paket - Aktuelle .NET 8 Laufzeitumgebung ist enthalten"
                    }
                ]
            });
    }
}
