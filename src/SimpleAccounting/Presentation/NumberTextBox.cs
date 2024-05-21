// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

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
[ExcludeFromCodeCoverage(Justification = "The view cannot be tested.")]
internal partial class NumberTextBox : TextBox
{
    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(
            nameof(Scale), typeof(uint),
            typeof(NumberTextBox),
            new FrameworkPropertyMetadata((uint)0, OnScaleChanged));

    private Regex? numberExpression;

    public NumberTextBox()
    {
        this.GotFocus += (_, _) => this.SelectAll();
        this.GotMouseCapture += (_, _) => this.SelectAll();
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

    private static void OnPreviewKeyDown(object sender, KeyEventArgs eventArgs)
    {
        // The space character is NOT routed through PreviewTextInput.
        // We need this hook only to remove SPACE from input.
        eventArgs.Handled = eventArgs.Key == Key.Space;
    }

    [GeneratedRegex("^[0-9]*$", RegexOptions.Compiled)]
    private static partial Regex NumberRegex();

    private void UpdateExpression()
    {
        if (this.Scale == 0)
        {
            this.numberExpression = NumberRegex();
            return;
        }

        var decimalSeparator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
        this.numberExpression = new Regex(
            $"^[0-9]*({decimalSeparator}[0-9]{{0,{this.Scale}}})?$",
            RegexOptions.Compiled,
            TimeSpan.FromSeconds(1));
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs eventArgs)
    {
        // Build resulting text from current text, current selection and new text.
        // Accept the new input only if result matches the number expression.
        var newText =
            this.Text[..this.SelectionStart]
            + eventArgs.Text
            + this.Text[(this.SelectionStart + this.SelectionLength)..];
        var isValid = this.numberExpression!.IsMatch(newText);
        eventArgs.Handled = !isValid;
    }

    private void OnPasteText(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        var text = (string?)e.DataObject.GetData(typeof(string));
        if (text == null || !this.numberExpression!.IsMatch(text))
        {
            e.CancelCommand();
        }
    }
}
