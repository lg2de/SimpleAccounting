// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    ///     Implements a <see cref="TextBox"/> only accepting unsigned integer numbers.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class NumberTextBox : TextBox
    {
        private static Regex numberExpression = new Regex("^[0-9]*$", RegexOptions.Compiled);

        public NumberTextBox()
        {
            this.GotFocus += (s, e) => this.SelectAll();
            this.GotMouseCapture += (s, e) => this.SelectAll();
            this.PreviewKeyDown += this.OnPreviewKeyDown;
            this.PreviewTextInput += this.OnPreviewTextInput;
            DataObject.AddPastingHandler(this, this.OnPasteText);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // the space character is NOT routed throgh PreviewTextInput
            e.Handled = e.Key == Key.Space;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // only numbers are accepted
            var isValid = numberExpression.IsMatch(e.Text);
            e.Handled = !isValid;
        }

        private void OnPasteText(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            var text = (string)e.DataObject.GetData(typeof(string));
            if (!numberExpression.IsMatch(text))
            {
                e.CancelCommand();
            }
        }
    }
}
