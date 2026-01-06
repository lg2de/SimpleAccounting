// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model;

using System;
using System.IO;
using lg2de.SimpleAccounting.Model;
using Xunit;

public class ReleaseDataTests
{
    [Fact]
    public void Deserialize_PublishedConfiguration_Validated()
    {
        var dotnetVersion = Environment.Version.Major;
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
                        EnglishDescription = $"Yes, the Small Package - .NET {dotnetVersion} runtime needs to be installed",
                        GermanDescription = $"Ja, das kleine Paket - .NET {dotnetVersion} Laufzeitumgebung muss installiert sein"
                    },
                    new ReleaseData.ReleaseItem
                    {
                        FileName = "SimpleAccounting-self-contained.zip",
                        EnglishDescription = $"Yes, the Large Package - Current .NET {dotnetVersion} runtime is included (recommended)",
                        GermanDescription = $"Ja, das große Paket - Aktuelle .NET {dotnetVersion} Laufzeitumgebung ist enthalten (empfohlen)"
                    }
                ]
            });
    }
}
