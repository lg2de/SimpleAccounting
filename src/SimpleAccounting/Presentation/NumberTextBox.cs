// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
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
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register(
                "Scale", typeof(uint),
                typeof(NumberTextBox),
                new FrameworkPropertyMetadata((uint)0, OnScaleChanged));

        private Regex? numberExpression;

        public NumberTextBox()
        {
            this.GotFocus += (s, e) => this.SelectAll();
            this.GotMouseCapture += (s, e) => this.SelectAll();
            this.PreviewKeyDown += OnPreviewKeyDown;
            this.PreviewTextInput += this.OnPreviewTextInput;
            this.UpdateExpression();
        }

        public uint Scale
        {
            get => (uint)this.GetValue(ScaleProperty);
            set
            {
                this.SetValue(ScaleProperty, value);
                this.UpdateExpression();
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            DataObject.AddPastingHandler(this, this.OnPasteText);
        }

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is NumberTextBox numberTextBox) || !(e.NewValue is uint))
            {
                return;
            }

            numberTextBox.Scale = (uint)e.NewValue;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // The space character is NOT routed through PreviewTextInput.
            // We need this hook only to remove SPACE from input.
            e.Handled = e.Key == Key.Space;
        }

        private void UpdateExpression()
        {
            if (this.Scale == 0)
            {
                this.numberExpression = new Regex("^[0-9]*$", RegexOptions.Compiled);
                return;
            }

            var decimalSeparator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            this.numberExpression = new Regex(
                $"^[0-9]*({decimalSeparator}[0-9]{{0,{this.Scale}}})?$", RegexOptions.Compiled);
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Build resulting text from current text, current selection and new text.
            // Accept the new input only if result matches the number expression.
            var newText =
                this.Text.Substring(0, this.SelectionStart)
                + e.Text
                + this.Text.Substring(this.SelectionStart + this.SelectionLength);
            var isValid = this.numberExpression!.IsMatch(newText);
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
            if (text == null || !this.numberExpression!.IsMatch(text))
            {
                e.CancelCommand();
            }
        }
    }
}
