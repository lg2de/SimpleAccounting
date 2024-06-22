// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
///     Implements a <see cref="TextBox" /> only accepting unsigned integer numbers.
/// </summary>
internal class NumberTextBox : TextBox
{
    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(
            nameof(Scale), typeof(uint),
            typeof(NumberTextBox),
            new FrameworkPropertyMetadata((uint)0, OnScaleChanged));

    public NumberTextBox()
    {
        this.GotFocus += (_, _) => this.SelectAll();
        this.GotMouseCapture += (_, _) => this.SelectAll();
    }

    public uint Scale
    {
        get => (uint)this.GetValue(ScaleProperty);
        set => this.SetValue(ScaleProperty, value);
    }

    [ExcludeFromCodeCoverage(Justification = "This view function cannot be tested.")]
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        DataObject.AddPastingHandler(this, this.OnPasteText);
    }

    [ExcludeFromCodeCoverage(Justification = "This view function cannot be tested.")]
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        // The space character is NOT routed through PreviewTextInput.
        // We need this hook only to remove SPACE from input.
        e.Handled = e.Key == Key.Space;
    }

    [ExcludeFromCodeCoverage(Justification = "This view function cannot be tested.")]
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        // Always take responsibility for the resulting text.
        e.Handled = true;

        // Process entered text into new text and cursor state.
        this.ProcessTextInput(e.Text);
    }

    internal void ProcessTextInput(string enteredText)
    {
        int selectionStart = this.SelectionStart;
        if (!this.TryBuildNewText(enteredText, selectionStart, this.SelectionLength, out var newText))
        {
            return;
        }

        this.Text = newText;
        this.SelectionStart = selectionStart + enteredText.Length;
        this.SelectionLength = 0;

        string separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
        if (this.SelectionStart > 0
            && this.Text.Substring(this.SelectionStart - 1, 1) == separator
            && enteredText != separator)
        {
            // In case reformatting cuts leading zero, the cursor gets unexpected position.
            this.SelectionStart--;
        }
    }

    private static void OnScaleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not NumberTextBox numberTextBox)
        {
            return;
        }

        if (eventArgs.NewValue is not uint newValue)
        {
            return;
        }

        numberTextBox.Scale = newValue;
    }

    /// <summary>
    ///     Checks whether the new text will result into valid number.
    /// </summary>
    /// <param name="enteredText">The new text entered to the box.</param>
    /// <param name="selectionStart">The current selection (cursor) position.</param>
    /// <param name="selectionLength">The length of the current selection.</param>
    /// <param name="newText">The resulting new text.</param>
    /// <returns>Value indicating whether the resulting text is valid.</returns>
    private bool TryBuildNewText(string enteredText, int selectionStart, int selectionLength, out string newText)
    {
        // Build new text from current text, current selection and new text.
        newText =
            this.Text[..selectionStart]
            + enteredText
            + this.Text[(selectionStart + selectionLength)..];

        // Remove duplicated separator.
        var separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
        newText = newText.Replace(separator + separator, separator, StringComparison.CurrentCulture);

        // Parse and reformat with current scale.
        if (!double.TryParse(newText, CultureInfo.CurrentUICulture, out double newValue))
        {
            return false;
        }

        string formattedValue = newValue.ToString($"F{this.Scale}", CultureInfo.CurrentUICulture);
        if (newText.Length > formattedValue.Length)
        {
            // Cut-off text below the precision of the current scale.
            newText = formattedValue;
        }

        return true;
    }

    [ExcludeFromCodeCoverage(Justification = "This view function cannot be tested.")]
    private void OnPasteText(object sender, DataObjectPastingEventArgs e)
    {
        // Always take responsibility for the resulting text.
        e.CancelCommand();

        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            return;
        }

        var text = (string?)e.DataObject.GetData(typeof(string));
        if (text == null)
        {
            return;
        }

        // Process entered text into new text and cursor state.
        this.ProcessTextInput(text);
    }
}
