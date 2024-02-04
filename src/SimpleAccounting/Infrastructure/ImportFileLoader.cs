// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using MagicFileEncoding;

internal sealed partial class ImportFileLoader : IDisposable
{
    private readonly List<AccountDefinition> accounts;
    private readonly CultureInfo cultureInfo;
    private readonly byte[] bytes;
    private readonly AccountDefinitionImportMapping importMapping;
    private readonly Regex duplicateSpaceExpression = DuplicateSpaceRegex();

    private StreamReader? streamReader;

    public ImportFileLoader(
        byte[] bytes,
        CultureInfo cultureInfo,
        List<AccountDefinition> accounts,
        AccountDefinitionImportMapping importMapping)
    {
        this.bytes = bytes;
        this.cultureInfo = cultureInfo;
        this.accounts = accounts;
        this.importMapping = importMapping;
    }

    public void Dispose()
    {
        this.streamReader?.Dispose();
    }

    public IEnumerable<ImportEntryViewModel> Load()
    {
        var encoding = FileEncoding.GetAcceptableEncoding(this.bytes);

        // note, the stream is disposed by the reader
        var stream = new MemoryStream(this.bytes);
        this.streamReader = new StreamReader(stream, encoding);
        var configuration = new CsvConfiguration(this.cultureInfo);
        return this.ImportBookings(this.streamReader, configuration);
    }

    private IEnumerable<ImportEntryViewModel> ImportBookings(TextReader reader, IReaderConfiguration configuration)
    {
        var dateField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnTarget.Date)?.Source
            ?? "date";
        var nameField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnTarget.Name)?.Source
            ?? "name";
        var textField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnTarget.Text);
        var valueField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnTarget.Value)?.Source
            ?? "value";

        using var csv = new CsvReader(reader, configuration);
        if (!csv.Read())
        {
            throw new InvalidOperationException("Failed to read initial record.");
        }

        csv.ReadHeader();
        if (csv.Context.HeaderRecord.Length <= 1)
        {
            throw new InvalidOperationException("Missing or incomplete file header.");
        }

        while (csv.Read())
        {
            yield return this.ImportBooking(csv, dateField, nameField, textField, valueField);
        }
    }

    private ImportEntryViewModel ImportBooking(
        CsvReader csv,
        string dateField,
        string nameField,
        AccountDefinitionImportMappingColumn? textField,
        string valueField)
    {
        // date and value are required
        csv.TryGetField(dateField, out DateTime date);
        csv.TryGetField<double>(valueField, out var value);

        // name and text may be empty
        csv.TryGetField(nameField, out string name);

        string text = string.Empty;
        if (textField != null)
        {
            csv.TryGetField(textField.Source, out text);
            text ??= string.Empty;
            if (!string.IsNullOrEmpty(textField.IgnorePattern))
            {
                text = Regex.Replace(
                    text, textField.IgnorePattern, string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
            }

            text = this.duplicateSpaceExpression.Replace(text, " ");
        }

        var item = new ImportEntryViewModel(this.accounts) { Date = date, Name = name, Text = text, Value = value };

        var modelValue = value.ToModelValue();
        var patterns = this.importMapping.Patterns ?? Enumerable.Empty<AccountDefinitionImportMappingPattern>();
        var matchedPattern = patterns.FirstOrDefault(PatternPredicate);
        item.RemoteAccount = this.accounts.SingleOrDefault(a => a.ID == matchedPattern?.AccountID);

        return item;

        bool PatternPredicate(AccountDefinitionImportMappingPattern pattern)
        {
            if (!pattern.Regex.IsMatch(text))
            {
                // mapping pattern does not match
                return false;
            }

            if (pattern.ValueSpecified && modelValue != pattern.Value)
            {
                // mapping value does not match
                return false;
            }

            // use first match
            return true;
        }
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex DuplicateSpaceRegex();
}
