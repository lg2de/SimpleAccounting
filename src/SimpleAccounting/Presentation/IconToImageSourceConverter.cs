// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
///     Implements the converter to create WPF image from standard Windows icon. 
/// </summary>
/// <remarks>
///     Implemented based on https://stackoverflow.com/a/29757845/3621055
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Cannot be tested.")]
public class IconToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Icon icon)
        {
            Trace.TraceWarning("Attempted to convert {0} instead of Icon object in IconToImageSourceConverter", value);
            return null;
        }

        ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
        return imageSource;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
