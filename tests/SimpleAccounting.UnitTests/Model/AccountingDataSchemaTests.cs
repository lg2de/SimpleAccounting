// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model;

using System;
using System.IO;
using Xunit;

public class AccountingDataSchemaTests
{
    [Fact]
    public void PublishedSchema_SameAsSourceSchema()
    {
        var rootFolder = FindRepositoryRoot();
        var sourceSchema = File.ReadAllText(
            Path.Combine(rootFolder, "src", "SimpleAccounting", "Model", "AccountingData.xsd"));
        var publishedSchema = File.ReadAllText(Path.Combine(rootFolder, "docs", "AccountingData.xsd"));

        publishedSchema.Should().Be(
            sourceSchema,
            "the schema in folder 'docs' is published by GitHub Pages and must be updated (by local build)");
    }

    private static string FindRepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder != null && !File.Exists(Path.Combine(folder.FullName, "docs", "AccountingData.xsd")))
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
