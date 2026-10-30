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
        var listSeparator = this.cultureInfo.TextInfo.ListSeparator;
        var options = new CsvReaderOptions
        {
            FormatProvider = this.cultureInfo,

            // keep the separator defined by the culture, detect it if the culture uses a multi-character separator
            Separator = listSeparator.Length == 1 ? listSeparator[0] : null
        };
        return this.ImportBookings(this.streamReader, options);
    }

    private IEnumerable<ImportEntryViewModel> ImportBookings(TextReader reader, CsvReaderOptions options)
    {
        var dateField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Date)?.Source
            ?? "date";
        var nameField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Name)?.Source
            ?? "name";
        var textField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Text);
        var valueField =
            this.importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Value)?.Source
            ?? "value";

        using var csv = CsvReader.Create(reader, options);

        // the header is read implicitly by the first access
        if (csv.Headers.Count == 0)
        {
            throw new InvalidOperationException("Failed to read initial record.");
        }

        if (csv.Headers.Count < 2)
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
        AccountDefinitionImportMappingColumnsColumn? textField,
        string valueField)
    {
        // date and value are required
        TryParseField(dateField, out DateTime date);
        TryParseField(valueField, out double value);

        // name and text may be empty
        var name = GetStringField(nameField);

        var text = string.Empty;
        if (textField != null)
        {
            text = GetStringField(textField.Source);
            if (!string.IsNullOrEmpty(textField.IgnorePattern))
            {
                text = Regex.Replace(
                    text, textField.IgnorePattern, string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
            }

            text = this.duplicateSpaceExpression.Replace(text, " ");
        }

        var item = new ImportEntryViewModel(this.accounts) { Date = date, Name = name, Text = text, Value = value };

        var modelValue = value.ToModelValue();
        var patterns = this.importMapping.Patterns ?? Enumerable.Empty<AccountDefinitionImportMappingPatternsPattern>();
        var matchedPattern = patterns.FirstOrDefault(PatternPredicate);
        item.RemoteAccount = this.accounts.SingleOrDefault(a => a.ID == matchedPattern?.AccountID);

        return item;

        void TryParseField<T>(string field, out T result)
            where T : ISpanParsable<T>
        {
            if (csv.TryGetFieldIndex(field, out var index) && csv.TryParse(index, out result))
            {
                return;
            }

            result = default!;
        }

        string GetStringField(string field)
        {
            return csv.TryGetFieldIndex(field, out var index) ? csv.GetString(index) : string.Empty;
        }

        bool PatternPredicate(AccountDefinitionImportMappingPatternsPattern pattern)
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
