// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    private readonly string dateField;
    private readonly string nameField;
    private readonly AccountDefinitionImportMappingColumnsColumn? textField;
    private readonly string valueField;

    private StreamReader? streamReader;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImportFileLoader" /> class.
    /// </summary>
    /// <param name="bytes">The content of the import file.</param>
    /// <param name="cultureInfo">
    ///     The culture of the application, used if the import mapping does not define a culture and
    ///     the culture of the import file cannot be detected unambiguously.
    /// </param>
    /// <param name="accounts">The accounts available as remote accounts.</param>
    /// <param name="importMapping">The import mapping of the account to import into.</param>
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

        this.dateField =
            importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Date)?.Source
            ?? "date";
        this.nameField =
            importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Name)?.Source
            ?? "name";
        this.textField =
            importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Text);
        this.valueField =
            importMapping.Columns
                .Find(x => x.Target == AccountDefinitionImportMappingColumnsColumnTarget.Value)?.Source
            ?? "value";
    }

    public void Dispose()
    {
        this.streamReader?.Dispose();
    }

    public IEnumerable<ImportEntryViewModel> Load()
    {
        var encoding = FileEncoding.GetAcceptableEncoding(this.bytes);

        var fileCulture = string.IsNullOrWhiteSpace(this.importMapping.Culture)
            ? this.DetectCulture(encoding) ?? this.cultureInfo
            : CultureInfo.GetCultureInfo(this.importMapping.Culture);

        // note, the stream is disposed by the reader
        var stream = new MemoryStream(this.bytes);
        this.streamReader = new StreamReader(stream, encoding);

        // the culture is used to parse numbers and dates only, the separator is detected from the file
        var options = new CsvReaderOptions { FormatProvider = fileCulture };
        return this.ImportBookings(this.streamReader, options);
    }

    private CultureInfo? DetectCulture(Encoding encoding)
    {
        using var csv = CsvReader.Create(new StreamReader(new MemoryStream(this.bytes), encoding));
        if (!csv.TryGetFieldIndex(this.dateField, out var dateIndex)
            || !csv.TryGetFieldIndex(this.valueField, out var valueIndex))
        {
            // missing columns are reported while importing
            return null;
        }

        var dates = new List<string>();
        var values = new List<string>();
        while (csv.Read())
        {
            dates.Add(csv.GetString(dateIndex));
            values.Add(csv.GetString(valueIndex));
        }

        return ImportFormatDetector.Detect(dates, values, this.cultureInfo);
    }

    private IEnumerable<ImportEntryViewModel> ImportBookings(TextReader reader, CsvReaderOptions options)
    {
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
            yield return this.ImportBooking(csv);
        }
    }

    private ImportEntryViewModel ImportBooking(CsvReader csv)
    {
        // date and value are required
        TryParseField(this.dateField, out DateTime date);
        TryParseField(this.valueField, out double value);

        // name and text may be empty
        var name = GetStringField(this.nameField);

        var text = string.Empty;
        if (this.textField != null)
        {
            text = GetStringField(this.textField.Source);
            if (!string.IsNullOrEmpty(this.textField.IgnorePattern))
            {
                text = Regex.Replace(
                    text, this.textField.IgnorePattern, string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
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
