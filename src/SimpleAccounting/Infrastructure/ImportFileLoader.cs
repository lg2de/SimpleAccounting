// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using CsvHelper;
    using CsvHelper.Configuration;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;

    internal sealed class ImportFileLoader : IDisposable
    {
        private readonly List<AccountDefinition> accounts;
        private readonly CultureInfo cultureInfo;
        private readonly string fileName;
        private readonly AccountDefinitionImportMapping importMapping;
        private readonly Regex duplicateSpaceExpression = new Regex(@"\s+", RegexOptions.Compiled);

        private StreamReader? streamReader;

        public ImportFileLoader(
            string fileName,
            CultureInfo cultureInfo,
            List<AccountDefinition> accounts,
            AccountDefinitionImportMapping importMapping)
        {
            this.fileName = fileName;
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
            // note, the stream is disposed by the reader
            var stream = new FileStream(
                this.fileName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite);
            var enc1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252);
            this.streamReader = new StreamReader(stream, enc1252!);
            var configuration = new CsvConfiguration(this.cultureInfo);
            return this.ImportBookings(this.streamReader, configuration);
        }

        internal IEnumerable<ImportEntryViewModel> ImportBookings(TextReader reader, CsvConfiguration configuration)
        {
            var dateField =
                this.importMapping.Columns
                    .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Date)?.Source
                ?? "date";
            var nameField =
                this.importMapping.Columns
                    .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Name)?.Source
                ?? "name";
            var textField =
                this.importMapping.Columns
                    .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Text);
            var valueField =
                this.importMapping.Columns
                    .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Value)?.Source
                ?? "value";

            using var csv = new CsvReader(reader, configuration);
            csv.Read();
            if (!csv.ReadHeader() || csv.Context.HeaderRecord.Length <= 1)
            {
                throw new InvalidOperationException("Missing or incomplete file header.");
            }

            while (csv.Read())
            {
                yield return this.ImportBooking(csv, dateField, nameField, textField, valueField);
            }
        }

        internal ImportEntryViewModel ImportBooking(
            CsvReader csv,
            string dateField,
            string nameField,
            AccountDefinitionImportMappingColumn textField,
            string valueField)
        {
            csv.TryGetField(dateField, out DateTime date);

            // date and value are required
            // name and text may be empty
            csv.TryGetField<double>(valueField, out var value);
            string name = string.Empty;
            string text = string.Empty;
            if (nameField != null)
            {
                csv.TryGetField(nameField, out name);
            }

            if (textField != null)
            {
                csv.TryGetField(textField.Source, out text);
                if (!string.IsNullOrEmpty(textField.IgnorePattern))
                {
                    text = Regex.Replace(text, textField.IgnorePattern, string.Empty);
                }

                text = this.duplicateSpaceExpression.Replace(text, " ");
            }

            var item = new ImportEntryViewModel(this.accounts) { Date = date, Name = name, Text = text, Value = value };

            var modelValue = value.ToModelValue();
            var patterns = this.importMapping?.Patterns ?? Enumerable.Empty<AccountDefinitionImportMappingPattern>();
            foreach (var mappingPattern in patterns)
            {
                if (!Regex.IsMatch(text, mappingPattern.Expression))
                {
                    // mapping does not match
                    continue;
                }

                if (mappingPattern.ValueSpecified && modelValue != mappingPattern.Value)
                {
                    // mapping does not match
                    continue;
                }

                // use first match
                item.RemoteAccount = this.accounts.SingleOrDefault(a => a.ID == mappingPattern.AccountID);
                break;
            }

            return item;
        }
    }
}
